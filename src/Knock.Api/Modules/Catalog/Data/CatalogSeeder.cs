using Knock.Api.Modules.Catalog.Domain;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace Knock.Api.Modules.Catalog.Data;

/// <summary>
/// Aplica las semillas versionadas <c>db/seeds/catalog/oficios.json</c> y <c>colonias.json</c> (<c>_id</c> fijos, iguales en todos
/// los entornos). Es repetible: hace upsert por la clave natural (<c>clave</c>; <c>municipio</c>, <c>cp</c>, <c>nombre</c>) y solo
/// sube <c>version</c> si cambia el contenido. Valida todos los documentos antes de escribir el primero.
/// </summary>
internal sealed class CatalogSeeder(CatalogStore store)
{
    public const string OficiosFile = "oficios.json";
    public const string ColoniasFile = "colonias.json";

    /// <summary>Lee los dos JSON de <c>&lt;seedsDirectory&gt;/catalog</c> (la carpeta <c>db/seeds</c> del repositorio) y los aplica.</summary>
    public async Task SeedAsync(string seedsDirectory, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(seedsDirectory);
        var oficios = await ReadAsync(Path.Combine(seedsDirectory, "catalog", OficiosFile), cancellationToken);
        var colonias = await ReadAsync(Path.Combine(seedsDirectory, "catalog", ColoniasFile), cancellationToken);
        await SeedAsync(oficios, colonias, cancellationToken);
    }

    /// <exception cref="InvalidDataException">Un documento no cumple el modelo de spec §5; no se escribe nada.</exception>
    /// <exception cref="MongoWriteException">El servidor rechazó un documento (p. ej. un polígono inválido para <c>2dsphere</c>).</exception>
    public async Task SeedAsync(
        IReadOnlyList<BsonDocument> oficios,
        IReadOnlyList<BsonDocument> colonias,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(oficios);
        ArgumentNullException.ThrowIfNull(colonias);
        var oficioDocuments = oficios.Select(ToOficio).ToList();
        var coloniaDocuments = colonias.Select(ToColonia).ToList();

        await store.EnsureIndexesAsync(cancellationToken);
        foreach (var oficio in oficioDocuments)
        {
            await UpsertAsync(
                store.Oficios,
                Builders<BsonDocument>.Filter.Eq(CatalogStore.ClaveField, oficio[CatalogStore.ClaveField]),
                oficio,
                cancellationToken);
        }

        foreach (var colonia in coloniaDocuments)
        {
            var filter = Builders<BsonDocument>.Filter;
            await UpsertAsync(
                store.Colonias,
                filter.Eq(CatalogStore.MunicipioField, colonia[CatalogStore.MunicipioField])
                    & filter.Eq(CatalogStore.CpField, colonia[CatalogStore.CpField])
                    & filter.Eq(CatalogStore.NombreField, colonia[CatalogStore.NombreField]),
                colonia,
                cancellationToken);
        }
    }

    private static async Task<IReadOnlyList<BsonDocument>> ReadAsync(string path, CancellationToken cancellationToken)
    {
        var json = await File.ReadAllTextAsync(path, cancellationToken);
        return BsonSerializer.Deserialize<BsonArray>(json).Select(value => value.AsBsonDocument).ToList();
    }

    /// <summary>Inserta con <c>version</c> 1; si ya existe y el contenido cambió, lo reemplaza con <c>version</c> + 1.</summary>
    private static async Task UpsertAsync(
        IMongoCollection<BsonDocument> collection,
        FilterDefinition<BsonDocument> naturalKey,
        BsonDocument document,
        CancellationToken cancellationToken)
    {
        var existing = await collection.Find(naturalKey).FirstOrDefaultAsync(cancellationToken);
        if (existing is null)
        {
            document[CatalogStore.VersionField] = 1;
            await collection.InsertOneAsync(document, cancellationToken: cancellationToken);
            return;
        }

        var version = existing[CatalogStore.VersionField].ToInt32();
        existing.Remove(CatalogStore.VersionField);
        if (existing.Equals(document))
        {
            return;
        }

        document[CatalogStore.VersionField] = version + 1;
        await collection.ReplaceOneAsync(naturalKey, document, cancellationToken: cancellationToken);
    }

    private static BsonDocument ToOficio(BsonDocument source)
    {
        var clave = RequiredString(source, CatalogStore.ClaveField, Oficio.MaxClaveLength, "oficio");
        var nombre = RequiredString(source, CatalogStore.NombreField, Oficio.MaxNombreLength, "oficio");
        var modalidad = source.GetValue(CatalogStore.ModalidadField, BsonNull.Value);
        if (!modalidad.IsString || !Oficio.IsValidModalidad(modalidad.AsString))
        {
            throw new InvalidDataException($"Seed oficio '{clave}' must have modalidad 'domicilio' or 'taller'.");
        }

        return new BsonDocument
        {
            { CatalogStore.IdField, RequiredObjectId(source, "oficio") },
            { CatalogStore.ClaveField, clave },
            { CatalogStore.NombreField, nombre },
            { CatalogStore.NombreBusquedaField, SearchNameNormalizer.Normalize(nombre) },
            { CatalogStore.ModalidadField, modalidad.AsString },
            { CatalogStore.ActivoField, RequiredBoolean(source, CatalogStore.ActivoField, "oficio") },
        };
    }

    /// <remarks>Sin polígono, <c>geom</c> y <c>centro</c> no existen (no <c>null</c>): así <c>2dsphere</c> no las indexa y nunca salen por punto.</remarks>
    private static BsonDocument ToColonia(BsonDocument source)
    {
        var nombre = RequiredString(source, CatalogStore.NombreField, Colonia.MaxNombreLength, "colonia");
        var cp = source.GetValue(CatalogStore.CpField, BsonNull.Value);
        if (!cp.IsString || !Colonia.IsPostalCode(cp.AsString))
        {
            throw new InvalidDataException($"Seed colonia '{nombre}' must have a five-digit cp.");
        }

        var piloto = RequiredBoolean(source, CatalogStore.PilotoField, "colonia");
        var document = new BsonDocument
        {
            { CatalogStore.IdField, RequiredObjectId(source, "colonia") },
            { CatalogStore.NombreField, nombre },
            { CatalogStore.NombreBusquedaField, SearchNameNormalizer.Normalize(nombre) },
            { CatalogStore.CpField, cp.AsString },
            { CatalogStore.MunicipioField, RequiredString(source, CatalogStore.MunicipioField, Colonia.MaxMunicipioLength, "colonia") },
            { CatalogStore.PilotoField, piloto },
            { CatalogStore.AproximadoField, RequiredBoolean(source, CatalogStore.AproximadoField, "colonia") },
        };

        var geom = source.GetValue(CatalogStore.GeomField, null);
        var centro = source.GetValue(CatalogStore.CentroField, null);
        if (!piloto)
        {
            return geom is null && centro is null
                ? document
                : throw new InvalidDataException($"Seed colonia '{nombre}' without polygon must not have geom or centro fields.");
        }

        if (!IsGeoJson(geom, "MultiPolygon") || !IsGeoJson(centro, "Point"))
        {
            throw new InvalidDataException($"Seed colonia '{nombre}' with polygon must have a MultiPolygon geom and a Point centro.");
        }

        document[CatalogStore.GeomField] = geom;
        document[CatalogStore.CentroField] = centro;
        return document;
    }

    private static bool IsGeoJson(BsonValue? value, string type) =>
        value is BsonDocument geometry
        && geometry.GetValue("type", BsonNull.Value) == type
        && geometry.GetValue("coordinates", BsonNull.Value).IsBsonArray;

    private static ObjectId RequiredObjectId(BsonDocument source, string kind) =>
        source.GetValue(CatalogStore.IdField, BsonNull.Value) is { IsObjectId: true } id
            ? id.AsObjectId
            : throw new InvalidDataException($"Seed {kind} must have an ObjectId _id.");

    private static string RequiredString(BsonDocument source, string field, int maxLength, string kind) =>
        source.GetValue(field, BsonNull.Value) is { IsString: true } value
        && !string.IsNullOrWhiteSpace(value.AsString)
        && value.AsString.Length <= maxLength
            ? value.AsString
            : throw new InvalidDataException($"Seed {kind} must have a non-empty {field} of at most {maxLength} characters.");

    private static bool RequiredBoolean(BsonDocument source, string field, string kind) =>
        source.GetValue(field, BsonNull.Value) is { IsBoolean: true } value
            ? value.AsBoolean
            : throw new InvalidDataException($"Seed {kind} must have a boolean {field}.");
}
