using System.Security.Cryptography;
using System.Text.RegularExpressions;
using AwesomeAssertions;
using Knock.Market.Api.Common.Crypto;
using Knock.Market.Api.Modules.Requests.Services;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using Xunit;

namespace Knock.UnitTest.Requests;

/// <summary>Spec 021, R-17: el código de visita sale cifrado, atado a su visita y con el vencimiento del día de la cita más 24 horas (llave sintética, solo en memoria).</summary>
public sealed class Spec021VisitCodeFactoryTests
{
    private static (VisitCodeFactory Factory, FieldCipher Cipher) Build()
    {
        var options = Options.Create(new FieldEncryptionOptions
        {
            ActiveVersion = "1",
            Keys = new Dictionary<string, string> { ["1"] = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)) },
        });
        var cipher = new FieldCipher(options);
        return (new VisitCodeFactory(cipher), cipher);
    }

    [Fact]
    public void Create_ReturnsAFourDigitCodeOnlyDecryptableForItsVisit_R_17()
    {
        var (factory, cipher) = Build();
        var visit = ObjectId.GenerateNewId();

        var code = factory.Create(visit, "2026-11-18");

        code.Encrypted.Should().StartWith("1.");
        Regex.IsMatch(code.Encrypted, @"^\d{4}$").Should().BeFalse();
        Regex.IsMatch(cipher.Decrypt(code.Encrypted, VisitCodeFactory.Context(visit)), @"^\d{4}$").Should().BeTrue();
        FluentActions.Invoking(() => cipher.Decrypt(code.Encrypted, VisitCodeFactory.Context(ObjectId.GenerateNewId()))).Should().Throw<CryptographicException>();
    }

    [Fact]
    public void Create_UsesDifferentCodesAndNoncesAcrossCalls_R_17()
    {
        var (factory, cipher) = Build();
        var visit = ObjectId.GenerateNewId();

        var codes = Enumerable.Range(0, 40).Select(_ => cipher.Decrypt(factory.Create(visit, "2026-11-18").Encrypted, VisitCodeFactory.Context(visit))).Distinct().ToList();

        codes.Count.Should().BeGreaterThan(10, "el generador es criptográfico, no constante");
    }

    [Fact]
    public void ExpiresAt_Is24HoursAfterTheEndOfTheAppointmentDayInMexicoCity_R_17()
    {
        // 2026-11-18 23:59:59 en Ciudad de México (UTC-6, sin horario de verano) = 2026-11-19 05:59:59 UTC; más 24 h.
        VisitCodeFactory.ExpiresAt("2026-11-18").Should().Be(new DateTime(2026, 11, 20, 5, 59, 59, DateTimeKind.Utc));
    }
}
