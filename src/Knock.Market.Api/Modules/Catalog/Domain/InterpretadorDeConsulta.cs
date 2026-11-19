namespace Knock.Market.Api.Modules.Catalog.Domain;

internal sealed record SearchTrade(string Key, string Name);
internal sealed record SearchSynonym(string Term, string TradeKey);
internal sealed record SearchInterpretation(string State, SearchTrade? Detected, IReadOnlyList<SearchTrade> Suggestions);

internal static class InterpretadorDeConsulta
{
    public static SearchInterpretation Interpret(string query, IReadOnlyList<SearchTrade> trades, IReadOnlyList<SearchSynonym> synonyms, SearchTrade? selected = null)
    {
        if (selected is not null) return new("elegido", selected, []);
        var activeKeys = trades.Select(t => t.Key).ToHashSet(StringComparer.Ordinal);
        var terms = synonyms.Where(s => activeKeys.Contains(s.TradeKey)).Select(s => (Term: SearchNameNormalizer.NormalizeQuery(s.Term), Key: s.TradeKey))
            .Concat(trades.Select(t => (Term: SearchNameNormalizer.NormalizeQuery(t.Name), t.Key))).ToArray();
        var scores = new Dictionary<string, int>(StringComparer.Ordinal);
        var words = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        for (var i = 0; i < words.Length; i++)
        {
            Score(words[i], terms, scores);
            if (i + 1 < words.Length) Score(words[i] + " " + words[i + 1], terms, scores);
        }

        var eligible = trades.Where(t => scores.ContainsKey(t.Key)).ToArray();
        if (eligible.Length == 0) return new("no_entendido", null, []);
        var maximum = eligible.Max(t => scores[t.Key]);
        var winners = eligible.Where(t => scores[t.Key] == maximum).OrderBy(t => t.Key, StringComparer.Ordinal).ToArray();
        return winners.Length == 1 ? new("detectado", winners[0], []) : new("ambiguo", null, winners.Take(3).ToArray());
    }

    private static void Score(string word, (string Term, string Key)[] terms, Dictionary<string, int> scores)
    {
        var matching = terms.Where(t => t.Term == word).ToArray();
        if (matching.Length == 0)
        {
            var limit = word.Length < 8 ? 1 : 2;
            if (word.Length < 4) limit = 0;
            if (limit == 0) return;
            matching = terms.Where(t => Distance(word, t.Term, limit) <= limit).ToArray();
        }

        foreach (var key in matching.Select(t => t.Key).Distinct(StringComparer.Ordinal))
            scores[key] = scores.GetValueOrDefault(key) + 1;
    }

    private static int Distance(string left, string right, int limit)
    {
        if (Math.Abs(left.Length - right.Length) > limit) return limit + 1;
        var infinity = left.Length + right.Length;
        var d = new int[left.Length + 2][];
        for (var i = 0; i < d.Length; i++) d[i] = new int[right.Length + 2];
        d[0][0] = infinity;
        for (var i = 0; i <= left.Length; i++) { d[i + 1][0] = infinity; d[i + 1][1] = i; }
        for (var j = 0; j <= right.Length; j++) { d[0][j + 1] = infinity; d[1][j + 1] = j; }
        var last = new Dictionary<char, int>();
        for (var i = 1; i <= left.Length; i++)
        {
            var lastMatch = 0;
            for (var j = 1; j <= right.Length; j++)
            {
                var previousRow = last.GetValueOrDefault(right[j - 1]);
                var previousColumn = lastMatch;
                var cost = left[i - 1] == right[j - 1] ? 0 : 1;
                if (cost == 0) lastMatch = j;
                var transposition = d[previousRow][previousColumn] + i - previousRow - 1 + 1 + j - previousColumn - 1;
                d[i + 1][j + 1] = Math.Min(transposition, Math.Min(Math.Min(d[i][j + 1] + 1, d[i + 1][j] + 1), d[i][j] + cost));
            }

            last[left[i - 1]] = i;
        }

        return d[left.Length + 1][right.Length + 1];
    }
}
