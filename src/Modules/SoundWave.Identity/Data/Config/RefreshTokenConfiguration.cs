using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SoundWave.Identity.Common;
using SoundWave.Identity.Data.Entites;

namespace SoundWave.Identity.Data.Config;

internal class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens", Constants.SCHEMA_NAME);

        builder.HasKey(rt => rt.Id);

        builder.Property(rt => rt.Id)
            .ValueGeneratedNever();

        builder.Property(rt => rt.TokenHash)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(rt => rt.ExpiresAt)
            .IsRequired();

        // RevokedAt is nullable — null means the token is still valid
        builder.Property(rt => rt.RevokedAt);

        builder.Property(rt => rt.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        // Index for fast lookup during token validation
        builder.HasIndex(rt => rt.TokenHash);

        // Index to quickly find all tokens for a user (e.g. bulk revoke on password change)
        builder.HasIndex(rt => rt.UserId);

        // Note: soft-delete filter is NOT applied here intentionally —
        // revoked/expired tokens should remain queryable for audit purposes.
        // Cleanup is done by a background worker that hard-deletes old rows.
    }
}
