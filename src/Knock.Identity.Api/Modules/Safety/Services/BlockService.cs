using Knock.Identity.Api.Common.Errors;
using Knock.Identity.Api.Modules.Safety.Data;
using Knock.Identity.Api.Modules.Safety.Domain;
using Knock.Identity.Api.Modules.Safety.Dtos;
using Knock.Identity.Api.Modules.Usuarios;

namespace Knock.Identity.Api.Modules.Safety.Services;

/// <summary>Cómo terminó bloquear a alguien.</summary>
internal enum BlockOutcome
{
    Blocked,
    Yourself,
}

/// <summary>
/// Bloqueos entre personas (spec 043, BLO-01 a BLO-03). La persona de cada operación es siempre la del <c>sub</c> del token (R-24). Bloquear a alguien que no existe es 404 y bloquear a
/// quien ya se bloqueó, 409; no se puede bloquear a uno mismo. A la persona bloqueada no se le avisa (R-17). Los efectos sobre mensajes, solicitudes y búsquedas en otros servicios
/// se aplican con los eventos de la bandeja de salida (R-23), que este paso todavía no emite. No toca MongoDB.
/// </summary>
internal sealed class BlockService(BloqueosStore store, IUsuariosApi users, TimeProvider clock)
{
    private const string _publicNameFallback = "Usuario de Knock";

    /// <summary>Bloquea a la persona (ya validada como GUID). Lanza 404 <c>user_not_found</c> o 409 <c>already_blocked</c>.</summary>
    public async Task<BlockOutcome> BlockAsync(string sub, string targetId, CancellationToken cancellationToken)
    {
        if (string.Equals(sub, targetId, StringComparison.OrdinalIgnoreCase))
        {
            return BlockOutcome.Yourself;
        }

        if (await users.FindPublicUserAsync(targetId, cancellationToken) is null)
        {
            throw new DomainException(DomainErrorKind.NotFound, "The person does not exist.", SafetyErrorCodes.UserNotFoundCode);
        }

        var now = clock.GetUtcNow().UtcDateTime;
        if (!await store.AddAsync(sub, targetId, now.AddTicks(-(now.Ticks % TimeSpan.TicksPerMillisecond)), cancellationToken))
        {
            throw new DomainException(DomainErrorKind.Conflict, "The person is already blocked.", SafetyErrorCodes.AlreadyBlockedCode);
        }

        return BlockOutcome.Blocked;
    }

    /// <summary>Quita el bloqueo; lanza 404 <c>block_not_found</c> si no había.</summary>
    public async Task UnblockAsync(string sub, string targetId, CancellationToken cancellationToken)
    {
        if (!await store.RemoveAsync(sub, targetId, cancellationToken))
        {
            throw new DomainException(DomainErrorKind.NotFound, "There is no block for that person.", SafetyErrorCodes.BlockNotFoundCode);
        }
    }

    /// <summary>Las personas bloqueadas por quien llama, con su nombre público. Un cursor que no se puede leer lanza 400 (<c>invalid_safety_cursor</c>).</summary>
    public async Task<BlockedPage> ListAsync(string sub, BlocksQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        var rows = await store.ListAsync(sub, SafetyCursor.Parse(query.Cursor), query.Limite + 1, cancellationToken);
        var page = rows.Take(query.Limite).ToList();
        var items = new List<BlockedPerson>(page.Count);
        foreach (var row in page)
        {
            var person = await users.FindPublicUserAsync(row.BlockedId, cancellationToken);
            items.Add(new BlockedPerson(row.BlockedId, string.IsNullOrWhiteSpace(person?.Name) ? _publicNameFallback : person.Name, row.CreatedAtUtc));
        }

        return new BlockedPage(items, rows.Count > query.Limite ? page[^1].RecordId.ToString() : null);
    }
}
