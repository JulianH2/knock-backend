using Knock.Api.Modules.Catalog.Domain;
using Knock.Api.Modules.Catalog.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Knock.IntegrationTest.Support;

/// <summary>Doble de la consulta interna de colonia por punto (TT-API-07): cuenta las llamadas y responde lo configurado.</summary>
internal sealed class CountingNeighborhoodLocator : INeighborhoodLocator
{
    /// <summary>Colonia sintética con la forma del catálogo (id de 24 hexadecimales).</summary>
    public static readonly Colonia Synthetic = new("0000000000000000000000c1", "Colonia Sintética", "42800", "Municipio Sintético");

    private int _calls;

    public int Calls => Volatile.Read(ref _calls);

    public Colonia? Result { get; set; } = Synthetic;

    /// <summary>Si es <see langword="true"/>, la consulta lanza una excepción no controlada (500) sin datos de la petición.</summary>
    public bool Fail { get; set; }

    public Task<Colonia?> FindAtAsync(double latitude, double longitude, CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _calls);
        return Fail ? throw new InvalidOperationException("synthetic catalog failure") : Task.FromResult(Result);
    }

    /// <summary>Host de pruebas con este doble en lugar de la consulta real.</summary>
    public KnockApiFactory CreateFactory(string environment = "Development", IReadOnlyDictionary<string, string?>? settings = null) =>
        new(environment, settings: settings, configureServices: services => services.AddSingleton<INeighborhoodLocator>(this));
}
