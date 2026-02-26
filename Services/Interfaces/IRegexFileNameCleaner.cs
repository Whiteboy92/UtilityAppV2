namespace UtilityAppV2.Services.Interfaces;

public interface IRegexFileNameCleaner
{
    string CleanFileName(string fileBaseName);

    (string Song, string Artist) ParseParts(string cleanedName);
}