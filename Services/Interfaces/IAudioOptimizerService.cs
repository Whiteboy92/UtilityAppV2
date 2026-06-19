using UtilityAppV2.Models;

namespace UtilityAppV2.Services.Interfaces;

public interface IAudioOptimizerService
{
    /// <summary>
    /// Shrinks every .mp3 in <paramref name="folder"/> by re-encoding to LAME V0
    /// (~245 kbps VBR) — transparent quality, audibly identical to the bloated
    /// 320 kbps CBR yt-dlp produces. Cover art is dropped, ID3 tags preserved.
    /// A file is replaced only when the result is actually smaller.
    /// </summary>
    Task<AudioOptimizeResult> OptimizeFolderAsync(
        string folder,
        IProgress<double>? progress,
        CancellationToken cancellationToken);
}
