using System.Net;
using AwesomeAssertions;
using Knock.Api.Hosting;
using Knock.IntegrationTest.Data;
using Knock.IntegrationTest.Support;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Xunit;

namespace Knock.IntegrationTest.Hosting;

public sealed class MongoReadinessTests
{
    [Fact]
    public async Task HealthReadyReflectsMongoAvailability_TC_TT_BD_06_15()
    {
        // PostgreSQL y Blob siguen en readiness (Catalog usa EF Core hasta la segunda parte de TT-BD-06): se levantan reales
        // para que el único cambio entre las dos peticiones sea MongoDB.
        await using var postgis = await PostgisFixture.StartAsync();
        await using var azurite = await AzuriteFixture.StartAsync();
        await using var mongo = await DocumentDbFixture.StartAsync();
        using var factory = new KnockApiFactory(
            connectionString: postgis.ConnectionString,
            settings: new Dictionary<string, string?>
            {
                ["ConnectionStrings:KnockBlob"] = azurite.ConnectionString,
                ["ConnectionStrings:KnockMongo"] = mongo.ConnectionString,
                ["Mongo:Databases:Identity"] = "identity",
            },
            configureServices: services => services
                .AddKnockMongoDatabase("Catalog")
                .AddKnockMongoDatabase("Identity"));
        using var client = factory.CreateClient();

        factory.Services.GetRequiredKeyedService<IMongoDatabase>("Catalog").DatabaseNamespace.DatabaseName.Should().Be("market");
        factory.Services.GetRequiredKeyedService<IMongoDatabase>("Identity").DatabaseNamespace.DatabaseName.Should().Be("identity");
        var registrations = factory.Services.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value.Registrations;
        registrations.Should().ContainSingle(
            registration => registration.Name == MongoExtensions.HealthCheckName,
            "dos módulos con base comparten la misma comprobación");

        using var readyUp = await client.GetAsync(new Uri("/health/ready", UriKind.Relative));
        using var liveUp = await client.GetAsync(new Uri("/health", UriKind.Relative));
        await mongo.StopAsync();
        using var readyDown = await client.GetAsync(new Uri("/health/ready", UriKind.Relative));
        using var liveDown = await client.GetAsync(new Uri("/health", UriKind.Relative));

        readyUp.StatusCode.Should().Be(HttpStatusCode.OK);
        liveUp.StatusCode.Should().Be(HttpStatusCode.OK);
        readyDown.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        liveDown.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
