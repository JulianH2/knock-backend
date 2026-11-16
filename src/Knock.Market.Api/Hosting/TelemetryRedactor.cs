using System.Collections.Frozen;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using OpenTelemetry;
using OpenTelemetry.Logs;
using Serilog.Events;
using Serilog.Formatting;

namespace Knock.Market.Api.Hosting;

/// <summary>
/// Segunda barrera de privacidad de la telemetría (TT-OBS-01 §6): la primera es no capturar cuerpos, encabezados ni la query.
/// Las claves conocidas se redactan siempre; en texto libre, celulares con prefijo internacional, JWT, <c>Bearer</c>, CURP (sin importar
/// mayúsculas), correos, <c>clave=valor</c> de esas claves, <c>Cookie</c>/<c>Set-Cookie</c> completos y pares de coordenadas (con separador).
/// Un número suelto, como el <c>12.3456 ms</c> del log de petición, no se toca (decisión P6). El celular nacional sin <c>+</c> no se
/// busca: riesgo aceptado (HU-001 solo maneja <c>+52</c>; ver <c>docs/OBSERVABILIDAD.md</c>). Las expresiones no tienen
/// cuantificadores anidados y empiezan en un límite de palabra (sin retroceso catastrófico ni reintentos a mitad de una corrida);
/// cada una corre solo si el texto trae el carácter que la dispara.
/// Si una expresión agota su tiempo, el valor se reemplaza entero.
/// </summary>
internal static partial class TelemetryRedactor
{
    public const string Placeholder = "[redacted]";

    /// <summary>Valor de un parámetro de query redactado, el mismo que usa la instrumentación de OpenTelemetry.</summary>
    public const string QueryPlaceholder = "Redacted";

    private const int _timeoutMilliseconds = 100;
    private const RegexOptions _options = RegexOptions.CultureInvariant;

    private static readonly FrozenSet<string> _sensitiveKeys = new[]
    {
        "lat", "lon", "phone", "celular", "curp", "token", "idToken", "id_token", "accessToken", "access_token", "refreshToken",
        "refresh_token", "code", "sig", "authorization", "cookie", "set-cookie", "password", "secret", "email", "correo",
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    /// <summary><see langword="true"/> si el nombre exacto de la clave (sin importar mayúsculas) es de las que siempre se redactan.</summary>
    public static bool IsSensitiveKey(string key) => _sensitiveKeys.Contains(key);

    public static string Redact(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (value.Length == 0)
        {
            return value;
        }

        try
        {
            var result = value.Contains("eyJ", StringComparison.Ordinal) ? Jwt().Replace(value, Placeholder) : value;
            result = result.Contains("bearer", StringComparison.OrdinalIgnoreCase) ? Bearer().Replace(result, Placeholder) : result;
            result = result.AsSpan().ContainsAny('=', ':') ? KeyValue().Replace(result, "${key}${separator}" + Placeholder) : result;
            result = result.Contains('@', StringComparison.Ordinal) ? Email().Replace(result, Placeholder) : result;
            result = result.Contains("cookie", StringComparison.OrdinalIgnoreCase) ? Cookie().Replace(result, "${key}${separator}" + Placeholder) : result;
            result = result.AsSpan().ContainsAny('+', '%') ? Phone().Replace(result, Placeholder) : result;
            result = result.Length >= 18 ? Curp().Replace(result, Placeholder) : result;
            return CoordinatePairs.Redact(result);
        }
        catch (RegexMatchTimeoutException)
        {
            return Placeholder;
        }
    }

    /// <summary>
    /// Valor de un atributo: el de una clave conocida se reemplaza entero; los textos pasan por <see cref="Redact(string)"/>, también
    /// dentro de <c>string[]</c>, <c>object[]</c> y cualquier <see cref="IEnumerable{T}"/> de <see cref="string"/> (se devuelve como
    /// <c>string[]</c>). Los demás tipos (números, booleanos) pasan tal cual: **un tipo nuevo que pueda llevar texto debe agregarse aquí**
    /// y a <c>RedactAttribute_TextContainers_AreRedacted_TC_TT_OBS_01_12</c>.
    /// </summary>
    public static object? RedactAttribute(string key, object? value)
    {
        ArgumentNullException.ThrowIfNull(key);
        if (value is null)
        {
            return null;
        }

        if (IsSensitiveKey(key))
        {
            return Placeholder;
        }

        return value switch
        {
            string text => Redact(text),
            string[] texts => Array.ConvertAll(texts, Redact),
            object?[] items => Array.ConvertAll(items, item => item is string text ? Redact(text) : item),
            IEnumerable<string> texts => texts.Select(Redact).ToArray(),
            _ => value,
        };
    }

    /// <summary>URL sin valores de la query (<c>?sig=Redacted&amp;se=Redacted</c>) y con la ruta redactada.</summary>
    public static string RedactUrl(string url)
    {
        ArgumentNullException.ThrowIfNull(url);
        var queryStart = url.IndexOf('?', StringComparison.Ordinal);
        if (queryStart < 0)
        {
            return Redact(url);
        }

        var redacted = new StringBuilder(Redact(url[..queryStart])).Append('?');
        var parameters = url[(queryStart + 1)..].Split('&');
        for (var i = 0; i < parameters.Length; i++)
        {
            var separator = parameters[i].IndexOf('=', StringComparison.Ordinal);
            var name = separator < 0 ? parameters[i] : parameters[i][..separator];
            redacted.Append(i == 0 ? string.Empty : "&").Append(Redact(name)).Append('=').Append(QueryPlaceholder);
        }

        return redacted.ToString();
    }

    /// <summary>Evento <c>exception</c> del span con el tipo de la excepción y su mensaje y pila ya redactados.</summary>
    public static ActivityEvent ExceptionEvent(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return new ActivityEvent("exception", tags: new ActivityTagsCollection
        {
            ["exception.type"] = exception.GetType().FullName,
            ["exception.message"] = Redact(exception.Message),
            ["exception.stacktrace"] = Redact(exception.ToString()),
        });
    }

    [GeneratedRegex(@"\beyJ[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+\.[A-Za-z0-9_-]*", _options, _timeoutMilliseconds)]
    private static partial Regex Jwt();

    [GeneratedRegex(@"\bbearer\s+[A-Za-z0-9._~+/=-]+", _options | RegexOptions.IgnoreCase, _timeoutMilliseconds)]
    private static partial Regex Bearer();

    /// <summary><c>clave=valor</c> o <c>"clave":"valor"</c> de una clave conocida; el nombre debe ser exacto (<c>StatusCode</c> no es <c>code</c>).</summary>
    [GeneratedRegex(
        @"\b(?<key>lat|lon|phone|celular|curp|token|idToken|id_token|accessToken|access_token|refreshToken|refresh_token|code|sig|authorization|password|secret|email|correo)(?<separator>""?\s*[=:]\s*""?)[^\s&;,""]+",
        _options | RegexOptions.IgnoreCase,
        _timeoutMilliseconds)]
    private static partial Regex KeyValue();

    /// <summary><c>Cookie</c> o <c>Set-Cookie</c> con el valor completo hasta el fin de línea o la comilla: lleva varios pares separados por <c>;</c>.</summary>
    [GeneratedRegex(@"\b(?<key>set-cookie|cookie)(?<separator>""?\s*[=:]\s*""?)[^\r\n""]+", _options | RegexOptions.IgnoreCase, _timeoutMilliseconds)]
    private static partial Regex Cookie();

    /// <summary>Correo electrónico en texto libre: parte local, <c>@</c> y dominio con punto y TLD.</summary>
    [GeneratedRegex(@"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}\b", _options, _timeoutMilliseconds)]
    private static partial Regex Email();

    /// <summary>Celular con prefijo internacional (<c>+</c> o <c>%2B</c>) y 10 a 14 dígitos, con o sin espacios, puntos o guiones.</summary>
    [GeneratedRegex(@"(?:\+|%2[Bb])[0-9](?:[\s.-]?[0-9]){9,13}", _options, _timeoutMilliseconds)]
    private static partial Regex Phone();

    [GeneratedRegex(@"\b[A-Z]{4}[0-9]{6}[HMX][A-Z]{5}[A-Z0-9][0-9]\b", _options | RegexOptions.IgnoreCase, _timeoutMilliseconds)]
    private static partial Regex Curp();
}

/// <summary>
/// Pares de coordenadas en texto libre (decisión P6): dos números con signo opcional, 1 a 3 enteros y 4 o más decimales, separados por
/// espacios, coma, punto y coma o <c>/</c> (<c>19.4326, -99.1332</c>, <c>19.4326/-99.1332</c>). Recorrido lineal que solo se detiene en
/// cada punto; un número suelto (<c>12.3456 ms</c>) no se toca.
/// </summary>
internal static class CoordinatePairs
{
    private const int _minDecimals = 4;
    private const int _maxIntegerDigits = 3;

    public static string Redact(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        StringBuilder? redacted = null;
        var copied = 0;
        var search = 0;
        int dot;
        while ((dot = value.IndexOf('.', search, StringComparison.Ordinal)) >= 0)
        {
            if (TryFirstNumber(value, dot, out var start, out var end) && TrySecondNumber(value, end, out var pairEnd))
            {
                redacted ??= new StringBuilder(value.Length);
                redacted.Append(value, copied, start - copied).Append(TelemetryRedactor.Placeholder);
                copied = search = pairEnd;
            }
            else
            {
                search = dot + 1;
            }
        }

        return redacted is null ? value : redacted.Append(value, copied, value.Length - copied).ToString();
    }

    /// <summary>Número cuyo punto está en <paramref name="dot"/> y que empieza en un límite de palabra (o con <c>-</c>).</summary>
    private static bool TryFirstNumber(string value, int dot, out int start, out int end)
    {
        start = dot;
        while (start > 0 && dot - start < _maxIntegerDigits + 1 && char.IsAsciiDigit(value[start - 1]))
        {
            start--;
        }

        end = DecimalsEnd(value, dot);
        var integerDigits = dot - start;
        if (integerDigits is 0 or > _maxIntegerDigits || end < 0)
        {
            return false;
        }

        if (start > 0 && value[start - 1] == '-')
        {
            start--;
            return true;
        }

        return start == 0 || !IsWordChar(value[start - 1]);
    }

    /// <summary>Separador obligatorio (espacios, o <c>,</c> <c>;</c> <c>/</c> con espacios opcionales) y el segundo número.</summary>
    private static bool TrySecondNumber(string value, int from, out int end)
    {
        end = -1;
        var index = SkipWhitespace(value, from);
        if (index < value.Length && value[index] is ',' or ';' or '/')
        {
            index = SkipWhitespace(value, index + 1);
        }
        else if (index == from)
        {
            return false;
        }

        if (index < value.Length && value[index] == '-')
        {
            index++;
        }

        var integerStart = index;
        while (index < value.Length && index - integerStart <= _maxIntegerDigits && char.IsAsciiDigit(value[index]))
        {
            index++;
        }

        var integerDigits = index - integerStart;
        if (integerDigits is 0 or > _maxIntegerDigits || index >= value.Length || value[index] != '.')
        {
            return false;
        }

        end = DecimalsEnd(value, index);
        return end >= 0;
    }

    /// <summary>Fin de los decimales que siguen al punto, o -1 si son menos de 4.</summary>
    private static int DecimalsEnd(string value, int dot)
    {
        var index = dot + 1;
        while (index < value.Length && char.IsAsciiDigit(value[index]))
        {
            index++;
        }

        return index - dot - 1 >= _minDecimals ? index : -1;
    }

    private static int SkipWhitespace(string value, int index)
    {
        while (index < value.Length && char.IsWhiteSpace(value[index]))
        {
            index++;
        }

        return index;
    }

    private static bool IsWordChar(char c) => char.IsLetterOrDigit(c) || c == '_';
}

/// <summary>
/// Redacta cada span antes de que lo vea un exportador (se registra primero): atributos, nombre y descripción del estado. Quita
/// <c>url.query</c> y cualquier encabezado capturado y deja <c>url.full</c> sin valores de la query.
/// </summary>
internal sealed class TelemetryRedactionProcessor : BaseProcessor<Activity>
{
    private const string _requestHeaderPrefix = "http.request.header.";
    private const string _responseHeaderPrefix = "http.response.header.";

    public override void OnEnd(Activity data)
    {
        ArgumentNullException.ThrowIfNull(data);
        foreach (var (key, value) in data.TagObjects.ToArray())
        {
            if (string.Equals(key, "url.query", StringComparison.Ordinal)
                || key.StartsWith(_requestHeaderPrefix, StringComparison.Ordinal)
                || key.StartsWith(_responseHeaderPrefix, StringComparison.Ordinal))
            {
                data.SetTag(key, null);
                continue;
            }

            var redacted = string.Equals(key, "url.full", StringComparison.Ordinal) && value is string url
                ? TelemetryRedactor.RedactUrl(url)
                : TelemetryRedactor.RedactAttribute(key, value);
            if (!Equals(redacted, value))
            {
                data.SetTag(key, redacted);
            }
        }

        data.DisplayName = TelemetryRedactor.Redact(data.DisplayName);
        if (data.StatusDescription is { } description)
        {
            data.SetStatus(data.Status, TelemetryRedactor.Redact(description));
        }
    }
}

/// <summary>Redacta cada log antes de que lo vea un exportador (se registra primero): cuerpo, mensaje formateado y atributos.</summary>
internal sealed class LogRedactionProcessor : BaseProcessor<LogRecord>
{
    public override void OnEnd(LogRecord data)
    {
        ArgumentNullException.ThrowIfNull(data);
        if (data.Body is { } body)
        {
            data.Body = TelemetryRedactor.Redact(body);
        }

        if (data.FormattedMessage is { } message)
        {
            data.FormattedMessage = TelemetryRedactor.Redact(message);
        }

        if (data.Attributes is { Count: > 0 } attributes)
        {
            data.Attributes = [.. attributes.Select(a => new KeyValuePair<string, object?>(a.Key, TelemetryRedactor.RedactAttribute(a.Key, a.Value)))];
        }
    }
}

/// <summary>
/// Formato de consola con la misma redacción que <see cref="LogRedactionProcessor"/> (spec 011): el formateador interno escribe el
/// evento en JSON y cada valor pasa por <see cref="TelemetryRedactor"/> antes de salir. El mensaje (<c>@m</c>, <c>@mt</c>) y la excepción
/// (<c>@x</c>) se redactan como texto; las propiedades, como atributos (una clave conocida se reemplaza entera, también dentro de objetos).
/// Los demás campos de control (<c>@t</c>, <c>@l</c>, <c>@i</c>, <c>@tr</c>, <c>@sp</c>) no llevan datos del usuario y no se tocan.
/// El resultado se vuelve a serializar, así que sigue siendo JSON válido aunque se reemplace un número (<c>"lat":19.4326</c>).
/// </summary>
internal sealed class RedactingJsonFormatter(ITextFormatter inner) : ITextFormatter
{
    private static readonly FrozenSet<string> _textFields = new[] { "@m", "@mt", "@x" }.ToFrozenSet(StringComparer.Ordinal);

    public void Format(LogEvent logEvent, TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(output);
        using var buffer = new StringWriter(CultureInfo.InvariantCulture);
        inner.Format(logEvent, buffer);
        if (JsonNode.Parse(buffer.ToString()) is not JsonObject source)
        {
            output.WriteLine(TelemetryRedactor.Redact(buffer.ToString().TrimEnd()));
            return;
        }

        var redacted = new JsonObject();
        foreach (var (key, value) in source)
        {
            redacted[key] = key is ['@', ..] && !_textFields.Contains(key) ? value?.DeepClone() : RedactNode(key, value);
        }

        output.WriteLine(redacted.ToJsonString());
    }

    private static JsonNode? RedactNode(string key, JsonNode? node)
    {
        if (node is null)
        {
            return null;
        }

        if (TelemetryRedactor.IsSensitiveKey(key))
        {
            return JsonValue.Create(TelemetryRedactor.Placeholder);
        }

        switch (node)
        {
            case JsonObject structure:
                var redacted = new JsonObject();
                foreach (var (name, value) in structure)
                {
                    redacted[name] = RedactNode(name, value);
                }

                return redacted;
            case JsonArray items:
                return new JsonArray([.. items.Select(item => RedactNode(string.Empty, item))]);
            case JsonValue value when value.TryGetValue(out string? text):
                return JsonValue.Create(TelemetryRedactor.Redact(text));
            default:
                return node.DeepClone();
        }
    }
}
