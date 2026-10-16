using AwesomeAssertions;
using Knock.Api.Modules.Catalog.Data;
using Knock.DatabaseTest.Support;
using Knock.IntegrationTest.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Knock.DatabaseTest;

public sealed class CatalogTradeSeedTests : IAsyncLifetime
{
    private readonly CatalogDatabaseFixture _database = new();

    public Task InitializeAsync() => _database.InitializeAsync();

    public Task DisposeAsync() => _database.DisposeAsync();

    [Fact]
    public async Task SeedContainsRequiredActiveTradesAndModalities_TC_TT_BD_02_03()
    {
        await using var scope = _database.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();

        var oficios = await context.Oficios.AsNoTracking()
            .OrderBy(o => o.Clave)
            .Select(o => new { o.Clave, o.Nombre, o.Modalidad, o.Activo })
            .ToListAsync();

        oficios.Should().BeEquivalentTo(
        [
            new { Clave = "ele", Nombre = "Electricidad", Modalidad = "domicilio", Activo = true },
            new { Clave = "mec", Nombre = "Mecánica", Modalidad = "taller", Activo = true },
            new { Clave = "plo", Nombre = "Plomería", Modalidad = "domicilio", Activo = true },
        ]);
    }

    [Fact]
    public async Task ReapplyingSeedDoesNotDuplicateCatalogRows_TC_TT_BD_02_09()
    {
        await using var scope = _database.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var oficiosBefore = await context.Oficios.Select(o => o.Clave).OrderBy(c => c).ToListAsync();
        var coloniasBefore = await ColoniaKeysAsync(context);

        await CatalogTestDatabase.SeedAsync(scope.ServiceProvider);

        var oficiosAfter = await context.Oficios.Select(o => o.Clave).OrderBy(c => c).ToListAsync();
        var coloniasAfter = await ColoniaKeysAsync(context);
        oficiosAfter.Should().Equal(oficiosBefore).And.OnlyHaveUniqueItems();
        coloniasAfter.Should().Equal(coloniasBefore).And.OnlyHaveUniqueItems();
        coloniasAfter.Should().Contain("Tula de Allende|42800|Centro");
    }

    private static Task<List<string>> ColoniaKeysAsync(CatalogDbContext context) =>
        context.Colonias
            .Select(c => c.Municipio + "|" + c.Cp + "|" + c.Nombre)
            .OrderBy(k => k)
            .ToListAsync();
}
