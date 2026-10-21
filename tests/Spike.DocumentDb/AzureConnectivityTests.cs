using AwesomeAssertions;
using Knock.Spike.DocumentDb.Support;

namespace Knock.Spike.DocumentDb;

/// <summary>
/// Chequeo único de red contra Azure (10 s). Si falla, es la única prueba que falla; las demás de Azure se omiten
/// con el mismo motivo en vez de esperar 30 s cada una.
/// </summary>
public sealed class AzureConnectivityTests
{
    [AzureFact]
    public void AzureCluster_RespondsToPingWithinTenSeconds()
    {
        AzureConnection.Failure.Should().BeNull();
    }
}
