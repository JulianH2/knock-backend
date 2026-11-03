namespace Knock.Api.Modules.Identity.Services;

/// <summary>
/// Identidad verificada por Firebase: el <c>uid</c> y el celular E.164 (<c>+52</c>) del token. Salen solo de aquí: el cliente nunca los manda.
/// <see cref="ToString"/> no los incluye para que ningún registro los reciba por accidente.
/// </summary>
internal sealed record FirebaseIdentity(string Uid, string PhoneNumber)
{
    public override string ToString() => nameof(FirebaseIdentity);
}

/// <summary>Valida un ID token de Firebase. Devuelve <c>null</c> por cualquier motivo de rechazo (sin distinguirlos).</summary>
internal interface IFirebaseIdTokenVerifier
{
    Task<FirebaseIdentity?> VerifyAsync(string idToken, CancellationToken cancellationToken);
}
