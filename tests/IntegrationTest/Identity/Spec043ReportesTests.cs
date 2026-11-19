using System.Net;
using AwesomeAssertions;
using Knock.Contracts.Moderation;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace Knock.IntegrationTest.Identity;

/// <summary>Spec 043, REP-01 a REP-03: reportes y apelación con MongoDB y Redis reales. Solo datos ficticios; la decisión la siembra la prueba, porque el panel (spec 053) todavía no existe.</summary>
public sealed class Spec043ReportesTests
{
    private const string _reports = "/api/v1/reportes";
    private const string _text = "El técnico llegó tarde y fue grosero conmigo.";

    private static object Body(string reported, string reason = "mal_servicio", string text = _text, string[]? evidence = null) =>
        new { reportadoId = reported, motivo = reason, descripcion = text, evidencia = evidence ?? Array.Empty<string>() };

    [Fact]
    public async Task Crear_Un_Reporte_Responde_201_Abierto_Y_Lo_Guarda_CA_043_13_CA_043_30()
    {
        await using var host = await SafetyHost.StartAsync();
        var reported = await host.SeedUserAsync("Demo Reportado");

        using var response = await host.PostJsonAsync(_reports, host.Caller, Body(reported, evidence: ["507f1f77bcf86cd799439011", "507f1f77bcf86cd799439012"]));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.CacheControl!.NoStore.Should().BeTrue();
        var json = await SafetyHost.ReadAsync(response);
        json.GetProperty("estado").GetString().Should().Be("abierto");
        json.TryGetProperty("avisoEmergencia", out _).Should().BeFalse("el motivo no implica peligro");
        var saved = await host.Reports.Find(FilterDefinition<BsonDocument>.Empty).SingleAsync();
        saved["_id"].AsObjectId.ToString().Should().Be(json.GetProperty("reporteId").GetString());
        saved["reportaId"].AsString.Should().Be(host.Caller);
        saved["reportadoId"].AsString.Should().Be(reported);
        saved["evidencia"].AsBsonArray.Should().HaveCount(2);
        host.Moderator.Calls.Should().Be(1, "la descripción pasó por la moderación");
    }

    [Fact]
    public async Task Motivo_De_Peligro_Incluye_El_Aviso_De_Emergencia_CA_043_30()
    {
        await using var host = await SafetyHost.StartAsync();
        var reported = await host.SeedUserAsync("Demo Peligro");

        using var response = await host.PostJsonAsync(_reports, host.Caller, Body(reported, reason: "peligro", text: "Me sentí en peligro durante la visita."));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        (await SafetyHost.ReadAsync(response)).GetProperty("avisoEmergencia").GetString().Should().Contain("911");
    }

    [Fact]
    public async Task Sin_Sesion_Las_Tres_Rutas_Responden_401_CA_043_14_CA_043_18_CA_043_21()
    {
        await using var host = await SafetyHost.StartAsync();
        var reported = await host.SeedUserAsync("Demo Uno");

        using var create = await host.PostJsonAsync(_reports, sub: null, Body(reported));
        using var list = await host.SendAsync(HttpMethod.Get, $"{_reports}/me", sub: null);
        using var appeal = await host.PostJsonAsync($"{_reports}/{ObjectId.GenerateNewId()}/apelacion", sub: null, new { texto = "No estoy de acuerdo con la decisión." });

        create.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        list.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        appeal.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await host.CountReportsAsync()).Should().Be(0, "no se ejecuta nada sin sesión");
    }

    [Theory]
    [InlineData("evidencia", "seis")]
    [InlineData("evidencia", "no-es-un-id")]
    [InlineData("descripcion", "corta")]
    [InlineData("motivo", "motivo-que-no-existe")]
    [InlineData("reportadoId", "no-es-un-id")]
    [InlineData("referencia", "tipo-raro")]
    public async Task Valor_Invalido_Responde_422_Sin_Repetir_El_Valor_Y_No_Guarda_Nada_CA_043_15(string field, string kind)
    {
        await using var host = await SafetyHost.StartAsync();
        var reported = await host.SeedUserAsync("Demo Reportado");
        var body = new Dictionary<string, object?>
        {
            ["reportadoId"] = reported,
            ["motivo"] = "mal_servicio",
            ["descripcion"] = _text,
            ["evidencia"] = Array.Empty<string>(),
        };
        body[field] = (field, kind) switch
        {
            ("evidencia", "seis") => Enumerable.Range(0, 6).Select(i => $"507f1f77bcf86cd79943901{i}").ToArray(),
            ("evidencia", _) => new[] { kind },
            ("referencia", _) => new { tipo = kind, id = "507f1f77bcf86cd799439011" },
            _ => kind,
        };

        using var response = await host.PostJsonAsync(_reports, host.Caller, body);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await response.Content.ReadAsStringAsync()).Should().NotContain(kind, "no repite el valor");
        (await host.CountReportsAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Reportarse_A_Uno_Mismo_O_A_Alguien_Que_No_Existe_Responde_422_CA_043_15_CA_043_30()
    {
        await using var host = await SafetyHost.StartAsync();

        using var himself = await host.PostJsonAsync(_reports, host.Caller, Body(host.Caller));
        using var unknown = await host.PostJsonAsync(_reports, host.Caller, Body(Guid.NewGuid().ToString("D")));

        himself.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        unknown.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await host.CountReportsAsync()).Should().Be(0);
    }

    [Theory]
    [InlineData("{ esto no es json")]
    [InlineData("")]
    public async Task Cuerpo_Ilegible_Responde_400_Y_No_Guarda_Nada_CA_043_16(string raw)
    {
        await using var host = await SafetyHost.StartAsync();

        using var response = await host.SendAsync(HttpMethod.Post, _reports, host.Caller, raw);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await host.CountReportsAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Moderacion_Rechaza_O_No_Responde_No_Se_Guarda_Nada_CA_043_30()
    {
        await using var host = await SafetyHost.StartAsync();
        var reported = await host.SeedUserAsync("Demo Moderada");

        host.Moderator.Outcome = ModerationOutcome.Rejected;
        using var rejected = await host.PostJsonAsync(_reports, host.Caller, Body(reported));
        host.Moderator.Outcome = ModerationOutcome.Unavailable;
        using var unavailable = await host.PostJsonAsync(_reports, host.Caller, Body(reported));

        rejected.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await SafetyHost.ReadAsync(rejected)).GetProperty("code").GetString().Should().Be("report_content_rejected");
        unavailable.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        (await SafetyHost.ReadAsync(unavailable)).GetProperty("code").GetString().Should().Be("report_moderation_unavailable");
        (await host.CountReportsAsync()).Should().Be(0, "falla cerrado: sin revisión no se guarda");
    }

    [Fact]
    public async Task Repetir_El_Mismo_Reporte_Abierto_Devuelve_El_Que_Ya_Existe_CA_043_37()
    {
        await using var host = await SafetyHost.StartAsync();
        var reported = await host.SeedUserAsync("Demo Repetida");

        using var first = await host.PostJsonAsync(_reports, host.Caller, Body(reported));
        using var again = await host.PostJsonAsync(_reports, host.Caller, Body(reported));

        first.StatusCode.Should().Be(HttpStatusCode.Created);
        again.StatusCode.Should().Be(HttpStatusCode.OK);
        (await SafetyHost.ReadAsync(again)).GetProperty("reporteId").GetString().Should().Be((await SafetyHost.ReadAsync(first)).GetProperty("reporteId").GetString());
        (await host.CountReportsAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Lista_Los_Reportes_Propios_Sin_Datos_De_La_Otra_Parte_CA_043_17_CA_043_32()
    {
        await using var host = await SafetyHost.StartAsync();
        var reported = await host.SeedUserAsync("Demo Reportada");
        (await host.PostJsonAsync(_reports, host.Caller, Body(reported))).EnsureSuccessStatusCode();

        using var mine = await host.SendAsync(HttpMethod.Get, $"{_reports}/me", host.Caller);
        using var theirs = await host.SendAsync(HttpMethod.Get, $"{_reports}/me", reported);

        var text = await mine.Content.ReadAsStringAsync();
        var items = JsonElementItems(await SafetyHost.ReadAsync(mine));
        items.Should().ContainSingle();
        items[0].EnumerateObject().Select(p => p.Name).Should().BeEquivalentTo("reporteId", "motivo", "estado", "creadoEn", "puedeApelar");
        text.Should().NotContain(_text).And.NotContain(reported, "nunca lleva el texto ni a la otra persona");
        JsonElementItems(await SafetyHost.ReadAsync(theirs)).Should().BeEmpty("la persona reportada no ve quién la reportó ni nada del reporte");
    }

    [Fact]
    public async Task Lista_Pagina_De_20_Con_Cursor_Y_Rechaza_Limites_Invalidos_CA_043_19()
    {
        await using var host = await SafetyHost.StartAsync();
        for (var i = 0; i < 25; i++)
        {
            var reported = await host.SeedUserAsync($"Demo {i:00}");
            (await host.PostJsonAsync(_reports, host.Caller, Body(reported))).EnsureSuccessStatusCode();
        }

        using var firstPage = await host.SendAsync(HttpMethod.Get, $"{_reports}/me", host.Caller);
        var first = await SafetyHost.ReadAsync(firstPage);
        var cursor = first.GetProperty("nextCursor").GetString()!;
        using var secondPage = await host.SendAsync(HttpMethod.Get, $"{_reports}/me?cursor={Uri.EscapeDataString(cursor)}", host.Caller);
        var second = await SafetyHost.ReadAsync(secondPage);
        using var tooMany = await host.SendAsync(HttpMethod.Get, $"{_reports}/me?limite=51", host.Caller);

        first.GetProperty("items").GetArrayLength().Should().Be(20);
        second.GetProperty("items").GetArrayLength().Should().Be(5);
        JsonElementItems(first).Concat(JsonElementItems(second)).Select(i => i.GetProperty("reporteId").GetString()).Should().OnlyHaveUniqueItems().And.HaveCount(25);
        tooMany.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Apelar_Una_Decision_Responde_200_Y_El_Reporte_Pasa_A_En_Apelacion_CA_043_20_CA_043_31()
    {
        await using var host = await SafetyHost.StartAsync();
        var reported = await host.SeedUserAsync("Demo Apela");
        var id = await host.CreateReportAsync(reported);
        await host.SeedDecisionAsync(id);

        using var response = await host.PostJsonAsync($"{_reports}/{id}/apelacion", reported, new { texto = "No estoy de acuerdo con la decisión tomada." });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await SafetyHost.ReadAsync(response);
        json.GetProperty("estado").GetString().Should().Be("en_apelacion");
        json.EnumerateObject().Select(p => p.Name).Should().BeEquivalentTo(["reporteId", "estado"], "no lleva quién reportó ni el texto");
        var saved = await host.Reports.Find(Builders<BsonDocument>.Filter.Eq("_id", ObjectId.Parse(id))).SingleAsync();
        saved["estado"].AsString.Should().Be("en_apelacion");
        saved["apelacion"]["texto"].AsString.Should().Be("No estoy de acuerdo con la decisión tomada.");
    }

    [Fact]
    public async Task Quien_No_Es_Parte_Del_Reporte_Recibe_403_Y_Nada_Cambia_CA_043_22()
    {
        await using var host = await SafetyHost.StartAsync();
        var reported = await host.SeedUserAsync("Demo Parte");
        var outsider = await host.SeedUserAsync("Demo Ajena");
        var id = await host.CreateReportAsync(reported);
        await host.SeedDecisionAsync(id);

        using var thirdParty = await host.PostJsonAsync($"{_reports}/{id}/apelacion", outsider, new { texto = "No estoy de acuerdo con la decisión tomada." });
        using var unknown = await host.PostJsonAsync($"{_reports}/{ObjectId.GenerateNewId()}/apelacion", host.Caller, new { texto = "No estoy de acuerdo con la decisión tomada." });

        thirdParty.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        unknown.StatusCode.Should().Be(HttpStatusCode.Forbidden, "un reporte que no existe se ve igual que uno ajeno");
        (await host.Reports.Find(Builders<BsonDocument>.Filter.Eq("_id", ObjectId.Parse(id))).SingleAsync()).Contains("apelacion").Should().BeFalse();
    }

    [Theory]
    [InlineData("corto")]
    [InlineData("")]
    public async Task Texto_Invalido_De_La_Apelacion_Responde_422_Y_No_Guarda_Nada_CA_043_23(string text)
    {
        await using var host = await SafetyHost.StartAsync();
        var reported = await host.SeedUserAsync("Demo Texto");
        var id = await host.CreateReportAsync(reported);
        await host.SeedDecisionAsync(id);

        using var response = await host.PostJsonAsync($"{_reports}/{id}/apelacion", host.Caller, new { texto = text });
        using var badId = await host.PostJsonAsync($"{_reports}/no-es-un-id/apelacion", host.Caller, new { texto = "No estoy de acuerdo con la decisión tomada." });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        badId.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await host.Reports.Find(Builders<BsonDocument>.Filter.Eq("_id", ObjectId.Parse(id))).SingleAsync()).Contains("apelacion").Should().BeFalse();
    }

    [Fact]
    public async Task Cuerpo_Ilegible_En_La_Apelacion_Responde_400_CA_043_24()
    {
        await using var host = await SafetyHost.StartAsync();
        var reported = await host.SeedUserAsync("Demo Ilegible");
        var id = await host.CreateReportAsync(reported);
        await host.SeedDecisionAsync(id);

        using var response = await host.SendAsync(HttpMethod.Post, $"{_reports}/{id}/apelacion", host.Caller, "{ esto no es json");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Sin_Decision_O_Con_Una_Apelacion_Previa_Responde_409_CA_043_25_CA_043_31()
    {
        await using var host = await SafetyHost.StartAsync();
        var reported = await host.SeedUserAsync("Demo Conflicto");
        var id = await host.CreateReportAsync(reported);
        var body = new { texto = "No estoy de acuerdo con la decisión tomada." };

        using var withoutDecision = await host.PostJsonAsync($"{_reports}/{id}/apelacion", host.Caller, body);
        await host.SeedDecisionAsync(id);
        using var first = await host.PostJsonAsync($"{_reports}/{id}/apelacion", host.Caller, body);
        using var second = await host.PostJsonAsync($"{_reports}/{id}/apelacion", reported, body);

        withoutDecision.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await SafetyHost.ReadAsync(withoutDecision)).GetProperty("code").GetString().Should().Be("appeal_not_allowed");
        first.StatusCode.Should().Be(HttpStatusCode.OK);
        second.StatusCode.Should().Be(HttpStatusCode.Conflict, "la apelación es una sola");
    }

    [Fact]
    public async Task Un_Identificador_De_Otra_Persona_En_El_Cuerpo_Se_Ignora_CA_043_34()
    {
        await using var host = await SafetyHost.StartAsync();
        var reported = await host.SeedUserAsync("Demo Suplanta");
        var victim = Guid.NewGuid().ToString("D");

        using var response = await host.PostJsonAsync(_reports, host.Caller, new { reportadoId = reported, motivo = "mal_servicio", descripcion = _text, reportaId = victim, sub = victim });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        (await host.Reports.Find(FilterDefinition<BsonDocument>.Empty).SingleAsync())["reportaId"].AsString.Should().Be(host.Caller);
    }

    [Fact]
    public async Task Los_Errores_No_Llevan_Datos_Personales_CA_043_35()
    {
        await using var host = await SafetyHost.StartAsync();
        var reported = await host.SeedUserAsync("Demo Privado Reportado");
        var outsider = await host.SeedUserAsync("Demo Privada Ajena");
        var id = await host.CreateReportAsync(reported);

        using var conflict = await host.PostJsonAsync($"{_reports}/{id}/apelacion", host.Caller, new { texto = "No estoy de acuerdo con la decisión tomada." });
        using var forbidden = await host.PostJsonAsync($"{_reports}/{id}/apelacion", outsider, new { texto = "No estoy de acuerdo con la decisión tomada." });
        host.Moderator.Outcome = ModerationOutcome.Unavailable;
        using var unavailable = await host.PostJsonAsync(_reports, host.Caller, Body(reported, reason: "acoso", text: "Texto privado que no debe aparecer en errores."));

        foreach (var response in new[] { conflict, forbidden, unavailable })
        {
            var body = await response.Content.ReadAsStringAsync();
            body.Should().NotContain(host.Caller).And.NotContain(reported).And.NotContain(outsider).And.NotContain("Demo Privado").And.NotContain("Texto privado");
        }
    }

    private static IReadOnlyList<System.Text.Json.JsonElement> JsonElementItems(System.Text.Json.JsonElement page) => [.. page.GetProperty("items").EnumerateArray()];
}

/// <summary>Atajos de las pruebas de reportes.</summary>
internal static class SafetyHostReports
{
    /// <summary>Crea un reporte válido de quien llama contra <paramref name="reported"/> y devuelve su identificador.</summary>
    public static async Task<string> CreateReportAsync(this SafetyHost host, string reported)
    {
        using var response = await host.PostJsonAsync(
            "/api/v1/reportes",
            host.Caller,
            new { reportadoId = reported, motivo = "mal_servicio", descripcion = "El técnico llegó tarde y fue grosero conmigo.", evidencia = Array.Empty<string>() });
        response.EnsureSuccessStatusCode();
        return (await SafetyHost.ReadAsync(response)).GetProperty("reporteId").GetString()!;
    }
}
