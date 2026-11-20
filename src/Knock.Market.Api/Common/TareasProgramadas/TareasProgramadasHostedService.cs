using Microsoft.Extensions.Options;

namespace Knock.Market.Api.Common.TareasProgramadas;

internal sealed class TareasProgramadasOptions
{
    public int IntervaloSegundos { get; set; } = 15;
}

internal sealed partial class TareasProgramadasHostedService(
    IServiceScopeFactory scopes,
    IOptions<TareasProgramadasOptions> options,
    TimeProvider clock,
    ILogger<TareasProgramadasHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(options.Value.IntervaloSegundos), clock);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = scopes.CreateScope();
                await scope.ServiceProvider.GetRequiredService<EjecutorDeTareasProgramadas>().ExecuteOneAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception error) when (error is not OperationCanceledException || !stoppingToken.IsCancellationRequested)
            {
                ExecutorUnavailable(logger, error.GetType().Name);
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Scheduled task executor unavailable: {ErrorType}")]
    private static partial void ExecutorUnavailable(ILogger logger, string errorType);
}
