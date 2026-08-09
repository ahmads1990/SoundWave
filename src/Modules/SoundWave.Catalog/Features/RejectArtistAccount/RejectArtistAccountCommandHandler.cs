using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SoundWave.Catalog.Common;
using SoundWave.Catalog.Contracts.IntegrationEvents;
using SoundWave.Catalog.Data;
using SoundWave.Catalog.Data.Entities;
using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Interfaces;

namespace SoundWave.Catalog.Features.RejectArtistAccount;

/// <summary>
/// Handles rejecting an artist account application with a reason.
/// </summary>
internal class RejectArtistAccountCommandHandler(
    CatalogDbContext dbContext,
    ICurrentUserService currentUserService,
    IPublishEndpoint publishEndpoint,
    ILogger<RejectArtistAccountCommandHandler> logger)
    : IRequestHandler<RejectArtistAccountCommand, Result<CatalogError, Guid>>
{
    public async Task<Result<CatalogError, Guid>> Handle(
        RejectArtistAccountCommand request,
        CancellationToken cancellationToken)
    {
        var approval = await dbContext.ArtistAccountApprovals
            .FirstOrDefaultAsync(a => a.Id == request.ApplicationId, cancellationToken);

        var validationError = ValidateRequest(approval);
        if (validationError != CatalogError.None)
            return Result<CatalogError, Guid>.Failure(validationError);

        await RejectApplicationAsync(approval!, request.Reason, cancellationToken);
        return Result<CatalogError, Guid>.Success(approval!.Id);
    }

    #region Private Methods

    private CatalogError ValidateRequest(ArtistAccountApproval? approval)
    {
        if (!currentUserService.IsAuthenticated || !currentUserService.UserId.HasValue || currentUserService.UserId.Value == Guid.Empty)
        {
            logger.LogWarning("Artist rejection failed — admin user is not authenticated");
            return CatalogError.UserNotAuthenticated;
        }

        if (approval is null)
        {
            logger.LogWarning("Artist rejection failed — application not found");
            return CatalogError.ArtistApplicationNotFound;
        }

        if (approval.Status != ArtistApprovalStatus.Pending)
        {
            logger.LogWarning("Artist rejection failed — application {ApplicationId} is already in status {Status}", approval.Id, approval.Status);
            return CatalogError.ArtistApplicationAlreadyProcessed;
        }

        return CatalogError.None;
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

        await publishEndpoint.Publish(new ArtistApplicationRejectedEvent(approval.Id, approval.UserId, approval.RejectionReason), cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Artist application {ApplicationId} rejected by admin {AdminId} with reason: {Reason}", approval.Id, adminUserId, reason);
        return approval;
    }

    #endregion
}
