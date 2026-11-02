using AwesomeAssertions;
using Knock.IntegrationTest.Support;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Knock.SecurityTest.Logging;

/// <summary>
/// Única barrera (TT-API-07, decisión (i)) contra «Request starting/finished» con la query: con <c>Information</c>, ASP.NET Core
/// registra la URL completa (coordenadas incluidas). Lee la configuración efectiva del host, sin bajar niveles salvo en los datos negativos.
/// </summary>
public sealed class AspNetCoreLogLevelGuardTests
{
    private const string _category = "Microsoft.AspNetCore.Hosting.Diagnostics";
    private const string _overrideSection = "Serilog:MinimumLevel:Override";

    [Theory]
    [InlineData("Production", null, null)]
    [InlineData("Development", null, null)]
    [InlineData("Production", "Microsoft.AspNetCore", "Information")]
    [InlineData("Development", "Microsoft.AspNetCore", "Information")]
    [InlineData("Production", "Microsoft.AspNetCore.Hosting", "Debug")]
    public void HostingDiagnosticsStaysAtWarningOrAbove_TC_TT_API_07_16(string environment, string? loweredOverride, string? level)
    {
        var settings = loweredOverride is null ? null : new Dictionary<string, string?> { [$"{_overrideSection}:{loweredOverride}"] = level };
        using var factory = new KnockApiFactory(environment, settings: settings);

        var failure = Check(factory.Services);

        if (loweredOverride is null)
        {
            failure.Should().BeNull($"la configuración de {environment} mantiene {_category} en Warning o más");
        }
        else
        {
            failure.Should().NotBeNull("bajar el override debe hacer fallar la guarda")
                .And.Contain($"{_overrideSection}:{loweredOverride}", "el mensaje nombra la clave que lo bajó");
        }
    }

    /// <summary><see langword="null"/> si el nivel efectivo es Warning o más; si no, un mensaje con la clave que aplica.</summary>
    private static string? Check(IServiceProvider services)
    {
        var effective = services.GetRequiredService<ILoggerFactory>().CreateLogger(_category);
        if (!effective.IsEnabled(LogLevel.Information))
        {
            return null;
        }

        var overrides = services.GetRequiredService<IConfiguration>().GetSection(_overrideSection).GetChildren()
            .Where(o => _category == o.Key || _category.StartsWith(o.Key + ".", StringComparison.Ordinal))
            .OrderByDescending(o => o.Key.Length)
            .FirstOrDefault();
        var key = overrides is null ? "Serilog:MinimumLevel:Default" : $"{_overrideSection}:{overrides.Key}";
        return $"{key} lets {_category} log at Information: \"Request starting/finished\" would log the query string. Keep it at Warning or higher.";
    }
}
