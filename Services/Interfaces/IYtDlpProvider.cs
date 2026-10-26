namespace UtilityAppV2.Services.Interfaces
{
    /// <summary>
    /// Supplies the yt-dlp executable from a writable per-user location and keeps
    /// it current. yt-dlp is deliberately not shipped inside the app folder: it
    /// releases often and self-updates in place (yt-dlp -U), which fails when the
    /// binary sits next to the exe under Program Files or gets overwritten by every
    /// republish. Storing it under %LOCALAPPDATA% makes updates stick.
    /// </summary>
    public interface IYtDlpProvider
    {
        /// <summary>Absolute path where yt-dlp.exe lives (whether or not it exists yet).</summary>
        string YtDlpPath { get; }

        /// <summary>
        /// Guarantees yt-dlp.exe is present, downloading the latest release from
        /// GitHub if it is missing. Returns the executable path.
        /// </summary>
        Task<string> EnsureAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Best-effort self-update. Downloads yt-dlp first if absent, then runs
        /// "yt-dlp -U". Never throws: an offline machine or a transient failure
        /// leaves the existing binary in place. Intended to run in the background
        /// at startup.
        /// </summary>
        Task EnsureAndUpdateAsync(CancellationToken cancellationToken = default);
    }
}
