// jscpd:ignore-start -- EXC-007: copia de src/Knock.Market.Api/Hosting/LoggingExtensions.cs (spec 011); se unifica en una biblioteca técnica.
using System.Text;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Routing.Patterns;
using Serilog;
using Serilog.AspNetCore;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Compact;

namespace Knock.Gateway.Hosting;

internal static class LoggingExtensions
{

    /// <summary>
    /// Serilog a consola en JSON (una línea por evento, redactada con <see cref="RedactingJsonFormatter"/>), configurable desde appsettings. Los mismos eventos, ya filtrados y enriquecidos,
    /// van a OpenTelemetry (TT-OBS-01) con <see cref="OpenTelemetryLogSink"/>.
    /// </summary>
    public static IServiceCollection AddKnockLogging(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSerilog((provider, logger) => logger
            .ReadFrom.Configuration(configuration)
            .Filter.ByExcluding(IsQueryBearingBelowWarning)
            .Enrich.FromLogContext()
            .Enrich.With<ScopeRequestPathEnricher>()
            .WriteTo.Console(new RedactingJsonFormatter(new RenderedCompactJsonFormatter()))
            .WriteTo.Sink(new OpenTelemetryLogSink(provider)));
        return services;
    }

    /// <summary>
    /// Log de petición sin datos del usuario: registra la plantilla de la ruta (por ejemplo <c>/api/v1/x/{id}</c>), nunca la ruta con sus valores
    /// ni la query. Una ruta sin endpoint se registra como <see cref="UnmatchedRoute"/>.
    /// </summary>
    public static void ConfigureRequestLogging(RequestLoggingOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.GetMessageTemplateProperties = (context, _, elapsed, statusCode) =>
        [
            new LogEventProperty("RequestMethod", new ScalarValue(context.Request.Method)),
            new LogEventProperty("RequestPath", new ScalarValue(RoutePath(context))),
            new LogEventProperty("StatusCode", new ScalarValue(statusCode)),
            new LogEventProperty("Elapsed", new ScalarValue(elapsed)),
        ];
    }

    public const string UnmatchedRoute = "(unmatched)";

    /// <summary>
    /// Fuentes que escriben la dirección completa, con su consulta, en eventos por debajo de <c>Warning</c> (spec 035, R-01): los eventos «Request starting» y
    /// «Request finished» de ASP.NET Core, el registro HTTP, los clientes HTTP y el reenvío de YARP. La petición la registra solo <c>UseSerilogRequestLogging</c>,
    /// con la plantilla de la ruta.
    /// </summary>
    private static readonly string[] _queryBearingSources =
    [
        "Microsoft.AspNetCore.Hosting.Diagnostics",
        "Microsoft.AspNetCore.HttpLogging",
        "System.Net.Http.HttpClient",
        "Yarp.ReverseProxy.Forwarder",
    ];

    /// <summary>
    /// Verdadero para un evento de esas fuentes (o de sus subcategorías) con nivel menor a <c>Warning</c>. El filtro va en código, después de leer la configuración:
    /// ningún nivel de <c>appsettings</c> ni variable de entorno vuelve a habilitar esos eventos. Los de <c>Warning</c> o más pasan por la redacción del formateador.
    /// </summary>
    internal static bool IsQueryBearingBelowWarning(LogEvent logEvent)
    {
        ArgumentNullException.ThrowIfNull(logEvent);
        return logEvent.Level < LogEventLevel.Warning
            && logEvent.Properties.TryGetValue("SourceContext", out var source)
            && source is ScalarValue { Value: string name }
            && _queryBearingSources.Any(prefix => name.StartsWith(prefix, StringComparison.Ordinal));
    }

    /// <summary>
    /// Plantilla de la ruta con los literales y la versión de la API (<c>/api/v1/x/{id}</c>); los demás parámetros quedan como <c>{nombre}</c>.
    /// También es el <c>url.path</c> de los spans de servidor (TT-OBS-01).
    /// </summary>
    internal static string RoutePath(HttpContext context)
    {
        var (current, routeValues) = OriginalRoute(context);
        if (current is not RouteEndpoint endpoint)
        {
            return UnmatchedRoute;
        }

        var path = new StringBuilder();
        foreach (var segment in endpoint.RoutePattern.PathSegments)
        {
            path.Append('/');
            foreach (var part in segment.Parts)
            {
                switch (part)
                {
                    case RoutePatternLiteralPart literal:
                        path.Append(literal.Content);
                        break;
                    case RoutePatternParameterPart parameter:
                        path.Append(IsVersion(parameter.Name, routeValues, out var version) ? version : $"{{{parameter.Name}}}");
                        break;
                    default:
                        break;
                }
            }
        }

        return path.Length == 0 ? "/" : path.ToString();
    }

    /// <summary>Tras una excepción, <c>UseExceptionHandler</c> vuelve a ejecutar sin endpoint: la ruta original queda en <see cref="IExceptionHandlerFeature"/>.</summary>
    private static (Endpoint? Endpoint, RouteValueDictionary RouteValues) OriginalRoute(HttpContext context)
    {
        if (context.GetEndpoint() is { } endpoint)
        {
            return (endpoint, context.Request.RouteValues);
        }

        var handled = context.Features.Get<IExceptionHandlerFeature>();
        return (handled?.Endpoint, handled?.RouteValues ?? context.Request.RouteValues);
    }

    private static bool IsVersion(string parameterName, RouteValueDictionary values, out string version)
    {
        version = string.Empty;
        if (!string.Equals(parameterName, "version", StringComparison.Ordinal) || values[parameterName] is not string value)
        {
            return false;
        }

        var safe = value.Length is > 0 and <= 8 && value.All(c => char.IsAsciiLetterOrDigit(c) || c == '.');
        version = safe ? value : string.Empty;
        return safe;
    }
}

/// <summary>
/// El scope «Hosting» de ASP.NET Core agrega <c>RequestPath</c> con la ruta CON sus valores (coordenadas, ids) a cada log de la petición.
/// Se quita; el único <c>RequestPath</c> que se conserva es el del log de petición, que lleva la plantilla de la ruta.
/// </summary>
internal sealed class ScopeRequestPathEnricher : ILogEventEnricher
{
    private const string _requestLoggingSource = "Serilog.AspNetCore.RequestLoggingMiddleware";

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        ArgumentNullException.ThrowIfNull(logEvent);
        var fromRequestLogging = logEvent.Properties.TryGetValue("SourceContext", out var source)
            && source is ScalarValue { Value: _requestLoggingSource };
        if (!fromRequestLogging)
        {
            logEvent.RemovePropertyIfPresent("RequestPath");
        }
    }
}
// jscpd:ignore-end -- EXC-007
