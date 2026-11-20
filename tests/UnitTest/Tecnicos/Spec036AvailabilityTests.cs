using Knock.Market.Api.Modules.Tecnicos.Domain;
using Xunit;

namespace Knock.UnitTest.Tecnicos;

public sealed class Spec036AvailabilityTests
{
    [Fact]
    public void Disponibilidad_Usa_Fecha_Futura_Y_Ausente_Es_Falso_CA_036_07()
    {
        var now = new DateTime(2026, 11, 19, 12, 0, 0, DateTimeKind.Utc);
        Assert.False(TechnicianRules.IsAvailable(null, now));
        Assert.False(TechnicianRules.IsAvailable(now.AddTicks(-1), now));
        Assert.False(TechnicianRules.IsAvailable(now, now));
        Assert.True(TechnicianRules.IsAvailable(now.AddTicks(1), now));
    }
}
