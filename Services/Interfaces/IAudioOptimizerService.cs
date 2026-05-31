using UtilityAppV2.Models;

namespace UtilityAppV2.Services.Interfaces;

public interface IAudioOptimizerService
{
    /// <summary>
    /// Losslessly shrinks every .mp3 in <paramref name="folder"/> by stripping
    /// embedded cover art / padding. The audio stream is copied bit-for-bit —
    /// no re-encode, no quality change. ID3 tags are preserved.
    /// </summary>
    Task<AudioOptimizeResult> OptimizeFolderAsync(
        string folder,
        IProgress<double>? progress,
        CancellationToken cancellationToken);
}
