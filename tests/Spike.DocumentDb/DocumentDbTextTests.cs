using AwesomeAssertions;
using Knock.Spike.DocumentDb.Support;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;
using Xunit.Abstractions;

namespace Knock.Spike.DocumentDb;

/// <summary>P5: búsqueda de oficios en español sin distinguir acentos ni mayúsculas.</summary>
public sealed class DocumentDbTextTests(ITestOutputHelper output)
{
    private static readonly Collation _spanishPrimary = new("es", strength: CollationStrength.Primary);

    [Theory]
    [LocalTargets]
    [AzureTarget]
    public async Task SpanishTextSearchIgnoresAccents_TC_TT_BD_05_05(string target)
    {
        await using var spike = await SpikePool.CreateDatabaseAsync(target);
        // Solo cabe un índice de texto por colección: una colección por idioma del índice.
        var spanish = await OficiosWithTextIndexAsync(spike.Database, SpikeSeeds.Oficios, "spanish");
        var noStemming = await OficiosWithTextIndexAsync(spike.Database, SpikeSeeds.Oficios + "_none", "none");

        var text = (await TextAsync(spanish, "plomeria", "spanish"), await TextAsync(spanish, "electricidad", "spanish"));
        var textNone = (await TextAsync(noStemming, "plomeria", "none"), await TextAsync(noStemming, "electricidad", "none"));
        var collation = (await CollationAsync(spanish, "plomeria"), await CollationAsync(spanish, "electricidad"));
        output.WriteLine($"[{target}] $text spanish: {text} · $text none: {textNone} · colación es/1: {collation}");

        // Spec P5: basta con que una estrategia funcione; la evidencia registra cuál.
        var expected = ("Plomería", "Electricidad");
        new[] { text, textNone, collation }.Should().Contain(expected);
    }

    private static async Task<IMongoCollection<BsonDocument>> OficiosWithTextIndexAsync(
        IMongoDatabase db, string name, string language)
    {
        var oficios = db.GetCollection<BsonDocument>(name);
        await oficios.InsertManyAsync(SpikeSeeds.LoadOficios());
        await oficios.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(
            Builders<BsonDocument>.IndexKeys.Text("nombre"),
            new CreateIndexOptions { DefaultLanguage = language }));
        return oficios;
    }

    private static async Task<string?> TextAsync(IMongoCollection<BsonDocument> oficios, string term, string language)
    {
        try
        {
            var doc = await oficios.Find(Builders<BsonDocument>.Filter.Text(term, language)).FirstOrDefaultAsync();
            return doc?["nombre"].AsString;
        }
        catch (MongoException ex)
        {
            return $"error {ex.GetType().Name}";
        }
    }

    private static async Task<string?> CollationAsync(IMongoCollection<BsonDocument> oficios, string term)
    {
        try
        {
            var doc = await oficios
                .Find(Builders<BsonDocument>.Filter.Eq("nombre", term), new FindOptions { Collation = _spanishPrimary })
                .FirstOrDefaultAsync();
            return doc?["nombre"].AsString;
        }
        catch (MongoException ex)
        {
            return $"error {ex.GetType().Name}";
        }
    }
}
