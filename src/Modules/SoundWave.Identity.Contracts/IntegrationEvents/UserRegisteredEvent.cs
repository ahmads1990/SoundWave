namespace SoundWave.Identity.Contracts.IntegrationEvents;

/// <summary>
/// Integration event published by the Identity module when a new user registers.
/// Consumed by other modules to provision user-specific resources (e.g., Liked Songs playlist).
/// </summary>
public record UserRegisteredEvent(Guid UserId, string Email, string DisplayName);
