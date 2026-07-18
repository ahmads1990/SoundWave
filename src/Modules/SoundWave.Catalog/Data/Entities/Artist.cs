using SoundWave.SharedKernel.Entities;

namespace SoundWave.Catalog.Data.Entities;

/// <summary>
/// Represents an approved artist profile.
/// This row only exists after an <see cref="ArtistAccountApproval"/> is approved by an admin.
/// <c>UserId</c> is a cross-module reference to <c>Identity.Users</c> — no FK constraint at the DB level.
/// </summary>
internal class Artist : BaseEntity
{
    /// <summary>Cross-module ref to Identity.Users. No DB-level FK. UNIQUE.</summary>
    public Guid UserId { get; set; }

    public string StageName { get; set; } = string.Empty;

    public string? Bio { get; set; }

    // Denormalized counters — updated by application logic, not DB constraints
    public int FollowerCount { get; set; }
    public int MonthlyListeners { get; set; }
    public long TotalStreams { get; set; }

    public DateTime? ApprovedAt { get; set; }

    // Navigation
    public ICollection<AlbumArtist> AlbumArtists { get; set; } = [];
    public ICollection<TrackArtist> TrackArtists { get; set; } = [];
}

