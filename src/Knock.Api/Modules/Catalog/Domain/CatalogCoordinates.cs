namespace Knock.Api.Modules.Catalog.Domain;

/// <summary>Valida latitud y longitud antes de cualquier consulta y las ordena como GeoJSON (longitud primero).</summary>
internal static class CatalogCoordinates
{
    /// <remarks>Las excepciones no incluyen el valor recibido para no exponer la ubicación.</remarks>
    public static GeoPosition ToPosition(double latitude, double longitude)
    {
        if (!double.IsFinite(latitude) || latitude is < -90 or > 90)
        {
            throw new ArgumentOutOfRangeException(nameof(latitude), "Latitude must be between -90 and 90.");
        }

        if (!double.IsFinite(longitude) || longitude is < -180 or > 180)
        {
            throw new ArgumentOutOfRangeException(nameof(longitude), "Longitude must be between -180 and 180.");
        }

        return new GeoPosition(longitude, latitude);
    }
}
