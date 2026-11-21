using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Knock.IntegrationTest.Support;

/// <summary>Guarda en memoria los eventos de log de un host (por id de evento), para que una prueba diga en su mensaje si ocurrió uno concreto.</summary>
internal sealed class CapturingLoggerProvider : ILoggerProvider
{
    private const int _maxErrorLength = 400;

    private readonly ConcurrentQueue<(int EventId, LogLevel Level, string Message)> _events = new();

    public int Count(int eventId) => _events.Count(e => e.EventId == eventId);

    public string Messages(int eventId) => string.Join(" | ", _events.Where(e => e.EventId == eventId).Select(e => e.Message).Distinct());

    /// <summary>Eventos de nivel Error o Critical (con el tipo y el mensaje de su excepción), sin repetir y acotados, para que una prueba que recibió un 500 diga por qué.</summary>
    public string Errors() => string.Join(" | ", _events.Where(e => e.Level >= LogLevel.Error)
        .Select(e => e.Message.Length > _maxErrorLength ? e.Message[.._maxErrorLength] : e.Message).Distinct().Take(5));

    public ILogger CreateLogger(string categoryName) => new CapturingLogger(_events);

    public void Dispose()
    {
    }

    private sealed class CapturingLogger(ConcurrentQueue<(int EventId, LogLevel Level, string Message)> events) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            ArgumentNullException.ThrowIfNull(formatter);
            events.Enqueue((eventId.Id, logLevel, formatter(state, exception) + (exception is null ? string.Empty : $" ({exception.GetType().Name}: {exception.Message})")));
        }
    }
}
