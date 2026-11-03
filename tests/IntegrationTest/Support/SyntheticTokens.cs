using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Knock.IntegrationTest.Support;

/// <summary>
/// Valores ficticios con forma de token armados en tiempo de ejecución: el texto fuente no tiene forma de secreto, así que los
/// escáneres (Gitleaks) no los marcan y no hace falta suprimir nada. Solo para afirmar que esos valores no salen en la telemetría.
/// </summary>
internal static class SyntheticTokens
{
    /// <summary>JWT sintético: encabezado y cuerpo JSON en base64url y una firma inventada (no verificable).</summary>
    public static string Jwt(string headerJson, string payloadJson, string signature) =>
        string.Join('.', Encode(headerJson), Encode(payloadJson), Encode(signature));

    /// <summary>Token opaco ficticio unido por guiones (p. ej. <c>tok-SECRET-05</c>).</summary>
    public static string Opaque(params string[] parts) => string.Join('-', parts);

    private static string Encode(string value) => Base64UrlEncoder.Encode(Encoding.UTF8.GetBytes(value));
}
