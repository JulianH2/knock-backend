using Knock.Identity.Api.Common.Errors;
using Knock.Identity.Api.Hosting;
using Knock.Identity.Api.Modules.Sesiones;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Knock.Identity.Api.Modules.Panel.Cli;

/// <summary>
/// Modo de herramienta del ensamblado de Identity (spec 033, R-25): <c>Knock.Identity.Api panel &lt;comando&gt;</c> arma solo los servicios que necesita
/// (MongoDB, Redis, llaves) con la misma configuración del entorno, ejecuta el comando y termina con código 0 o distinto de 0. Nunca levanta Kestrel,
/// ni controllers, ni el pipeline HTTP. No registra el comando ni sus argumentos en ningún registro ni en la telemetría: no agrega proveedores de registro.
/// </summary>
internal static class PanelCliHost
{
    public const string Verb = "panel";

    public static bool IsToolMode(IReadOnlyList<string> args) => args.Count > 0 && string.Equals(args[0], Verb, StringComparison.Ordinal);

    public static async Task<int> RunAsync(string[] commandArgs)
    {
        ArgumentNullException.ThrowIfNull(commandArgs);
        var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
            ?? Environments.Production;
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            ContentRootPath = AppContext.BaseDirectory,
            EnvironmentName = environmentName,
        });
        builder.Logging.ClearProviders();
        builder.Services.AddKnockData();
        builder.Services.AddSesionesModule();
        builder.Services.AddPanelModule();
        builder.Services.TryAddSingleton<IPanelCliConsole, SystemPanelCliConsole>();
        builder.Services.AddSingleton<PanelCli>();

        using var host = builder.Build();
        var console = host.Services.GetRequiredService<IPanelCliConsole>();
        try
        {
            return await host.Services.GetRequiredService<PanelCli>().RunAsync(commandArgs, CancellationToken.None);
        }
        catch (DomainException exception) when (exception.Code is not null)
        {
            await console.Error.WriteLineAsync($"Error: la auditoría no se pudo escribir ({exception.Code}). Revise si el cambio se aplicó antes de repetirlo.");
            return PanelCli.ExitFailure;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            // El mensaje de una excepción de dependencias puede traer la cadena de conexión: solo se dice el tipo de problema.
            await console.Error.WriteLineAsync("Error: no se pudo completar el comando (configuración o dependencias no disponibles). No se imprime el detalle para no exponer secretos.");
            return PanelCli.ExitFailure;
        }
    }
}
