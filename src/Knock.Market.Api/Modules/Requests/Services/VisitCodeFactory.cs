using System.Globalization;
using System.Security.Cryptography;
using Knock.Market.Api.Common.Crypto;
using MongoDB.Bson;

namespace Knock.Market.Api.Modules.Requests.Services;

/// <summary>Código de visita ya cifrado y su vencimiento. El valor en claro nunca sale de <see cref="VisitCodeFactory"/> (spec 021, R-17).</summary>
internal sealed record VisitCode(string Encrypted, DateTime ExpiresAtUtc);

/// <summary>
/// Genera el código de cuatro dígitos de una cita a domicilio con un generador criptográfico, lo cifra (AES-256-GCM con llave versionada, atado al id de la visita) y
/// calcula su vencimiento: 23:59:59 del día de la cita en <c>America/Mexico_City</c>, más 24 horas (spec 021, R-17). El código no se devuelve ni se registra.
/// </summary>
internal sealed class VisitCodeFactory(FieldCipher cipher)
{
    private const string _timeZone = "America/Mexico_City";

    public VisitCode Create(ObjectId visitId, string day)
    {
        var plain = RandomNumberGenerator.GetInt32(0, 10_000).ToString("D4", CultureInfo.InvariantCulture);
        return new VisitCode(cipher.Encrypt(plain, Context(visitId)), ExpiresAt(day));
    }

    /// <summary>Contexto del cifrado: el código copiado a otra visita no se descifra. Lo usa también la spec 022 al mostrarlo.</summary>
    public static string Context(ObjectId visitId) => $"visitas|codigo|{visitId}";

    internal static DateTime ExpiresAt(string day)
    {
        var endOfDay = DateOnly.ParseExact(day, "yyyy-MM-dd", CultureInfo.InvariantCulture).ToDateTime(new TimeOnly(23, 59, 59), DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(endOfDay, TimeZoneInfo.FindSystemTimeZoneById(_timeZone)).AddHours(24);
    }
}
