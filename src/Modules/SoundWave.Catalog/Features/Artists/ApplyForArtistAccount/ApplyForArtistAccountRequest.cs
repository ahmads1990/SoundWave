namespace SoundWave.Catalog.Features.Artists.ApplyForArtistAccount;

/// <summary>
/// Request contract for submitting an application for an artist account.
/// </summary>
/// <param name="StageName">The desired artist stage name.</param>
/// <param name="Bio">Optional biography or details provided by the applicant.</param>
internal record ApplyForArtistAccountRequest(string StageName, string? Bio);
