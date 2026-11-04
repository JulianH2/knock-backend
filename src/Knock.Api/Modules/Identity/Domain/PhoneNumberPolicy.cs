namespace Knock.Api.Modules.Identity.Domain;

/// <summary>
/// Celulares aceptados: E.164 de México, exactamente <c>+52</c> y 10 dígitos ASCII (constitución §6: los SMS solo salen a +52).
/// No usa <c>\d</c>: en .NET coincide con dígitos de otros alfabetos, y <c>$</c> acepta un salto de línea al final.
/// </summary>
internal static class PhoneNumberPolicy
{
    private const string _prefix = "+52";
    private const int _length = 13;

    public static bool IsValid(string? phone)
    {
        if (phone is null || phone.Length != _length || !phone.StartsWith(_prefix, StringComparison.Ordinal))
        {
            return false;
        }

        for (var i = _prefix.Length; i < _length; i++)
        {
            if (phone[i] is < '0' or > '9')
            {
                return false;
            }
        }

        return true;
    }
}
