using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace Knock.IntegrationTest.Support;

/// <summary>Controller que existe solo en el host de pruebas (no se publica con la API). Sin try/catch a propósito.</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/test")]
public sealed class TestController : ControllerBase
{
    public const string SecretMessage = "internal-detail-must-not-leak";

    [HttpGet("ping")]
    public IActionResult Ping() => Ok(new { status = "ok" });

    [HttpGet("boom")]
    public IActionResult Boom()
    {
        ThrowUnhandled(Request.Method);
        return NoContent();
    }

    private static void ThrowUnhandled(string method) =>
        throw new InvalidOperationException($"{SecretMessage} ({method})");
}
