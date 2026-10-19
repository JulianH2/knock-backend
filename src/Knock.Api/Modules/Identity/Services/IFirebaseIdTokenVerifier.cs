namespace Knock.Api.Modules.Identity.Services;

/// <summary>Identidad verificada por Firebase: solo el <c>uid</c> (el teléfono no se guarda ni sale de aquí).</summary>
internal sealed record FirebaseIdentity(string Uid);

/// <summary>Valida un ID token de Firebase. Devuelve <c>null</c> por cualquier motivo de rechazo (sin distinguirlos).</summary>
internal interface IFirebaseIdTokenVerifier
{
    Task<FirebaseIdentity?> VerifyAsync(string idToken, CancellationToken cancellationToken);
}
