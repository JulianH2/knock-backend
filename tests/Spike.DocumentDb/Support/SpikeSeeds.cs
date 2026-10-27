using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace Knock.Spike.DocumentDb.Support;

/// <summary>
/// Scripts de índices y semillas del spike (spec §5; la ruta definitiva db/mongo/ es de TT-BD-09).
/// Lee las semillas versionadas del repositorio sin modificarlas.
/// </summary>
internal static class SpikeSeeds
{
    public const string Colonias = "colonias";
    public const string Oficios = "oficios";
    public const string Solicitudes = "solicitudes";
    public const string Outbox = "outbox";
    public const string TokensRevocados = "tokens_revocados";

    /// <summary>
    /// SINTÉTICO, solo spike: centros inventados para las 4 colonias del catálogo sin polígono (spec §8, (d)).
    /// Están dentro del área de Tula pero fuera de los 8 polígonos; no representan la ubicación real de la colonia.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, (double Lng, double Lat)> SyntheticCenters =
        new Dictionary<string, (double Lng, double Lat)>
        {
            ["Mastrantos"] = (-99.340, 20.038),
            ["Bugambilias"] = (-99.325, 20.038),
            ["Del Llano"] = (-99.312, 20.066),
            ["Tierra Blanca"] = (-99.345, 20.070),
        };

    /// <summary>Puntos sintéticos interiores, a ≥ 60 m de cualquier borde y fuera de las zonas donde el seed se traslapa.</summary>
    public static readonly IReadOnlyList<(string Colonia, double Lng, double Lat)> InteriorPoints =
    [
        ("Centro", -99.3495, 20.0505),
        ("El Cielito", -99.3321, 20.0541),
        ("El Llano 1a Sección", -99.3243, 20.0616),
        ("El Llano 2a Sección", -99.3060, 20.0460),
        ("San Lorenzo", -99.3372, 20.0484),
        ("Tultengo", -99.32637, 20.06399),
        ("Iturbe", -99.3234, 20.0757),
        ("La Huerta", -99.3531, 20.0556),
    ];

    private static string FixturePath(string file) => Path.Combine(AppContext.BaseDirectory, "Fixtures", file);

    /// <summary>12 colonias: 8 con polígono del GeoJSON y 4 del CSV con centro sintético.</summary>
    public static IReadOnlyList<BsonDocument> LoadColonias()
    {
        var geojson = BsonDocument.Parse(File.ReadAllText(FixturePath("colonias_tula.geojson")));
        var docs = new List<BsonDocument>();
        foreach (var feature in geojson["features"].AsBsonArray.Select(f => f.AsBsonDocument))
        {
            var props = feature["properties"].AsBsonDocument;
            var geom = feature["geometry"].AsBsonDocument;
            docs.Add(new BsonDocument
            {
                { "nombre", props["nombre"] },
                { "cp", props["cp"] },
                { "municipio", props["municipio"] },
                { "piloto", true },
                { "geom", geom },
                { "centro", Point(VertexAverage(geom)) },
            });
        }

        var withPolygon = docs.Select(d => d["nombre"].AsString).ToHashSet(StringComparer.Ordinal);
        foreach (var row in File.ReadLines(FixturePath("tula_colonias.csv")).Skip(1).Select(l => l.Split(',')))
        {
            if (withPolygon.Contains(row[1]))
            {
                continue;
            }

            var center = SyntheticCenters[row[1]];
            docs.Add(new BsonDocument
            {
                { "nombre", row[1] },
                { "cp", row[0] },
                { "municipio", row[3] },
                { "piloto", false },
                { "geom", BsonNull.Value },
                { "centro", Point(center) },
                { "centroSintetico", "SINTÉTICO, solo spike" },
            });
        }

        return docs;
    }

    /// <summary>Oficios de db/seeds/catalog/oficios.json (clave, nombre); oficios.sql se retiró en TT-BD-06.</summary>
    public static IReadOnlyList<BsonDocument> LoadOficios() =>
        BsonSerializer.Deserialize<BsonArray>(File.ReadAllText(FixturePath("oficios.json")))
            .Select(o => new BsonDocument { { "clave", o["clave"] }, { "nombre", o["nombre"] } })
            .ToList();

    public static async Task CreateColoniasAsync(IMongoDatabase db)
    {
        var colonias = db.GetCollection<BsonDocument>(Colonias);
        await colonias.Indexes.CreateManyAsync(
        [
            new CreateIndexModel<BsonDocument>(Builders<BsonDocument>.IndexKeys.Geo2DSphere("geom")),
            new CreateIndexModel<BsonDocument>(Builders<BsonDocument>.IndexKeys.Geo2DSphere("centro")),
        ]);
        await colonias.InsertManyAsync(LoadColonias());
    }

    public static BsonDocument Point((double Lng, double Lat) p) => new()
    {
        { "type", "Point" },
        { "coordinates", new BsonArray { p.Lng, p.Lat } },
    };

    /// <summary>Promedio de los vértices del anillo exterior (sin repetir el de cierre); cae dentro de los 8 polígonos.</summary>
    private static (double Lng, double Lat) VertexAverage(BsonDocument multiPolygon)
    {
        var ring = multiPolygon["coordinates"][0][0].AsBsonArray;
        var vertices = ring.Take(ring.Count - 1).Select(v => v.AsBsonArray).ToList();
        return (
            Math.Round(vertices.Average(v => v[0].ToDouble()), 6),
            Math.Round(vertices.Average(v => v[1].ToDouble()), 6));
    }
}
