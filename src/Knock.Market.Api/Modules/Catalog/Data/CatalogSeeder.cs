using Knock.Market.Api.Modules.Catalog.Domain;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace Knock.Market.Api.Modules.Catalog.Data;

/// <summary>
/// Aplica las semillas versionadas <c>db/seeds/catalog/oficios.json</c> y <c>colonias.json</c> (<c>_id</c> fijos, iguales en todos
/// los entornos). Es repetible: hace upsert por la clave natural (<c>clave</c>; <c>municipio</c>, <c>cp</c>, <c>nombre</c>) y solo
/// sube <c>version</c> si cambia el contenido. Valida todos los documentos antes de escribir el primero.
/// </summary>
internal sealed class CatalogSeeder(CatalogStore store, ICatalogCache cache)
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
        var before = await SnapshotColoniasAsync(cancellationToken);
        var touchedPostalCodes = new HashSet<string>(StringComparer.Ordinal);
        var touchedPairs = new HashSet<(ObjectId Id, string PostalCode)>();
        try
        {
            // Spec 010, R-02 + KNOCK-205: cada escritura invalida sus claves enseguida. Si la siembra se interrumpe a la mitad, el
            // bloque protegido invalida también lo tocado (más el CP anterior de cada colonia), así que una siembra parcial no deja caché vieja.
            foreach (var oficio in oficioDocuments)
            {
                await UpsertAsync(
                    store.Oficios,
                    Builders<BsonDocument>.Filter.Eq(CatalogStore.ClaveField, oficio[CatalogStore.ClaveField]),
                    oficio,
                    cancellationToken);
                await cache.RemoveOficiosAsync(cancellationToken);
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
                var id = colonia[CatalogStore.IdField].AsObjectId;
                var postalCode = colonia[CatalogStore.CpField].AsString;
                await cache.RemoveNeighborhoodNamesAsync(postalCode, cancellationToken);
                await cache.RemoveColoniaExistsAsync(id, postalCode, cancellationToken);
                touchedPostalCodes.Add(postalCode);
                touchedPairs.Add((id, postalCode));
            }

            // Cubre lo que ya no está en los documentos escritos: el CP anterior de una colonia cuyo CP cambió, y cualquier CP/pair
            // que hubiera antes y ya no exista (documento retirado del seed o borrado).
            foreach (var postalCode in before.PostalCodes)
            {
                await cache.RemoveNeighborhoodNamesAsync(postalCode, cancellationToken);
                touchedPostalCodes.Add(postalCode);
            }

            foreach (var (id, postalCode) in before.Pairs)
            {
                await cache.RemoveColoniaExistsAsync(id, postalCode, cancellationToken);
                touchedPairs.Add((id, postalCode));
            }

            // Y lo que ahora vive en la base aunque el seed no lo tocó (p. ej. un documento agregado por otra vía): su clave tampoco puede quedar activa con datos viejos.
            var after = await SnapshotColoniasAsync(cancellationToken);
            foreach (var postalCode in after.PostalCodes)
            {
                await cache.RemoveNeighborhoodNamesAsync(postalCode, cancellationToken);
            }

            foreach (var (id, postalCode) in after.Pairs)
            {
                await cache.RemoveColoniaExistsAsync(id, postalCode, cancellationToken);
            }
        }
        catch (Exception original)
        {
            // Invalidación de emergencia con CancellationToken.None: si la siembra falló por cancelación (apagado o timeout), el token ya
            // cancelado impediría invalidar justo lo que este bloque debe cubrir. Una invalidación que falle no detiene a las demás.
            var failures = new List<Exception>();
            foreach (var postalCode in touchedPostalCodes.Concat(before.PostalCodes))
            {
                await TryInvalidateAsync(cache.RemoveNeighborhoodNamesAsync(postalCode, CancellationToken.None), failures);
            }

            foreach (var (id, postalCode) in touchedPairs.Concat(before.Pairs))
            {
                await TryInvalidateAsync(cache.RemoveColoniaExistsAsync(id, postalCode, CancellationToken.None), failures);
            }

            await TryInvalidateAsync(cache.RemoveOficiosAsync(CancellationToken.None), failures);
            if (failures.Count > 0)
            {
                throw new AggregateException("La siembra del catálogo falló y no se pudo invalidar toda la caché.", failures.Prepend(original));
            }

            throw;
        }
    }

    // Espera la invalidación sin lanzar: Task.WhenAny no propaga la excepción, así que se registra y se sigue con las demás claves.
    private static async Task TryInvalidateAsync(Task invalidation, List<Exception> failures)
    {
        await Task.WhenAny(invalidation);
        if (invalidation.Exception is { } error)
        {
            failures.Add(error.InnerExceptions.Count == 1 ? error.InnerExceptions[0] : error);
        }
    }

    private sealed record ColoniasSnapshot(IReadOnlyList<string> PostalCodes, IReadOnlyList<(ObjectId Id, string PostalCode)> Pairs);

    private async Task<ColoniasSnapshot> SnapshotColoniasAsync(CancellationToken cancellationToken)
    {
        var projection = Builders<BsonDocument>.Projection.Include(CatalogStore.CpField);
        var documents = await store.Colonias.Find(Builders<BsonDocument>.Filter.Empty)
            .Project(projection)
            .ToListAsync(cancellationToken);
        return new ColoniasSnapshot(
            documents.Select(document => document[CatalogStore.CpField].AsString).Distinct(StringComparer.Ordinal).ToList(),
            documents.Select(document => (document[CatalogStore.IdField].AsObjectId, document[CatalogStore.CpField].AsString)).ToList());
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
