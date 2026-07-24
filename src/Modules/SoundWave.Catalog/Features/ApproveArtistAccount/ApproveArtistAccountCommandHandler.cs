using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SoundWave.Catalog.Common;
using SoundWave.Catalog.Data;
using SoundWave.Catalog.Data.Entities;
using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Interfaces;

namespace SoundWave.Catalog.Features.ApproveArtistAccount;

/// <summary>
/// Handles approving an artist account application, marking status as Approved,
/// and creating the corresponding <see cref="Artist"/> profile.
/// </summary>
internal class ApproveArtistAccountCommandHandler(
    CatalogDbContext dbContext,
    ICurrentUserService currentUserService,
    ILogger<ApproveArtistAccountCommandHandler> logger)
    : IRequestHandler<ApproveArtistAccountCommand, Result<CatalogError, Guid>>
{
    public async Task<Result<CatalogError, Guid>> Handle(
        ApproveArtistAccountCommand request,
        CancellationToken cancellationToken)
    {
        var approval = await dbContext.ArtistAccountApprovals
            .FirstOrDefaultAsync(a => a.Id == request.ApplicationId, cancellationToken);

        var validationError = ValidateRequest(approval);
        if (validationError != CatalogError.None)
            return Result<CatalogError, Guid>.Failure(validationError);

        var artist = await ApproveAndCreateArtistAsync(approval!, cancellationToken);
        return Result<CatalogError, Guid>.Success(artist.Id);
    }

    #region Private Methods

    private CatalogError ValidateRequest(ArtistAccountApproval? approval)
    {
        if (!currentUserService.IsAuthenticated || !currentUserService.UserId.HasValue || currentUserService.UserId.Value == Guid.Empty)
        {
            logger.LogWarning("Artist approval rejected — admin user is not authenticated");
            return CatalogError.UserNotAuthenticated;
        }

        if (approval is null)
        {
            logger.LogWarning("Artist approval failed — application not found");
            return CatalogError.ArtistApplicationNotFound;
        }

        if (approval.Status != ArtistApprovalStatus.Pending)
        {
            logger.LogWarning("Artist approval failed — application {ApplicationId} is already in status {Status}", approval.Id, approval.Status);
            return CatalogError.ArtistApplicationAlreadyProcessed;
        }

        return CatalogError.None;
    }

    private async Task<Artist> ApproveAndCreateArtistAsync(
        ArtistAccountApproval approval,
        CancellationToken cancellationToken)
    {
        var adminUserId = currentUserService.UserId!.Value;
        var now = DateTime.UtcNow;

        approval.Status = ArtistApprovalStatus.Approved;
        approval.ReviewedBy = adminUserId;
        approval.ReviewedAt = now;

        var artist = new Artist
        {
            UserId = approval.UserId,
            StageName = approval.StageName,
            Bio = approval.Bio,
            ApprovedAt = now
        };

        await dbContext.Artists.AddAsync(artist, cancellationToken);

        // TODO (Phase 1.7): Create & add OutboxMessage (ArtistApproved) to dbContext.OutboxMessages before SaveChangesAsync

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Artist application {ApplicationId} approved by admin {AdminId}. Created Artist profile {ArtistId}", approval.Id, adminUserId, artist.Id);
        return artist;
    }

    #endregion
}
