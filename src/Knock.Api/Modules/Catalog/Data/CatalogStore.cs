using Knock.Api.Hosting;
using Knock.Api.Modules.Catalog.Domain;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GeoJsonObjectModel;

namespace Knock.Api.Modules.Catalog.Data;

/// <summary>
/// Colecciones <c>oficios</c> y <c>colonias</c> de la base del módulo (<c>Mongo:Databases:Catalog</c>, hoy <c>market</c>).
/// Es el único punto del módulo con <c>IMongoCollection</c> (constitución, principio 10). Los filtros son tipados del driver:
/// ninguna entrada se arma como JSON. Cada consulta lleva <c>maxTimeMS</c> (<c>Mongo:QueryMaxTimeSeconds</c>).
/// </summary>
/// <remarks>
/// Caché declarada (spec §5; la capa cache-aside es de TT-BD-08): oficios 86400 s; colonias por CP y por <c>_id</c> + CP 86400 s;
/// colonia por punto sin caché, porque la clave llevaría coordenadas.
/// </remarks>
internal sealed class CatalogStore(
    [FromKeyedServices(CatalogModuleExtensions.ModuleKey)] IMongoDatabase database,
    IOptions<KnockMongoOptions> mongoOptions)
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
        var documents = await ActiveOficiosQuery().ToListAsync(cancellationToken);
        return [.. documents.Select(document => new OficioListing(
            document[ClaveField].AsString,
            document[NombreField].AsString,
            document[ModalidadField].AsString))];
    }

    /// <summary>Colonias cuyo polígono contiene o toca el punto (<c>$geoIntersects</c>), con el desempate; la primera o nada.</summary>
    public async Task<Colonia?> FindIntersectingAsync(GeoPosition position, CancellationToken cancellationToken) =>
        ToColonia(await IntersectingQuery(position).FirstOrDefaultAsync(cancellationToken));

    /// <summary>Colonia de centro más cercano a <paramref name="maxMeters"/> o menos (<c>$near</c>); las colonias sin centro no salen.</summary>
    public async Task<Colonia?> FindNearestCenterAsync(GeoPosition position, double maxMeters, CancellationToken cancellationToken) =>
        ToColonia(await NearestCenterQuery(position, maxMeters).FirstOrDefaultAsync(cancellationToken));

    public async Task<IReadOnlyList<string>> FindNamesByPostalCodeAsync(string postalCode, CancellationToken cancellationToken)
    {
        var documents = await NamesByPostalCodeQuery(postalCode).ToListAsync(cancellationToken);
        return documents.Select(document => document[NombreField].AsString).ToList();
    }

    public async Task<bool> ColoniaExistsAsync(ObjectId id, string postalCode, CancellationToken cancellationToken) =>
        await ColoniaExistsQuery(id, postalCode).FirstOrDefaultAsync(cancellationToken) is not null;

    // Las consultas se arman aparte para que las pruebas lean sus opciones (maxTimeMS) sin servidor.
    internal IFindFluent<BsonDocument, BsonDocument> ActiveOficiosQuery() =>
        Oficios
            .Find(_filter.Eq(ActivoField, true), _findOptions)
            .Project(_oficioProjection)
            .Sort(Builders<BsonDocument>.Sort.Ascending(NombreBusquedaField).Ascending(ClaveField));

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
            .Project(Builders<BsonDocument>.Projection.Include(NombreField).Exclude(IdField))
            .Sort(Builders<BsonDocument>.Sort.Ascending(NombreBusquedaField).Ascending(NombreField));

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
