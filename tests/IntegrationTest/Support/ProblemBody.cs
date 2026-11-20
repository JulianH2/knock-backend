using System.Text.Json;

namespace Knock.IntegrationTest.Support;

/// <summary>
/// El texto de un error donde buscar valores que nunca deben repetirse. El cuerpo completo trae <c>traceId</c> y <c>correlationId</c>,
/// aleatorios y a veces con ese mismo valor ("123", "hi"), y la prueba fallaba de forma intermitente: aquí van solo los motivos.
/// </summary>
internal static class ProblemBody
{
    private static readonly string[] _randomIds = ["traceId", "correlationId"];

    /// <summary>La sección <c>errors</c> si existe; si no, el cuerpo sin <c>traceId</c> ni <c>correlationId</c> (y el texto tal cual si no es un objeto JSON).</summary>
    public static string Reasons(string raw)
    {
        ArgumentNullException.ThrowIfNull(raw);
        try
        {
            using var document = JsonDocument.Parse(raw);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return raw;
            }

            if (document.RootElement.TryGetProperty("errors", out var errors))
            {
                return errors.GetRawText();
            }

            var kept = document.RootElement.EnumerateObject().Where(p => !_randomIds.Contains(p.Name, StringComparer.Ordinal));
            return JsonSerializer.Serialize(kept.ToDictionary(p => p.Name, p => p.Value.Clone(), StringComparer.Ordinal));
        }
        catch (JsonException)
        {
            return raw;
        }
    }
}
