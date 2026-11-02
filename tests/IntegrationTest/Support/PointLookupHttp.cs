using MongoDB.Bson;
using MongoDB.Driver;

namespace Knock.IntegrationTest.Support;

/// <summary>Llamadas de las pruebas a <c>GET /api/v1/colonias/en-punto</c> y puntos SINTÉTICOS del seed de TT-BD-06 (texto <c>lat</c>, <c>lon</c>).</summary>
internal static class PointLookupHttp
{
    public const string Route = "/api/v1/colonias/en-punto";

    /// <summary>Un punto interior por colonia piloto (los del spike P2/P3, a ≥ 78 m de cualquier borde).</summary>
    public static readonly IReadOnlyList<(string Colonia, string Lat, string Lon)> PilotInteriorPoints =
    [
        ("Centro", "20.0505", "-99.3495"),
        ("El Cielito", "20.0541", "-99.3321"),
        ("El Llano 1a Sección", "20.0616", "-99.3243"),
        ("El Llano 2a Sección", "20.0460", "-99.3060"),
        ("San Lorenzo", "20.0484", "-99.3372"),
        ("Tultengo", "20.06399", "-99.32637"),
        ("Iturbe", "20.0757", "-99.3234"),
        ("La Huerta", "20.0556", "-99.3531"),
    ];

    /// <summary>Interior de cada traslape y vértice del polígono que pierde sobre el borde del que gana (TT-BD-06 §5).</summary>
    public static readonly IReadOnlyList<(string Lat, string Lon)> BoundaryAndOverlapPoints =
    [
        ("20.05399", "-99.34942"), ("20.055532", "-99.345888"),
        ("20.05079", "-99.33802"), ("20.052984", "-99.337329"),
        ("20.04939", "-99.33302"), ("20.052948", "-99.33238"),
        ("20.06559", "-99.32272"), ("20.062463", "-99.322428"),
    ];

    /// <summary>Fuera de los polígonos, con un centro a 2 km o menos (respaldo por centro).</summary>
    public static readonly (string Lat, string Lon) NearCenter = ("20.0640", "-99.3300");

    /// <summary>Fuera de cobertura: Pachuca sintético y un punto a más de 2 km de cualquier centro.</summary>
    public static readonly IReadOnlyList<(string Lat, string Lon)> OutOfCoverage = [("20.1000", "-98.7500"), ("20.1100", "-99.3000")];

    public static string Path(string query) => $"{Route}?{query}";

    public static string Query(string lat, string lon) => $"lat={Uri.EscapeDataString(lat)}&lon={Uri.EscapeDataString(lon)}";

    /// <summary>GET con un JWT sintético recién emitido por el host de pruebas.</summary>
    public static Task<HttpResponseMessage> GetAsync(KnockApiFactory factory, HttpClient client, string query, string? remoteIp = null) =>
        TestHttp.GetAsync(client, Path(query), remoteIp, bearerToken: TestHttp.IssueToken(factory, Guid.NewGuid()));

    /// <summary>Host con MongoDB real y la semilla de TT-BD-06 en una base propia de la prueba.</summary>
    public static async Task<(KnockApiFactory Factory, string Database)> CreateSeededFactoryAsync(
        string connectionString, string environment = "Development", IReadOnlyDictionary<string, string?>? settings = null)
    {
        var database = "market_point_" + Guid.NewGuid().ToString("N")[..8];
        var values = new Dictionary<string, string?>(settings ?? new Dictionary<string, string?>())
        {
            ["ConnectionStrings:KnockMongo"] = connectionString,
            ["Mongo:Databases:Catalog"] = database,
        };
        var factory = new KnockApiFactory(environment, settings: values);
        await CatalogTestDatabase.SeedAsync(factory.Services);
        return (factory, database);
    }

    /// <summary>Documento <c>{ id, nombre, cp, municipio }</c> de una colonia del seed, leído de <c>colonias</c>.</summary>
    public static async Task<(string Id, string Nombre, string Cp, string Municipio)> ReadColoniaAsync(IMongoDatabase database, string nombre)
    {
        var document = await database.GetCollection<BsonDocument>("colonias")
            .Find(Builders<BsonDocument>.Filter.Eq("nombre", nombre))
            .SingleAsync();
        return (document["_id"].AsObjectId.ToString(), document["nombre"].AsString, document["cp"].AsString, document["municipio"].AsString);
    }
}
