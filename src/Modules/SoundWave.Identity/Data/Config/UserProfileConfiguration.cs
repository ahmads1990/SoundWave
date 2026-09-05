using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SoundWave.Identity.Common;
using SoundWave.Identity.Data.Entites;

namespace SoundWave.Identity.Data.Config;

internal class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        builder.ToTable("UserProfiles", Constants.SCHEMA_NAME);

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .ValueGeneratedNever();

        builder.Property(p => p.FirstName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(p => p.LastName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(p => p.DisplayName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(p => p.ProfilePicUrl)
            .HasMaxLength(500);

        builder.Property(p => p.CoverImageUrl)
            .HasMaxLength(500);

        builder.Property(p => p.PhoneNumber)
            .HasMaxLength(20);

        builder.Property(p => p.Language)
            .IsRequired()
            .HasMaxLength(10)
            .HasDefaultValue("en");

        builder.Property(p => p.Gender)
            .IsRequired();

        builder.Property(p => p.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        // FK to Countries lookup table — nullable because user may not set country
        builder.HasOne(p => p.Country)
            .WithMany()
            .HasForeignKey(p => p.CountryId)
            .OnDelete(DeleteBehavior.SetNull);

        // Unique index on UserId — enforces the 1:1 from this side
        builder.HasIndex(p => p.UserId)
            .IsUnique();

        builder.HasQueryFilter(p => !p.IsDeleted);
    }
}
