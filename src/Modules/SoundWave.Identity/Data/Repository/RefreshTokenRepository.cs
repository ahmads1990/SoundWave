using SoundWave.Identity.Data.Entites;
using SoundWave.Identity.Data.IRepository;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace SoundWave.Identity.Data.Repository;

internal class RefreshTokenRepository : IdentityRepository<RefreshToken>, IRefreshTokenRepository
{
    private readonly IdentityDbContext _dbContext;

    public RefreshTokenRepository(IdentityDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc/>
    public async Task<RefreshToken?> GetValidRefreshTokenAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await GetByCondition(rt => rt.UserId == userId && !rt.RevokedAt.HasValue)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
