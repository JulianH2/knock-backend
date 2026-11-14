using System.Security.Cryptography;
using AwesomeAssertions;
using Knock.Market.Api.Modules.Catalog;
using Knock.Market.Api.Modules.Catalog.Domain;
using Knock.Market.Api.Modules.Catalog.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Xunit;

namespace Knock.UnitTest.Catalog;

public sealed class Spec027SearchSafetyTests
{
    [Theory]
    [InlineData("una búsqueda cualquiera")]
    [InlineData("correo@ejemplo.test")]
    [InlineData("calle inventada 123")]
    [InlineData("5555550101")]
    public void Filtro_No_Disponible_Impide_Contar_R_027_12(string term)
    {
        var filter = new FiltroDatosPersonalesNoDisponible();
        filter.CanCount(term).Should().BeFalse();
    }

    [Fact]
    public void Clave_Cursor_Exige_256_Bits_CA_027_11()
    {
        var validator = new SearchCursorOptionsValidator();
        validator.Validate(null, new SearchCursorOptions()).Failed.Should().BeTrue();
        validator.Validate(null, new SearchCursorOptions { Key = "no-base64" }).Failed.Should().BeTrue();
        validator.Validate(null, new SearchCursorOptions { Key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(31)) }).Failed.Should().BeTrue();
        validator.Validate(null, new SearchCursorOptions { Key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)) }).Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Estado_Del_Cursor_Para_Paginacion_En_Base_CA_027_11()
    {
        var key = RandomNumberGenerator.GetBytes(32);
        var query = new SearchQuery("boiler", "colonia", null, false, 1);
        var last = new SearchRank("id-demo", 1, true, 4, 1.2);
        var token = CursorDeBusqueda.CreateNext(query, last, 20, key);
        var state = CursorDeBusqueda.Read(token, query, key);
        state.Last.Should().Be(last);
        state.Count.Should().Be(20);
        Action wrong = () => CursorDeBusqueda.Read(token, query with { DictionaryVersion = 2 }, key);
        wrong.Should().Throw<FormatException>();
        Action invalidCount = () => CursorDeBusqueda.CreateNext(query, last, 100, key);
        invalidCount.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData("Production", false)]
    [InlineData("Staging", false)]
    [InlineData("Development", false)]
    [InlineData("Production", true)]
    [InlineData("Development", true)]
    public async Task Arranque_Exige_Clave_Cursor_R_027_11(string environment, bool configured)
    {
        var values = new Dictionary<string, string?>
        {
            ["ConnectionStrings:KnockMongo"] = new MongoUrlBuilder { Server = new MongoServerAddress("localhost", 27017) }.ToString(),
            ["Mongo:MaxPoolSize"] = "5",
            ["Mongo:Databases:Catalog"] = "market_test",
            [SearchCursorOptions.KeyName] = configured ? Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)) : null,
        };
        using var host = new HostBuilder().UseEnvironment(environment).ConfigureAppConfiguration(builder => builder.AddInMemoryCollection(values))
            .ConfigureServices((context, services) => services.AddCatalogModule(context.Configuration)).Build();
        if (configured || environment == "Development")
        {
            await host.StartAsync();
            host.Services.GetRequiredService<BusquedaService>().Should().NotBeNull();
            host.Services.GetRequiredService<IFiltroDatosPersonales>().Should().BeOfType<FiltroDatosPersonalesNoDisponible>();
            var key = host.Services.GetRequiredService<IOptions<SearchCursorOptions>>().Value.Key;
            Convert.FromBase64String(key!).Length.Should().Be(32);
            host.Services.GetRequiredService<IOptionsMonitor<SearchCursorOptions>>().CurrentValue.Key.Should().Be(key);
            if (configured) key.Should().Be(values[SearchCursorOptions.KeyName]);
            await host.StopAsync();
        }
        else
        {
            var act = async () => await host.StartAsync();
            (await act.Should().ThrowAsync<OptionsValidationException>()).Which.Failures.Should().Contain(f => f.Contains(SearchCursorOptions.KeyName, StringComparison.Ordinal));
        }
    }
}
