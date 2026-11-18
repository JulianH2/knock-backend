using Knock.Market.Api.Modules.Tecnicos.Domain;
using Knock.Market.Api.Modules.Tecnicos.Dtos;
using Microsoft.Extensions.Caching.Memory;
using MongoDB.Bson;

namespace Knock.Market.Api.Modules.Tecnicos.Services;

/// <summary>Lo que se lee de <c>tecnicos</c> para el perfil público (spec 046, TEC-05). No incluye teléfono, dirección del taller ni ubicación.</summary>
internal sealed record TechnicianProfileRecord(
    ObjectId Id,
    string Nombre,
    string? NombreNegocio,
    string? FotoUrl,
    string Verificacion,
    double? Calificacion,
    int? Anios,
    string? Descripcion,
    IReadOnlyList<string> Oficios,
    string? Atencion,
    int? RadioKm,
    string? ColoniaId,
    string Estado,
    bool Oculto,
    DateTime? OcultoHasta,
    DateTime? DisponibleHasta);

internal interface ITechnicianProfileReader
{
    Task<TechnicianProfileRecord?> FindByIdAsync(ObjectId id, CancellationToken cancellationToken);
}

/// <summary>
/// Perfil público (spec 046, TEC-05, R-42 y R-17): un técnico que no existe, no está <c>activo</c>, está oculto o con <c>ocultoHasta</c> futuro responde
/// como no encontrado. El documento se guarda 1 minuto en la caché de la réplica con una clave que solo lleva el identificador del técnico; la visibilidad y
/// «Disponible hoy» se calculan en cada lectura, así que un perfil en caché nunca se muestra después de ocultarse.
/// </summary>
internal sealed class TechnicianProfileService(ITechnicianProfileReader reader, IMemoryCache cache, TimeProvider clock)
{
    public static readonly TimeSpan CacheLifetime = TimeSpan.FromMinutes(1);

    /// <summary>Largo máximo de la descripción corta del resumen.</summary>
    public const int ShortDescriptionLength = 140;

    public static string CacheKey(ObjectId id) => $"tecnicos:perfil:{id}";

    public async Task<TechnicianProfileResponse?> GetAsync(ObjectId id, bool withDetail, CancellationToken cancellationToken)
    {
        var key = CacheKey(id);
        if (!cache.TryGetValue(key, out TechnicianProfileRecord? record))
        {
            record = await reader.FindByIdAsync(id, cancellationToken);
            if (record is not null)
            {
                cache.Set(key, record, CacheLifetime);
            }
        }

        var now = clock.GetUtcNow().UtcDateTime;
        if (record is null || !IsVisible(record, now))
        {
            return null;
        }

        var detail = withDetail
            ? new TechnicianProfileDetail(record.Descripcion, record.Atencion, record.RadioKm, record.ColoniaId, record.DisponibleHasta > now)
            : null;
        return new TechnicianProfileResponse(record.Id.ToString(), record.Nombre, record.NombreNegocio, record.FotoUrl, record.Verificacion,
            record.Calificacion is { } rating ? Math.Round(rating, 1, MidpointRounding.AwayFromZero) : null, record.Anios, Shorten(record.Descripcion),
            record.Oficios, detail);
    }

    /// <summary>Quita el perfil de la caché de esta réplica; lo llaman las escrituras del perfil (TEC-08 y TEC-09).</summary>
    public void Forget(ObjectId id) => cache.Remove(CacheKey(id));

    internal static bool IsVisible(TechnicianProfileRecord record, DateTime nowUtc) =>
        record.Estado == TechnicianRules.ActiveState && !record.Oculto && (record.OcultoHasta is null || record.OcultoHasta <= nowUtc);

    internal static string? Shorten(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return null;
        }

        var text = description.Trim();
        return text.Length <= ShortDescriptionLength ? text : string.Concat(text.AsSpan(0, ShortDescriptionLength - 1).TrimEnd(), "…");
    }
}
