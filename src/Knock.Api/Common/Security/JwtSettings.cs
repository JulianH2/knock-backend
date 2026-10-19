using Microsoft.IdentityModel.Tokens;

namespace Knock.Api.Common.Security;

/// <summary>Parámetros del JWT propio: los comparten el emisor (Identity) y la validación (Hosting).</summary>
internal sealed record JwtSettings(string Issuer, string Audience, SymmetricSecurityKey SigningKey);
