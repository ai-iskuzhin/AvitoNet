namespace AvitoNet.Messenger;

/// <summary>Text helpers for Avito's messenger limits.</summary>
public static class AvitoText
{
    /// <summary>
    /// Split text into chunks of at most <paramref name="maxLength"/> (Avito: 1000) characters, preferring paragraph,
    /// then line, then sentence, then word boundaries; a single over-long word is cut hard. Chunks are trimmed; empty
    /// ones are dropped. Never splits a surrogate pair.
    /// </summary>
    public static IReadOnlyList<string> Split(string text, int maxLength = AvitoMessengerClient.MaxTextLength)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxLength, 2);
        var chunks = new List<string>();
        var rest = (text ?? "").Trim();
        while (rest.Length > maxLength)
        {
            var cut = BestCut(rest, maxLength);
            var head = rest[..cut].TrimEnd();
            if (head.Length > 0)
            {
                chunks.Add(head);
            }

            rest = rest[cut..].TrimStart();
        }

        if (rest.Length > 0)
        {
            chunks.Add(rest);
        }

        return chunks;
    }

    private static int BestCut(string s, int max)
    {
        var window = s[..max];
        foreach (var sep in new[] { "\n\n", "\n", ". ", "! ", "? ", " " })
        {
            var at = window.LastIndexOf(sep, StringComparison.Ordinal);
            if (at >= max / 2)
            {
                return at + sep.Length;
            }
        }

        // Hard cut, but not between the halves of a surrogate pair.
        return char.IsHighSurrogate(s[max - 1]) ? max - 1 : max;
    }
}
