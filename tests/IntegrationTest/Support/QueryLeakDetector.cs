using System.Text.RegularExpressions;

namespace Knock.IntegrationTest.Support;

/// <summary>
/// Detector de la spec 035 (R-08): busca en lo que salió por consola los valores o los nombres de los parámetros de una consulta de prueba.
/// Se prueba aparte para demostrar que falla cuando la consulta sí sale (CA-04).
/// </summary>
internal static partial class QueryLeakDetector
{
    public const string Query = "?lat=19.4326&lon=-99.1332&q=fuga%20de%20agua&cp=42000&token=abc";

    /// <summary>Ajustes que dejan todos los niveles de Serilog en <c>Verbose</c>, incluidas las anulaciones por fuente.</summary>
    public static IReadOnlyDictionary<string, string?> VerboseSettings { get; } = new Dictionary<string, string?>
    {
        ["Serilog:MinimumLevel:Default"] = "Verbose",
        ["Serilog:MinimumLevel:Override:Microsoft"] = "Verbose",
        ["Serilog:MinimumLevel:Override:Microsoft.AspNetCore"] = "Verbose",
        ["Serilog:MinimumLevel:Override:Microsoft.AspNetCore.Hosting.Diagnostics"] = "Verbose",
        ["Serilog:MinimumLevel:Override:Microsoft.AspNetCore.HttpLogging"] = "Verbose",
        ["Serilog:MinimumLevel:Override:System"] = "Verbose",
        ["Serilog:MinimumLevel:Override:System.Net.Http.HttpClient"] = "Verbose",
        ["Serilog:MinimumLevel:Override:Yarp"] = "Verbose",
        ["Serilog:MinimumLevel:Override:Yarp.ReverseProxy.Forwarder"] = "Verbose",
    };

    /// <summary>Lo que encontró: cada valor o nombre de parámetro de la consulta que aparece en <paramref name="output"/>.</summary>
    public static IReadOnlyList<string> Find(string output)
    {
        ArgumentNullException.ThrowIfNull(output);
        var found = new List<string>();
        foreach (var match in ParameterName().Matches(output).Select(m => m.Value).Distinct(StringComparer.Ordinal))
        {
            found.Add(match);
        }

        foreach (var value in new[] { "19.4326", "99.1332", "fuga%20de%20agua", "fuga de agua", "cp=42000" })
        {
            if (output.Contains(value, StringComparison.Ordinal))
            {
                found.Add(value);
            }
        }

        return found;
    }

    [GeneratedRegex(@"(?<![A-Za-z0-9_])(lat|lon|q|cp|token)=", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 500)]
    private static partial Regex ParameterName();
}
