using AwesomeAssertions;
using Knock.Market.Api.Modules.Requests.Domain;
using Xunit;

namespace Knock.UnitTest.Requests;

/// <summary>Spec 030, R-03: cuándo una conversación admite mensajes y, si no, el motivo del cierre. Función pura, todas las combinaciones.</summary>
public sealed class Spec030ConversationRulesTests
{
    [Theory]
    [InlineData("Enviada", "Enviada")]
    [InlineData("Cotizando", "Enviada")]
    [InlineData("CitaConfirmada", "Aceptada")]
    public void Evaluate_IsOpen_WhileTheTechnicianIsInPlay_R_03(string request, string quote)
    {
        var opening = ConversationRules.Evaluate(Enum.Parse<RequestState>(request), Enum.Parse<QuoteState>(quote));

        opening.Open.Should().BeTrue();
        opening.Reason.Should().BeNull();
    }

    [Theory]
    [InlineData("Enviada", "Retirada", "dismissed")]
    [InlineData("Cotizando", "Retirada", "dismissed")]
    [InlineData("Cotizando", "Rechazada", "dismissed")]
    [InlineData("Cancelada", "Enviada", "request_cancelled")]
    [InlineData("Cancelada", "Aceptada", "request_cancelled")]
    [InlineData("Vencida", "Enviada", "request_expired")]
    [InlineData("CitaConfirmada", "Rechazada", "other_quote_chosen")]
    [InlineData("CitaConfirmada", "Enviada", "other_quote_chosen")]
    [InlineData("Terminada", "Aceptada", "service_finished")]
    [InlineData("Calificada", "Aceptada", "service_finished")]
    [InlineData("Terminada", "Rechazada", "other_quote_chosen")]
    public void Evaluate_IsClosedWithItsReason_R_03(string request, string quote, string reason)
    {
        var opening = ConversationRules.Evaluate(Enum.Parse<RequestState>(request), Enum.Parse<QuoteState>(quote));

        opening.Open.Should().BeFalse();
        opening.Reason.Should().Be(reason);
    }

    [Fact]
    public void Evaluate_CoversEveryCombinationWithoutThrowing_R_03()
    {
        foreach (var request in Enum.GetValues<RequestState>())
        {
            foreach (var quote in Enum.GetValues<QuoteState>())
            {
                var opening = ConversationRules.Evaluate(request, quote);

                (opening.Open == (opening.Reason is null)).Should().BeTrue("abierta y sin motivo, o cerrada con motivo");
            }
        }
    }
}
