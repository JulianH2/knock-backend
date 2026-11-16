extern alias identity;
using System.Reflection;
using AwesomeAssertions;
using Knock.Market.Api.Modules.Requests;
using Knock.Market.Api.Modules.Requests.Data;
using Knock.Market.Api.Modules.Requests.Services;
using Microsoft.Extensions.DependencyInjection;
using NetArchTest.Rules;
using Xunit;
using DataExtensions = identity::Knock.Identity.Api.Hosting.DataExtensions;
using DemoClientSeeder = identity::Knock.Identity.Api.Modules.Usuarios.Services.DemoClientSeeder;
using UsuariosStore = identity::Knock.Identity.Api.Modules.Usuarios.Data.UsuariosStore;

namespace Knock.UnitTest.Architecture;

public sealed class DemoSeedBoundaryTests
{
    [Theory]
    [InlineData(false, "Development", false)]
    [InlineData(true, "Production", false)]
    [InlineData(true, "production", false)]
    [InlineData(true, "Development", true)]
    public void MarketDemoStartup_OnlyRunsWhenEnabledOutsideProduction(bool enabled, string environment, bool expected)
    {
        DemoTechnicianStartup.ShouldRun(enabled, environment).Should().Be(expected);
    }

    [Fact]
    public void MarketDemoBoundary_UsesRequestsDataOnly()
    {
        var assembly = typeof(DemoTechnicianSeeder).Assembly;
        Types.InAssembly(assembly).That().HaveName(nameof(DemoTechnicianSeeder))
            .ShouldNot().HaveDependencyOnAny("MongoDB.Driver", "Knock.Identity.Api", "Knock.Messaging.Api")
            .GetResult().IsSuccessful.Should().BeTrue();
        typeof(DemoTechnicianSeeder).GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .SelectMany(x => x.GetParameters()).Select(x => x.ParameterType).Should().Contain(typeof(DemoTechnicianStore));
        var database = typeof(DemoTechnicianStore).GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .SelectMany(x => x.GetParameters()).Single(x => x.ParameterType.FullName == "MongoDB.Driver.IMongoDatabase");
        database.GetCustomAttribute<FromKeyedServicesAttribute>()!.Key.Should().Be(RequestsModuleExtensions.ModuleKey);
    }
    [Fact]
    public void IdentityBoundary_CA_038_08()
    {
        var assembly = typeof(DemoClientSeeder).Assembly;
        var seeder = Types.InAssembly(assembly).That().HaveName(nameof(DemoClientSeeder)).GetTypes().ToArray();
        seeder.Should().ContainSingle();
        Types.InAssembly(assembly).That().HaveName(nameof(DemoClientSeeder))
            .ShouldNot().HaveDependencyOnAny("MongoDB.Driver", "Knock.Market.Api", "Knock.Messaging.Api")
            .GetResult().IsSuccessful.Should().BeTrue();
        typeof(DemoClientSeeder).GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .SelectMany(constructor => constructor.GetParameters()).Select(parameter => parameter.ParameterType)
            .Should().Contain(typeof(UsuariosStore));
        UsuariosStore.UsersCollection.Should().Be("usuarios");
        DataExtensions.DatabaseKey.Should().Be("Identity");
        typeof(UsuariosStore).GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(property => property.PropertyType.IsGenericType && property.PropertyType.GetGenericTypeDefinition().FullName == "MongoDB.Driver.IMongoCollection`1")
            .Select(property => property.Name).Should().Equal("Users");
        var database = typeof(UsuariosStore).GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .SelectMany(constructor => constructor.GetParameters()).Single(parameter => parameter.ParameterType.FullName == "MongoDB.Driver.IMongoDatabase");
        var keyed = database.GetCustomAttribute<FromKeyedServicesAttribute>();
        keyed.Should().NotBeNull();
        keyed.Key.Should().Be(DataExtensions.DatabaseKey);
    }
}
