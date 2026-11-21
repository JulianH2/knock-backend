namespace Knock.Messaging.Api.Modules.Conversaciones.Domain;

/// <summary>
/// Vive bajo <c>Modules</c> porque S3871 exige que las excepciones sean públicas y CA1515 solo se exime en esa ruta (<c>.editorconfig</c>); el manejador de <c>Hosting</c> la traduce para el servicio completo.
/// Catálogo de errores de dominio de Mensajería. El 401 y el 429 no son excepciones de dominio: los producen la autenticación y el limitador.</summary>
public enum DomainErrorKind
{
    BadRequest,
    Forbidden,
    NotFound,
    Conflict,
    Unprocessable,
    ServiceUnavailable,
}

// jscpd:ignore-start -- EXC-030
/// <summary>
/// Error de dominio con un <c>code</c> estable que la app lee en el ProblemDetails (por ejemplo <c>conversation_closed</c>). El mensaje es solo para el registro del servidor:
/// nunca lleva datos de la persona, texto de mensajes ni identificadores, y no se devuelve fuera de Development.
/// </summary>
public sealed class DomainException : Exception
{
    public DomainException()
        : this(DomainErrorKind.BadRequest, "Domain error.", null)
    {
    }

    public DomainException(string message)
        : this(DomainErrorKind.BadRequest, message, null)
    {
    }

    public DomainException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public DomainException(DomainErrorKind kind, string message, string? code)
        : base(message)
    {
        Kind = kind;
        Code = code;
    }

    public DomainErrorKind Kind { get; }

    /// <summary>Código estable opcional; nunca lleva datos de la persona ni del recurso.</summary>
    public string? Code { get; }
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
        DomainErrorKind.ServiceUnavailable => StatusCodes.Status503ServiceUnavailable,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown domain error kind."),
    };
}
// jscpd:ignore-end
