namespace Knock.Api.Modules.Catalog.Domain;

/// <summary>Punto WGS 84 ya validado. En GeoJSON va como <c>[Longitude, Latitude]</c>.</summary>
internal readonly record struct GeoPosition(double Longitude, double Latitude);
