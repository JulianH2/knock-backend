using AwesomeAssertions;
using Knock.Market.Api.Modules.Requests.Domain;
using Xunit;

namespace Knock.UnitTest.Requests;

/// <summary>Spec 022, R-03 a R-09 y R-12: ventana de la cita, visibilidad del código, bloqueos de la llegada y texto del aviso. Funciones puras con un reloj fijo.</summary>
public sealed class Spec022VisitRulesTests
{
    // 2026-11-18 00:00 en Ciudad de México (UTC-6) = 2026-11-18 06:00 UTC.
    private static readonly DateTime _windowStart = new(2026, 11, 18, 6, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime _windowEnd = new(2026, 11, 20, 5, 59, 59, DateTimeKind.Utc);

    private static VisitState Visit(string estado = "confirmada", string modalidad = "domicilio", bool locked = false, bool used = false, DateTime? expires = null, string day = "2026-11-18") =>
        new(estado, modalidad, day, locked, used, modalidad == "domicilio" ? expires ?? _windowEnd : null);

    [Fact]
    public void WindowStart_IsMidnightOfTheAppointmentDayInMexicoCity_R_05() =>
        VisitRules.WindowStart("2026-11-18").Should().Be(_windowStart);

    [Fact]
    public void CanArrive_OneMinuteBeforeAndAfterTheWindow_R_05()
    {
        VisitRules.CanArrive(Visit(), _windowStart.AddMinutes(-1)).Should().BeFalse();
        VisitRules.CanArrive(Visit(), _windowStart).Should().BeTrue();
        VisitRules.CanArrive(Visit(), _windowEnd).Should().BeTrue();
        VisitRules.CanArrive(Visit(), _windowEnd.AddMinutes(1)).Should().BeFalse();
    }

    [Theory]
    [InlineData("en_curso", false, false, "domicilio")]
    [InlineData("cancelada", false, false, "domicilio")]
    [InlineData("confirmada", true, false, "domicilio")]
    [InlineData("confirmada", false, true, "domicilio")]
    [InlineData("confirmada", false, false, "taller")]
    public void CanArrive_IsFalseWhenAnyConditionFails_R_05(string estado, bool locked, bool used, string modality) =>
        VisitRules.CanArrive(Visit(estado, modality, locked, used), _windowStart.AddHours(1)).Should().BeFalse();

    [Theory]
    [InlineData("confirmada", "domicilio", false, false, 0, null)]
    [InlineData("en_curso", "domicilio", false, false, 0, null)]
    [InlineData("confirmada", "taller", false, false, 0, "no_aplica")]
    [InlineData("cancelada", "domicilio", false, false, 0, "no_aplica")]
    [InlineData("terminada", "domicilio", false, false, 0, "no_aplica")]
    [InlineData("confirmada", "domicilio", true, false, 0, "bloqueada")]
    [InlineData("confirmada", "domicilio", false, true, 0, "usado")]
    [InlineData("confirmada", "domicilio", false, false, 1, "vencido")]
    public void CodeHiddenReason_FollowsTheSpecOrder_R_03(string estado, string modality, bool locked, bool used, int minutesAfterExpiry, string? expected)
    {
        var now = _windowEnd.AddMinutes(minutesAfterExpiry == 0 ? -5 : minutesAfterExpiry);

        VisitRules.CodeHiddenReason(Visit(estado, modality, locked, used), now).Should().Be(expected);
    }

    [Theory]
    [InlineData("cancelada", "domicilio", false, false, "VisitCancelled")]
    [InlineData("terminada", "domicilio", false, false, "VisitNotActive")]
    [InlineData("no_presentado", "domicilio", false, false, "VisitNotActive")]
    [InlineData("en_curso", "domicilio", false, false, "CodeUsed")]
    [InlineData("confirmada", "domicilio", false, true, "CodeUsed")]
    [InlineData("confirmada", "domicilio", true, false, "VisitLocked")]
    [InlineData("confirmada", "taller", false, false, "VisitWithoutCode")]
    public void BlockForArrival_ReturnsTheSpecCode_R_09(string estado, string modality, bool locked, bool used, string expected) =>
        VisitRules.BlockForArrival(Visit(estado, modality, locked, used), _windowStart.AddHours(1)).Should().Be(Enum.Parse<ArrivalBlock>(expected));

    [Fact]
    public void BlockForArrival_ExpiredAndBeforeTheWindow_R_07()
    {
        VisitRules.BlockForArrival(Visit(), _windowEnd.AddSeconds(1)).Should().Be(ArrivalBlock.CodeExpired);
        VisitRules.BlockForArrival(Visit(), _windowStart.AddSeconds(-1)).Should().Be(ArrivalBlock.OutsideWindow);
        VisitRules.BlockForArrival(Visit(), _windowStart).Should().BeNull();
    }

    [Fact]
    public void BlockForArrival_LockedBeatsTheOtherConditionsExceptFinalStates_R_09()
    {
        VisitRules.BlockForArrival(Visit(locked: true, expires: _windowStart), _windowEnd).Should().Be(ArrivalBlock.VisitLocked);
        VisitRules.BlockForArrival(Visit("cancelada", locked: true), _windowEnd).Should().Be(ArrivalBlock.VisitCancelled);
    }

    [Theory]
    [InlineData("confirmada", true)]
    [InlineData("en_curso", true)]
    [InlineData("terminada", false)]
    [InlineData("cancelada", false)]
    [InlineData("no_presentado", false)]
    public void CanNotify_OnlyForConfirmedOrInProgress_R_13(string estado, bool expected) => VisitRules.CanNotify(estado).Should().Be(expected);

    [Theory]
    [InlineData("confirmada", false, true)]
    [InlineData("en_curso", false, true)]
    [InlineData("confirmada", true, false)]
    [InlineData("en_curso", true, false)]
    [InlineData("terminada", false, false)]
    [InlineData("cancelada", false, false)]
    [InlineData("no_presentado", false, false)]
    public void TechnicianMaySeeAddress_OnlyWithAnOpenUnlockedVisit_R_05(string estado, bool locked, bool expected) =>
        VisitRules.TechnicianMaySeeAddress(Visit(estado, locked: locked)).Should().Be(expected);

    [Fact]
    public void SafetyRule_UsesTheFixedText_R_04() =>
        VisitRules.SafetyRule("Demo Uno").Should().Be("Pídele a Demo Uno que te diga este código en la puerta. Si no lo sabe, no abras.");

    [Theory]
    [InlineData("completa", "con verificación completa")]
    [InlineData("basica", "con verificación básica")]
    [InlineData("ninguno", "sin verificar")]
    public void TrustNotice_SaysTheVerificationAndNeverCarriesTheCodeOrAddress_R_12(string level, string verification)
    {
        var text = VisitRules.TrustNotice("Demo Uno", level, "2026-11-18", "10:00");

        text.Should().Contain("Demo Uno").And.Contain(verification).And.Contain("2026-11-18").And.Contain("10:00").And.Contain("código en la puerta");
        text.Should().NotMatchRegex("[0-9]{4}(?![-0-9])", "ningún número de cuatro dígitos que parezca el código");
    }
}
