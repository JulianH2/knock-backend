using AwesomeAssertions;
using Knock.Identity.Api.Modules.Safety.Domain;
using Knock.Identity.Api.Modules.Safety.Dtos;
using Knock.Identity.Api.Modules.Safety.Validators;
using Xunit;

namespace Knock.UnitTest.Safety;

/// <summary>Spec 043: reglas puras de los reportes, el cursor del módulo y los validadores de bloqueos y reportes.</summary>
public sealed class Spec043SafetyRulesTests
{
    private const string _guid = "7b484abb-e9e0-8104-a08d-d30ce15e8148";
    private const string _objectId = "507f1f77bcf86cd799439011";

    [Theory]
    [InlineData("12345678", true)]
    [InlineData("  doce caracteres  ", true)]
    [InlineData("1234567", false)]
    [InlineData("       ", false)]
    [InlineData(null, false)]
    [InlineData("hola\u0007mundo largo", false)]
    public void Text_IsEightToFiveHundredCharactersWithoutControls_R_20(string? text, bool valid) =>
        ReportRules.IsValidText(text).Should().Be(valid);

    [Fact]
    public void Text_AcceptsLineBreaksAndTheLimits_R_20()
    {
        ReportRules.IsValidText("primera línea\nsegunda línea").Should().BeTrue();
        ReportRules.IsValidText(new string('a', 500)).Should().BeTrue();
        ReportRules.IsValidText(new string('a', 501)).Should().BeFalse();
    }

    [Fact]
    public void OnlyTheDangerReasonCarriesTheEmergencyNotice_R_20()
    {
        foreach (var reason in ReportRules.Reasons)
        {
            (ReportRules.NoticeFor(reason) is not null).Should().Be(reason == "peligro", reason);
        }

        ReportRules.NoticeFor("peligro").Should().Contain("911");
    }

    [Theory]
    [InlineData("decidido", true, false, true)]
    [InlineData("decidido", true, true, false)]
    [InlineData("decidido", false, false, false)]
    [InlineData("abierto", false, false, false)]
    [InlineData("abierto", true, false, false)]
    [InlineData("en_apelacion", true, true, false)]
    public void Appeal_NeedsADecisionAndOnlyOneAppeal_R_21(string state, bool hasDecision, bool hasAppeal, bool allowed) =>
        ReportRules.CanBeAppealed(state, hasDecision, hasAppeal).Should().Be(allowed);

    [Theory]
    [InlineData(1, true)]
    [InlineData(50, true)]
    [InlineData(0, false)]
    [InlineData(51, false)]
    public void ListQueries_LimitIsOneToFifty_R_08_R_12(int limit, bool valid)
    {
        new BlocksQueryValidator().Validate(new BlocksQuery { Limite = limit }).IsValid.Should().Be(valid);
        new ReportsQueryValidator().Validate(new ReportsQuery { Limite = limit }).IsValid.Should().Be(valid);
    }

    [Theory]
    [InlineData(_guid, true)]
    [InlineData("7B484ABB-E9E0-8104-A08D-D30CE15E8148", true)]
    [InlineData(_objectId, false)]
    [InlineData("7b484abbe9e08104a08dd30ce15e8148", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void BlockRoute_IdIsAPublicIdentifier_R_02(string? id, bool valid) =>
        new BlockRouteValidator().Validate(new BlockRoute { Id = id }).IsValid.Should().Be(valid);

    [Fact]
    public void CreateReport_ValidBodyPasses_R_10()
    {
        var body = new CreateReportBody { ReportadoId = _guid, Motivo = "acoso", Descripcion = "Me escribió varias veces sin parar.", Evidencia = [_objectId], Referencia = new ReportReferenceBody { Tipo = "visita", Id = _objectId } };

        new CreateReportBodyValidator().Validate(body).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("sinMotivo")]
    [InlineData("seisEvidencias")]
    [InlineData("evidenciaRepetida")]
    [InlineData("textoCorto")]
    [InlineData("sinReportado")]
    [InlineData("referenciaSinId")]
    public void CreateReport_InvalidBodyFails_R_10(string problem)
    {
        var body = new CreateReportBody { ReportadoId = _guid, Motivo = "acoso", Descripcion = "Me escribió varias veces sin parar.", Evidencia = [] };
        switch (problem)
        {
            case "sinMotivo": body.Motivo = "motivo-raro"; break;
            case "seisEvidencias": body.Evidencia = [.. Enumerable.Range(0, 6).Select(i => $"507f1f77bcf86cd79943901{i}")]; break;
            case "evidenciaRepetida": body.Evidencia = [_objectId, _objectId]; break;
            case "textoCorto": body.Descripcion = "corto"; break;
            case "sinReportado": body.ReportadoId = null; break;
            default: body.Referencia = new ReportReferenceBody { Tipo = "solicitud", Id = null }; break;
        }

        new CreateReportBodyValidator().Validate(body).IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(_objectId, true)]
    [InlineData("507f1f77bcf86cd79943901", false)]
    [InlineData(_guid, false)]
    [InlineData(null, false)]
    public void ReportRoute_IdIsAnObjectId_R_14(string? id, bool valid) =>
        new AppealRequestValidator().Validate(new ReportRoute { Id = id }).IsValid.Should().Be(valid);

    [Theory]
    [InlineData("No estoy de acuerdo con la decisión.", true)]
    [InlineData("corto", false)]
    [InlineData(null, false)]
    public void AppealBody_TextIsEightToFiveHundredCharacters_R_14(string? text, bool valid) =>
        new AppealBodyValidator().Validate(new AppealBody { Texto = text }).IsValid.Should().Be(valid);

    [Fact]
    public void Cursor_EmptyMeansTheFirstPage_AndAnObjectIdIsRead()
    {
        SafetyCursor.Parse(null).Should().BeNull();
        SafetyCursor.Parse(string.Empty).Should().BeNull();
        SafetyCursor.Parse(_objectId).Should().NotBeNull();
    }

    [Theory]
    [InlineData("no-es-un-cursor")]
    [InlineData("507f1f77bcf86cd79943901")]
    [InlineData("zzzzzzzzzzzzzzzzzzzzzzzz")]
    public void Cursor_Unreadable_Throws(string cursor) =>
        FluentActions.Invoking(() => SafetyCursor.Parse(cursor)).Should().Throw<Exception>().Which.GetType().Name.Should().Be("DomainException");
}
