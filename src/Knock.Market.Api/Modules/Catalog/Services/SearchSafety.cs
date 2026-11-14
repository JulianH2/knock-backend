using System.Security.Cryptography;
using Microsoft.Extensions.Options;

namespace Knock.Market.Api.Modules.Catalog.Services;

/// <summary>Checks the original input before normalization; an unavailable filter must reject counting.</summary>
internal interface IFiltroDatosPersonales
{
    bool CanCount(string originalText);
}

internal sealed class FiltroDatosPersonalesNoDisponible : IFiltroDatosPersonales
{
    public bool CanCount(string originalText) => false;
}

internal sealed class SearchCursorOptions
{
    public const string SectionName = "Busqueda:Cursor";
    public const string KeyName = "Busqueda:Cursor:Key";
    public string? Key { get; set; }
}

internal sealed partial class DevelopmentSearchCursorKey(IHostEnvironment environment, ILogger<DevelopmentSearchCursorKey> logger) : IPostConfigureOptions<SearchCursorOptions>
{
    private readonly Lazy<string> _ephemeralKey = new(() => GenerateKey(logger));

    public void PostConfigure(string? name, SearchCursorOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (environment.IsDevelopment() && options.Key is null)
        {
            options.Key = _ephemeralKey.Value;
        }
    }

    private static string GenerateKey(ILogger logger)
    {
        LogEphemeralKey(logger);
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    }

    [LoggerMessage(EventId = 2701, Level = LogLevel.Warning, Message = "Using an ephemeral search cursor key in Development; cursors do not survive restarts or work across replicas.")]
    private static partial void LogEphemeralKey(ILogger logger);
}

internal sealed class SearchCursorOptionsValidator : IValidateOptions<SearchCursorOptions>
{
    public ValidateOptionsResult Validate(string? name, SearchCursorOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        Span<byte> bytes = stackalloc byte[32];
        return options.Key is not null && Convert.TryFromBase64String(options.Key, bytes, out var count) && count == bytes.Length
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail($"{SearchCursorOptions.KeyName} must be a base64-encoded 256-bit key.");
    }
}
