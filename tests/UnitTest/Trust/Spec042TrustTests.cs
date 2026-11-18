using AwesomeAssertions;
using Knock.Market.Api.Common.Errors;
using Knock.Market.Api.Modules.Trust.Domain;
using Knock.Market.Api.Modules.Trust.Dtos;
using Knock.Market.Api.Modules.Trust.Validators;
using MongoDB.Bson;
using Xunit;

namespace Knock.UnitTest.Trust;

/// <summary>Spec 042, R-02, R-04 y R-07: el cursor de la lista y los validadores de la consulta y de la ruta.</summary>
public sealed class Spec042TrustTests
{
    private static readonly DateTime _saved = new(2026, 11, 18, 15, 30, 0, DateTimeKind.Utc);

    [Fact]
    public void Cursor_RoundTripsTheSavedDateAndTheRecordId_R_02()
    {
        var id = ObjectId.GenerateNewId();

        var decoded = TrustCursor.Decode(TrustCursor.Encode(_saved, id));

        decoded.SavedAtUtc.Should().Be(_saved);
        decoded.SavedAtUtc.Kind.Should().Be(DateTimeKind.Utc);
        decoded.Id.Should().Be(id);
    }

    [Theory]
    [InlineData("esto-no-es-un-cursor")]
    [InlineData("____")]
    [InlineData("MTIzNDU")]
    [InlineData("")]
    public void Cursor_Unreadable_Throws400WithItsCode_R_02(string cursor)
    {
        Action act = () => TrustCursor.Decode(cursor);

        act.Should().Throw<Exception>().Which.Should().Match<Exception>(e => e is DomainException || e is ArgumentException);
    }

    [Fact]
    public void Cursor_WellFormedButWithBadParts_IsRejected_R_02()
    {
        var forged = Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlEncode(System.Text.Encoding.UTF8.GetBytes("-5;no-es-un-id"));

        Action act = () => TrustCursor.Decode(forged);

        act.Should().Throw<DomainException>().Which.Code.Should().Be("invalid_cursor");
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(20, true)]
    [InlineData(50, true)]
    [InlineData(0, false)]
    [InlineData(51, false)]
    [InlineData(-3, false)]
    public void ListQuery_Limit_IsOneToFifty_R_02(int limit, bool valid) =>
        new TrustedListQueryValidator().Validate(new TrustedListQuery { Limite = limit }).IsValid.Should().Be(valid);

    [Fact]
    public void ListQuery_DefaultsToTwenty_R_02() =>
        new TrustedListQuery().Limite.Should().Be(20);

    [Theory]
    [InlineData("507f1f77bcf86cd799439011", true)]
    [InlineData("507F1F77BCF86CD799439011", true)]
    [InlineData("507f1f77bcf86cd79943901", false)]
    [InlineData("507f1f77bcf86cd7994390111", false)]
    [InlineData("zzzzzzzzzzzzzzzzzzzzzzzz", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void Route_TechnicianId_IsAnObjectId_R_04(string? id, bool valid) =>
        new TrustedTechnicianRouteValidator().Validate(new TrustedTechnicianRoute { TecnicoId = id }).IsValid.Should().Be(valid);
}
