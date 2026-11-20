using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Knock.IntegrationTest.Support;

/// <summary>Guarda en memoria los eventos de log de un host (por id de evento), para que una prueba diga en su mensaje si ocurrió uno concreto.</summary>
internal sealed class CapturingLoggerProvider : ILoggerProvider
{
    private readonly ConcurrentQueue<(int EventId, string Message)> _events = new();

    public int Count(int eventId) => _events.Count(e => e.EventId == eventId);

    public string Messages(int eventId) => string.Join(" | ", _events.Where(e => e.EventId == eventId).Select(e => e.Message).Distinct());

    public ILogger CreateLogger(string categoryName) => new CapturingLogger(_events);

    public void Dispose()
    {
    }

    private sealed class CapturingLogger(ConcurrentQueue<(int EventId, string Message)> events) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            ArgumentNullException.ThrowIfNull(formatter);
            events.Enqueue((eventId.Id, formatter(state, exception) + (exception is null ? string.Empty : $" ({exception.GetType().Name}: {exception.Message})")));
        }
    }
}
