using Microsoft.Extensions.Options;

namespace Knock.Messaging.Api.Modules.Chat.Options;

/// <summary>
/// Configuración del chat de visita (spec 026, R-08, R-11, R-14 y R-17). <c>Chat:Habilitado</c> no tiene valor por omisión: un servicio sin ese valor no arranca, así el
/// interruptor no se queda encendido por olvido. Ninguna opción lleva secretos.
/// </summary>
internal sealed class ChatOptions
{
    public const string SectionName = "Chat";
    public const string MarketBaseUrlKey = "Services:Market:BaseUrl";
    public const string MarketTimeoutKey = "Services:Market:TiempoLimiteSegundos";
    public const int DefaultMaxCharacters = 1_000;
    public const int DefaultMessagesPerMinute = 30;
    public const int DefaultMarketTimeoutSeconds = 3;
    public const int MaxInvocationBytes = 4_096;
    public const int MaxReceiveBytes = 8_192;
    public const int WindowSeconds = 60;

    /// <summary>Interruptor del canal. Sin valor, la configuración es inválida.</summary>
    public bool? Habilitado { get; set; }

    /// <summary>Máximo de caracteres de un mensaje (1 a 1 000).</summary>
    public int MaxCaracteres { get; set; } = DefaultMaxCharacters;

    /// <summary>Mensajes por minuto por persona (compartido con la spec 030, R-11).</summary>
    public int MensajesPorMinuto { get; set; } = DefaultMessagesPerMinute;
}

/// <summary>Dirección de Mercado y su límite de espera (spec 026, R-08): solo el host, sin ruta, consulta ni fragmento.</summary>
internal sealed class MarketServiceOptions
{
    public const string SectionName = "Services:Market";

    public string? BaseUrl { get; set; }

    public int TiempoLimiteSegundos { get; set; } = ChatOptions.DefaultMarketTimeoutSeconds;
}

/// <summary>Valida el chat al arrancar. Los mensajes nombran la clave, nunca el valor.</summary>
internal sealed class ChatOptionsValidator : IValidateOptions<ChatOptions>
{
    public ValidateOptionsResult Validate(string? name, ChatOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var failures = new List<string>();
        if (options.Habilitado is null)
        {
            failures.Add($"{ChatOptions.SectionName}:Habilitado must be set explicitly to true or false.");
        }

        if (options.MaxCaracteres is < 1 or > ChatOptions.DefaultMaxCharacters)
        {
            failures.Add($"{ChatOptions.SectionName}:MaxCaracteres must be an integer from 1 to {ChatOptions.DefaultMaxCharacters}.");
        }

        if (options.MensajesPorMinuto is < 1 or > 600)
        {
            failures.Add($"{ChatOptions.SectionName}:MensajesPorMinuto must be an integer from 1 to 600.");
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }
}

/// <summary>Valida la dirección de Mercado al arrancar: debe existir, ser http(s) y no llevar ruta, consulta ni fragmento (R-08).</summary>
internal sealed class MarketServiceOptionsValidator : IValidateOptions<MarketServiceOptions>
{
    public ValidateOptionsResult Validate(string? name, MarketServiceOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var failures = new List<string>();
        if (!IsHostOnly(options.BaseUrl))
        {
            failures.Add($"{ChatOptions.MarketBaseUrlKey} must be an http(s) address with only a host (no path, query or fragment).");
        }

        if (options.TiempoLimiteSegundos is < 1 or > 30)
        {
            failures.Add($"{ChatOptions.MarketTimeoutKey} must be an integer from 1 to 30.");
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }

    private static bool IsHostOnly(string? value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri)
        && uri.Scheme is "http" or "https"
        && string.IsNullOrEmpty(uri.UserInfo)
        && uri.AbsolutePath == "/"
        && string.IsNullOrEmpty(uri.Query)
        && string.IsNullOrEmpty(uri.Fragment)
        && !value.Contains('?', StringComparison.Ordinal)
        && !value.Contains('#', StringComparison.Ordinal);
}
