using Asp.Versioning;
using FluentValidation;
using Knock.Api.Common.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Knock.IntegrationTest.Support;

/// <summary>Controller que existe solo en el host de pruebas (no se publica con la API). Sin try/catch a propósito.</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/test")]
public sealed partial class TestController(ILogger<TestController> logger) : ControllerBase
{
    public const string SecretMessage = "internal-detail-must-not-leak";

    /// <summary>Cuántas veces llegó una petición al cuerpo de <see cref="Validated"/> (prueba que el filtro corta antes).</summary>
    private static int _validatedCalls;

    public static int ValidatedCalls => Volatile.Read(ref _validatedCalls);

    [HttpGet("ping")]
    public IActionResult Ping() => Ok(new { status = "ok" });

    [HttpGet("boom")]
    public IActionResult Boom()
    {
        ThrowUnhandled(Request.Method);
        return NoContent();
    }

    /// <summary>Endpoint protegido: 401 sin sesión, 200 con JWT propio.</summary>
    [HttpGet("secure")]
    [Authorize]
    public IActionResult Secure() => Ok(new { status = "secure" });

    /// <summary>Lanza la excepción de dominio del tipo pedido, con un mensaje interno sensible a propósito.</summary>
    [HttpGet("domain/{kind}")]
    public IActionResult Domain(string kind) =>
        throw new DomainException(Enum.Parse<DomainErrorKind>(kind), $"{SecretMessage} ({Request.Method}) token=sensitive-token-value phone=+15555550100");

    /// <summary>Endpoint de ruta con coordenadas ficticias: registra un log de «servicio» y falla (log de error).</summary>
    [HttpGet("at/{latitude}/{longitude}")]
    public IActionResult At(string latitude, string longitude)
    {
        ArgumentNullException.ThrowIfNull(latitude);
        ArgumentNullException.ThrowIfNull(longitude);
        LogServiceStep(logger);
        throw new InvalidOperationException("synthetic failure for log checks");
    }

    /// <summary>Registra un evento propio y responde 200 (log de «servicio» sin error).</summary>
    [HttpGet("log")]
    public IActionResult Log()
    {
        LogServiceStep(logger);
        return Ok(new { status = "logged" });
    }

    /// <summary>Cuerpo con validador de FluentValidation (el filtro responde 422 antes de ejecutar esto).</summary>
    [HttpPost("validated")]
    public IActionResult Validated([FromBody] ValidatedInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        Interlocked.Increment(ref _validatedCalls);
        return Ok(new { executed = true });
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Test service step executed")]
    private static partial void LogServiceStep(ILogger logger);

    private static void ThrowUnhandled(string method) =>
        throw new InvalidOperationException($"{SecretMessage} ({method})");
}

/// <summary>Entrada ficticia: <c>Phone</c> y <c>Token</c> son sensibles; <c>Age</c> usa un validador que repite el valor en su mensaje.</summary>
public sealed record ValidatedInput(string? Phone, string? Token, int? Age, string? Echo);

public sealed class ValidatedInputValidator : AbstractValidator<ValidatedInput>
{
    public ValidatedInputValidator()
    {
        RuleFor(x => x.Phone).NotEmpty().Matches(@"^\+?[0-9]{10,15}$");
        RuleFor(x => x.Token).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Age).InclusiveBetween(18, 120);
        // Un validador con mensaje propio que repite el valor enviado: el filtro no debe devolverlo.
        RuleFor(x => x.Echo).Must(value => value is null).WithMessage("Rejected value {PropertyValue}.");
    }
}
