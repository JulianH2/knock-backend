using Knock.Market.Api.Common.Crypto;
using Knock.Market.Api.Modules.Catalog;
using Knock.Market.Api.Modules.Tecnicos.Domain;
using Knock.Market.Api.Modules.Tecnicos.Dtos;
using Microsoft.Extensions.Options;
using MongoDB.Bson;

namespace Knock.Market.Api.Modules.Tecnicos.Services;

/// <summary>Opciones del módulo (sección <c>Tecnicos</c>): hosts de los que se acepta <c>fotoUrl</c> (la subida del servidor, spec 032). Vacío: no se acepta ninguna.</summary>
internal sealed class TecnicosOptions
{
    public const string SectionName = "Tecnicos";

    public IReadOnlyList<string> HostsFoto { get; set; } = [];
}

/// <summary>Lo que hace falta del perfil propio para validar una edición: forma de atención, si ya tiene dirección de taller y su radio.</summary>
internal sealed record OwnTechnician(ObjectId Id, string? Atencion, bool HasWorkshop, int? RadioKm);

internal interface ITechnicianSelfStore
{
    Task<OwnTechnician?> FindOwnAsync(string sub, CancellationToken cancellationToken);

    Task<TechnicianResponse?> ReadOwnAsync(string sub, CancellationToken cancellationToken);

    Task<ObjectId?> SetAvailableUntilAsync(string sub, DateTime untilUtc, CancellationToken cancellationToken);

    /// <summary>Horario guardado, lista vacía si aún no guarda uno, o <see langword="null"/> si la cuenta no es técnico.</summary>
    Task<IReadOnlyList<DayScheduleDto>?> ReadScheduleAsync(string sub, CancellationToken cancellationToken);

    Task<ObjectId?> SetScheduleAsync(string sub, IReadOnlyList<DayScheduleDto> schedule, CancellationToken cancellationToken);

    /// <summary>Aplica los cambios: un valor <see langword="null"/> quita el campo.</summary>
    Task UpdateAsync(ObjectId id, IReadOnlyDictionary<string, BsonValue?> changes, CancellationToken cancellationToken);
}

internal enum UpdateTechnicianStatus
{
    Updated,
    NotATechnician,
    InvalidField,
    TradeInvalid,
}

internal sealed record UpdateTechnicianOutcome(UpdateTechnicianStatus Status, TechnicianResponse? Technician = null, string? Field = null);

/// <summary>
/// Rutas del propio técnico (spec 046, TEC-08 a TEC-11). La persona es siempre la del <c>sub</c>; una cuenta sin perfil de técnico no puede cambiar nada.
/// Cada escritura borra el perfil de la caché de esta réplica (R-45).
/// </summary>
internal sealed class MyTechnicianService(
    ITechnicianSelfStore store,
    ICatalogApi catalog,
    FieldCipher cipher,
    TechnicianProfileService profiles,
    IOptions<TecnicosOptions> options,
    TimeProvider clock)
{
    /// <summary>TEC-09 (R-46): encendido, hasta hoy a las 23:59:59 de la ciudad; apagado, desde este momento. <see langword="null"/> si no es técnico.</summary>
    public async Task<AvailabilityResponse?> SetAvailabilityAsync(string sub, bool available, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var until = available ? ScheduleRules.EndOfCityDayUtc(now) : now;
        var id = await store.SetAvailableUntilAsync(sub, until, cancellationToken);
        if (id is null)
        {
            return null;
        }

        profiles.Forget(id.Value);
        return new AvailabilityResponse(until > now, until);
    }

    /// <summary>TEC-10: los siete días; sin horario guardado, todos cerrados.</summary>
    public async Task<IReadOnlyList<DayScheduleDto>?> GetScheduleAsync(string sub, CancellationToken cancellationToken)
    {
        var schedule = await store.ReadScheduleAsync(sub, cancellationToken);
        if (schedule is null)
        {
            return null;
        }

        return schedule.Count == 0 ? ScheduleRules.AllClosed() : schedule;
    }

    /// <summary>TEC-11 (R-47): reemplaza la lista completa, ya validada.</summary>
    public async Task<IReadOnlyList<DayScheduleDto>?> SetScheduleAsync(string sub, IReadOnlyList<DayScheduleDto> schedule, CancellationToken cancellationToken)
    {
        var normalized = ScheduleRules.Normalize(schedule);
        var id = await store.SetScheduleAsync(sub, normalized, cancellationToken);
        if (id is null)
        {
            return null;
        }

        profiles.Forget(id.Value);
        return normalized;
    }

    /// <summary>TEC-08 (R-45): cambia solo lo enviado y revisa la combinación final de forma de atención, dirección y radio.</summary>
    public async Task<UpdateTechnicianOutcome> UpdateAsync(string sub, UpdateTechnicianBody body, CancellationToken cancellationToken)
    {
        var current = await store.FindOwnAsync(sub, cancellationToken);
        if (current is null)
        {
            return new UpdateTechnicianOutcome(UpdateTechnicianStatus.NotATechnician);
        }

        var changes = new Dictionary<string, BsonValue?>(StringComparer.Ordinal);
        var invalid = AddAttention(current, body, changes) ?? AddPhoto(body.FotoUrl, changes);
        if (invalid is not null)
        {
            return new UpdateTechnicianOutcome(UpdateTechnicianStatus.InvalidField, Field: invalid);
        }

        if (body.Oficios is { } trades)
        {
            var found = await Task.WhenAll(trades.Select(trade => catalog.FindActiveTradeAsync(trade, cancellationToken)));
            if (Array.Exists(found, trade => trade is null))
            {
                return new UpdateTechnicianOutcome(UpdateTechnicianStatus.TradeInvalid, Field: "oficios");
            }

            changes["oficios"] = new BsonArray(trades);
        }

        AddText(changes, "nombre", body.Nombre);
        AddText(changes, "nombreNegocio", body.NombreNegocio);
        AddText(changes, "descripcion", body.Descripcion);
        AddText(changes, "otroOficio", body.OtroOficio);
        if (body.Anios is { } years) changes["anios"] = years;
        if (changes.Count > 0)
        {
            changes["actualizadoEn"] = clock.GetUtcNow().UtcDateTime;
            await store.UpdateAsync(current.Id, changes, cancellationToken);
            profiles.Forget(current.Id);
        }

        return new UpdateTechnicianOutcome(UpdateTechnicianStatus.Updated, await store.ReadOwnAsync(sub, cancellationToken));
    }

    /// <summary>Forma de atención, dirección del taller (cifrada) y radio. Devuelve el campo inválido o <see langword="null"/>.</summary>
    private string? AddAttention(OwnTechnician current, UpdateTechnicianBody body, Dictionary<string, BsonValue?> changes)
    {
        var attention = body.Atencion ?? current.Atencion;
        if (body.Atencion is not null) changes["atencion"] = body.Atencion;

        if (TechnicianRules.UsesWorkshop(attention))
        {
            if (body.DireccionTaller is { } address) changes["tallerDireccion"] = WorkshopAddressCipher.Encrypt(cipher, current.Id, address.Trim());
            else if (!current.HasWorkshop) return "direccionTaller";
        }
        else if (body.DireccionTaller is not null)
        {
            return "direccionTaller";
        }
        else if (current.HasWorkshop)
        {
            changes["tallerDireccion"] = null;
        }

        if (TechnicianRules.VisitsHomes(attention))
        {
            var radius = body.RadioKm ?? current.RadioKm;
            if (radius is null) return "radioKm";
            if (body.RadioKm is { } value) changes["radioAtencionKm"] = value;
        }
        else if (body.RadioKm is not null)
        {
            return "radioKm";
        }
        else if (current.RadioKm is not null)
        {
            changes["radioAtencionKm"] = null;
        }

        return null;
    }

    private string? AddPhoto(string? photoUrl, Dictionary<string, BsonValue?> changes)
    {
        if (photoUrl is null)
        {
            return null;
        }

        var host = new Uri(photoUrl).Host;
        if (!options.Value.HostsFoto.Contains(host, StringComparer.OrdinalIgnoreCase))
        {
            return "fotoUrl";
        }

        changes["fotoUrl"] = photoUrl;
        return null;
    }

    /// <summary>Texto enviado: vacío quita el campo; con contenido, se guarda sin espacios a los lados.</summary>
    private static void AddText(Dictionary<string, BsonValue?> changes, string field, string? value)
    {
        if (value is null) return;
        changes[field] = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
