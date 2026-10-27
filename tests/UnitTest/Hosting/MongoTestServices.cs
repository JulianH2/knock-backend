using System.Security.Cryptography;
using Knock.Api.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Knock.UnitTest.Hosting;

/// <summary>Contenedor de servicios con <c>AddKnockMongo</c> y configuración en memoria; el cliente no se conecta (el host no responde).</summary>
internal static class MongoTestServices
{
    /// <summary>Cadena sintética con usuario y contraseña de un solo uso y <c>maxPoolSize=100</c>, hacia un host que no responde.</summary>
    public static string SyntheticConnectionString()
    {
        var url = new MongoUrlBuilder
        {
            Server = new MongoServerAddress("127.0.0.1", 9),
            Username = "knock-fake",
            Password = "Fake" + Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(12)),
            DirectConnection = true,
        };

        // MongoUrlBuilder omite el valor por defecto (100); se agrega a mano para que la cadena lo pida explícitamente.
        return url + "&maxPoolSize=100";
    }

    /// <summary><c>src/Knock.Api/appsettings.json</c> del repositorio.</summary>
    public static string RepositoryAppSettings()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Knock.slnx")))
        {
            dir = dir.Parent;
        }

        return dir is null
            ? throw new InvalidOperationException("No se encontró Knock.slnx subiendo desde el directorio de pruebas.")
            : Path.Combine(dir.FullName, "src", "Knock.Api", "appsettings.json");
    }

    /// <summary>Servicios con la configuración dada (sobre el appsettings.json del repositorio si se pide) y la base de cada módulo.</summary>
    public static ServiceProvider Build(
        IReadOnlyDictionary<string, string?> values, bool withRepositoryAppSettings = false, params string[] modules)
    {
        var builder = new ConfigurationBuilder();
        if (withRepositoryAppSettings)
        {
            builder.AddJsonFile(RepositoryAppSettings(), optional: false);
        }

        var services = new ServiceCollection()
            .AddSingleton<IConfiguration>(builder.AddInMemoryCollection(values).Build())
            .AddKnockMongo();
        foreach (var module in modules)
        {
            services.AddKnockMongoDatabase(module);
        }

        return services.BuildServiceProvider();
    }
}
