using MongoDB.Bson;
using MongoDB.Driver;

namespace Knock.Market.Api.Common.TareasProgramadas;

internal sealed record TareaReservada(ObjectId Id, string Tipo, string ClaveNegocio, string ReservaId, int Intentos);

internal interface ITareasProgramadasStore
{
    Task<TareaReservada?> ReserveAsync(DateTime now, CancellationToken cancellationToken, IReadOnlyCollection<string>? types = null);
    Task<bool> CompleteAsync(TareaReservada task, DateTime now, CancellationToken cancellationToken);
    Task<bool> FailAsync(TareaReservada task, DateTime now, CancellationToken cancellationToken);
    Task<bool> ConfirmLeaseAsync(IClientSessionHandle session, TareaReservada task, DateTime now, CancellationToken cancellationToken);
}
