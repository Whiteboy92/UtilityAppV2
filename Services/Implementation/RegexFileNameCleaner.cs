using System.Text.RegularExpressions;
using UtilityAppV2.Services.Interfaces;

namespace UtilityAppV2.Services.Implementation;

public class RegexFileNameCleaner : IRegexFileNameCleaner
{
    private static readonly string[] UnwantedPatterns =
    {
        @"\(\s*Official\s+Music\s+Video\s*\)",
        @"\(\s*Official\s+Lyric\s+Video\s*\)",
        @"\[COPYRIGHT\s+FREE\s+Music\]",
        @"\(\s*Lyric\s+Video\s*\)",
        @"\(\s*Official\s+Video\s*\)",
        @"\(\s*Official\s+Audio\s*\)",
        @"\[\s*Official\s+Visualizer\s*\]",
        @"\(\s*Official\s+Visualizer\s*\)",
        @"\(\s*Visualizer\s*\)",
        @"\(\s*Audio\s*\)",
        @"\(\s*HD\s*\)",
        @"\(\s*4K\s*\)",
        @"\(\s*Hardstyle\s*\)",
        @"\(\s*frenchcore\s*\)",
        @"french\s*core",
        @"\(\s*Lyrics\s*\)",
        @"\[\s*Lyrics\s*\]",
        @"\(\s*Music\s+Video\s*\)",
        @"\[\s*Music\s+Video\s*\]",
        @"\(\s*Prod\..*?\)",
        @"\(\s*ft\..*?\)",
        @"\(\s*feat\..*?\)",
        @"\(\s*Explicit\s*\)",
        @"\[\s*Explicit\s*\]",
        @"\(\s*Clean\s*\)",
        @"\(\s*Remastered\s*\)",
        "\"",
        "'",
    };

    public string CleanFileName(string fileBaseName)
    {
        if (string.IsNullOrWhiteSpace(fileBaseName)) return string.Empty;

        string res = fileBaseName;
        foreach (var pattern in UnwantedPatterns)
        {
            res = Regex.Replace(res, pattern, "", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        }

        res = Regex.Replace(res, @"\s{2,}", " ").Trim();
        res = res.Trim('-', ' ', '_');
        return res;
    }

    public (string Song, string Artist) ParseParts(string cleanedName)
    {
        if (string.IsNullOrWhiteSpace(cleanedName))
            return (string.Empty, string.Empty);

        int idx = cleanedName.LastIndexOf('-');
        if (idx <= 0)
            return (cleanedName.Trim(), string.Empty);

        string left = cleanedName.Substring(0, idx).Trim();
        string right = cleanedName.Substring(idx + 1).Trim();

        return (left, right);
    }
}
