extern alias identity;
using System.Reflection;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using NetArchTest.Rules;
using Xunit;
using DataExtensions = identity::Knock.Identity.Api.Hosting.DataExtensions;
using DemoClientSeeder = identity::Knock.Identity.Api.Modules.Usuarios.Services.DemoClientSeeder;
using UsuariosStore = identity::Knock.Identity.Api.Modules.Usuarios.Data.UsuariosStore;

namespace Knock.UnitTest.Architecture;

public sealed class DemoSeedBoundaryTests
{
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
