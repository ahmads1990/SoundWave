using Microsoft.EntityFrameworkCore;
using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Entities;
using SoundWave.SharedKernel.Interfaces;

namespace SoundWave.SharedKernel.Data;

/// <summary>
/// Dedicated EF Core database context for the SharedKernel schema.
/// Owns shared entities like <see cref="FileMetadata"/> and MassTransit outbox tables.
/// </summary>
public class SharedKernelDbContext : BaseModuleDbContext
{
    /// <inheritdoc />
    protected override string SchemaName => SharedConstants.SchemaName;

    /// <summary>
    /// Gets or sets the database set of file metadata records.
    /// </summary>
    public DbSet<FileMetadata> FileMetadata { get; set; } = null!;

    /// <summary>
    /// Initializes a new instance of the <see cref="SharedKernelDbContext"/> class.
    /// </summary>
    /// <param name="options">The options to be used by the DbContext.</param>
    /// <param name="currentUserService">The current authenticated user service.</param>
    public SharedKernelDbContext(
        DbContextOptions<SharedKernelDbContext> options,
        ICurrentUserService currentUserService) : base(options, currentUserService)
    {
    }
}
