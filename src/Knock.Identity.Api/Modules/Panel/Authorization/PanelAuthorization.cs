using Knock.Identity.Api.Hosting;
using Knock.Identity.Api.Modules.Panel.Data;
using Knock.Identity.Api.Modules.Panel.Domain;
using Knock.Identity.Api.Modules.Panel.Services;
using Knock.Identity.Api.Modules.Sesiones.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;

namespace Knock.Identity.Api.Modules.Panel.Authorization;

/// <summary>
/// Declara el único permiso que exige un endpoint del panel (spec 033, R-20). Aplica el esquema <c>PanelBearer</c> y la política <c>RequierePermiso</c>:
/// sin declaración la política deniega, y una prueba de arquitectura falla si un controller bajo <c>/api/v1/panel/</c> no la declara (salvo los de sesión).
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
internal sealed class RequierePermisoAttribute : AuthorizeAttribute
{
    public const string PolicyPrefix = "panel-permiso:";

    public RequierePermisoAttribute(string permission)
    {
        Permission = permission;
        Policy = PolicyPrefix + permission;
        AuthenticationSchemes = PanelBearer.Scheme;
    }

    public string Permission { get; }
}

/// <summary>Requisito de la política: el permiso que el endpoint declaró.</summary>
internal sealed record PermisoPanelRequirement(string Permission) : IAuthorizationRequirement;

/// <summary>Arma la política de cada <c>RequierePermiso</c> sobre la marcha; cualquier otra política sigue el camino normal del framework.</summary>
internal sealed class PanelPolicyProvider(IOptions<AuthorizationOptions> options) : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider _fallback = new(options);

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => _fallback.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => _fallback.GetFallbackPolicyAsync();

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        ArgumentNullException.ThrowIfNull(policyName);
        if (!policyName.StartsWith(RequierePermisoAttribute.PolicyPrefix, StringComparison.Ordinal))
        {
            return _fallback.GetPolicyAsync(policyName);
        }

        var permission = policyName[RequierePermisoAttribute.PolicyPrefix.Length..];
        var policy = new AuthorizationPolicyBuilder(PanelBearer.Scheme)
            .RequireAuthenticatedUser()
            .AddRequirements(new PermisoPanelRequirement(permission))
            .Build();
        return Task.FromResult<AuthorizationPolicy?>(policy);
    }
}

/// <summary>Decisión de denegación que el manejador deja en <c>HttpContext.Items</c> para que el resultado HTTP lleve el código correcto (401 o 403).</summary>
internal sealed record PanelDenial(int Status, string Code);

/// <summary>
/// Autoriza cada petición de un endpoint con permiso (spec 033, R-21 y R-24): lee la cuenta del panel (por caché de 60 s), y si está deshabilitada o su
/// <c>permisosVersion</c> no coincide con el claim <c>pv</c> responde 401 <c>permissions_changed</c> y revoca la sesión; si no tiene el permiso, 403
/// <c>insufficient_permission</c>. La decisión usa siempre los roles vigentes en el servidor. Cada decisión, permitida o denegada, se audita antes de ejecutar
/// la acción: si la auditoría falla, la acción se niega con 503 <c>audit_unavailable</c>. Un permiso fuera de la lista cerrada se deniega.
/// </summary>
internal sealed class PermisoPanelHandler(
    IPanelCuentaCache cache,
    IPanelCuentasStore accounts,
    IPanelSesionesStore sessions,
    ISesionCancelada cancelled,
    AuditoriaPanel audit,
    IHttpContextAccessor accessor) : AuthorizationHandler<PermisoPanelRequirement>
{
    public const string DenialItem = "Knock.Panel.Denial";

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermisoPanelRequirement requirement)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(requirement);
        var http = accessor.HttpContext;
        var sub = context.User.FindFirst("sub")?.Value;
        var sid = context.User.FindFirst("sid")?.Value;
        if (http is null || string.IsNullOrWhiteSpace(sub) || string.IsNullOrWhiteSpace(sid) || !int.TryParse(context.User.FindFirst("pv")?.Value, out var tokenVersion))
        {
            context.Fail();
            return;
        }

        var (route, resource) = Describe(http);
        var decision = await PanelInfra.GuardAsync(() => DecideAsync(sub, sid, tokenVersion, requirement.Permission, route, resource, http.RequestAborted));
        if (decision is null)
        {
            context.Succeed(requirement);
            return;
        }

        http.Items[DenialItem] = decision;
        context.Fail();
    }

    private async Task<PanelDenial?> DecideAsync(string sub, string sid, int tokenVersion, string permission, string? route, string? resource, CancellationToken cancellationToken)
    {
        var account = await ReadAccountAsync(sub, cancellationToken);
        if (account is null || account.Estado != PanelCuentaEstado.Activa || account.PermisosVersion != tokenVersion)
        {
            await sessions.RevokeAsync(sid, cancellationToken);
            await cancelled.CancelAsync(sid, PanelSesionReglas.AccessTokenLifetime, cancellationToken);
            await audit.WriteAsync(PanelAuditValues.TipoAccion, PanelAuditValues.PermisosCambiados, PanelAuditValues.ResultadoDenegado, sub, sid, permission, route, resource, cancellationToken);
            return new PanelDenial(StatusCodes.Status401Unauthorized, PanelErrorCodes.PermissionsChangedCode);
        }

        // Un permiso fuera de la lista cerrada nunca se concede, ni siquiera a un administrador: sin declaración válida, la política deniega.
        var allowed = PanelPermisos.IsKnown(permission) && MapaDeRoles.PermisosDe(account.Roles).Contains(permission, StringComparer.Ordinal);
        await audit.WriteAsync(
            PanelAuditValues.TipoAccion,
            PanelAuditValues.Accion,
            allowed ? PanelAuditValues.ResultadoPermitido : PanelAuditValues.ResultadoDenegado,
            sub,
            sid,
            permission,
            route,
            resource,
            cancellationToken);
        return allowed ? null : new PanelDenial(StatusCodes.Status403Forbidden, PanelErrorCodes.InsufficientPermissionCode);
    }

    private async Task<CuentaPanelEnCache?> ReadAccountAsync(string sub, CancellationToken cancellationToken)
    {
        var cached = await cache.GetAsync(sub, cancellationToken);
        if (cached is not null)
        {
            return cached;
        }

        var account = await accounts.FindBySubAsync(sub, cancellationToken);
        if (account is null)
        {
            return null;
        }

        var value = new CuentaPanelEnCache(account.Roles, account.Estado, account.PermisosVersion, account.Nombre);
        await cache.SetAsync(sub, value, cancellationToken);
        return value;
    }

    /// <summary>Plantilla de la ruta y, si el endpoint toca un recurso, su identificador (valor de ruta <c>id</c>); la auditoría descarta lo que no parezca un identificador.</summary>
    private static (string? Route, string? Resource) Describe(HttpContext http)
    {
        var route = (http.GetEndpoint() as RouteEndpoint)?.RoutePattern.RawText;
        var resource = http.Request.RouteValues.TryGetValue("id", out var id) ? id?.ToString() : null;
        return (route, resource);
    }
}

/// <summary>
/// Resultado HTTP de la autorización: si el manejador del panel dejó una denegación, responde con su estado y su <c>code</c>; lo demás sigue el
/// comportamiento normal del framework (401 del esquema, 403 genérico y las rutas de la app).
/// </summary>
internal sealed class PanelAuthorizationResultHandler : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _default = new();

    public async Task HandleAsync(RequestDelegate next, HttpContext context, AuthorizationPolicy policy, PolicyAuthorizationResult authorizeResult)
    {
        ArgumentNullException.ThrowIfNull(next);
        ArgumentNullException.ThrowIfNull(context);
        if (!authorizeResult.Succeeded && context.Items.TryGetValue(PermisoPanelHandler.DenialItem, out var item) && item is PanelDenial denial)
        {
            await ProblemResponses.WriteAsync(context, denial.Status, denial.Status == StatusCodes.Status401Unauthorized ? "Unauthorized" : "Forbidden", denial.Code);
            return;
        }

        await _default.HandleAsync(next, context, policy, authorizeResult);
    }
}
