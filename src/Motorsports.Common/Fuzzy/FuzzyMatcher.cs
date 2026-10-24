using System.Buffers;

namespace Motorsports.Common.Fuzzy;

/// <summary>
/// Allocation-free fuzzy matcher over pre-normalized text (lower case, diacritics removed; see <see cref="Normalize"/>).
/// Every whitespace separated query token must match; token order is irrelevant.
/// </summary>
public static class FuzzyMatcher
{
    public const int PrefixScore = 100;
    public const int WordStartScore = 80;
    public const int SubstringScore = 60;
    public const int SubsequenceScore = 30;

    // Folding tables (no ICU/normalization dependency, so InvariantGlobalization is safe on Linux).
    private const string Latin1Lower = "aaaaaaaceeeeiiiidnooooo÷ouuuuyty"; // U+00E0..U+00FF
    private const string LatinExtA =
        "aaaaaa"
        + "cccccccc"
        + "dddd"
        + "eeeeeeeeee"
        + "gggggggg"
        + "hhhh"
        + "iiiiiiiiii"
        + "ii"
        + "jj"
        + "kkk"
        + "llllllllll"
        + "nnnnnnnnn"
        + "oooooooo"
        + "rrrrrr"
        + "ssssssss"
        + "tttttt"
        + "uuuuuuuuuuuu"
        + "ww"
        + "yyy"
        + "zzzzzz"
        + "s"; // U+0100..U+017F

    /// <summary>Lower-cases and strips diacritics. Allocates; call once per item/query, never per comparison.</summary>
    public static string Normalize(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "";
        }

        return string.Create(
                value.Length,
                value,
                static (span, source) =>
                {
                    for (var i = 0; i < source.Length; i++)
                    {
                        span[i] = Fold(source[i]);
                    }
                }
            )
            .Replace("\0", "", StringComparison.Ordinal);
    }

    private static char Fold(char c)
    {
        if (c is >= '\u0300' and <= '\u036F')
        {
            return '\0'; // combining mark, removed by caller
        }

        c = char.ToLowerInvariant(c);
        return c switch
        {
            >= '\u00E0' and <= '\u00FF' => Latin1Lower[c - '\u00E0'],
            >= '\u0100' and <= '\u017F' => LatinExtA[c - '\u0100'],
            _ => c,
        };
    }

    /// <summary>Returns 0 when no match, otherwise the average token score (higher is better).</summary>
    public static int Score(ReadOnlySpan<char> query, ReadOnlySpan<char> text)
    {
        var total = 0;
        var tokens = 0;
        var rest = query;
        while (true)
        {
            rest = rest.TrimStart();
            if (rest.IsEmpty)
            {
                break;
            }

            var end = rest.IndexOf(' ');
            var token = end < 0 ? rest : rest[..end];
            var score = ScoreToken(token, text);
            if (score == 0)
            {
                return 0;
            }

            total += score;
            tokens++;
            if (end < 0)
            {
                break;
            }

            rest = rest[end..];
        }

        return tokens == 0 ? 0 : total / tokens;
    }

    private static int ScoreToken(ReadOnlySpan<char> token, ReadOnlySpan<char> text)
    {
        if (text.StartsWith(token))
        {
            return PrefixScore;
        }

        var offset = 0;
        var found = false;
        while (true)
        {
            var i = text[offset..].IndexOf(token);
            if (i < 0)
            {
                break;
            }

            found = true;
            var index = offset + i;
            if (index == 0 || text[index - 1] is ' ' or '-' or '\'' or '.')
            {
                return WordStartScore;
            }

            offset = index + 1;
        }

        if (found)
        {
            return SubstringScore;
        }

        // Typo tolerance: token characters appear in order, only for tokens of 3+ characters.
        return token.Length >= 3 && IsSubsequence(token, text) ? SubsequenceScore : 0;
    }

    private static bool IsSubsequence(ReadOnlySpan<char> token, ReadOnlySpan<char> text)
    {
        var t = 0;
        foreach (var c in text)
        {
            if (c == token[t] && ++t == token.Length)
            {
                return true;
            }
        }

        return false;
    }
}

/// <summary>Immutable search index. Build once (cache), search many times without allocating per item.</summary>
public sealed class FuzzyIndex<T>
{
    private readonly T[] _items;
    private readonly string[][] _keys;

    public FuzzyIndex(IReadOnlyList<(T Item, string[] Keys)> entries)
    {
        _items = new T[entries.Count];
        _keys = new string[entries.Count][];
        for (var i = 0; i < entries.Count; i++)
        {
            _items[i] = entries[i].Item;
            var keys = entries[i].Keys;
            var normalized = new string[keys.Length];
            for (var k = 0; k < keys.Length; k++)
            {
                normalized[k] = FuzzyMatcher.Normalize(keys[k]);
            }

            _keys[i] = normalized;
        }
    }

    public int Count => _items.Length;

    public IReadOnlyList<T> Search(string? query, int max = 50)
    {
        var normalizedQuery = FuzzyMatcher.Normalize(query);
        if (normalizedQuery.Length == 0)
        {
            return _items.Length <= max ? _items : _items.AsSpan(0, max).ToArray();
        }

        var pool = ArrayPool<(int Score, int Index)>.Shared;
        var hits = pool.Rent(_items.Length);
        try
        {
            var count = 0;
            for (var i = 0; i < _items.Length; i++)
            {
                var best = 0;
                foreach (var key in _keys[i])
                {
                    var s = FuzzyMatcher.Score(normalizedQuery, key);
                    if (s > best)
                    {
                        best = s;
                    }
                }

                if (best > 0)
                {
                    hits[count++] = (best, i);
                }
            }

            // Higher score first, then original (already sorted) order.
            Array.Sort(hits, 0, count, HitComparer.Instance);
            var take = Math.Min(count, max);
            var result = new T[take];
            for (var i = 0; i < take; i++)
            {
                result[i] = _items[hits[i].Index];
            }

            return result;
        }
        finally
        {
            pool.Return(hits);
        }
    }

    private sealed class HitComparer : IComparer<(int Score, int Index)>
    {
        public static readonly HitComparer Instance = new();

        public int Compare((int Score, int Index) x, (int Score, int Index) y)
        {
            var c = y.Score.CompareTo(x.Score);
            return c != 0 ? c : x.Index.CompareTo(y.Index);
        }
    }
}
