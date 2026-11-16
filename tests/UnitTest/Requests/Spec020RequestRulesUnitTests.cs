using System.Security.Cryptography;
using AwesomeAssertions;
using FluentValidation.TestHelper;
using Knock.Market.Api.Common.Crypto;
using Knock.Market.Api.Common.Errors;
using Knock.Market.Api.Modules.Requests.Domain;
using Knock.Market.Api.Modules.Requests.Dtos;
using Knock.Market.Api.Modules.Requests.Validators;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using Xunit;

namespace Knock.UnitTest.Requests;

/// <summary>Spec 020, T-05 y T-06: cifrado de campo, cursor y validadores de formato de las solicitudes. Con datos ficticios y un reloj fijo.</summary>
public sealed class Spec020RequestRulesUnitTests
{
    private sealed class FixedTime(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private static FieldCipher Cipher(string? active = "1", params (string Version, byte[] Key)[] keys) =>
        new(Options.Create(new FieldEncryptionOptions
        {
            ActiveVersion = active,
            Keys = keys.Length == 0
                ? new Dictionary<string, string> { ["1"] = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)) }
                : keys.ToDictionary(k => k.Version, k => Convert.ToBase64String(k.Key)),
        }));

    [Fact]
    public void Cipher_RoundTrips_AndDoesNotLeakThePlaintext_R_06()
    {
        var cipher = Cipher();

        var stored = cipher.Encrypt("Calle Ficticia 123", "solicitudes|direccion|a");

        stored.Should().StartWith("1.").And.NotContain("Calle").And.NotContain("Ficticia");
        cipher.Decrypt(stored, "solicitudes|direccion|a").Should().Be("Calle Ficticia 123");
        cipher.Encrypt("Calle Ficticia 123", "solicitudes|direccion|a").Should().NotBe(stored, "cada cifrado usa un nonce nuevo");
    }

    [Fact]
    public void Cipher_ValueCopiedToAnotherDocumentOrTampered_DoesNotDecrypt_R_06()
    {
        var cipher = Cipher();
        var stored = cipher.Encrypt("Calle Ficticia 123", "solicitudes|direccion|a");
        var tampered = stored[..^2] + (stored[^2] == 'A' ? "B" : "A") + stored[^1];

        var anotherDocument = () => cipher.Decrypt(stored, "solicitudes|direccion|b");
        var changedBytes = () => cipher.Decrypt(tampered, "solicitudes|direccion|a");

        anotherDocument.Should().Throw<CryptographicException>();
        changedBytes.Should().Throw<CryptographicException>();
    }

    [Fact]
    public void Cipher_OldKeyVersionStillDecrypts_AfterRotation_R_06()
    {
        var oldKey = RandomNumberGenerator.GetBytes(32);
        var newKey = RandomNumberGenerator.GetBytes(32);
        var stored = Cipher("1", ("1", oldKey)).Encrypt("Calle Ficticia 123", "ctx");

        var rotated = Cipher("2", ("1", oldKey), ("2", newKey));

        rotated.Decrypt(stored, "ctx").Should().Be("Calle Ficticia 123");
        rotated.Encrypt("otro", "ctx").Should().StartWith("2.");
    }

    [Fact]
    public void Cipher_WithoutAKey_FailsWhenUsed_NotSilently_R_06()
    {
        var cipher = Cipher(active: null);

        var act = () => cipher.Encrypt("Calle Ficticia 123", "ctx");

        act.Should().Throw<InvalidOperationException>().WithMessage("*not configured*");
    }

    [Fact]
    public void Cursor_RoundTrips_R_09()
    {
        var id = ObjectId.GenerateNewId();
        var when = new DateTime(2026, 11, 13, 18, 30, 0, 123, DateTimeKind.Utc);

        var (decodedWhen, decodedId) = RequestCursor.Decode(RequestCursor.Encode(when, id));

        decodedWhen.Should().Be(when);
        decodedId.Should().Be(id);
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("!!!")]
    [InlineData("MTIzNDU")]
    public void Cursor_Garbage_Is400InvalidCursor_R_09(string cursor)
    {
        var act = () => RequestCursor.Decode(cursor);

        var error = act.Should().Throw<DomainException>().Which;
        error.Kind.Should().Be(DomainErrorKind.BadRequest);
        error.Code.Should().Be("invalid_cursor");
        error.Message.Should().NotContain(cursor.Length > 0 ? cursor : "\0", "no repite lo recibido");
    }

    private static CreateRequestBodyValidator Validator(DateTimeOffset now) => new(new FixedTime(now));

    private static CreateRequestBody Body(string? dia = "2026-11-14", string? descripcion = "Se rompió el tubo debajo del fregadero", string[]? fotos = null) =>
        new("plo", descripcion, fotos ?? [ObjectId.GenerateNewId().ToString()], dia, "tarde", ObjectId.GenerateNewId().ToString(), null);

    [Fact]
    public void Validator_AcceptsAValidBody_R_01()
    {
        // 2026-11-14 00:30 UTC es todavía 13 de noviembre en Ciudad de México.
        var result = Validator(new DateTimeOffset(2026, 11, 14, 0, 30, 0, TimeSpan.Zero)).TestValidate(Body());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("2026-11-12", false)]
    [InlineData("2026-11-13", true)]
    [InlineData("2026-11-18", true)]
    [InlineData("2026-11-19", false)]
    [InlineData("13/11/2026", false)]
    [InlineData("2026-13-01", false)]
    [InlineData("", false)]
    public void Validator_DayIsFromTodayToTodayPlusFive_InMexicoCityTime_R_05(string day, bool valid)
    {
        // A las 18:00 UTC del 13 es mediodía en Ciudad de México: hoy es el 13.
        var result = Validator(new DateTimeOffset(2026, 11, 13, 18, 0, 0, TimeSpan.Zero)).TestValidate(Body(dia: day));

        if (valid)
        {
            result.ShouldNotHaveValidationErrorFor(x => x.Dia);
        }
        else
        {
            result.ShouldHaveValidationErrorFor(x => x.Dia).WithErrorMessage(CreateRequestBodyValidator.FormatReason);
        }
    }

    [Fact]
    public void Validator_TodayInMexicoCityIsYesterdayOfUtcLateAtNight_R_05()
    {
        // 03:00 UTC del 14 son las 21:00 del 13 en Ciudad de México: ayer (el 12) ya no es válido y el 13 sí.
        var clock = new DateTimeOffset(2026, 11, 14, 3, 0, 0, TimeSpan.Zero);

        Validator(clock).TestValidate(Body(dia: "2026-11-13")).ShouldNotHaveValidationErrorFor(x => x.Dia);
        Validator(clock).TestValidate(Body(dia: "2026-11-12")).ShouldHaveValidationErrorFor(x => x.Dia);
    }

    [Theory]
    [InlineData("1234567", false)]
    [InlineData("12345678", true)]
    [InlineData("   1234567   ", false)]
    public void Validator_DescriptionHasBetween8And500Characters_R_03(string description, bool valid)
    {
        var result = Validator(new DateTimeOffset(2026, 11, 13, 18, 0, 0, TimeSpan.Zero)).TestValidate(Body(dia: "2026-11-14", descripcion: description));

        if (valid)
        {
            result.ShouldNotHaveValidationErrorFor(x => x.Descripcion);
        }
        else
        {
            result.ShouldHaveValidationErrorFor(x => x.Descripcion);
        }

        Validator(new DateTimeOffset(2026, 11, 13, 18, 0, 0, TimeSpan.Zero)).TestValidate(Body(dia: "2026-11-14", descripcion: new string('a', 501))).ShouldHaveValidationErrorFor(x => x.Descripcion);
    }

    [Fact]
    public void Validator_PhotosAreOneToFiveObjectIdsWithoutRepeats_R_04()
    {
        var clock = new DateTimeOffset(2026, 11, 13, 18, 0, 0, TimeSpan.Zero);
        var one = ObjectId.GenerateNewId().ToString();

        Validator(clock).TestValidate(Body(fotos: [])).ShouldHaveValidationErrorFor(x => x.Fotos);
        Validator(clock).TestValidate(Body(fotos: [.. Enumerable.Range(0, 6).Select(_ => ObjectId.GenerateNewId().ToString())])).ShouldHaveValidationErrorFor(x => x.Fotos);
        Validator(clock).TestValidate(Body(fotos: [one, one.ToUpperInvariant()])).ShouldHaveValidationErrorFor(x => x.Fotos);
        Validator(clock).TestValidate(Body(fotos: ["no-es-un-id"])).ShouldHaveValidationErrorFor(x => x.Fotos);
        Validator(clock).TestValidate(Body(fotos: [.. Enumerable.Range(0, 5).Select(_ => ObjectId.GenerateNewId().ToString())])).ShouldNotHaveValidationErrorFor(x => x.Fotos);
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(1, false)]
    [InlineData(50, false)]
    [InlineData(51, true)]
    public void ListQueryValidator_LimitIsOneToFifty_R_09(int limit, bool invalid)
    {
        var result = new ListRequestsQueryValidator().TestValidate(new ListRequestsQuery { Limit = limit });

        if (invalid)
        {
            result.ShouldHaveValidationErrorFor(x => x.Limit);
        }
        else
        {
            result.ShouldNotHaveValidationErrorFor(x => x.Limit);
        }
    }

    [Theory]
    [InlineData("enviada", true)]
    [InlineData("cita_confirmada", true)]
    [InlineData("vencida", true)]
    [InlineData("inventado", false)]
    [InlineData("Enviada", false)]
    public void ListQueryValidator_StateIsOneOfTheSevenValues_R_09(string state, bool valid)
    {
        var result = new ListRequestsQueryValidator().TestValidate(new ListRequestsQuery { Estado = state });

        if (valid)
        {
            result.ShouldNotHaveValidationErrorFor(x => x.Estado);
        }
        else
        {
            result.ShouldHaveValidationErrorFor(x => x.Estado);
        }
    }
}
