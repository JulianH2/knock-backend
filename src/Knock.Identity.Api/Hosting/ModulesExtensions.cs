// jscpd:ignore-start -- EXC-015
using Knock.Identity.Api.Modules.HomeAndCar;
using Knock.Identity.Api.Modules.Sesiones;
using Knock.Identity.Api.Modules.Usuarios;

namespace Knock.Identity.Api.Hosting;

internal static class ModulesExtensions
{
    /// <summary>Registra los módulos del servicio de Identidad.</summary>
    public static IServiceCollection AddKnockModules(this IServiceCollection services) => services.AddSesionesModule().AddUsuariosModule().AddHomeAndCarModule();
}
// jscpd:ignore-end
