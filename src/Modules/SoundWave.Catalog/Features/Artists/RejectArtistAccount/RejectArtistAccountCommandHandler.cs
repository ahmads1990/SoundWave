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

namespace SoundWave.Catalog.Features.Artists.RejectArtistAccount;

/// <summary>
/// Handles rejecting an artist account application with a reason.
/// </summary>
internal class RejectArtistAccountCommandHandler(
    ICatalogRepository<ArtistAccountApproval> approvalRepository,
    ICurrentUserService currentUserService,
    IPublishEndpoint publishEndpoint,
    ILogger<RejectArtistAccountCommandHandler> logger)
    : IRequestHandler<RejectArtistAccountCommand, Result<CatalogError, Guid>>
{
    public async Task<Result<CatalogError, Guid>> Handle(
        RejectArtistAccountCommand request,
        CancellationToken cancellationToken)
    {
        var (approval, validationError) = await GetAndValidateAsync(request.ApplicationId, cancellationToken);
        if (validationError != CatalogError.None)
            return Result<CatalogError, Guid>.Failure(validationError);

        await RejectApplicationAsync(approval!, request.Reason, cancellationToken);
        return Result<CatalogError, Guid>.Success(approval!.Id);
    }

    #region Private Methods

    private async Task<(ArtistAccountApproval? Approval, CatalogError Error)> GetAndValidateAsync(
        Guid applicationId,
        CancellationToken cancellationToken)
    {
        if (!currentUserService.IsAuthenticated || !currentUserService.UserId.HasValue || currentUserService.UserId.Value == Guid.Empty)
        {
            logger.LogWarning("Artist rejection failed — admin user is not authenticated");
            return (null, CatalogError.UserNotAuthenticated);
        }

        var approval = await approvalRepository.GetAll()
            .FirstOrDefaultAsync(a => a.Id == applicationId, cancellationToken);

        if (approval is null)
        {
            logger.LogWarning("Artist rejection failed — application not found");
            return (null, CatalogError.ArtistApplicationNotFound);
        }

        if (approval.Status != ArtistApprovalStatus.Pending)
        {
            logger.LogWarning("Artist rejection failed — application {ApplicationId} is already in status {Status}", approval.Id, approval.Status);
            return (null, CatalogError.ArtistApplicationAlreadyProcessed);
        }

        return (approval, CatalogError.None);
    }

    private async Task<ArtistAccountApproval> RejectApplicationAsync(
        ArtistAccountApproval approval,
        string reason,
        CancellationToken cancellationToken)
    {
        var adminUserId = currentUserService.UserId!.Value;
        var now = DateTime.UtcNow;

        approval.Status = ArtistApprovalStatus.Rejected;
        approval.RejectionReason = reason;
        approval.ReviewedBy = adminUserId;
        approval.ReviewedAt = now;

        approvalRepository.SaveInclude(approval, nameof(ArtistAccountApproval.Status), nameof(ArtistAccountApproval.RejectionReason), nameof(ArtistAccountApproval.ReviewedBy), nameof(ArtistAccountApproval.ReviewedAt));

        await publishEndpoint.Publish(new ArtistApplicationRejectedEvent(approval.Id, approval.UserId, approval.RejectionReason), cancellationToken);

        await approvalRepository.SaveChanges(cancellationToken);

        logger.LogInformation("Artist application {ApplicationId} rejected by admin {AdminId} with reason: {Reason}", approval.Id, adminUserId, reason);
        return approval;
    }

    #endregion
}
