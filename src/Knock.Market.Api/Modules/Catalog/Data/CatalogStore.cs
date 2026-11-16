using Knock.Market.Api.Hosting;
using Knock.Market.Api.Modules.Catalog.Domain;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GeoJsonObjectModel;

namespace Knock.Market.Api.Modules.Catalog.Data;

/// <summary>
/// Colecciones <c>oficios</c> y <c>colonias</c> de la base del módulo (<c>Mongo:Databases:Catalog</c>, hoy <c>market</c>).
/// Es el único punto del módulo con <c>IMongoCollection</c> (constitución, principio 10). Los filtros son tipados del driver:
/// ninguna entrada se arma como JSON. Cada consulta lleva <c>maxTimeMS</c> (<c>Mongo:QueryMaxTimeSeconds</c>).
/// </summary>
/// <remarks>
/// Caché en Redis (spec 010, R-01, R-02 y R-04, vía <see cref="ICatalogCache"/>): oficios, colonias por CP y por <c>_id</c> + CP con
/// vida de 24 horas; colonia por punto sin caché, porque la clave llevaría coordenadas. Escribir (semillas) invalida las claves
/// afectadas antes de responder.
/// </remarks>
internal sealed class CatalogStore(
    [FromKeyedServices(CatalogModuleExtensions.ModuleKey)] IMongoDatabase database,
    IOptions<KnockMongoOptions> mongoOptions,
    ICatalogCache cache)
{
    public const string OficiosCollection = "oficios";
    public const string ColoniasCollection = "colonias";

    public const string IdField = "_id";
    public const string ClaveField = "clave";
    public const string NombreField = "nombre";
    public const string NombreBusquedaField = "nombreBusqueda";
    public const string ModalidadField = "modalidad";
    public const string ActivoField = "activo";
    public const string CpField = "cp";
    public const string MunicipioField = "municipio";
    public const string PilotoField = "piloto";
    public const string AproximadoField = "aproximado";
    public const string GeomField = "geom";
    public const string CentroField = "centro";
    public const string VersionField = "version";

    private static readonly FilterDefinitionBuilder<BsonDocument> _filter = Builders<BsonDocument>.Filter;

    private static readonly ProjectionDefinition<BsonDocument> _coloniaProjection = Builders<BsonDocument>.Projection
        .Include(NombreField)
        .Include(NombreBusquedaField)
        .Include(CpField)
        .Include(MunicipioField);

    private static readonly ProjectionDefinition<BsonDocument> _oficioProjection = Builders<BsonDocument>.Projection
        .Include(ClaveField)
        .Include(NombreField)
        .Include(ModalidadField)
        .Exclude(IdField);

    /// <summary>Desempate permanente (decisión (d)): menor <c>nombreBusqueda</c> y, si empata, menor <c>_id</c>.</summary>
    private static readonly SortDefinition<BsonDocument> _tieBreak = Builders<BsonDocument>.Sort
        .Ascending(NombreBusquedaField)
        .Ascending(IdField);

    private readonly FindOptions _findOptions = new() { MaxTime = mongoOptions.Value.QueryMaxTime };

    public IMongoCollection<BsonDocument> Oficios { get; } = database.GetCollection<BsonDocument>(OficiosCollection);

    public IMongoCollection<BsonDocument> Colonias { get; } = database.GetCollection<BsonDocument>(ColoniasCollection);

    /// <summary>Índices de spec §5. Crearlos otra vez con la misma definición no cambia nada.</summary>
    public async Task EnsureIndexesAsync(CancellationToken cancellationToken)
    {
        var keys = Builders<BsonDocument>.IndexKeys;
        await Colonias.Indexes.CreateManyAsync(
            [
                new CreateIndexModel<BsonDocument>(keys.Geo2DSphere(GeomField)),
                new CreateIndexModel<BsonDocument>(keys.Geo2DSphere(CentroField)),
                new CreateIndexModel<BsonDocument>(keys.Ascending(CpField)),
                new CreateIndexModel<BsonDocument>(keys.Ascending(NombreBusquedaField)),
                new CreateIndexModel<BsonDocument>(
                    keys.Ascending(MunicipioField).Ascending(CpField).Ascending(NombreField),
                    new CreateIndexOptions { Unique = true }),
            ],
            cancellationToken);
        await Oficios.Indexes.CreateManyAsync(
            [
                new CreateIndexModel<BsonDocument>(keys.Ascending(ClaveField), new CreateIndexOptions { Unique = true }),
                new CreateIndexModel<BsonDocument>(keys.Ascending(NombreBusquedaField)),
            ],
            cancellationToken);
    }

    /// <summary>Oficios activos ordenados por <c>nombreBusqueda</c> y, a igual valor, por <c>clave</c> (spec TT-PRU-01 §8 D4).</summary>
    public async Task<IReadOnlyList<OficioListing>> FindActiveOficiosAsync(CancellationToken cancellationToken)
    {
        if (await cache.GetOficiosAsync(cancellationToken) is { } cached)
        {
            return cached;
        }

        // La generación se captura antes de leer MongoDB: si la siembra invalida entre medias, el guardado se descarta y no resucita lo viejo.
        var generation = await cache.GetOficiosGenerationAsync(cancellationToken);
        var documents = await ActiveOficiosQuery().ToListAsync(cancellationToken);
        var oficios = documents.Select(document => new OficioListing(
            document[ClaveField].AsString,
            document[NombreField].AsString,
            document[ModalidadField].AsString)).ToList();
        await cache.SetOficiosIfGenerationAsync(oficios, generation, cancellationToken);
        return oficios;
    }

    /// <summary>Colonias cuyo polígono contiene o toca el punto (<c>$geoIntersects</c>), con el desempate; la primera o nada.</summary>
    public async Task<Colonia?> FindIntersectingAsync(GeoPosition position, CancellationToken cancellationToken) =>
        ToColonia(await IntersectingQuery(position).FirstOrDefaultAsync(cancellationToken));

    /// <summary>Colonia de centro más cercano a <paramref name="maxMeters"/> o menos (<c>$near</c>); las colonias sin centro no salen.</summary>
    public async Task<Colonia?> FindNearestCenterAsync(GeoPosition position, double maxMeters, CancellationToken cancellationToken) =>
        ToColonia(await NearestCenterQuery(position, maxMeters).FirstOrDefaultAsync(cancellationToken));

    public async Task<IReadOnlyList<string>> FindNamesByPostalCodeAsync(string postalCode, CancellationToken cancellationToken) =>
        (await FindByPostalCodeAsync(postalCode, cancellationToken)).Select(colonia => colonia.Nombre).ToList();

    public async Task<IReadOnlyList<Colonia>> FindByPostalCodeAsync(string postalCode, CancellationToken cancellationToken)
    {
        if (await cache.GetNeighborhoodNamesAsync(postalCode, cancellationToken) is { } cached)
        {
            return cached;
        }

        var generation = await cache.GetNeighborhoodNamesGenerationAsync(postalCode, cancellationToken);
        var documents = await NamesByPostalCodeQuery(postalCode).ToListAsync(cancellationToken);
        var colonias = documents.Select(document => ToColonia(document)!).ToList();
        await cache.SetNeighborhoodNamesIfGenerationAsync(postalCode, colonias, generation, cancellationToken);
        return colonias;
    }

    /// <summary>Indica si hay alguna colonia con ese <c>nombreBusqueda</c> (nombre ya normalizado con <c>SearchNameNormalizer</c>).</summary>
    public async Task<bool> ColoniaNameExistsAsync(string normalizedName, CancellationToken cancellationToken) =>
        await ColoniaNameExistsQuery(normalizedName).FirstOrDefaultAsync(cancellationToken) is not null;

    public async Task<bool> ColoniaExistsAsync(ObjectId id, string postalCode, CancellationToken cancellationToken)
    {
        if (await cache.GetColoniaExistsAsync(id, postalCode, cancellationToken) is { } cached)
        {
            return cached;
        }

        var generation = await cache.GetColoniaExistsGenerationAsync(id, postalCode, cancellationToken);
        var exists = await ColoniaExistsQuery(id, postalCode).FirstOrDefaultAsync(cancellationToken) is not null;
        await cache.SetColoniaExistsIfGenerationAsync(id, postalCode, exists, generation, cancellationToken);
        return exists;
    }

    public async Task<(double Latitude, double Longitude)?> FindCenterAsync(ObjectId id, CancellationToken cancellationToken)
    {
        var document = await Colonias.Find(_filter.Eq(IdField, id), _findOptions)
            .Project(Builders<BsonDocument>.Projection.Include(CentroField)).FirstOrDefaultAsync(cancellationToken);
        if (document is null || !document.TryGetValue(CentroField, out var center) || !center.IsBsonDocument ||
            !center.AsBsonDocument.TryGetValue("coordinates", out var coordinates) || !coordinates.IsBsonArray || coordinates.AsBsonArray.Count != 2)
            return null;
        var values = coordinates.AsBsonArray;
        return (values[1].ToDouble(), values[0].ToDouble());
    }

    /// <summary>Nombre y modalidad del oficio activo con esa clave (spec 020, R-02).</summary>
    public async Task<OficioListing?> FindActiveOficioAsync(string clave, CancellationToken cancellationToken)
    {
        var document = await ActiveOficioQuery(clave).FirstOrDefaultAsync(cancellationToken);
        return document is null
            ? null
            : new OficioListing(document[ClaveField].AsString, document[NombreField].AsString, document[ModalidadField].AsString);
    }

    /// <summary>La colonia con ese <c>_id</c> (spec 020, R-02).</summary>
    public async Task<Colonia?> FindColoniaAsync(ObjectId id, CancellationToken cancellationToken) =>
        ToColonia(await ColoniaByIdQuery(id).FirstOrDefaultAsync(cancellationToken));

    // Las consultas se arman aparte para que las pruebas lean sus opciones (maxTimeMS) sin servidor.
    internal IFindFluent<BsonDocument, BsonDocument> ActiveOficiosQuery() =>
        Oficios
            .Find(_filter.Eq(ActivoField, true), _findOptions)
            .Project(_oficioProjection)
            .Sort(Builders<BsonDocument>.Sort.Ascending(NombreBusquedaField).Ascending(ClaveField));

    internal IFindFluent<BsonDocument, BsonDocument> ActiveOficioQuery(string clave) =>
        Oficios
            .Find(_filter.Eq(ClaveField, clave) & _filter.Eq(ActivoField, true), _findOptions)
            .Project(_oficioProjection)
            .Limit(1);

    internal IFindFluent<BsonDocument, BsonDocument> ColoniaByIdQuery(ObjectId id) =>
        Colonias
            .Find(_filter.Eq(IdField, id), _findOptions)
            .Project(_coloniaProjection)
            .Limit(1);

    internal IFindFluent<BsonDocument, BsonDocument> IntersectingQuery(GeoPosition position) =>
        Colonias
            .Find(_filter.GeoIntersects(GeomField, ToGeoJson(position)), _findOptions)
            .Project(_coloniaProjection)
            .Sort(_tieBreak)
            .Limit(1);

    internal IFindFluent<BsonDocument, BsonDocument> NearestCenterQuery(GeoPosition position, double maxMeters) =>
        Colonias
            .Find(_filter.Near(CentroField, ToGeoJson(position), maxDistance: maxMeters), _findOptions)
            .Project(_coloniaProjection)
            .Limit(1);

    internal IFindFluent<BsonDocument, BsonDocument> NamesByPostalCodeQuery(string postalCode) =>
        Colonias
            .Find(_filter.Eq(CpField, postalCode), _findOptions)
            .Project(Builders<BsonDocument>.Projection.Include(NombreField).Include(CpField).Include(MunicipioField))
            .Sort(Builders<BsonDocument>.Sort.Ascending(NombreBusquedaField).Ascending(NombreField));

    internal IFindFluent<BsonDocument, BsonDocument> ColoniaNameExistsQuery(string normalizedName) =>
        Colonias
            .Find(_filter.Eq(NombreBusquedaField, normalizedName), _findOptions)
            .Project(Builders<BsonDocument>.Projection.Include(IdField))
            .Limit(1);

    internal IFindFluent<BsonDocument, BsonDocument> ColoniaExistsQuery(ObjectId id, string postalCode) =>
        Colonias
            .Find(_filter.Eq(IdField, id) & _filter.Eq(CpField, postalCode), _findOptions)
            .Project(Builders<BsonDocument>.Projection.Include(IdField))
            .Limit(1);

    private static GeoJsonPoint<GeoJson2DGeographicCoordinates> ToGeoJson(GeoPosition position) =>
        GeoJson.Point(GeoJson.Geographic(position.Longitude, position.Latitude));

    private static Colonia? ToColonia(BsonDocument? document) =>
        document is null
            ? null
            : new Colonia(
                document[IdField].AsObjectId.ToString(),
                document[NombreField].AsString,
                document[CpField].AsString,
                document[MunicipioField].AsString);
}
