using AwesomeAssertions;
using Knock.Api.Modules.Identity.Dtos;
using Knock.Api.Modules.Identity.Validators;
using Xunit;

namespace Knock.UnitTest.Identity;

public class ProfileRequestValidatorTests
{
    private static readonly ProfileRequestValidator _validator = new();

    private static IReadOnlyList<string> FieldsWithErrors(string? name) =>
        [.. _validator.Validate(new ProfileRequest(name)).Errors.Select(e => e.PropertyName).Distinct()];

    public static TheoryData<string?> InvalidNames() =>
    [
        null,
        string.Empty,
        "   ",
        "J",
        " J ",
        new string('a', 61),
        "José123",
        "José_Pérez",
        "José@Pérez",
        "José.Pérez",
        "José\tPérez",
        "José\nPérez",
        "José\u0000Pérez",
        "José​Pérez",
        "José­Pérez",
        "José‮Pérez",
        "-- --",
        "'-'",
        "😀😀",
        "<script>alert(1)</script>",
        "Robert'); DROP TABLE usuarios;--",
        "\ud83d",
    ];

    [Theory]
    [MemberData(nameof(InvalidNames))]
    public void InvalidNames_ProduceFieldError_TC_HU_002_03(string? name)
    {
        FieldsWithErrors(name).Should().Equal("Nombre");
    }

    [Fact]
    public void ErrorMessagesNeverRepeatTheValue_TC_HU_002_03()
    {
        const string sent = "SECRETO-1234";

        var messages = _validator.Validate(new ProfileRequest(sent)).Errors.Select(e => e.ErrorMessage);

        messages.Should().NotBeEmpty().And.OnlyContain(message => !message.Contains(sent, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void SixtyCharacters_AreAccepted_TC_HU_002_03()
    {
        FieldsWithErrors(new string('a', 60)).Should().BeEmpty();
    }

    [Fact]
    public void TwoCharacters_AreAccepted_TC_HU_002_03()
    {
        FieldsWithErrors("Li").Should().BeEmpty();
    }

    [Theory]
    [InlineData("José Pérez")]
    [InlineData("María-José")]
    [InlineData("D'Angelo")]
    [InlineData("D’Angelo")]
    [InlineData("Núñez")]
    [InlineData("Ñandú Güemes")]
    [InlineData("  José   Pérez ")]
    [InlineData("José Pérez")]
    [InlineData("Åsa Öberg")]
    [InlineData("Wang Xiǎomíng")]
    public void RealWorldNamesAreAccepted_TC_HU_002_04(string name)
    {
        FieldsWithErrors(name).Should().BeEmpty();
    }
}
