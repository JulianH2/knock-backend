using AwesomeAssertions;
using Knock.Spike.DocumentDb.Support;
using MongoDB.Driver;
using Xunit;
using Xunit.Abstractions;

namespace Knock.Spike.DocumentDb;

/// <summary>P5: búsqueda de oficios en español sin distinguir acentos ni mayúsculas.</summary>
public sealed class DocumentDbTextTests(ITestOutputHelper output)
{
    [Theory]
    [LocalTargets]
    [AzureTarget]
    public async Task SpanishTextSearchIgnoresAccents_TC_TT_BD_05_05(string target)
    {
        await using var spike = await SpikePool.CreateDatabaseAsync(target);
        // Solo cabe un índice de texto por colección: una colección por idioma del índice.
        var spanish = await SpikeQueries.OficiosWithTextIndexAsync(spike.Database, SpikeSeeds.Oficios, "spanish");
        var noStemming = await SpikeQueries.OficiosWithTextIndexAsync(spike.Database, SpikeSeeds.Oficios + "_none", "none");

        var text = (await SpikeQueries.TextAsync(spanish, "plomeria", "spanish"),
            await SpikeQueries.TextAsync(spanish, "electricidad", "spanish"));
        var textNone = (await SpikeQueries.TextAsync(noStemming, "plomeria", "none"),
            await SpikeQueries.TextAsync(noStemming, "electricidad", "none"));
        var collation = (await SpikeQueries.CollationAsync(spanish, "plomeria"),
            await SpikeQueries.CollationAsync(spanish, "electricidad"));
        output.WriteLine($"[{target}] $text spanish: {text} · $text none: {textNone} · colación es/1: {collation}");

        // Spec P5: basta con que una estrategia funcione; la evidencia registra cuál.
        var expected = ("Plomería", "Electricidad");
        new[] { text, textNone, collation }.Should().Contain(expected);
    }

    /// <summary>
    /// Alternativa propuesta a P5 (hallazgo; el criterio de P5 no cambia): campo sin acentos calculado en la app,
    /// índice ascendente normal y búsqueda por prefijo anclado.
    /// </summary>
    [Theory]
    [LocalTargets]
    [AzureTarget]
    public async Task NormalizedFieldPrefixSearch_IgnoresAccentsAndCase_TC_TT_BD_05_05(string target)
    {
        await using var spike = await SpikePool.CreateDatabaseAsync(target);
        var oficios = await SpikeQueries.OficiosWithNormalizedFieldAsync(spike.Database, SpikeSeeds.Oficios);

        var plomeria = await SpikeQueries.NormalizedPrefixAsync(oficios, "plomeria");
        var electricidad = await SpikeQueries.NormalizedPrefixAsync(oficios, "electricidad");
        var prefix = await SpikeQueries.NormalizedPrefixAsync(oficios, "PLOM");
        var accentedQuery = await SpikeQueries.NormalizedPrefixAsync(oficios, "Electrí");
        // Con pocos oficios el planificador puede preferir COLLSCAN; el hint comprueba que el índice sirve el prefijo.
        var explain = await SpikeQueries.ExplainFindAsync(
            spike.Database, SpikeSeeds.Oficios, SpikeQueries.NormalizedPrefix("plom"));
        var stage = explain is null ? null : SpikeQueries.FindKey(explain, "stage");
        var hinted = await oficios
            .Find(SpikeQueries.NormalizedPrefix("plom"), new FindOptions { Hint = SpikeQueries.NormalizedField + "_1" })
            .ToListAsync();
        output.WriteLine($"[{target}] normalizado: plomeria → [{string.Join(", ", plomeria)}] · electricidad → "
            + $"[{string.Join(", ", electricidad)}] · PLOM → [{string.Join(", ", prefix)}] · Electrí → "
            + $"[{string.Join(", ", accentedQuery)}] · plan ganador: {stage?.ToString() ?? "explain no disponible"} · "
            + $"con hint al índice: [{string.Join(", ", hinted.Select(d => d["nombre"].AsString))}]");

        plomeria.Should().Equal("Plomería");
        electricidad.Should().Equal("Electricidad");
        prefix.Should().Equal("Plomería");
        accentedQuery.Should().Equal("Electricidad");
        hinted.Select(d => d["nombre"].AsString).Should().Equal("Plomería");
    }
}
