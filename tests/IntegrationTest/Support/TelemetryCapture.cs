using System.Collections;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Knock.IntegrationTest.Support;

/// <summary>
/// Exportador en memoria de trazas, métricas y logs (TT-OBS-01): recibe exactamente lo que recibiría Azure Monitor, después de la
/// redacción. Solo existe en el host de pruebas; la API nunca lo registra.
/// </summary>
internal sealed class TelemetryCapture
{
    private static readonly TimeSpan _timeout = TimeSpan.FromSeconds(10);

    private readonly LockedCollection<Activity> _spans = new();
    private readonly LockedCollection<LogRecord> _logs = new();
    private readonly LockedCollection<MetricSnapshot> _metrics = new();

    public IReadOnlyList<Activity> Spans => _spans.Snapshot();

    public IReadOnlyList<LogRecord> Logs => _logs.Snapshot();

    public IReadOnlyList<MetricSnapshot> Metrics => _metrics.Snapshot();

    public IReadOnlyList<Activity> ServerSpans => [.. Spans.Where(s => s.Kind == ActivityKind.Server)];

    public void Register(IServiceCollection services)
    {
        services.ConfigureOpenTelemetryTracerProvider(tracing => tracing.AddInMemoryExporter(_spans));
        services.ConfigureOpenTelemetryLoggerProvider(logging => logging.AddInMemoryExporter(_logs));
        services.ConfigureOpenTelemetryMeterProvider(metrics => metrics.AddInMemoryExporter(_metrics));
    }

    /// <summary>El span de servidor se exporta al cerrar la petición, un poco después de que el cliente recibe la respuesta.</summary>
    public async Task<IReadOnlyList<Activity>> WaitForServerSpansAsync(int count)
    {
        var deadline = DateTime.UtcNow + _timeout;
        while (ServerSpans.Count < count && DateTime.UtcNow < deadline)
        {
            await Task.Delay(20);
        }

        return ServerSpans.Count >= count
            ? ServerSpans
            : throw new TimeoutException($"Se esperaban {count} spans de servidor y llegaron {ServerSpans.Count}.");
    }

    /// <summary>Recoge las métricas acumuladas hasta ahora.</summary>
    public IReadOnlyList<MetricSnapshot> CollectMetrics(IServiceProvider services)
    {
        services.GetRequiredService<MeterProvider>().ForceFlush();
        return Metrics;
    }

    /// <summary>Lo exportado de un span completo (nombre, atributos, eventos, estado, enlaces y baggage) como texto, para buscar fugas.</summary>
    public static string Dump(Activity span)
    {
        ArgumentNullException.ThrowIfNull(span);
        var parts = new List<string> { span.DisplayName, span.OperationName, span.StatusDescription ?? string.Empty };
        parts.AddRange(span.TagObjects.Select(t => $"{t.Key}={t.Value}"));
        parts.AddRange(span.Events.SelectMany(e => e.Tags.Select(t => $"{e.Name}:{t.Key}={t.Value}")));
        parts.AddRange(span.Links.SelectMany(l => (l.Tags ?? []).Select(t => $"link:{t.Key}={t.Value}")));
        parts.AddRange(span.Baggage.Select(b => $"baggage:{b.Key}={b.Value}"));
        return string.Join('\n', parts);
    }

    /// <summary>Lo exportado de un log completo (categoría, cuerpo, mensaje, atributos y scopes) como texto, para buscar fugas.</summary>
    public static string Dump(LogRecord log)
    {
        ArgumentNullException.ThrowIfNull(log);
        var parts = new List<string> { log.CategoryName ?? string.Empty, log.Body ?? string.Empty, log.FormattedMessage ?? string.Empty };
        parts.AddRange((log.Attributes ?? []).Select(a => $"{a.Key}={a.Value}"));
        log.ForEachScope(
            static (scope, list) =>
            {
                foreach (var item in scope)
                {
                    list.Add($"scope:{item.Key}={item.Value}");
                }
            },
            parts);
        parts.Add(log.Exception?.ToString() ?? string.Empty);
        return string.Join('\n', parts);
    }

    /// <summary>Valor de un atributo de un log, o <see langword="null"/>.</summary>
    public static object? Attribute(LogRecord log, string key)
    {
        ArgumentNullException.ThrowIfNull(log);
        return (log.Attributes ?? []).FirstOrDefault(a => a.Key == key).Value;
    }

    /// <summary>Lista con bloqueo: los exportadores escriben desde los hilos de las peticiones y del lector de métricas.</summary>
    private sealed class LockedCollection<T> : ICollection<T>
    {
        private readonly List<T> _items = [];
        private readonly Lock _lock = new();

        public int Count
        {
            get
            {
                lock (_lock)
                {
                    return _items.Count;
                }
            }
        }

        public bool IsReadOnly => false;

        public IReadOnlyList<T> Snapshot()
        {
            lock (_lock)
            {
                return [.. _items];
            }
        }

        public void Add(T item)
        {
            lock (_lock)
            {
                _items.Add(item);
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                _items.Clear();
            }
        }

        public bool Contains(T item)
        {
            lock (_lock)
            {
                return _items.Contains(item);
            }
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            lock (_lock)
            {
                _items.CopyTo(array, arrayIndex);
            }
        }

        public bool Remove(T item)
        {
            lock (_lock)
            {
                return _items.Remove(item);
            }
        }

        public IEnumerator<T> GetEnumerator() => Snapshot().GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
