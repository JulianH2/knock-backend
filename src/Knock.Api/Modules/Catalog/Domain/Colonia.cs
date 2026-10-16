using NetTopologySuite.Geometries;

namespace Knock.Api.Modules.Catalog.Domain;

/// <summary>Colonia SEPOMEX con polígono aproximado opcional (no oficial) y punto central.</summary>
internal sealed class Colonia
{
    private Colonia()
    {
    }

    public Guid Id { get; init; }

    public string Nombre { get; private set; } = string.Empty;

    public string Cp { get; private set; } = string.Empty;

    public string Municipio { get; private set; } = string.Empty;

    public bool Piloto { get; private set; }

    public MultiPolygon? Geom { get; private set; }

    public Point Centro { get; private set; } = Point.Empty;

    public static Colonia Create(string nombre, string cp, string municipio, MultiPolygon? geom, Point centro) =>
        new()
        {
            Nombre = nombre,
            Cp = cp,
            Municipio = municipio,
            Piloto = geom is not null,
            Geom = geom,
            Centro = centro,
        };
}
