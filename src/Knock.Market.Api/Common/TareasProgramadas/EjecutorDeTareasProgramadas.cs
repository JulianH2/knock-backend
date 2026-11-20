namespace Knock.Market.Api.Common.TareasProgramadas;

internal interface IManejadorDeTarea
{
    string Tipo { get; }
    Task HandleAsync(TareaReservada task, CancellationToken cancellationToken);
}

internal sealed partial class EjecutorDeTareasProgramadas(
    ITareasProgramadasStore store,
    IEnumerable<IManejadorDeTarea> handlers,
    TimeProvider clock,
    ILogger<EjecutorDeTareasProgramadas> logger)
{
    private readonly Dictionary<string, IManejadorDeTarea> _handlers = handlers.ToDictionary(x => x.Tipo, StringComparer.Ordinal);

    public async Task<bool> ExecuteOneAsync(CancellationToken cancellationToken)
    {
        if (_handlers.Count == 0)
        {
            return false;
        }

        var task = await store.ReserveAsync(clock.GetUtcNow().UtcDateTime, cancellationToken, _handlers.Keys.ToArray());
        if (task is null)
        {
            return false;
        }

        try
        {
            await _handlers[task.Tipo].HandleAsync(task, cancellationToken);
            if (!await store.CompleteAsync(task, clock.GetUtcNow().UtcDateTime, cancellationToken))
            {
                LeaseLost(logger, task.Id.ToString());
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception error) when (error is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            await RecordFailureAsync(task, cancellationToken);
        }

        return true;
    }

    private async Task RecordFailureAsync(TareaReservada task, CancellationToken cancellationToken)
    {
        if (!await store.FailAsync(task, clock.GetUtcNow().UtcDateTime, cancellationToken))
        {
            LeaseLost(logger, task.Id.ToString());
        }
        else if (task.Intentos + 1 >= 5)
        {
            TaskFailed(logger, task.Tipo, task.Id.ToString());
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Scheduled task lease lost: {TaskId}")]
    private static partial void LeaseLost(ILogger logger, string taskId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Scheduled task failed: {TaskType} {TaskId}")]
    private static partial void TaskFailed(ILogger logger, string taskType, string taskId);
}
