using Knock.Market.Api.Modules.Tecnicos.Domain;
using Xunit;

namespace Knock.UnitTest.Tecnicos;

public sealed class Spec036TrabajoRulesTests
{
    [Theory]
    [InlineData("confirmada", true)]
    [InlineData("en_curso", true)]
    [InlineData("terminada", false)]
    [InlineData("cancelada", false)]
    [InlineData(null, false)]
    public void Direccion_Solo_Tras_Eleccion_CA_036_01(string? state, bool expected) =>
        Assert.Equal(expected, TrabajoRules.CanShowAddress(state));

    [Theory]
    [InlineData("rechazada", "other_quote_chosen", "other_quote_chosen")]
    [InlineData("retirada", "dismissed", "dismissed")]
    [InlineData("enviada", "other_quote_chosen", null)]
    [InlineData("aceptada", "other_quote_chosen", null)]
    [InlineData("rechazada", null, null)]
    public void Motivo_Solo_En_Estados_Cerrados_CA_036_04(string state, string? reason, string? expected) =>
        Assert.Equal(expected, TrabajoRules.RejectionReason(state, reason));

    [Fact]
    public void Terminadas_Orden_Descendente_Y_Desempate_CA_036_03()
    {
        var old = new DateTime(2026, 11, 18, 0, 0, 0, DateTimeKind.Utc);
        var recent = old.AddDays(1);
        Assert.True(TrabajoRules.CompareFinished(recent, "b", old, "z") < 0);
        Assert.True(TrabajoRules.CompareFinished(recent, "b", recent, "a") < 0);
        Assert.Equal(0, TrabajoRules.CompareFinished(recent, "a", recent, "a"));
        Assert.True(TrabajoRules.CompareFinished(old, "a", recent, "a") > 0);
    }
}
