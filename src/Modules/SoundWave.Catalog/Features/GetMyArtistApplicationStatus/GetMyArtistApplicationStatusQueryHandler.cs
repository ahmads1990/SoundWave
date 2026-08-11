using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SoundWave.Catalog.Common;
using SoundWave.Catalog.Data;
using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Interfaces;

namespace SoundWave.Catalog.Features.GetMyArtistApplicationStatus;

/// <summary>
/// Handles retrieving the current authenticated user's artist account application status.
/// </summary>
internal class GetMyArtistApplicationStatusQueryHandler(
    CatalogReadDbContext dbContext,
    ICurrentUserService currentUserService,
    ILogger<GetMyArtistApplicationStatusQueryHandler> logger)
    : IRequestHandler<GetMyArtistApplicationStatusQuery, Result<CatalogError, ArtistApplicationStatusDto>>
{
    public async Task<Result<CatalogError, ArtistApplicationStatusDto>> Handle(
        GetMyArtistApplicationStatusQuery request,
        CancellationToken cancellationToken)
    {
        if (!currentUserService.IsAuthenticated || currentUserService.UserId == Guid.Empty)
        {
            logger.LogWarning("Unauthorized attempt to retrieve artist application status");
            return Result<CatalogError, ArtistApplicationStatusDto>.Failure(CatalogError.UserNotAuthenticated, "User is not authenticated.");
        }

        var userId = currentUserService.UserId!.Value;
        logger.LogInformation("Retrieving artist application status for User {UserId}", userId);

        var application = await GetApplicationStatusAsync(userId, cancellationToken);
        if (application is null)
        {
            logger.LogInformation("No artist application found for User {UserId}", userId);
            return Result<CatalogError, ArtistApplicationStatusDto>.Failure(CatalogError.ArtistApplicationNotFound, "No artist application found for the current user.");
        }

        return Result<CatalogError, ArtistApplicationStatusDto>.Success(application);
    }

    #region Private Methods

    private async Task<ArtistApplicationStatusDto?> GetApplicationStatusAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        return await dbContext.ArtistAccountApprovals
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.CreatedDate)
            .Select(a => new ArtistApplicationStatusDto(a.Id, a.UserId, a.StageName, a.Bio, a.Status, a.RejectionReason, a.ReviewedAt, a.CreatedDate))
            .FirstOrDefaultAsync(cancellationToken);
    }

    #endregion
}


