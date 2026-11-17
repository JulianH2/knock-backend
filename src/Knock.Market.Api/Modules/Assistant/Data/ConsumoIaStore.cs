using System.Globalization;
using StackExchange.Redis;

namespace Knock.Market.Api.Modules.Assistant.Data;

internal enum ConsumptionResult { Reserved, BudgetExhausted, LimitUnavailable }

internal sealed record ConsumptionReservation(string User, DateOnly Day, Guid Id);

internal sealed record ReservationResult(ConsumptionResult Result, ConsumptionReservation? Reservation);

internal sealed class ConsumoIaStore(IConnectionMultiplexer redis)
{
    private const string _reserveScript = """
        local global = tonumber(redis.call('GET', KEYS[1]) or '0')
        local user = tonumber(redis.call('GET', KEYS[2]) or '0')
        local amount = tonumber(ARGV[1])
        if global + amount > tonumber(ARGV[3]) or user + amount > tonumber(ARGV[2]) then return 0 end
        if not redis.call('SET', KEYS[3], amount, 'EX', 172800, 'NX') then return -1 end
        redis.call('INCRBY', KEYS[1], amount)
        redis.call('INCRBY', KEYS[2], amount)
        if redis.call('TTL', KEYS[1]) == -1 then redis.call('EXPIRE', KEYS[1], 172800) end
        if redis.call('TTL', KEYS[2]) == -1 then redis.call('EXPIRE', KEYS[2], 172800) end
        return 1
        """;

    private const string _adjustScript = """
        local global = redis.call('GET', KEYS[1])
        local user = redis.call('GET', KEYS[2])
        local reserved = tonumber(redis.call('GET', KEYS[3]) or '0')
        local actual = tonumber(ARGV[1])
        if reserved == 0 or not global or not user or actual > reserved or tonumber(global) < reserved or tonumber(user) < reserved then return 0 end
        redis.call('INCRBY', KEYS[1], actual - reserved)
        redis.call('INCRBY', KEYS[2], actual - reserved)
        redis.call('DEL', KEYS[3])
        return 1
        """;

    public async Task<ReservationResult> ReserveAsync(string user, DateOnly day, int amount, int userLimit, int globalLimit, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(user);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(amount);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(userLimit);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(globalLimit);
        cancellationToken.ThrowIfCancellationRequested();
        var reservation = new ConsumptionReservation(user, day, Guid.NewGuid());
        try
        {
            var result = (int)await redis.GetDatabase().ScriptEvaluateAsync(_reserveScript, Keys(reservation), [amount, userLimit, globalLimit]);
            return result switch
            {
                1 => new(ConsumptionResult.Reserved, reservation),
                0 => new(ConsumptionResult.BudgetExhausted, null),
                _ => new(ConsumptionResult.LimitUnavailable, null),
            };
        }
        catch (Exception ex) when (ex is RedisException or TimeoutException)
        {
            return new(ConsumptionResult.LimitUnavailable, null);
        }
    }

    public async Task<bool> AdjustAsync(ConsumptionReservation reservation, int actual, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(reservation);
        ArgumentException.ThrowIfNullOrWhiteSpace(reservation.User);
        ArgumentOutOfRangeException.ThrowIfNegative(actual);
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            return (int)await redis.GetDatabase().ScriptEvaluateAsync(_adjustScript, Keys(reservation), [actual]) == 1;
        }
        catch (Exception ex) when (ex is RedisException or TimeoutException)
        {
            return false;
        }
    }

    private static RedisKey[] Keys(ConsumptionReservation reservation)
    {
        var date = reservation.Day.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        return [$"ia:consumo:{date}", $"ia:usuario:{reservation.User}:{date}", $"ia:reserva:{reservation.User}:{date}:{reservation.Id:N}"];
    }
}
