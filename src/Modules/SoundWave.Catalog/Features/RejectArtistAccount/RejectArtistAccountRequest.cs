namespace SoundWave.Catalog.Features.RejectArtistAccount;

/// <summary>
/// Request contract for rejecting an artist account application.
/// </summary>
/// <param name="Reason">The reason for rejecting the application.</param>
internal record RejectArtistAccountRequest(string Reason);
