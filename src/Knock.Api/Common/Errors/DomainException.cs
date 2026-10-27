namespace Knock.Api.Common.Errors;

/// <summary>Catálogo de errores de dominio. El 401 y el 429 no son excepciones de dominio: los producen la autenticación y el limitador.</summary>
public enum DomainErrorKind
{
    BadRequest,
    Forbidden,
    NotFound,
    Conflict,
    Unprocessable,
}

/// <summary>
/// Excepción base de dominio. El manejador la traduce a ProblemDetails con el código del catálogo.
/// El mensaje es solo para el log del servidor: nunca debe llevar datos del usuario y no se devuelve fuera de Development.
/// </summary>
public sealed class DomainException : Exception
{
    public DomainException()
        : this(DomainErrorKind.BadRequest, "Domain error.")
    {
    }

    public DomainException(string message)
        : this(DomainErrorKind.BadRequest, message)
    {
    }

    public DomainException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public DomainException(DomainErrorKind kind, string message)
        : base(message)
    {
        Kind = kind;
    }

    public DomainErrorKind Kind { get; }
}

/// <summary>Código HTTP de cada tipo del catálogo de dominio.</summary>
internal static class DomainErrors
{
    public static int ToStatusCode(DomainErrorKind kind) => kind switch
    {
        DomainErrorKind.BadRequest => StatusCodes.Status400BadRequest,
        DomainErrorKind.Forbidden => StatusCodes.Status403Forbidden,
        DomainErrorKind.NotFound => StatusCodes.Status404NotFound,
        DomainErrorKind.Conflict => StatusCodes.Status409Conflict,
        DomainErrorKind.Unprocessable => StatusCodes.Status422UnprocessableEntity,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown domain error kind."),
    };
}
