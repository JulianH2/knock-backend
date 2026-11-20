namespace Knock.Identity.Api.Modules.Panel.Authorization;

/// <summary>
/// Marca los endpoints del panel que NO declaran permiso porque son de sesión o de activación (spec 033, R-20): verificar el celular, el TOTP, renovar,
/// cerrar y consumir el enlace de activación. Cualquier otro endpoint bajo <c>/api/v1/panel/</c> debe declarar <c>RequierePermiso</c>;
/// una prueba de arquitectura lo exige.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
internal sealed class PanelSesionEndpointAttribute : Attribute;
