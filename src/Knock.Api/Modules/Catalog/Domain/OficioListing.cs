namespace Knock.Api.Modules.Catalog.Domain;

/// <summary>Oficio activo tal como lo devuelve <c>CatalogStore.FindActiveOficiosAsync</c>: solo clave, nombre y modalidad.</summary>
internal sealed record OficioListing(string Clave, string Nombre, string Modalidad);
