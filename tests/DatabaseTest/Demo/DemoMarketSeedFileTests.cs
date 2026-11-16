extern alias identity;

using System.Globalization;
using System.Text.Json;
using Knock.IntegrationTest.Support;
using Knock.Market.Api.Modules.Requests.Domain;
using Knock.Market.Api.Modules.Requests.Validators;
using Microsoft.Extensions.Options;
using Xunit;
using UserIdDerivation = identity::Knock.Identity.Api.Modules.Sesiones.Services.UserIdDerivation;

namespace Knock.DatabaseTest.Demo;

public sealed class DemoMarketSeedFileTests
{
    private static readonly string[] _modalities = ["domicilio", "taller"];
    private static JsonElement[] Load(string path)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(CatalogTestDatabase.SeedsDirectory, path)));
        Assert.Equal(JsonValueKind.Array, document.RootElement.ValueKind);
        return document.RootElement.EnumerateArray().Select(x => x.Clone()).ToArray();
    }

    private static string Text(JsonElement item, string name) => item.GetProperty(name).GetString()!;
    private static string Id(JsonElement item) => Text(item.GetProperty("_id"), "$oid");
    private static DateOnly Day(JsonElement item) => DateOnly.ParseExact(Text(item, "dia"), "yyyy-MM-dd", CultureInfo.InvariantCulture);
    private static int? Amount(JsonElement item, string name) => item.TryGetProperty(name, out var value) && value.ValueKind != JsonValueKind.Null ? value.GetInt32() : null;

    [Fact]
    public void SeedFiles_FormatAndUniqueIds_T13()
    {
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var root = CatalogTestDatabase.SeedsDirectory;
        foreach (var path in Directory.GetFiles(root, "*.json", SearchOption.AllDirectories))
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            Assert.Equal(JsonValueKind.Array, document.RootElement.ValueKind);
            foreach (var item in document.RootElement.EnumerateArray())
            {
                Assert.Matches("^[0-9a-f]{24}$", Id(item));
                Assert.Single(item.GetProperty("_id").EnumerateObject());
                Assert.True(ids.Add(Id(item)));
                if (path.Contains(Path.Combine("demo", "market"), StringComparison.Ordinal))
                {
                    CheckNames(item);
                    Assert.True(item.GetProperty("demo").GetBoolean());
                    Assert.Equal(1, item.GetProperty("version").GetInt32());
                }
            }
        }
        Assert.NotEmpty(Load("demo/market/tecnicos.json"));
        Assert.NotEmpty(Load("demo/market/solicitudes.json"));
        Assert.NotEmpty(Load("demo/market/cotizaciones.json"));
    }

    [Fact]
    public void Requests_UseCatalogModalitiesAndDemoClients_T13()
    {
        var requests = Load("demo/market/solicitudes.json");
        var trades = Load("catalog/oficios.json");
        var colonies = Load("catalog/colonias.json");
        var clients = Load("demo/identity/usuarios.json").Select(x => UserIdDerivation.Derive(Text(x, "uid")).ToString("D")).ToHashSet(StringComparer.Ordinal);
        Assert.Equal(_modalities, requests.Select(x => Text(x, "modalidad")).Distinct().Order(StringComparer.Ordinal));
        foreach (var request in requests)
        {
            Assert.Contains(Text(request, "clienteId"), clients);
            var trade = Assert.Single(trades, x => Text(x, "clave") == Text(request, "oficioClave"));
            Assert.True(trade.GetProperty("activo").GetBoolean());
            Assert.Equal(Text(trade, "modalidad"), Text(request, "modalidad"));
            var colony = Assert.Single(colonies, x => Id(x) == Text(request, "coloniaId"));
            Assert.Equal(Text(colony, "nombre"), Text(request, "coloniaNombre"));
            Assert.Equal(Text(colony, "cp"), Text(request, "coloniaCp"));
            Assert.Equal("cotizando", Text(request, "estado"));
            var created = DateTimeOffset.Parse(Text(request, "creadoEn"), CultureInfo.InvariantCulture);
            var expires = DateTimeOffset.Parse(Text(request, "venceEn"), CultureInfo.InvariantCulture);
            Assert.Equal(TimeSpan.FromHours(48), expires - created);
            Assert.InRange(Day(request).DayNumber - DateOnly.FromDateTime(created.UtcDateTime).DayNumber, 0, 5);
        }
    }

    [Fact]
    public void Quotes_ThreeEligibleTechniciansAndDomainRules_T13()
    {
        var quotes = Load("demo/market/cotizaciones.json");
        var requests = Load("demo/market/solicitudes.json");
        var technicians = Load("demo/market/tecnicos.json");
        Assert.Equal(3, quotes.Length);
        Assert.Equal(3, quotes.Select(x => Text(x.GetProperty("tecnicoId"), "$oid")).Distinct().Count());
        var pricing = new QuotePricingOptions();
        var validator = new QuoteValidator(Options.Create(pricing));
        foreach (var quote in quotes)
        {
            var request = Assert.Single(requests, x => Id(x) == Text(quote.GetProperty("solicitudId"), "$oid"));
            var technician = Assert.Single(technicians, x => Id(x) == Text(quote.GetProperty("tecnicoId"), "$oid"));
            Assert.Equal("cotizando", Text(request, "estado"));
            Assert.Equal("activo", Text(technician, "estado"));
            Assert.False(technician.GetProperty("oculto").GetBoolean());
            Assert.StartsWith("Demo ", Text(technician, "nombre"), StringComparison.Ordinal);
            Assert.Matches("^demo-tecnico-[0-9]{2}$", Text(technician, "uid"));
            Assert.Equal(UserIdDerivation.Derive(Text(technician, "uid")).ToString("D"), Text(technician, "usuarioId"));
            Assert.Equal(Text(technician, "usuarioId"), Text(quote, "tecnicoUsuarioId"));
            Assert.Contains(technician.GetProperty("oficios").EnumerateArray(), x => x.GetString() == Text(request, "oficioClave"));
            Assert.Equal(Text(request, "coloniaId"), Text(technician, "coloniaBaseId"));
            var colony = Assert.Single(Load("catalog/colonias.json"), x => Id(x) == Text(request, "coloniaId"));
            Assert.Equal(colony.GetProperty("centro").GetRawText(), technician.GetProperty("ubicacionBase").GetRawText());
            Assert.InRange(technician.GetProperty("radioAtencionKm").GetInt32(), 1, 30);
            var type = Text(quote, "tipoPrecio");
            var price = Amount(quote, "precio");
            var min = Amount(quote, "precioMin");
            var max = Amount(quote, "precioMax");
            var input = new QuoteValidationInput(type, price, min, max, Day(quote), Text(quote, "hora"), Text(quote, "nota"), Day(request));
            Assert.True(validator.Validate(input).IsValid);
            var cap = QuotePricing.Calculate(Enum.Parse<PriceType>(type, true), Text(request, "oficioClave"), price, min, max, pricing);
            Assert.Equal(cap.Tope, quote.GetProperty("tope").GetInt32());
            Assert.Equal(cap.RequiereAprobacion, quote.GetProperty("requiereAprobacion").GetBoolean());
            Assert.Equal("enviada", Text(quote, "estado"));
        }
        foreach (var group in quotes.GroupBy(x => Text(x.GetProperty("solicitudId"), "$oid")))
        {
            Assert.InRange(group.Count(), 1, 5);
            Assert.Equal(group.Count(), group.Select(x => Text(x.GetProperty("tecnicoId"), "$oid")).Distinct().Count());
        }
    }

    private static void CheckNames(JsonElement item)
    {
        if (item.ValueKind == JsonValueKind.Array)
            foreach (var value in item.EnumerateArray()) CheckNames(value);
        if (item.ValueKind != JsonValueKind.Object) return;
        foreach (var property in item.EnumerateObject())
        {
            Assert.Matches("^(?:_id|\\$oid|[a-z][a-zA-Z0-9]*)$", property.Name);
            CheckNames(property.Value);
        }
    }
}
