using System.Text.RegularExpressions;

namespace Knock.Messaging.Api.Modules.Conversaciones.Domain;

/// <summary>El texto ya con los datos de contacto reemplazados y si hubo algo que reemplazar (<c>contactoOculto</c>).</summary>
internal readonly record struct TextoOculto(string Texto, bool Oculto);

/// <summary>
/// Oculta los datos de contacto de un mensaje antes de moderarlo y guardarlo (spec 030, R-12): enlaces de WhatsApp y Telegram, correos y teléfonos (10 dígitos con o sin lada, con espacios,
/// guiones, puntos o paréntesis, o con <c>+52</c>) pasan a «[dato de contacto oculto]». Es una función pura sin estado: el servicio decide cuándo llamarla (solo mientras el técnico no sea el
/// elegido) y nunca guarda ni registra el texto original. Si una expresión agota su tiempo, falla cerrado: se oculta el texto completo.
/// </summary>
/// <remarks>
/// Límite conocido: no reconoce números escritos con letras ni con símbolos intercalados («siete siete uno…»); eso lo atrapa la moderación (spec 031) y el reporte de la persona.
/// Los patrones se tomaron como base para la spec 024 (filtro de datos personales); cuando exista allí, esta copia se retira o se une con ella.
/// </remarks>
internal static partial class OcultadorDeContacto
{
    public const string Marcador = "[dato de contacto oculto]";

    private const int _timeoutMilliseconds = 200;

    public static TextoOculto Ocultar(string texto)
    {
        ArgumentNullException.ThrowIfNull(texto);
        try
        {
            var result = MessagingLink().Replace(texto, Marcador);
            result = Email().Replace(result, Marcador);
            result = Phone().Replace(result, Marcador);
            return new TextoOculto(result, !string.Equals(result, texto, StringComparison.Ordinal));
        }
        catch (RegexMatchTimeoutException)
        {
            return new TextoOculto(Marcador, true);
        }
    }

    [GeneratedRegex(@"(?<![a-z0-9\-])(?:https?://)?(?:[a-z0-9\-]+\.)*(?:wa\.me|whatsapp\.com|t\.me|telegram\.me|telegram\.dog)(?:/\S*)?", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, _timeoutMilliseconds)]
    private static partial Regex MessagingLink();

    [GeneratedRegex(@"[a-z0-9._%+\-]+@[a-z0-9\-]+(?:\.[a-z0-9\-]+)+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, _timeoutMilliseconds)]
    private static partial Regex Email();

    [GeneratedRegex(@"(?<!\d)(?:\+\s?52[\s.\-]*)?\(?(?:\d[\s.\-()]*){9}\d(?!\d)", RegexOptions.CultureInvariant, _timeoutMilliseconds)]
    private static partial Regex Phone();
}
