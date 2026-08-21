using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SoundWave.Catalog.Common;
using SoundWave.Catalog.Contracts.IntegrationEvents;
using SoundWave.Catalog.Data.Entities;
using SoundWave.Catalog.Data.IRepository;
using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Interfaces;

namespace SoundWave.Catalog.Features.Artists.ApproveArtistAccount;

/// <summary>
/// Handles approving an artist account application, marking status as Approved,
/// and creating the corresponding <see cref="Artist"/> profile.
/// </summary>
internal class ApproveArtistAccountCommandHandler(
    ICatalogRepository<ArtistAccountApproval> approvalRepository,
    ICatalogRepository<Artist> artistRepository,
    ICurrentUserService currentUserService,
    IPublishEndpoint publishEndpoint,
    ILogger<ApproveArtistAccountCommandHandler> logger)
    : IRequestHandler<ApproveArtistAccountCommand, Result<CatalogError, Guid>>
{
    public async Task<Result<CatalogError, Guid>> Handle(
        ApproveArtistAccountCommand request,
        CancellationToken cancellationToken)
    {
        var (approvalInfo, validationError) = await GetAndValidateAsync(request.ApplicationId, cancellationToken);
        if (validationError != CatalogError.None)
            return Result<CatalogError, Guid>.Failure(validationError);

        var artist = await ApproveAndCreateArtistAsync(approvalInfo!, cancellationToken);
        return Result<CatalogError, Guid>.Success(artist.Id);
    }

    #region Private Methods

    private async Task<(ArtistAccountApproval? Approval, CatalogError Error)> GetAndValidateAsync(
        Guid applicationId,
        CancellationToken cancellationToken)
    {
        if (!currentUserService.IsAuthenticated || !currentUserService.UserId.HasValue || currentUserService.UserId.Value == Guid.Empty)
        {
            logger.LogWarning("Artist approval rejected — admin user is not authenticated");
            return (null, CatalogError.UserNotAuthenticated);
        }

        var approval = await approvalRepository.GetAll()
            .FirstOrDefaultAsync(a => a.Id == applicationId, cancellationToken);

        if (approval is null)
        {
            logger.LogWarning("Artist approval failed — application not found");
            return (null, CatalogError.ArtistApplicationNotFound);
        }

        if (approval.Status != ArtistApprovalStatus.Pending)
        {
            logger.LogWarning("Artist approval failed — application {ApplicationId} is already in status {Status}", approval.Id, approval.Status);
            return (null, CatalogError.ArtistApplicationAlreadyProcessed);
        }

        return (approval, CatalogError.None);
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

        approvalRepository.SaveInclude(approval, nameof(ArtistAccountApproval.Status), nameof(ArtistAccountApproval.ReviewedBy), nameof(ArtistAccountApproval.ReviewedAt));

        var artist = new Artist
        {
            UserId = approval.UserId,
            StageName = approval.StageName,
            Bio = approval.Bio,
            ApprovedAt = now
        };

        await artistRepository.Add(artist, cancellationToken);
        await publishEndpoint.Publish(new ArtistApplicationApprovedEvent(approval.Id, artist.UserId, artist.Id), cancellationToken);
        await approvalRepository.SaveChanges(cancellationToken);

        logger.LogInformation("Artist application {ApplicationId} approved by admin {AdminId}. Created Artist profile {ArtistId}", approval.Id, adminUserId, artist.Id);
        return artist;
    }

    #endregion
}
