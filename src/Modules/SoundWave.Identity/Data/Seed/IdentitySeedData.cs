using Microsoft.EntityFrameworkCore;
using SoundWave.Identity.Common;
using SoundWave.Identity.Data.Entites;
using SoundWave.SharedKernel.Common;

namespace SoundWave.Identity.Data.Seed;

public static class IdentitySeedData
{
    public static readonly Guid AdminUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    public static readonly Guid AdminUserProfileId = Guid.Parse("00000000-0000-0000-0000-000000000002");
    public static readonly Guid AdminProfileId = Guid.Parse("00000000-0000-0000-0000-000000000003");

    public static void Seed(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().HasData(new User
        {
            Id = AdminUserId,
            Email = "admin@soundwave.com",
            PasswordHash = "$2a$11$IuYQ6gpIFG5UdYqEi3U88.cSBGwoiZgqQOJwA37t7U7OOgmF//J5W",
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
    }
}
