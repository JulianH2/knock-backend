using System.Net;
using AwesomeAssertions;
using Knock.IntegrationTest.Support;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace Knock.IntegrationTest.Requests;

/// <summary>Spec 022, T-04 y T-05: confirmar la llegada con el código, con intentos, bloqueo y un solo uso (MongoDB real).</summary>
public sealed class Spec022LlegadaTests
{
    private static async Task<string> CodeOf(HttpResponseMessage response) => (await Spec020Host.ReadAsync(response)).GetProperty("code").GetString()!;

    [Fact]
    public async Task Llegada_Correcta_CA_022_04()
    {
        await using var host = await Spec020Host.StartAsync();
        var visit = await host.ConfirmedVisitAsync();
        await host.OpenWindowAsync(visit.VisitId);

        using var response = await host.ArriveAsync(visit.VisitId, visit.Technician.Sub, visit.Code);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.CacheControl!.NoStore.Should().BeTrue();
        var raw = await response.Content.ReadAsStringAsync();
        var json = System.Text.Json.JsonElement.Parse(raw);
        json.GetProperty("rol").GetString().Should().Be("tecnico");
        json.GetProperty("estado").GetString().Should().Be("en_curso");
        json.GetProperty("llegadaEn").GetDateTimeOffset().Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(1));
        raw.Should().NotContain(visit.Code!);
        var document = await host.VisitDocAsync(visit.VisitId);
        document["estado"].AsString.Should().Be("en_curso");
        document["codigoUsado"].AsBoolean.Should().BeTrue();
        document.Contains("llegadaEn").Should().BeTrue();
        (await host.EventsAsync("VisitaEnCurso")).Should().Be(1);
    }

    [Fact]
    public async Task Codigo_Incorrecto_CA_022_05()
    {
        await using var host = await Spec020Host.StartAsync();
        var visit = await host.ConfirmedVisitAsync();
        await host.OpenWindowAsync(visit.VisitId);

        using var response = await host.ArriveAsync(visit.VisitId, visit.Technician.Sub, Spec022Host.WrongCode(visit.Code!));

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var json = await Spec020Host.ReadAsync(response);
        json.GetProperty("code").GetString().Should().Be("wrong_code");
        json.GetProperty("intentosRestantes").GetInt32().Should().Be(2);
        var document = await host.VisitDocAsync(visit.VisitId);
        document["estado"].AsString.Should().Be("confirmada");
        document["intentos"].AsInt32.Should().Be(1);
        document["codigoUsado"].AsBoolean.Should().BeFalse();
        document["bloqueada"].AsBoolean.Should().BeFalse();
    }

    [Fact]
    public async Task Bloqueo_Tras_Tres_Intentos_CA_022_06()
    {
        await using var host = await Spec020Host.StartAsync();
        var visit = await host.ConfirmedVisitAsync();
        await host.OpenWindowAsync(visit.VisitId);
        var wrong = Spec022Host.WrongCode(visit.Code!);

        using var first = await host.ArriveAsync(visit.VisitId, visit.Technician.Sub, wrong);
        using var second = await host.ArriveAsync(visit.VisitId, visit.Technician.Sub, wrong);
        using var third = await host.ArriveAsync(visit.VisitId, visit.Technician.Sub, wrong);
        using var correctAfterLock = await host.ArriveAsync(visit.VisitId, visit.Technician.Sub, visit.Code);

        (await Spec020Host.ReadAsync(second)).GetProperty("intentosRestantes").GetInt32().Should().Be(1);
        third.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await CodeOf(third)).Should().Be("visit_locked");
        correctAfterLock.StatusCode.Should().Be(HttpStatusCode.Conflict, "con la visita bloqueada ni el código correcto sirve");
        (await CodeOf(correctAfterLock)).Should().Be("visit_locked");
        var document = await host.VisitDocAsync(visit.VisitId);
        document["bloqueada"].AsBoolean.Should().BeTrue();
        document["intentos"].AsInt32.Should().Be(3);
        document["estado"].AsString.Should().Be("confirmada");
        (await host.EventsAsync("VisitaBloqueada")).Should().Be(1);
        var closed = await host.Database.GetCollection<BsonDocument>("outbox").Find(Builders<BsonDocument>.Filter.Eq("tipo", "VisitaCerrada")).SingleAsync();
        closed["datos"]["motivo"].AsString.Should().Be("bloqueada");
        first.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Codigo_Usado_CA_022_07()
    {
        await using var host = await Spec020Host.StartAsync();
        var visit = await host.ConfirmedVisitAsync();
        await host.OpenWindowAsync(visit.VisitId);
        (await host.ArriveAsync(visit.VisitId, visit.Technician.Sub, visit.Code)).Dispose();

        using var again = await host.ArriveAsync(visit.VisitId, visit.Technician.Sub, visit.Code);
        using var wrongAgain = await host.ArriveAsync(visit.VisitId, visit.Technician.Sub, Spec022Host.WrongCode(visit.Code!));

        again.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await CodeOf(again)).Should().Be("code_used");
        (await CodeOf(wrongAgain)).Should().Be("code_used", "una visita ya en curso no cuenta intentos");
        (await host.VisitDocAsync(visit.VisitId))["intentos"].AsInt32.Should().Be(0);
    }

    [Fact]
    public async Task Llegada_Repetida_Responde_409_CA_022_12()
    {
        await using var host = await Spec020Host.StartAsync();
        var visit = await host.ConfirmedVisitAsync();
        await host.OpenWindowAsync(visit.VisitId);
        (await host.ArriveAsync(visit.VisitId, visit.Technician.Sub, visit.Code)).Dispose();
        var arrivedAt = (await host.VisitDocAsync(visit.VisitId))["llegadaEn"];

        using var again = await host.ArriveAsync(visit.VisitId, visit.Technician.Sub, visit.Code);

        (await CodeOf(again)).Should().Be("code_used");
        (await host.EventsAsync("VisitaEnCurso")).Should().Be(1, "no hay un segundo evento");
        (await host.VisitDocAsync(visit.VisitId))["llegadaEn"].Should().Be(arrivedAt, "la hora de llegada no cambia");
    }

    [Fact]
    public async Task Cinco_Llegadas_Simultaneas_Dejan_Una_Sola_R_10()
    {
        await using var host = await Spec020Host.StartAsync();
        var visit = await host.ConfirmedVisitAsync();
        await host.OpenWindowAsync(visit.VisitId);

        var responses = await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => host.ArriveAsync(visit.VisitId, visit.Technician.Sub, visit.Code)));

        try
        {
            responses.Count(r => r.StatusCode == HttpStatusCode.OK).Should().Be(1);
            responses.Where(r => r.StatusCode != HttpStatusCode.OK).Should().OnlyContain(r => r.StatusCode == HttpStatusCode.Conflict);
            (await host.EventsAsync("VisitaEnCurso")).Should().Be(1);
        }
        finally
        {
            foreach (var response in responses)
            {
                response.Dispose();
            }
        }
    }

    [Theory]
    [InlineData("cancelada", "visit_cancelled")]
    [InlineData("vencido", "code_expired")]
    public async Task Visita_Cancelada_O_Vencida_CA_022_08(string situation, string expected)
    {
        await using var host = await Spec020Host.StartAsync();
        var visit = await host.ConfirmedVisitAsync();
        await host.OpenWindowAsync(visit.VisitId);
        await host.SetVisitAsync(visit.VisitId, situation == "cancelada"
            ? Builders<BsonDocument>.Update.Set("estado", "cancelada")
            : Builders<BsonDocument>.Update.Set("codigoVenceEn", DateTime.UtcNow.AddMinutes(-1)));

        using var response = await host.ArriveAsync(visit.VisitId, visit.Technician.Sub, visit.Code);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await CodeOf(response)).Should().Be(expected);
        var document = await host.VisitDocAsync(visit.VisitId);
        document["intentos"].AsInt32.Should().Be(0);
        document["codigoUsado"].AsBoolean.Should().BeFalse();
        (await host.EventsAsync("VisitaEnCurso")).Should().Be(0);
    }

    [Theory]
    [InlineData("123")]
    [InlineData("12345")]
    [InlineData("12a4")]
    [InlineData("١٢٣٤")]
    [InlineData(" 123")]
    [InlineData("")]
    public async Task Codigo_Mal_Formado_No_Cuenta_CA_022_15(string code)
    {
        await using var host = await Spec020Host.StartAsync();
        var visit = await host.ConfirmedVisitAsync();
        await host.OpenWindowAsync(visit.VisitId);

        using var response = await host.ArriveAsync(visit.VisitId, visit.Technician.Sub, code);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var raw = await response.Content.ReadAsStringAsync();
        System.Text.Json.JsonElement.Parse(raw).GetProperty("errors").TryGetProperty("codigo", out _).Should().BeTrue();
        if (code.Length > 0)
        {
            ProblemBody.Reasons(raw).Should().NotContain(code.Trim(), "el motivo nunca repite el valor enviado");
        }

        (await host.VisitDocAsync(visit.VisitId))["intentos"].AsInt32.Should().Be(0);
    }

    [Fact]
    public async Task Fuera_De_Ventana_CA_022_16()
    {
        await using var host = await Spec020Host.StartAsync();
        var visit = await host.ConfirmedVisitAsync();

        using var response = await host.ArriveAsync(visit.VisitId, visit.Technician.Sub, visit.Code);
        using var wrong = await host.ArriveAsync(visit.VisitId, visit.Technician.Sub, Spec022Host.WrongCode(visit.Code!));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await CodeOf(response)).Should().Be("outside_window");
        (await CodeOf(wrong)).Should().Be("outside_window", "antes de la ventana ni un código incorrecto cuenta como intento");
        var document = await host.VisitDocAsync(visit.VisitId);
        document["intentos"].AsInt32.Should().Be(0);
        document["codigoUsado"].AsBoolean.Should().BeFalse();
        document["estado"].AsString.Should().Be("confirmada");
    }

    [Theory]
    [InlineData("terminada")]
    [InlineData("no_presentado")]
    public async Task Visita_No_Vigente_CA_022_18(string state)
    {
        await using var host = await Spec020Host.StartAsync();
        var visit = await host.ConfirmedVisitAsync();
        await host.OpenWindowAsync(visit.VisitId);
        await host.SetVisitAsync(visit.VisitId, Builders<BsonDocument>.Update.Set("estado", state));

        using var arrival = await host.ArriveAsync(visit.VisitId, visit.Technician.Sub, visit.Code);
        using var notice = await host.NoticeAsync(visit.VisitId);

        arrival.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await CodeOf(arrival)).Should().Be("visit_not_active");
        notice.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await CodeOf(notice)).Should().Be("visit_not_notifiable");
        var document = await host.VisitDocAsync(visit.VisitId);
        document["estado"].AsString.Should().Be(state);
        document["intentos"].AsInt32.Should().Be(0);
        document.GetValue("avisosEnviados", 0).AsInt32.Should().Be(0);
    }
}
