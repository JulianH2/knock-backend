using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.AspNetCore.Diagnostics;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog.Core;
using Serilog.Events;

namespace Knock.Api.Hosting;

/// <summary>Opciones sin secretos de <c>Observability:*</c> (TT-OBS-01). La cadena de conexión nunca vive aquí.</summary>
internal sealed class ObservabilityOptions
{
    public const string SectionName = "Observability";

    public string ServiceName { get; set; } = "knock-api";

    /// <summary>Fracción de trazas que se envían: 1.0 (100 %) con el tope diario del recurso (decisión P7).</summary>
    public float SamplingRatio { get; set; } = 1.0f;

    /// <summary>Live Metrics apagado (decisión P7).</summary>
    public bool EnableLiveMetrics { get; set; }
}

/// <summary>Valores con los que el exportador de Azure Monitor construyó una señal.</summary>
internal sealed record AzureMonitorSignalSettings(bool DisableOfflineStorage, bool EnableLiveMetrics, float SamplingRatio, double? TracesPerSecond);

/// <summary>Qué señales se exportan a Azure Monitor: cada exportador se anota al construirse (lo leen las pruebas y el diagnóstico).</summary>
internal sealed class AzureMonitorExportStatus
{
    private readonly ConcurrentDictionary<string, AzureMonitorSignalSettings> _signals = new(StringComparer.Ordinal);

    public bool Enabled { get; init; }

    public IReadOnlyDictionary<string, AzureMonitorSignalSettings> Signals => _signals;

    public void Record(string signal, AzureMonitorExporterOptions options) =>
        _signals[signal] = new AzureMonitorSignalSettings(options.DisableOfflineStorage, options.EnableLiveMetrics, options.SamplingRatio, options.TracesPerSecond);
}

internal static class ObservabilityExtensions
{
    /// <summary>Única fuente de la cadena de conexión: variable de entorno o referencia de Key Vault en Container Apps.</summary>
    public const string ConnectionStringKey = "APPLICATIONINSIGHTS_CONNECTION_STRING";

    public const string CorrelationIdAttribute = "knock.correlation_id";
    public const string TracesSignal = "traces";
    public const string MetricsSignal = "metrics";
    public const string LogsSignal = "logs";

    /// <summary>
    /// OpenTelemetry siempre (trazas, métricas y logs con redacción) y los exportadores de Azure Monitor solo si hay cadena de conexión.
    /// La tubería es la misma en todos los entornos; las pruebas solo agregan un exportador en memoria (decisión previa 3).
    /// </summary>
    public static IServiceCollection AddKnockObservability(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);
        var options = configuration.GetSection(ObservabilityOptions.SectionName).Get<ObservabilityOptions>() ?? new ObservabilityOptions();
        if (options.SamplingRatio is < 0f or > 1f || string.IsNullOrWhiteSpace(options.ServiceName))
        {
            throw new InvalidOperationException("Observability:SamplingRatio must be between 0 and 1 and Observability:ServiceName is required.");
        }

        var connectionString = configuration[ConnectionStringKey];
        var status = new AzureMonitorExportStatus { Enabled = !string.IsNullOrWhiteSpace(connectionString) };
        services.AddSingleton(status);
        // Decisión P9: ASP.NET Core no toma el contexto de traza ni el baggage del cliente, y OpenTelemetry tampoco los vuelve a
        // extraer (solo lo hace si el propagador por defecto no es W3C Trace Context). El traceparent saliente no cambia.
        services.AddSingleton<DistributedContextPropagator>(new IgnoreIncomingTraceContextPropagator());
        Sdk.SetDefaultTextMapPropagator(new TraceContextPropagator());
        // Después del manejador de dominio (AddKnockProblemDetails): solo anota en el span las excepciones que terminan en 500.
        services.AddExceptionHandler<TelemetryExceptionRecorder>();

        void Configure(string signal, AzureMonitorExporterOptions exporter)
        {
            exporter.ConnectionString = connectionString;
            exporter.DisableOfflineStorage = true;
            exporter.EnableLiveMetrics = options.EnableLiveMetrics;
            // El exportador limita a 5 trazas por segundo si TracesPerSecond tiene valor; se quita para enviar el SamplingRatio.
            exporter.SamplingRatio = options.SamplingRatio;
            exporter.TracesPerSecond = null;
            status.Record(signal, exporter);
        }

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(options.ServiceName, serviceVersion: ServiceVersion())
                .AddAttributes([new KeyValuePair<string, object>("deployment.environment", environment.EnvironmentName)]))
            .WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation(ConfigureServerSpans)
                    .AddHttpClientInstrumentation(http => http.RecordException = false)
                    .AddProcessor(new TelemetryRedactionProcessor());
                if (status.Enabled)
                {
                    tracing.AddAzureMonitorTraceExporter(exporter => Configure(TracesSignal, exporter));
                }
            })
            .WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation().AddMeter("System.Runtime");
                if (status.Enabled)
                {
                    metrics.AddAzureMonitorMetricExporter(exporter => Configure(MetricsSignal, exporter));
                }
            })
            .WithLogging(
                logging =>
                {
                    logging.AddProcessor(new LogRedactionProcessor());
                    if (status.Enabled)
                    {
                        logging.AddAzureMonitorLogExporter(exporter => Configure(LogsSignal, exporter));
                    }
                },
                logs =>
                {
                    logs.IncludeFormattedMessage = true;
                    logs.IncludeScopes = false;
                });
        return services;
    }

    /// <summary>
    /// Sin spans de <c>/health*</c>; traza nueva siempre, aunque llegue un <c>traceparent</c> (decisión P9, hasta que exista el gateway);
    /// <c>url.path</c> con la plantilla de la ruta y sin <c>url.query</c>. Las excepciones las anota <see cref="TelemetryExceptionRecorder"/>.
    /// </summary>
    private static void ConfigureServerSpans(AspNetCoreTraceInstrumentationOptions server)
    {
        server.Filter = context => !context.Request.Path.StartsWithSegments(HealthExtensions.LivenessPath, StringComparison.OrdinalIgnoreCase);
        server.RecordException = false;
        server.EnrichWithHttpResponse = (activity, response) =>
        {
            activity.SetTag("url.path", LoggingExtensions.RoutePath(response.HttpContext));
            activity.SetTag("url.query", null);
        };
    }

    private static string ServiceVersion() =>
        typeof(ObservabilityExtensions).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? typeof(ObservabilityExtensions).Assembly.GetName().Version?.ToString()
        ?? "0.0.0";
}

/// <summary>
/// Anota en el span de la petición la excepción no controlada (tipo, mensaje y pila redactados) y no la maneja: la respuesta sigue
/// siendo la de <see cref="ProblemDetailsExtensions"/>. La instrumentación no ve las excepciones que atiende <c>UseExceptionHandler</c>.
/// </summary>
internal sealed class TelemetryExceptionRecorder : IExceptionHandler
{
    public ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(exception);
        if (Activity.Current is { } activity)
        {
            activity.AddEvent(TelemetryRedactor.ExceptionEvent(exception));
            activity.SetStatus(ActivityStatusCode.Error);
        }

        return ValueTask.FromResult(false);
    }
}

/// <summary>
/// Propagador de ASP.NET Core que ignora el <c>traceparent</c>, el <c>tracestate</c> y el <c>baggage</c> de la petición: cada petición
/// inicia una traza nueva (decisión P9). Inyectar usa el formato W3C por defecto.
/// </summary>
internal sealed class IgnoreIncomingTraceContextPropagator : DistributedContextPropagator
{
    private static readonly DistributedContextPropagator _default = CreateDefaultPropagator();

    public override IReadOnlyCollection<string> Fields => _default.Fields;

    public override void Inject(Activity? activity, object? carrier, PropagatorSetterCallback? setter) => _default.Inject(activity, carrier, setter);

    public override void ExtractTraceIdAndState(object? carrier, PropagatorGetterCallback? getter, out string? traceId, out string? traceState)
    {
        traceId = null;
        traceState = null;
    }

    public override IEnumerable<KeyValuePair<string, string?>>? ExtractBaggage(object? carrier, PropagatorGetterCallback? getter) => null;
}

/// <summary>
/// Reenvía a OpenTelemetry los eventos que ya pasaron los niveles y enriquecedores de Serilog (TT-API-03), con sus propiedades
/// como atributos y la excepción como <c>exception.*</c>. Si OpenTelemetry no está registrado, no hace nada.
/// </summary>
internal sealed class OpenTelemetryLogSink(IServiceProvider services) : ILogEventSink
{
    private const string _defaultCategory = "Knock.Api";

    private readonly Lazy<ILoggerProvider?> _provider = new(
        () => services.GetServices<ILoggerProvider>().OfType<OpenTelemetryLoggerProvider>().FirstOrDefault());

    private readonly ConcurrentDictionary<string, ILogger> _loggers = new(StringComparer.Ordinal);

    public void Emit(LogEvent logEvent)
    {
        ArgumentNullException.ThrowIfNull(logEvent);
        if (_provider.Value is not { } provider)
        {
            return;
        }

        var category = logEvent.Properties.TryGetValue("SourceContext", out var source) && source is ScalarValue { Value: string name }
            ? name
            : _defaultCategory;
        var logger = _loggers.GetOrAdd(category, provider.CreateLogger);
        var level = Level(logEvent.Level);
        if (logger.IsEnabled(level))
        {
            logger.Log(level, default, new SerilogLogState(logEvent), null, static (state, _) => state.Message);
        }
    }

    private static LogLevel Level(LogEventLevel level) => level switch
    {
        LogEventLevel.Verbose => LogLevel.Trace,
        LogEventLevel.Debug => LogLevel.Debug,
        LogEventLevel.Information => LogLevel.Information,
        LogEventLevel.Warning => LogLevel.Warning,
        LogEventLevel.Error => LogLevel.Error,
        _ => LogLevel.Critical,
    };
}

/// <summary>Estado de log con las propiedades del evento de Serilog, la excepción como atributos y la plantilla en <c>{OriginalFormat}</c>.</summary>
internal sealed class SerilogLogState : IReadOnlyList<KeyValuePair<string, object?>>
{
    private readonly List<KeyValuePair<string, object?>> _attributes;

    public SerilogLogState(LogEvent logEvent)
    {
        ArgumentNullException.ThrowIfNull(logEvent);
        _attributes = new List<KeyValuePair<string, object?>>(logEvent.Properties.Count + 4);
        foreach (var (name, value) in logEvent.Properties)
        {
            _attributes.Add(new(name, value is ScalarValue scalar ? scalar.Value : value.ToString()));
        }

        if (logEvent.Exception is { } exception)
        {
            _attributes.Add(new("exception.type", exception.GetType().FullName));
            _attributes.Add(new("exception.message", exception.Message));
            _attributes.Add(new("exception.stacktrace", exception.ToString()));
        }

        _attributes.Add(new("{OriginalFormat}", logEvent.MessageTemplate.Text));
        Message = logEvent.RenderMessage(CultureInfo.InvariantCulture);
    }

    public string Message { get; }

    public int Count => _attributes.Count;

    public KeyValuePair<string, object?> this[int index] => _attributes[index];

    public IEnumerator<KeyValuePair<string, object?>> GetEnumerator() => _attributes.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
