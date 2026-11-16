using System.Net;
using System.Text.Json;
using AwesomeAssertions;
using MongoDB.Bson;
using Xunit;

namespace Knock.IntegrationTest.Requests;

/// <summary>Spec 020, T-06: listado propio con paginación por cursor (MongoDB real).</summary>
public sealed class Spec020ListadoTests
{
    private static async Task SeedRequestsAsync(Spec020Host host, string client, int count, string state = "enviada", int minuteOffset = 0)
    {
        var start = DateTime.UtcNow.AddHours(-3);
        var documents = Enumerable.Range(0, count).Select(i => new BsonDocument
        {
            { "_id", ObjectId.GenerateNewId() },
            { "clienteId", client },
            { "oficioClave", Spec020Host.Plumbing },
            { "oficioNombre", "Plomería" },
            { "modalidad", "domicilio" },
            { "descripcion", Spec020Host.Description },
            { "fotos", new BsonArray { ObjectId.GenerateNewId().ToString() } },
            { "dia", Spec020Host.Tomorrow() },
            { "franja", "tarde" },
            { "coloniaId", host.ColoniaId.ToString() },
            { "coloniaNombre", "Centro" },
            { "coloniaCp", "42800" },
            { "estado", state },
            { "venceEn", DateTime.UtcNow.AddHours(48) },
            { "creadoEn", start.AddMinutes(minuteOffset + i) },
            { "actualizadoEn", start.AddMinutes(minuteOffset + i) },
        });
        await host.Database.GetCollection<BsonDocument>("solicitudes").InsertManyAsync(documents);
    }

    private static async Task<(JsonElement Page, string[] Ids)> PageAsync(Spec020Host host, string query)
    {
        using var response = await host.SendAsync(HttpMethod.Get, "/api/v1/solicitudes" + query, host.ClientSub);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await Spec020Host.ReadAsync(response);
        return (page, [.. page.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("id").GetString()!)]);
    }

    [Fact]
    public async Task Pagina_Solicitudes_Propias_CA_020_14()
    {
        await using var host = await Spec020Host.StartAsync();
        await SeedRequestsAsync(host, host.ClientSub, 25);
        await SeedRequestsAsync(host, Guid.NewGuid().ToString("D"), 3);

        var (first, firstIds) = await PageAsync(host, string.Empty);
        var (second, secondIds) = await PageAsync(host, "?cursor=" + Uri.EscapeDataString(first.GetProperty("nextCursor").GetString()!));
        var (all, allIds) = await PageAsync(host, "?limit=50");

        firstIds.Should().HaveCount(20);
        first.GetProperty("nextCursor").GetString().Should().NotBeNullOrEmpty();
        secondIds.Should().HaveCount(5);
        second.GetProperty("nextCursor").ValueKind.Should().Be(JsonValueKind.Null, "la última página no trae cursor");
        allIds.Should().HaveCount(25, "solo las del sub del token");
        all.GetProperty("nextCursor").ValueKind.Should().Be(JsonValueKind.Null);
        firstIds.Concat(secondIds).Should().Equal(allIds, "las dos páginas siguen el mismo orden");
        var created = all.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("creadoEn").GetDateTimeOffset()).ToList();
        created.Should().BeInDescendingOrder("de la más reciente a la más antigua");
        var row = all.GetProperty("items")[0];
        row.TryGetProperty("descripcion", out _).Should().BeFalse("el resumen no lleva descripción");
        row.TryGetProperty("direccion", out _).Should().BeFalse();
        row.GetProperty("totalFotos").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task Filtra_Por_Estado_R_09()
    {
        await using var host = await Spec020Host.StartAsync();
        await SeedRequestsAsync(host, host.ClientSub, 3, "enviada");
        await SeedRequestsAsync(host, host.ClientSub, 2, "cotizando", minuteOffset: 100);

        var (_, ids) = await PageAsync(host, "?estado=cotizando");

        ids.Should().HaveCount(2);
    }

    [Fact]
    public async Task Cursor_Y_Limite_Invalidos_CA_020_21()
    {
        await using var host = await Spec020Host.StartAsync();
        await SeedRequestsAsync(host, host.ClientSub, 3);

        using var altered = await host.SendAsync(HttpMethod.Get, "/api/v1/solicitudes?cursor=abc", host.ClientSub);
        using var zero = await host.SendAsync(HttpMethod.Get, "/api/v1/solicitudes?limit=0", host.ClientSub);
        using var tooMany = await host.SendAsync(HttpMethod.Get, "/api/v1/solicitudes?limit=51", host.ClientSub);
        using var badState = await host.SendAsync(HttpMethod.Get, "/api/v1/solicitudes?estado=inventado", host.ClientSub);

        altered.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await Spec020Host.ReadAsync(altered)).GetProperty("code").GetString().Should().Be("invalid_cursor");
        zero.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await Spec020Host.ReadAsync(zero)).GetProperty("errors").GetProperty("limit").GetArrayLength().Should().BeGreaterThan(0);
        tooMany.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        badState.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }
}
