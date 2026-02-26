using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace UtilityAppV2.Helpers;

public static class FileSystemHelper
{
    public static IEnumerable<string> GetAllFilesFromFolder(string folderPath)
    {
        return !Directory.Exists(folderPath)
            ? Array.Empty<string>()
            : Directory.GetFiles(folderPath, "*.*", SearchOption.TopDirectoryOnly);
    }

    public static string GetUniqueFilePath(string path)
    {
        if (!File.Exists(path))
        {
            return path;
        }

        string dir = Path.GetDirectoryName(path)!;
        string baseName = Path.GetFileNameWithoutExtension(path);
        string ext = Path.GetExtension(path);
        int counter = 1;
        string candidate;

        do
        {
            candidate = Path.Combine(dir, $"{baseName} ({counter}){ext}");
            counter++;
        }
        while (File.Exists(candidate));

        return candidate;
    }

    public static string RemoveInvalidFileNameChars(string input)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sb = new StringBuilder(input.Length);

        foreach (var ch in input)
        {
            if (!invalid.Contains(ch))
            {
                sb.Append(ch);
            }
        }

        return Regex.Replace(sb.ToString(), @"\s{2,}", " ").Trim();
    }

    public static void EnsureFoldersExist(params string[] folders)
    {
        foreach (var folder in folders)
        {
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
        }
    }
}