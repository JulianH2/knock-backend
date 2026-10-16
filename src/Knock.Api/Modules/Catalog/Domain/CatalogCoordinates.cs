using NetTopologySuite.Geometries;

namespace Knock.Api.Modules.Catalog.Domain;

/// <summary>Convierte latitud/longitud en un punto SRID 4326 con X = longitud e Y = latitud.</summary>
internal static class CatalogCoordinates
{
    public const int Srid = 4326;

    /// <remarks>Las excepciones no incluyen el valor recibido para no exponer la ubicación.</remarks>
    public static Point ToPoint(double latitude, double longitude)
    {
        if (!double.IsFinite(latitude) || latitude is < -90 or > 90)
        {
            throw new ArgumentOutOfRangeException(nameof(latitude), "Latitude must be between -90 and 90.");
        }

        if (!double.IsFinite(longitude) || longitude is < -180 or > 180)
        {
            throw new ArgumentOutOfRangeException(nameof(longitude), "Longitude must be between -180 and 180.");
        }

        return new Point(longitude, latitude) { SRID = Srid };
    }
}
