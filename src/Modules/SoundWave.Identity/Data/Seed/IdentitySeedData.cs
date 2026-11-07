using Microsoft.EntityFrameworkCore;
using SoundWave.Identity.Common;
using SoundWave.Identity.Data.Entites;
using SoundWave.SharedKernel.Common;

namespace SoundWave.Identity.Data.Seed;

public static class IdentitySeedData
{
    // Admin User
    public static readonly Guid AdminUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    public static readonly Guid AdminUserProfileId = Guid.Parse("00000000-0000-0000-0000-000000000002");
    public static readonly Guid AdminProfileId = Guid.Parse("00000000-0000-0000-0000-000000000003");

    // Artist User (Luna Echo)
    public static readonly Guid ArtistUserId = Guid.Parse("00000000-0000-0000-0000-000000000010");
    public static readonly Guid ArtistUserProfileId = Guid.Parse("00000000-0000-0000-0000-000000000011");

    // Listener User (Alex Rivera)
    public static readonly Guid ListenerUserId = Guid.Parse("00000000-0000-0000-0000-000000000020");
    public static readonly Guid ListenerUserProfileId = Guid.Parse("00000000-0000-0000-0000-000000000021");

    // Pre-computed BCrypt hash for "Password123!"
    public const string DefaultPasswordHash = "$2a$11$AoniWBuNHQEcD61jvgci4OWzGxe4m54kcNj/hYXC2lzwH/hxJOoe2";

    public static void Seed(ModelBuilder modelBuilder)
    {
        // 1. Admin Account
        modelBuilder.Entity<User>().HasData(new User
        {
            Id = AdminUserId,
            Email = "admin@soundwave.com",
            PasswordHash = DefaultPasswordHash,
            Role = UserRole.Admin,
            IsEmailVerified = true,
            CreatedDate = new DateTime(2026, 6, 13, 0, 0, 0, DateTimeKind.Utc),
            IsDeleted = false
        });

        modelBuilder.Entity<UserProfile>().HasData(new UserProfile
        {
            Id = AdminUserProfileId,
            UserId = AdminUserId,
            FirstName = "System",
            LastName = "Admin",
            DisplayName = "Administrator",
            Language = "en",
            Gender = Gender.Other,
            CreatedDate = new DateTime(2026, 6, 13, 0, 0, 0, DateTimeKind.Utc),
            IsDeleted = false
        });

        modelBuilder.Entity<AdminProfile>().HasData(new AdminProfile
        {
            Id = AdminProfileId,
            UserId = AdminUserId,
            Department = "IT",
            CanApproveArtists = true,
            CanLockUsers = true,
            CanViewAuditLogs = true,
            CreatedDate = new DateTime(2026, 6, 13, 0, 0, 0, DateTimeKind.Utc),
            IsDeleted = false
        });

        // 2. Artist Account
        modelBuilder.Entity<User>().HasData(new User
        {
            Id = ArtistUserId,
            Email = "artist@soundwave.com",
            PasswordHash = DefaultPasswordHash,
            Role = UserRole.Artist,
            IsEmailVerified = true,
            CreatedDate = new DateTime(2026, 11, 7, 0, 0, 0, DateTimeKind.Utc),
            IsDeleted = false
        });

        modelBuilder.Entity<UserProfile>().HasData(new UserProfile
        {
            Id = ArtistUserProfileId,
            UserId = ArtistUserId,
            FirstName = "Luna",
            LastName = "Echo",
            DisplayName = "Luna Echo",
            Language = "en",
            Gender = Gender.Female,
            CreatedDate = new DateTime(2026, 11, 7, 0, 0, 0, DateTimeKind.Utc),
            IsDeleted = false
        });

        // 3. Listener Account
        modelBuilder.Entity<User>().HasData(new User
        {
            Id = ListenerUserId,
            Email = "listener@soundwave.com",
            PasswordHash = DefaultPasswordHash,
            Role = UserRole.Listener,
            IsEmailVerified = true,
            CreatedDate = new DateTime(2026, 11, 7, 0, 0, 0, DateTimeKind.Utc),
            IsDeleted = false
        });

        modelBuilder.Entity<UserProfile>().HasData(new UserProfile
        {
            Id = ListenerUserProfileId,
            UserId = ListenerUserId,
            FirstName = "Alex",
            LastName = "Rivera",
            DisplayName = "Alex Rivera",
            Language = "en",
            Gender = Gender.Male,
            CreatedDate = new DateTime(2026, 11, 7, 0, 0, 0, DateTimeKind.Utc),
            IsDeleted = false
        });
    }
}
