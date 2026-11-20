using System.Net;

namespace Knock.IntegrationTest.Support;

/// <summary>Resumen de una ráfaga de respuestas para el mensaje de una aserción: cuántas por código y el motivo de las que no son éxito.</summary>
internal sealed record ResponseSummary(IReadOnlyList<HttpStatusCode> Statuses, string Detail, bool EmulatorDeadlock = false)
{
    public int Count(HttpStatusCode status) => Statuses.Count(s => s == status);

    public override string ToString() =>
        string.Join(", ", Statuses.GroupBy(s => s).OrderBy(g => (int)g.Key).Select(g => $"{(int)g.Key}x{g.Count()}")) + (Detail.Length == 0 ? string.Empty : $"; {Detail}");

    /// <summary>Lee y descarta las respuestas: guarda los códigos y, de las de error, el texto de motivos (sin traceId ni instance).</summary>
    public static async Task<ResponseSummary> ReadAsync(IEnumerable<HttpResponseMessage> responses)
    {
        ArgumentNullException.ThrowIfNull(responses);
        var statuses = new List<HttpStatusCode>();
        var reasons = new List<string>();
        var deadlock = false;
        foreach (var response in responses)
        {
            using (response)
            {
                statuses.Add(response.StatusCode);
                if ((int)response.StatusCode >= 400 && response.StatusCode != HttpStatusCode.Conflict)
                {
                    var text = await response.Content.ReadAsStringAsync();
                    deadlock |= response.StatusCode == HttpStatusCode.InternalServerError && text.Contains("due to deadlock", StringComparison.Ordinal);
                    if (reasons.Count < 3)
                    {
                        reasons.Add($"{(int)response.StatusCode}: {Trim(ProblemBody.Reasons(text))}");
                    }
                }
            }
        }

        return new ResponseSummary(statuses, string.Join(" | ", reasons), deadlock);
    }

    /// <summary>
    /// Repite la ráfaga con datos nuevos mientras alguna respuesta sea el bloqueo mutuo (deadlock) del emulador documentdb-local, que lo devuelve como 500 bajo escrituras simultáneas
    /// sobre el mismo índice único. Es un artefacto del emulador (el servicio real contesta 11000); cualquier otro resultado, incluido un 500 distinto, se devuelve tal cual para que la aserción lo juzgue.
    /// </summary>
    public static async Task<ResponseSummary> WithoutEmulatorDeadlockAsync(Func<Task<ResponseSummary>> burst, int attempts = 5)
    {
        ArgumentNullException.ThrowIfNull(burst);
        var summary = await burst();
        for (var attempt = 1; attempt < attempts && summary.EmulatorDeadlock; attempt++)
        {
            summary = await burst();
        }

        return summary;
    }

    private static string Trim(string text) => text.Length <= 300 ? text : text[..300] + "...";
}
