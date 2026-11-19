using System.Security.Cryptography;
using System.Text.Json;
using Knock.Contracts.Pagination;

namespace Knock.Market.Api.Modules.Catalog.Domain;

internal sealed record SearchQuery(string Query, string ColoniaId, string? Trade, bool VerifiedOnly, int DictionaryVersion);
internal sealed record SearchCursorPayload(SearchRank Last, int Count, byte[] Fingerprint);

internal static class CursorDeBusqueda
{
    private const int _maximum = 100;
    private const int _nonceLength = 12;
    private const int _tagLength = 16;
    private const int _maxCursorLength = 2048;

    public static SearchCursorPayload Read(string cursor, SearchQuery query, byte[] key)
    {
        ArgumentNullException.ThrowIfNull(cursor);
        ArgumentNullException.ThrowIfNull(query);
        ValidateKey(key);
        return Decode(cursor, Fingerprint(query), key);
    }

    public static string CreateNext(SearchQuery query, SearchRank last, int count, byte[] key)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(last);
        ValidateKey(key);
        if (count is < 1 or >= _maximum) throw new ArgumentOutOfRangeException(nameof(count));
        if (string.IsNullOrEmpty(last.Id) || last.Group is < 0 or > 2) throw new ArgumentException("Invalid rank.", nameof(last));
        var fingerprint = Fingerprint(query);
        return Encode(new SearchCursorPayload(last, count, fingerprint), fingerprint, key);
    }

    private static void ValidateKey(byte[] key)
    {
        ArgumentNullException.ThrowIfNull(key);
        if (key.Length != 32) throw new ArgumentException("A 256-bit cursor key is required.", nameof(key));
    }

    public static CursorPage<SearchRank> Page(IEnumerable<SearchRank> ranks, SearchQuery query, int limit, string? cursor, byte[] key)
    {
        ArgumentNullException.ThrowIfNull(ranks);
        ArgumentNullException.ThrowIfNull(query);
        ValidateKey(key);
        if (limit is < 1 or > PageLimit.Default) throw new ArgumentOutOfRangeException(nameof(limit));

        var fingerprint = Fingerprint(query);
        var previous = cursor is null ? null : Decode(cursor, fingerprint, key);
        var count = previous?.Count ?? 0;
        var available = Math.Min(limit, _maximum - count);
        var ordered = OrdenDeResultados.Order(ranks);
        var remaining = previous is null ? ordered : ordered.Where(rank => OrdenDeResultados.Compare(rank, previous.Last) > 0);
        var candidates = remaining.Take(available + 1).ToArray();
        var items = candidates.Take(available).ToArray();
        var nextCount = count + items.Length;
        var next = candidates.Length > available && nextCount < _maximum
            ? Encode(new SearchCursorPayload(items[^1], nextCount, fingerprint), fingerprint, key)
            : null;
        return new CursorPage<SearchRank>(items, next);
    }

    private static byte[] Fingerprint(SearchQuery query) => SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(query with
    {
        Query = SearchNameNormalizer.NormalizeQuery(query.Query),
    }));

    private static string Encode(SearchCursorPayload payload, byte[] fingerprint, byte[] key)
    {
        var plaintext = JsonSerializer.SerializeToUtf8Bytes(payload);
        var nonce = RandomNumberGenerator.GetBytes(_nonceLength);
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[_tagLength];
        using var cipher = new AesGcm(key, _tagLength);
        cipher.Encrypt(nonce, plaintext, ciphertext, tag, fingerprint);
        return Convert.ToBase64String([.. nonce, .. tag, .. ciphertext]).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static SearchCursorPayload Decode(string cursor, byte[] fingerprint, byte[] key)
    {
        try
        {
            if (cursor.Length is < 39 or > _maxCursorLength || cursor.Any(c => !char.IsAsciiLetterOrDigit(c) && c is not '-' and not '_'))
                throw new FormatException("Invalid search cursor.");
            var data = Convert.FromBase64String(cursor.Replace('-', '+').Replace('_', '/') + new string('=', (4 - cursor.Length % 4) % 4));
            var canonical = Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');
            if (canonical != cursor || data.Length <= _nonceLength + _tagLength) throw new FormatException("Invalid search cursor.");
            var plaintext = new byte[data.Length - _nonceLength - _tagLength];
            using var cipher = new AesGcm(key, _tagLength);
            cipher.Decrypt(data.AsSpan(0, _nonceLength), data.AsSpan(_nonceLength + _tagLength), data.AsSpan(_nonceLength, _tagLength), plaintext, fingerprint);
            var payload = JsonSerializer.Deserialize<SearchCursorPayload>(plaintext);
            if (payload?.Last is null || string.IsNullOrEmpty(payload.Last.Id) || payload.Last.Group is < 0 or > 2 || payload.Count is < 1 or >= _maximum ||
                payload.Fingerprint is null || !CryptographicOperations.FixedTimeEquals(payload.Fingerprint, fingerprint))
                throw new FormatException("Invalid search cursor.");
            return payload;
        }
        catch (Exception ex) when (ex is FormatException or CryptographicException or JsonException)
        {
            throw new FormatException("Invalid search cursor.");
        }
    }
}
