namespace SoundWave.Catalog.Contracts.IntegrationEvents;

public record ArtistApplicationRejectedEvent(Guid ApplicationId, Guid UserId, string RejectionReason);
