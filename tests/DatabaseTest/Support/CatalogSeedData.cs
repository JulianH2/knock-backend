using Knock.IntegrationTest.Support;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace Knock.DatabaseTest.Support;

/// <summary>Lectura de los JSON versionados y puntos sintéticos de las pruebas (nunca ubicaciones de personas).</summary>
internal static class CatalogSeedData
{
    /// <summary>Colonias sin polígono del extracto SEPOMEX: visibles por CP, nunca por punto.</summary>
    public static readonly IReadOnlyList<string> WithoutPolygon = ["Mastrantos", "Bugambilias", "Del Llano", "Tierra Blanca"];

    /// <summary>Puntos sintéticos interiores (los del spike P2/P3): a ≥ 78 m de cualquier borde y dentro de un solo polígono.</summary>
    public static readonly IReadOnlyList<(string Colonia, double Longitude, double Latitude)> InteriorPoints = Knock.DatabaseTest.Support.ProbePointsLoader.LoadLongLat();

    /// <summary>Fuera de los 8 polígonos y a ≤ 1.2 km de cada vértice de Tultengo: hay un centro a 2 km o menos.</summary>
    public static readonly (double Longitude, double Latitude) NearCenterOutsidePolygons = (-99.3300, 20.0640);

    /// <summary>Al norte de Tula, a ≥ 3.7 km de cualquier polígono y de cualquier centro.</summary>
    public static readonly (double Longitude, double Latitude) BeyondTwoKilometers = (-99.3000, 20.1100);

    /// <summary>Otra ciudad (zona de Pachuca), a decenas de km.</summary>
    public static readonly (double Longitude, double Latitude) OtherCity = (-98.7500, 20.1000);

    /// <summary>Ubicaciones aproximadas SINTÉTICAS (del spike) de las colonias sin polígono; fuera de los 8 polígonos.</summary>
    public static readonly IReadOnlyList<(string Colonia, double Longitude, double Latitude)> PolygonlessApproximateLocations =
    [
        ("Mastrantos", -99.340, 20.038),
        ("Bugambilias", -99.325, 20.038),
        ("Del Llano", -99.312, 20.066),
        ("Tierra Blanca", -99.345, 20.070),
    ];

    public static IReadOnlyList<BsonDocument> Colonias { get; } = Read("colonias.json");

    public static IReadOnlyList<BsonDocument> Oficios { get; } = Read("oficios.json");

    public static BsonDocument Colonia(string nombre) => Colonias.Single(colonia => colonia["nombre"] == nombre);

    /// <summary>Distancia de círculo máximo en metros (radio medio de la Tierra).</summary>
    public static double HaversineMeters(double longitude1, double latitude1, double longitude2, double latitude2)
    {
        const double earthRadius = 6_371_008.8;
        static double Radians(double degrees) => degrees * Math.PI / 180;
        var a = Math.Pow(Math.Sin(Radians(latitude2 - latitude1) / 2), 2)
            + (Math.Cos(Radians(latitude1)) * Math.Cos(Radians(latitude2)) * Math.Pow(Math.Sin(Radians(longitude2 - longitude1) / 2), 2));
        return 2 * earthRadius * Math.Asin(Math.Sqrt(a));
    }

    private static List<BsonDocument> Read(string file) =>
        BsonSerializer.Deserialize<BsonArray>(File.ReadAllText(Path.Combine(CatalogTestDatabase.SeedsDirectory, "catalog", file)))
            .Select(value => value.AsBsonDocument)
            .ToList();
}
