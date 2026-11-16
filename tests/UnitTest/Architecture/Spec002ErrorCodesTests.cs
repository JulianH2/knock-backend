using System.Reflection;
using System.Text.RegularExpressions;
using AwesomeAssertions;
using Knock.Identity.Api.Modules.Sesiones.Services;
using Knock.Market.Api.Common.Errors;
using Xunit;

namespace Knock.UnitTest.Architecture;

/// <summary>
/// Spec 002, CA-07: los <c>code</c> de error de todos los servicios, declarados como constantes <c>*Code</c>, están en inglés y en <c>snake_case</c> y no se repiten.
/// Market declara los de transporte (R-11 y R-09) en <see cref="ErrorCodes"/>.
/// </summary>
public partial class Spec002ErrorCodesTests
{
    private static List<(string Owner, string Value)> Codes() =>
        [.. new[] { typeof(LoginService).Assembly, ModuleCatalog.ApiAssembly }
            .SelectMany(assembly => assembly.GetTypes())
            .SelectMany(type => type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Where(field => field is { IsLiteral: true, IsInitOnly: false } && field.FieldType == typeof(string) && field.Name.EndsWith("Code", StringComparison.Ordinal))
                .Select(field => (Owner: $"{type.FullName}.{field.Name}", Value: (string)field.GetRawConstantValue()!)))];

    /// <summary>
    /// Códigos que dos servicios devuelven con el mismo significado: <c>colonia_not_found</c> lo da Identidad al guardar la colonia del perfil (spec 017) y Mercado al crear una
    /// solicitud (spec 020, §5). La app lee un solo nombre para el mismo error; cualquier otro repetido sigue prohibido.
    /// </summary>
    private static readonly string[] _sharedAcrossServices = ["colonia_not_found"];

    [GeneratedRegex("^[a-z]+(_[a-z]+)*$", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 200)]
    private static partial Regex SnakeCase();

    [Fact]
    public void ErrorCodes_AreSnakeCaseEnglish_AndUnique_CA_002_07()
    {
        var codes = Codes();

        codes.Should().NotBeEmpty("si no se encuentra ninguna constante la regla pasaría en vacío");
        codes.Where(code => !SnakeCase().IsMatch(code.Value)).Select(code => code.Owner).Should().BeEmpty();
        codes.GroupBy(code => code.Value).Where(group => group.Count() > 1).Select(group => group.Key).Except(_sharedAcrossServices).Should().BeEmpty();
    }

    [Fact]
    public void ASharedCodeMeansTheSameInEveryService_AndIsNeverDeclaredTwiceInOneService_CA_002_07()
    {
        foreach (var shared in _sharedAcrossServices)
        {
            var owners = Codes().Where(code => code.Value == shared).Select(code => code.Owner).ToList();

            owners.Should().HaveCount(2, "un código compartido está declarado una vez en cada servicio que lo usa");
            owners.Select(owner => owner.Split('.')[1]).Distinct().Should().HaveCount(2, "y esas dos declaraciones son de servicios distintos");
        }
    }

    [Fact]
    public void MarketDeclaresTheTransportCodes_R_08_R_09_R_11()
    {
        var values = Codes().Select(code => code.Value).ToList();

        values.Should().Contain([ErrorCodes.UnauthorizedCode, ErrorCodes.PayloadTooLargeCode, ErrorCodes.ValidationFailedCode, ErrorCodes.RateLimitedCode]);
        new[] { ErrorCodes.UnauthorizedCode, ErrorCodes.PayloadTooLargeCode, ErrorCodes.ValidationFailedCode, ErrorCodes.RateLimitedCode }
            .Should().Equal("unauthorized", "payload_too_large", "validation_failed", "rate_limited");
    }
}
