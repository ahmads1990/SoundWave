namespace SoundWave.Catalog.Features.Artists.ApproveArtistAccount;

/// <summary>
/// Request contract for approving an artist account application.
/// </summary>
/// <param name="ApplicationId">The unique ID of the artist account application.</param>
internal record ApproveArtistAccountRequest(Guid ApplicationId);
