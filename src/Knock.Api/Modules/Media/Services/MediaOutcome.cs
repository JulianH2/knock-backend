namespace Knock.Api.Modules.Media.Services;

/// <summary>Resultado de una operación del módulo; el controller lo traduce a HTTP sin lógica propia.</summary>
internal enum MediaOutcomeStatus
{
    Success,

    /// <summary>No existe o no es de quien llama: no se distingue para no filtrar existencia (404).</summary>
    NotFound,

    /// <summary>El archivo ya no está pendiente (409).</summary>
    Conflict,

    /// <summary>El archivo o lo declarado no es válido (422).</summary>
    Unprocessable,
}

internal sealed record MediaOutcome<T>(MediaOutcomeStatus Status, T? Value = default, string? Detail = null)
{
    public static MediaOutcome<T> Ok(T value) => new(MediaOutcomeStatus.Success, value);

    public static MediaOutcome<T> NotFound() => new(MediaOutcomeStatus.NotFound);

    public static MediaOutcome<T> Conflict(string detail) => new(MediaOutcomeStatus.Conflict, Detail: detail);

    public static MediaOutcome<T> Unprocessable(string detail) => new(MediaOutcomeStatus.Unprocessable, Detail: detail);
}
