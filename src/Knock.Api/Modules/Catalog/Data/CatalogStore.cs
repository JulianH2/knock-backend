using Knock.Api.Modules.Catalog.Domain;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GeoJsonObjectModel;

namespace Knock.Api.Modules.Catalog.Data;

/// <summary>
/// Colecciones <c>oficios</c> y <c>colonias</c> de la base del módulo (<c>Mongo:Databases:Catalog</c>, hoy <c>market</c>).
/// Es el único punto del módulo con <c>IMongoCollection</c> (constitución, principio 10). Los filtros son tipados del driver:
/// ninguna entrada se arma como JSON.
/// </summary>
/// <remarks>
/// Caché declarada (spec §5; la capa cache-aside es de TT-BD-08): oficios 86400 s; colonias por CP y por <c>_id</c> + CP 86400 s;
/// colonia por punto sin caché, porque la clave llevaría coordenadas.
/// </remarks>
internal sealed class CatalogStore([FromKeyedServices(CatalogModuleExtensions.ModuleKey)] IMongoDatabase database)
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

    /// <summary>Desempate permanente (decisión (d)): menor <c>nombreBusqueda</c> y, si empata, menor <c>_id</c>.</summary>
    private static readonly SortDefinition<BsonDocument> _tieBreak = Builders<BsonDocument>.Sort
        .Ascending(NombreBusquedaField)
        .Ascending(IdField);

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

    /// <summary>Colonias cuyo polígono contiene o toca el punto (<c>$geoIntersects</c>), con el desempate; la primera o nada.</summary>
    public async Task<Colonia?> FindIntersectingAsync(GeoPosition position, CancellationToken cancellationToken)
    {
        var document = await Colonias
            .Find(_filter.GeoIntersects(GeomField, ToGeoJson(position)))
            .Project(_coloniaProjection)
            .Sort(_tieBreak)
            .Limit(1)
            .FirstOrDefaultAsync(cancellationToken);
        return ToColonia(document);
    }

    /// <summary>Colonia de centro más cercano a <paramref name="maxMeters"/> o menos (<c>$near</c>); las colonias sin centro no salen.</summary>
    public async Task<Colonia?> FindNearestCenterAsync(GeoPosition position, double maxMeters, CancellationToken cancellationToken)
    {
        var document = await Colonias
            .Find(_filter.Near(CentroField, ToGeoJson(position), maxDistance: maxMeters))
            .Project(_coloniaProjection)
            .Limit(1)
            .FirstOrDefaultAsync(cancellationToken);
        return ToColonia(document);
    }

    public async Task<IReadOnlyList<string>> FindNamesByPostalCodeAsync(string postalCode, CancellationToken cancellationToken)
    {
        var documents = await Colonias
            .Find(_filter.Eq(CpField, postalCode))
            .Project(Builders<BsonDocument>.Projection.Include(NombreField).Exclude(IdField))
            .Sort(Builders<BsonDocument>.Sort.Ascending(NombreBusquedaField).Ascending(NombreField))
            .ToListAsync(cancellationToken);
        return documents.Select(document => document[NombreField].AsString).ToList();
    }

    public async Task<bool> ColoniaExistsAsync(ObjectId id, string postalCode, CancellationToken cancellationToken)
    {
        var document = await Colonias
            .Find(_filter.Eq(IdField, id) & _filter.Eq(CpField, postalCode))
            .Project(Builders<BsonDocument>.Projection.Include(IdField))
            .Limit(1)
            .FirstOrDefaultAsync(cancellationToken);
        return document is not null;
    }

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
