namespace UtilityAppV2.Models;

/// <summary>
/// An incoming file whose song title matches an existing library file's title
/// but has a different full name (i.e. a different artist — likely a cover).
/// The user decides per file whether to move it or leave it in the playlist.
/// </summary>
/// <param name="IncomingFile">Full path of the source file about to be moved.</param>
/// <param name="ExistingFileName">Name of the colliding file already in the library.</param>
public readonly record struct CoverCollision(string IncomingFile, string ExistingFileName);
