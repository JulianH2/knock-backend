using Knock.Identity.Api.Modules.Sesiones.Domain;

namespace Knock.Identity.Api.Modules.Sesiones.Services;

/// <summary>Validación del celular E.164 de México expuesta a otros módulos (spec 039) sin abrir el dominio de Sesiones.</summary>
internal static class PhoneNumbers
{
    public static bool IsValid(string? phone) => PhoneNumberPolicy.IsValid(phone);
}
