using AwesomeAssertions;
using Knock.Market.Api.Modules.Requests.Domain;
using Xunit;

namespace Knock.UnitTest.Requests;

public sealed class Spec029TransitionsTests
{
    [Theory]
    [InlineData("Enviada", "Cancelada", true)]
    [InlineData("Cotizando", "Cancelada", true)]
    [InlineData("Enviada", "Vencida", true)]
    [InlineData("Cotizando", "Vencida", true)]
    [InlineData("Enviada", "Cotizando", false)]
    [InlineData("Cotizando", "CitaConfirmada", false)]
    [InlineData("CitaConfirmada", "Cancelada", false)]
    [InlineData("Terminada", "Cancelada", false)]
    [InlineData("Calificada", "Cancelada", false)]
    [InlineData("CitaConfirmada", "Vencida", false)]
    [InlineData("Terminada", "Vencida", false)]
    [InlineData("Calificada", "Vencida", false)]
    public void ClosingTransitions_OnlyOpenRequests_CA_029_02(string from, string to, bool expected) =>
        TransicionesDeSolicitud.CanClose(Enum.Parse<RequestState>(from), Enum.Parse<RequestState>(to)).Should().Be(expected);

    [Theory]
    [InlineData("CitaConfirmada", "request_with_appointment")]
    [InlineData("Terminada", "request_with_appointment")]
    [InlineData("Calificada", "request_with_appointment")]
    [InlineData("Enviada", null)]
    [InlineData("Cotizando", null)]
    [InlineData("Cancelada", null)]
    public void CancellationError_AppointmentAndOtherStates_CA_029_02(string state, string? expected) =>
        TransicionesDeSolicitud.CancellationError(Enum.Parse<RequestState>(state)).Should().Be(expected);

    [Fact]
    public void CancellationError_Expired_CA_029_11() =>
        TransicionesDeSolicitud.CancellationError(RequestState.Vencida).Should().Be("request_expired");

    [Fact]
    public void UnknownStates_FailClosed_CA_029_11()
    {
        var invalid = (RequestState)999;
        TransicionesDeSolicitud.CanClose(invalid, RequestState.Cancelada).Should().BeFalse();
        TransicionesDeSolicitud.CanClose(RequestState.Enviada, invalid).Should().BeFalse();
        var act = () => TransicionesDeSolicitud.CancellationError(invalid);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData("Cancelada")]
    [InlineData("Vencida")]
    public void FinalStates_CannotTransitionToAnyState_CA_029_11(string name)
    {
        var state = Enum.Parse<RequestState>(name);
        foreach (var destination in Enum.GetValues<RequestState>())
        {
            TransicionesDeSolicitud.CanClose(state, destination).Should().BeFalse();
        }
        RequestRules.AcceptsQuotes(state).Should().BeFalse();
    }
}
