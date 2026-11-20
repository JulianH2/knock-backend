using System.Net;
using System.Text;
using System.Text.Json;
using AwesomeAssertions;
using Knock.IntegrationTest.Requests;
using Knock.IntegrationTest.Support;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace Knock.IntegrationTest.Trust;

/// <summary>Spec 042 (TCO-01 a TCO-03): técnicos de confianza de la persona, con MongoDB real. Solo datos ficticios.</summary>
public sealed class Spec042ConfianzaTests
{
    private const string _route = "/api/v1/me/tecnicos-confianza";

    [Fact]
    public async Task Lista_Los_Guardados_Con_Datos_Publicos_Del_Mas_Reciente_Al_Mas_Antiguo_CA_042_01()
    {
        await using var host = await Spec020Host.StartAsync();
        var sub = Guid.NewGuid().ToString("D");
        var first = await host.SeedPublicTechnicianAsync(name: "Demo Uno");
        var second = await host.SeedPublicTechnicianAsync(name: "Demo Dos");
        (await host.SaveTrustedAsync(sub, first.Id)).EnsureSuccessStatusCode();
        await Task.Delay(20);
        (await host.SaveTrustedAsync(sub, second.Id)).EnsureSuccessStatusCode();

        using var response = await host.SendAsync(HttpMethod.Get, _route, sub);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.CacheControl!.NoStore.Should().BeTrue();
        var json = await Spec020Host.ReadAsync(response);
        var items = json.GetProperty("items").EnumerateArray().ToList();
        items.Select(i => i.GetProperty("nombre").GetString()).Should().Equal("Demo Dos", "Demo Uno");
        items[0].GetProperty("tecnicoId").GetString().Should().Be(second.Id.ToString());
        items[0].EnumerateObject().Select(p => p.Name).Should().BeSubsetOf(["tecnicoId", "nombre", "fotoUrl", "nivelVerificacion", "guardadoEn"], "solo datos públicos");
        json.TryGetProperty("nextCursor", out _).Should().BeFalse("no hay otra página");
    }

    [Fact]
    public async Task Sin_Sesion_Las_Tres_Rutas_Responden_401_CA_042_02_CA_042_05_CA_042_10()
    {
        await using var host = await Spec020Host.StartAsync();
        var id = ObjectId.GenerateNewId();

        using var list = await host.SendAsync(HttpMethod.Get, _route);
        using var save = await host.SendAsync(HttpMethod.Put, $"{_route}/{id}");
        using var remove = await host.SendAsync(HttpMethod.Delete, $"{_route}/{id}");

        list.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        save.StatusCode.Should().Be(HttpStatusCode.Unauthorized, "CA-05");
        remove.StatusCode.Should().Be(HttpStatusCode.Unauthorized, "CA-10");
        (await host.CountAsync("tecnicos_confianza")).Should().Be(0, "no se ejecuta nada sin sesión");
    }

    [Fact]
    public async Task Pagina_De_20_Con_Cursor_Y_Rechaza_Limites_Invalidos_CA_042_03()
    {
        await using var host = await Spec020Host.StartAsync();
        var sub = Guid.NewGuid().ToString("D");
        var ids = new List<string>();
        for (var i = 0; i < 25; i++)
        {
            var technician = await host.SeedPublicTechnicianAsync(name: $"Demo {i:00}");
            (await host.SaveTrustedAsync(sub, technician.Id)).EnsureSuccessStatusCode();
            ids.Add(technician.Id.ToString());
            await Task.Delay(5);
        }

        using var firstPage = await host.SendAsync(HttpMethod.Get, _route, sub);
        var first = await Spec020Host.ReadAsync(firstPage);
        var cursor = first.GetProperty("nextCursor").GetString()!;
        using var secondPage = await host.SendAsync(HttpMethod.Get, $"{_route}?cursor={Uri.EscapeDataString(cursor)}", sub);
        var second = await Spec020Host.ReadAsync(secondPage);
        using var tooMany = await host.SendAsync(HttpMethod.Get, $"{_route}?limite=51", sub);
        using var zero = await host.SendAsync(HttpMethod.Get, $"{_route}?limite=0", sub);
        using var badCursor = await host.SendAsync(HttpMethod.Get, $"{_route}?cursor=esto-no-es-un-cursor", sub);

        first.GetProperty("items").GetArrayLength().Should().Be(20);
        second.GetProperty("items").GetArrayLength().Should().Be(5);
        second.TryGetProperty("nextCursor", out _).Should().BeFalse();
        first.GetProperty("items").EnumerateArray().Concat(second.GetProperty("items").EnumerateArray()).Select(i => i.GetProperty("tecnicoId").GetString())
            .Should().OnlyHaveUniqueItems().And.BeEquivalentTo(ids, "no se repite ni se salta ninguno");
        tooMany.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        zero.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        badCursor.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Guardar_Responde_201_La_Primera_Vez_Y_200_Al_Repetir_Sin_Duplicar_CA_042_04_CA_042_14()
    {
        await using var host = await Spec020Host.StartAsync();
        var sub = Guid.NewGuid().ToString("D");
        var technician = await host.SeedPublicTechnicianAsync(name: "Demo Tres");

        using var first = await host.SaveTrustedAsync(sub, technician.Id);
        using var again = await host.SaveTrustedAsync(sub, technician.Id);

        first.StatusCode.Should().Be(HttpStatusCode.Created, "R-10: 201 la primera vez");
        again.StatusCode.Should().Be(HttpStatusCode.OK, "CA-14: 200 al repetir");
        var created = await Spec020Host.ReadAsync(first);
        created.GetProperty("tecnicoId").GetString().Should().Be(technician.Id.ToString());
        created.GetProperty("nombre").GetString().Should().Be("Demo Tres");
        (await Spec020Host.ReadAsync(again)).GetProperty("guardadoEn").GetDateTimeOffset().Should().Be(created.GetProperty("guardadoEn").GetDateTimeOffset(), "la repetición no cambia la fecha");
        (await host.CountAsync("tecnicos_confianza")).Should().Be(1, "queda un solo registro");
    }

    [Theory]
    [InlineData("no-es-un-id")]
    [InlineData("507f1f77bcf86cd79943901")]
    [InlineData("507f1f77bcf86cd7994390111")]
    public async Task Id_Invalido_Responde_422_Sin_Repetir_El_Valor_Y_No_Guarda_Nada_CA_042_06_CA_042_11(string invalid)
    {
        await using var host = await Spec020Host.StartAsync();
        var sub = Guid.NewGuid().ToString("D");

        using var save = await host.SendAsync(HttpMethod.Put, $"{_route}/{invalid}", sub);
        using var remove = await host.SendAsync(HttpMethod.Delete, $"{_route}/{invalid}", sub);

        save.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        remove.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity, "CA-11");
        ProblemBody.Reasons(await save.Content.ReadAsStringAsync()).Should().NotContain(invalid, "no repite el valor");
        (await host.CountAsync("tecnicos_confianza")).Should().Be(0);
    }

    [Fact]
    public async Task Tecnico_Inexistente_U_Oculto_Responde_404_Con_Su_Codigo_CA_042_08()
    {
        await using var host = await Spec020Host.StartAsync();
        var sub = Guid.NewGuid().ToString("D");
        var hidden = await host.SeedPublicTechnicianAsync();
        await host.SetTechnicianAsync(hidden.Id, Builders<BsonDocument>.Update.Set("oculto", true));

        using var unknown = await host.SaveTrustedAsync(sub, ObjectId.GenerateNewId());
        using var hiddenResponse = await host.SaveTrustedAsync(sub, hidden.Id);

        foreach (var response in new[] { unknown, hiddenResponse })
        {
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            (await Spec020Host.ReadAsync(response)).GetProperty("code").GetString().Should().Be("technician_not_found");
        }

        (await host.CountAsync("tecnicos_confianza")).Should().Be(0, "no cambia el estado");
    }

    [Fact]
    public async Task Tecnico_No_Disponible_Responde_404_Y_No_El_201_Que_Dice_El_Texto_De_La_Spec_CA_042_07()
    {
        // CA-07 dice «recurso no disponible → 201 con ProblemDetails», pero R-05 y CA-08 piden 404 `technician_not_found` para un técnico que no existe o está oculto: se implementa lo segundo.
        await using var host = await Spec020Host.StartAsync();
        var sub = Guid.NewGuid().ToString("D");
        var unavailable = await host.SeedPublicTechnicianAsync();
        await host.SetTechnicianAsync(unavailable.Id, Builders<BsonDocument>.Update.Set("estado", "suspendido"));

        using var response = await host.SaveTrustedAsync(sub, unavailable.Id);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await Spec020Host.ReadAsync(response)).GetProperty("code").GetString().Should().Be("technician_not_found");
        (await host.CountAsync("tecnicos_confianza")).Should().Be(0, "no cambia el estado");
    }

    [Fact]
    public async Task Quitar_Responde_204_Y_Si_No_Estaba_404_Con_Su_Codigo_CA_042_09_CA_042_12()
    {
        await using var host = await Spec020Host.StartAsync();
        var sub = Guid.NewGuid().ToString("D");
        var technician = await host.SeedPublicTechnicianAsync();
        var other = await host.SeedPublicTechnicianAsync();
        (await host.SaveTrustedAsync(sub, technician.Id)).EnsureSuccessStatusCode();

        using var removed = await host.SendAsync(HttpMethod.Delete, $"{_route}/{technician.Id}", sub);
        using var notThere = await host.SendAsync(HttpMethod.Delete, $"{_route}/{other.Id}", sub);
        using var again = await host.SendAsync(HttpMethod.Delete, $"{_route}/{technician.Id}", sub);

        removed.StatusCode.Should().Be(HttpStatusCode.NoContent);
        notThere.StatusCode.Should().Be(HttpStatusCode.NotFound, "CA-12 y CA-14");
        (await Spec020Host.ReadAsync(notThere)).GetProperty("code").GetString().Should().Be("trusted_technician_not_found");
        again.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await host.CountAsync("tecnicos_confianza")).Should().Be(0);
    }

    [Fact]
    public async Task No_Existe_Ninguna_Coleccion_Ni_Campo_Con_Numeros_De_Terceros_CA_042_13()
    {
        await using var host = await Spec020Host.StartAsync();
        var sub = Guid.NewGuid().ToString("D");
        var technician = await host.SeedPublicTechnicianAsync();
        (await host.SaveTrustedAsync(sub, technician.Id)).EnsureSuccessStatusCode();

        var collections = await (await host.Database.ListCollectionNamesAsync()).ToListAsync();
        var saved = await host.Database.GetCollection<BsonDocument>("tecnicos_confianza").Find(FilterDefinition<BsonDocument>.Empty).SingleAsync();

        collections.Should().NotContain(name => name.Contains("contacto", StringComparison.OrdinalIgnoreCase));
        saved.Names.Should().BeEquivalentTo(["_id", "clienteId", "tecnicoId", "guardadoEn"], "solo la relación y la fecha, ningún número");
    }

    [Fact]
    public async Task Tecnico_Oculto_Sale_De_La_Lista_Y_Reaparece_Al_Volver_A_Ser_Visible_CA_042_15()
    {
        await using var host = await Spec020Host.StartAsync();
        var sub = Guid.NewGuid().ToString("D");
        var visible = await host.SeedPublicTechnicianAsync(name: "Demo Visible");
        var toHide = await host.SeedPublicTechnicianAsync(name: "Demo Oculto");
        (await host.SaveTrustedAsync(sub, toHide.Id)).EnsureSuccessStatusCode();
        (await host.SaveTrustedAsync(sub, visible.Id)).EnsureSuccessStatusCode();

        await host.SetTechnicianAsync(toHide.Id, Builders<BsonDocument>.Update.Set("oculto", true));
        var hidden = await host.TrustedNamesAsync(sub);
        await host.SetTechnicianAsync(toHide.Id, Builders<BsonDocument>.Update.Set("oculto", false));
        var back = await host.TrustedNamesAsync(sub);

        hidden.Should().Equal("Demo Visible");
        back.Should().BeEquivalentTo("Demo Visible", "Demo Oculto");
        (await host.CountAsync("tecnicos_confianza")).Should().Be(2, "el registro no se borra");
    }

    [Fact]
    public async Task Identificador_De_Otra_Persona_En_El_Cuerpo_Se_Ignora_Y_La_Lista_Es_Solo_De_Quien_Llama_CA_042_16()
    {
        await using var host = await Spec020Host.StartAsync();
        var caller = Guid.NewGuid().ToString("D");
        var victim = Guid.NewGuid().ToString("D");
        var technician = await host.SeedPublicTechnicianAsync();

        using var save = await host.SendAsync(HttpMethod.Put, $"{_route}/{technician.Id}", caller, rawJson: $"{{\"clienteId\":\"{victim}\",\"usuarioId\":\"{victim}\"}}");

        save.StatusCode.Should().Be(HttpStatusCode.Created);
        (await host.TrustedNamesAsync(caller)).Should().HaveCount(1);
        (await host.TrustedNamesAsync(victim)).Should().BeEmpty("la lista de otra persona no se toca ni se ve");
        var saved = await host.Database.GetCollection<BsonDocument>("tecnicos_confianza").Find(FilterDefinition<BsonDocument>.Empty).SingleAsync();
        saved["clienteId"].AsString.Should().Be(caller);
    }

    [Fact]
    public async Task Los_Errores_No_Llevan_Datos_Personales_CA_042_17()
    {
        await using var host = await Spec020Host.StartAsync();
        var sub = Guid.NewGuid().ToString("D");
        var technician = await host.SeedPublicTechnicianAsync(name: "Demo Privado");
        var unknown = ObjectId.GenerateNewId();

        using var notFound = await host.SaveTrustedAsync(sub, unknown);
        using var notSaved = await host.SendAsync(HttpMethod.Delete, $"{_route}/{technician.Id}", sub);
        using var invalid = await host.SendAsync(HttpMethod.Put, $"{_route}/no-es-un-id", sub);

        foreach (var response in new[] { notFound, notSaved, invalid })
        {
            var body = ProblemBody.Reasons(await response.Content.ReadAsStringAsync());
            body.Should().NotContain(sub).And.NotContain(unknown.ToString()).And.NotContain(technician.Id.ToString()).And.NotContain("Demo Privado");
        }
    }

    [Fact]
    public async Task Pasado_El_Limite_Responde_429_Y_Un_Cuerpo_Grande_413_CA_042_18()
    {
        await using var host = await Spec020Host.StartAsync();
        var sub = Guid.NewGuid().ToString("D");
        var id = ObjectId.GenerateNewId();
        using var tooLarge = await host.SendAsync(HttpMethod.Put, $"{_route}/{id}", sub, rawJson: new string('x', 2048));

        HttpResponseMessage? limited = null;
        for (var i = 0; i < 31; i++)
        {
            limited?.Dispose();
            limited = await host.SendAsync(HttpMethod.Get, _route, sub);
        }

        using var response = limited!;
        tooLarge.StatusCode.Should().Be(HttpStatusCode.RequestEntityTooLarge);
        response.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        response.Headers.RetryAfter.Should().NotBeNull();
    }

    [Fact]
    public async Task Dos_Peticiones_Iguales_A_La_Vez_No_Dejan_Duplicados_CA_042_19()
    {
        await using var host = await Spec020Host.StartAsync();
        var technician = await host.SeedPublicTechnicianAsync();
        var sub = Guid.NewGuid().ToString("D");
        var collection = host.Database.GetCollection<BsonDocument>("tecnicos_confianza");

        // El emulador a veces contesta 500 por bloqueo mutuo; esa ráfaga se repite con la colección vacía, así el conteo sigue siendo exacto.
        var summary = await ResponseSummary.WithoutEmulatorDeadlockAsync(async () =>
        {
            await collection.DeleteManyAsync(FilterDefinition<BsonDocument>.Empty);
            return await ResponseSummary.ReadAsync(await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => host.SaveTrustedAsync(sub, technician.Id))));
        });

        summary.Count(HttpStatusCode.Created).Should().Be(1, $"solo una crea; respuestas: {summary}");
        summary.Count(HttpStatusCode.OK).Should().Be(4, $"las otras cuatro encuentran el registro; respuestas: {summary}");
        (await host.CountAsync("tecnicos_confianza")).Should().Be(1, $"respuestas: {summary}");
    }
}

/// <summary>Apoyo de las pruebas de la spec 042 sobre el host de la 020.</summary>
internal static class Spec042Host
{
    public static Task<HttpResponseMessage> SaveTrustedAsync(this Spec020Host host, string sub, ObjectId technicianId) =>
        host.SendAsync(HttpMethod.Put, $"/api/v1/me/tecnicos-confianza/{technicianId}", sub);

    public static Task SetTechnicianAsync(this Spec020Host host, ObjectId technicianId, UpdateDefinition<BsonDocument> update) =>
        host.Database.GetCollection<BsonDocument>("tecnicos").UpdateOneAsync(Builders<BsonDocument>.Filter.Eq("_id", technicianId), update);

    public static async Task<IReadOnlyList<string?>> TrustedNamesAsync(this Spec020Host host, string sub)
    {
        using var response = await host.SendAsync(HttpMethod.Get, "/api/v1/me/tecnicos-confianza", sub);
        response.EnsureSuccessStatusCode();
        return [.. (await Spec020Host.ReadAsync(response)).GetProperty("items").EnumerateArray().Select(i => i.GetProperty("nombre").GetString())];
    }
}
