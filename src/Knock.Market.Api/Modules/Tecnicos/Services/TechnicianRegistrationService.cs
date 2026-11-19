using Knock.Contracts.Catalog;
using Knock.Market.Api.Common.Crypto;
using Knock.Market.Api.Modules.Catalog;
using Knock.Market.Api.Modules.Tecnicos.Domain;
using Knock.Market.Api.Modules.Tecnicos.Dtos;
using MongoDB.Bson;

namespace Knock.Market.Api.Modules.Tecnicos.Services;

internal enum RegisterTechnicianStatus
{
    Created,
    AlreadyTechnician,
    TradeInvalid,
    ColoniaNotFound,
}

internal sealed record RegisterTechnicianOutcome(RegisterTechnicianStatus Status, TechnicianResponse? Technician = null);

/// <summary>Lo que se guarda al registrar: ya validado, con la dirección del taller cifrada y la ubicación aproximada (centro de la colonia).</summary>
internal sealed record NewTechnician(
    ObjectId Id,
    string Sub,
    string Nombre,
    IReadOnlyList<string> Oficios,
    string? OtroOficio,
    string Atencion,
    string? TallerDireccionCifrada,
    int? RadioKm,
    string ColoniaId,
    (double Latitude, double Longitude)? Centro,
    TechnicianProfileBody? Perfil,
    DateTime CreadoEn);

internal interface ITechnicianProfileStore
{
    Task<bool> ExistsAsync(string sub, CancellationToken cancellationToken);

    /// <summary>Guarda el perfil. Devuelve <see langword="false"/> si el índice único de <c>usuarioId</c> ya tenía uno (dos registros a la vez, CA-69).</summary>
    Task<bool> InsertAsync(NewTechnician technician, CancellationToken cancellationToken);
}

/// <summary>
/// Registro como técnico (spec 046, TEC-01, R-39): la persona es la del <c>sub</c>; el perfil nace <c>activo</c> y sin verificación (D-04), con
/// <c>disponibleHasta</c> en el momento del registro (apagado, D-05). Revisa que cada oficio esté activo y que la colonia exista en el catálogo.
/// </summary>
internal sealed class TechnicianRegistrationService(
    ITechnicianProfileStore store,
    ICatalogApi catalog,
    INeighborhoodCenterCatalog centers,
    FieldCipher cipher,
    TimeProvider clock)
{
    public async Task<RegisterTechnicianOutcome> RegisterAsync(string sub, RegisterTechnicianBody body, CancellationToken cancellationToken)
    {
        if (await store.ExistsAsync(sub, cancellationToken))
        {
            return new RegisterTechnicianOutcome(RegisterTechnicianStatus.AlreadyTechnician);
        }

        var trades = body.Oficios!;
        var found = await Task.WhenAll(trades.Select(trade => catalog.FindActiveTradeAsync(trade, cancellationToken)));
        if (Array.Exists(found, trade => trade is null))
        {
            return new RegisterTechnicianOutcome(RegisterTechnicianStatus.TradeInvalid);
        }

        if (await catalog.FindNeighborhoodAsync(body.ColoniaId!, cancellationToken) is null)
        {
            return new RegisterTechnicianOutcome(RegisterTechnicianStatus.ColoniaNotFound);
        }

        var center = await centers.FindNeighborhoodCenterAsync(body.ColoniaId!, cancellationToken);
        var id = ObjectId.GenerateNewId();
        var now = clock.GetUtcNow().UtcDateTime;
        var workshop = TechnicianRules.UsesWorkshop(body.Atencion) ? WorkshopAddressCipher.Encrypt(cipher, id, body.DireccionTaller!.Trim()) : null;
        var radius = TechnicianRules.VisitsHomes(body.Atencion) ? body.RadioKm : null;
        var technician = new NewTechnician(id, sub, body.Nombre!.Trim(), trades, body.OtroOficio?.Trim(), body.Atencion!, workshop, radius,
            ObjectId.Parse(body.ColoniaId).ToString(), center, Trimmed(body.Perfil), now);
        if (!await store.InsertAsync(technician, cancellationToken))
        {
            return new RegisterTechnicianOutcome(RegisterTechnicianStatus.AlreadyTechnician);
        }

        return new RegisterTechnicianOutcome(RegisterTechnicianStatus.Created, ToResponse(technician));
    }

    private static TechnicianProfileBody? Trimmed(TechnicianProfileBody? profile) =>
        profile is null ? null : new TechnicianProfileBody(Blank(profile.NombreNegocio), Blank(profile.Descripcion), profile.Anios);

    private static string? Blank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static TechnicianResponse ToResponse(NewTechnician t) =>
        new(t.Id.ToString(), t.Nombre, t.Oficios, t.OtroOficio, t.Atencion, t.RadioKm, t.ColoniaId, t.Perfil?.NombreNegocio, t.Perfil?.Descripcion,
            t.Perfil?.Anios, TechnicianRules.ActiveState, TechnicianRules.NoVerification, DisponibleHoy: false, t.CreadoEn);
}
