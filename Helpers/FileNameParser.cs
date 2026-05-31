namespace UtilityAppV2.Helpers;

/// <summary>
/// Single source of truth for splitting a "Song - Artist" file name.
/// Splits on the last " - " separator (not a bare '-') so names containing
/// hyphens (e.g. "Jay-Z", "AC-DC", "Re-Up") parse correctly.
/// </summary>
public static class FileNameParser
{
    public const string Separator = " - ";

    /// <summary>
    /// Returns the trimmed (Song, Artist) pair. When no separator is present the
    /// whole name is treated as the song and the artist is empty.
    /// </summary>
    public static (string Song, string Artist) SplitSongArtist(string nameWithoutExtension)
    {
        if (string.IsNullOrWhiteSpace(nameWithoutExtension))
            return (string.Empty, string.Empty);

        // Prefer the canonical spaced " - " separator so hyphenated names
        // (e.g. "99 Problems - Jay-Z") split on the real boundary.
        int idx = nameWithoutExtension.LastIndexOf(Separator, StringComparison.Ordinal);
        if (idx > 0)
        {
            string song = nameWithoutExtension[..idx].Trim();
            string artist = nameWithoutExtension[(idx + Separator.Length)..].Trim();
            return (song, artist);
        }

        // Fall back to a bare '-' for malformed names lacking the spaced separator
        // (e.g. "Digitoll -Coldrain"), so the artist is still recovered.
        int dash = nameWithoutExtension.LastIndexOf('-');
        if (dash > 0)
        {
            string song = nameWithoutExtension[..dash].Trim();
            string artist = nameWithoutExtension[(dash + 1)..].Trim();
            if (artist.Length > 0)
                return (song, artist);
        }

        return (nameWithoutExtension.Trim(), string.Empty);
    }
}
