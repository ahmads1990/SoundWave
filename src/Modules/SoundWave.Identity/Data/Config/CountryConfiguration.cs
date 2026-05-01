using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SoundWave.Identity.Common;
using SoundWave.Identity.Data.Entites;

namespace SoundWave.Identity.Data.Config;

internal class CountryConfiguration : IEntityTypeConfiguration<Country>
{
    public void Configure(EntityTypeBuilder<Country> builder)
    {
        builder.ToTable("Countries", Constants.SCHEMA_NAME);

        builder.HasKey(c => c.Id);

        // int PK — DB-generated identity (this is a lookup table, not a business entity)
        builder.Property(c => c.Id)
            .ValueGeneratedOnAdd();

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(100);

        // ISO 3166-1 alpha-2 code e.g. "US", "SA"
        builder.Property(c => c.Code)
            .IsRequired()
            .HasMaxLength(2)
            .IsFixedLength();

        builder.HasIndex(c => c.Code)
            .IsUnique();
    }
}
