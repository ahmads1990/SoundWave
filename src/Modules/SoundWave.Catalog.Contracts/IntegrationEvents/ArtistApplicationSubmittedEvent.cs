namespace SoundWave.Catalog.Contracts.IntegrationEvents;

public record ArtistApplicationSubmittedEvent(Guid ApplicationId, Guid UserId, string StageName);
