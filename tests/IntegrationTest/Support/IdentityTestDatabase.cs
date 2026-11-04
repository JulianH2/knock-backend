using System.Security.Cryptography;
using Knock.Api.Modules.Identity.Data;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Knock.IntegrationTest.Support;

/// <summary>
/// Host de pruebas con la base de Identity sobre documentdb-local (contenedor compartido): una base propia por host, con los índices de la
/// spec creados, para que las pruebas no se pisen. Solo datos ficticios.
/// </summary>
internal static class IdentityTestDatabase
{
    public static async Task<KnockApiFactory> CreateFactoryAsync(
        Action<IServiceCollection>? configureServices = null, IReadOnlyDictionary<string, string?>? settings = null)
    {
        var mongo = await DocumentDbFixture.SharedAsync();
        var values = new Dictionary<string, string?>
        {
            ["ConnectionStrings:KnockMongo"] = mongo.ConnectionString,
            ["Mongo:Databases:Identity"] = "identity_it_" + Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(6)),
        };
        foreach (var (key, value) in settings ?? new Dictionary<string, string?>())
        {
            values[key] = value;
        }

        var factory = new KnockApiFactory(settings: values, configureServices: configureServices);
        await factory.Services.GetRequiredService<IdentityStore>().EnsureIndexesAsync(CancellationToken.None);
        return factory;
    }

    /// <summary>Base de Identity de un host creado con <see cref="CreateFactoryAsync"/>.</summary>
    public static IMongoDatabase DatabaseOf(KnockApiFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        var name = factory.Services.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>()["Mongo:Databases:Identity"]!;
        return factory.Services.GetRequiredService<IMongoClient>().GetDatabase(name);
    }
}
