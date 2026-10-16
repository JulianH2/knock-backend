namespace Knock.IntegrationTest.Support;

/// <summary>Captura lo que se escribe en la consola (sink JSON de Serilog) mientras corre la acción.</summary>
internal static class ConsoleCapture
{
    public static async Task<string> RunAsync(Func<Task> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        var original = Console.Out;
        using var captured = new StringWriter();
        try
        {
            Console.SetOut(TextWriter.Synchronized(captured));
            await action();
            return captured.ToString();
        }
        finally
        {
            Console.SetOut(original);
        }
    }
}
