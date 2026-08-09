namespace SoundWave.Catalog.Contracts.IntegrationEvents;

public record ArtistApplicationApprovedEvent(Guid ApplicationId, Guid UserId, Guid ArtistId);
