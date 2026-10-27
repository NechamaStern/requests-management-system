using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using AutoMapper;
using RequestsManagement.Bll.Exceptions;
using RequestsManagement.Bll.Mapping.Dtos;
using RequestsManagement.Dal.Entities;
using RequestsManagement.Dal.Models;
using RequestsManagement.Dal.Repositories;

namespace RequestsManagement.Bll.Services;

internal sealed class RequestsService : IRequestsService
{
    private const string SummaryCacheKey = "requests-summary";

    private static readonly TimeSpan SummaryCacheDuration = TimeSpan.FromMinutes(5);

    private readonly IRequestsRepository _requestsRepository;
    private readonly IMemoryCache _cache;
    private readonly IMapper _mapper;
    private readonly ILogger<RequestsService> _logger;

    public RequestsService(
        IRequestsRepository requestsRepository, IMemoryCache cache, IMapper mapper, ILogger<RequestsService> logger)
    {
        _requestsRepository = requestsRepository;
        _cache = cache;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<RequestListItemDto> GetAsync(int id, CancellationToken cancellationToken)
    {
        var item = await _requestsRepository.GetItemAsync(id, cancellationToken)
            ?? throw new NotFoundException(ErrorMessages.RequestNotFound(id));
        return _mapper.Map<RequestListItemDto>(item);
    }

    public async Task<IReadOnlyList<StatusHistoryItemDto>> GetHistoryAsync(int id, CancellationToken cancellationToken)
    {
        var history = await _requestsRepository.GetHistoryAsync(id, cancellationToken);

        // An empty history is valid for an existing request; the existence check runs only in that case.
        if (history.Count == 0 && !await _requestsRepository.ExistsAsync(id, cancellationToken))
        {
            throw new NotFoundException(ErrorMessages.RequestNotFound(id));
        }

        return _mapper.Map<List<StatusHistoryItemDto>>(history);
    }

    public async Task<RequestListItemDto> UpdateAsync(
        int id, UpdateRequestDto dto, CancellationToken cancellationToken)
    {
        var request = await _requestsRepository.GetForUpdateAsync(id, cancellationToken)
            ?? throw new NotFoundException(ErrorMessages.RequestNotFound(id));

        // Fast path: the client's copy is already stale. The database re-checks it atomically on save.
        if (!request.RowVersion.SequenceEqual(dto.RowVersion))
        {
            throw new ConflictException(ErrorMessages.StaleRequest);
        }

        var statusChanged = dto.Status != request.Status;
        if (statusChanged && !StatusTransitions.IsAllowed(request.Status, dto.Status))
        {
            throw new BusinessValidationException(nameof(dto.Status),
                ErrorMessages.TransitionNotAllowed(request.Status, dto.Status));
        }

        if (!await _requestsRepository.ActiveUserExistsAsync(dto.ChangedByUserId, cancellationToken))
        {
            throw new BusinessValidationException(nameof(dto.ChangedByUserId), ErrorMessages.InvalidActingUser);
        }

        var assigneeChanged = dto.AssignedToUserId != request.AssignedToUserId;
        if (assigneeChanged && dto.AssignedToUserId is { } assigneeId &&
            !await _requestsRepository.ActiveUserExistsAsync(assigneeId, cancellationToken))
        {
            throw new BusinessValidationException(nameof(dto.AssignedToUserId), ErrorMessages.InvalidAssignee);
        }

        var priorityChanged = dto.Priority != request.Priority;
        if (statusChanged || priorityChanged || assigneeChanged)
        {
            var now = DateTime.UtcNow;
            var previousStatus = request.Status;

            request.Status = dto.Status;
            request.Priority = dto.Priority;
            request.AssignedToUserId = dto.AssignedToUserId;
            request.UpdatedAt = now;

            // History and request are saved in one transaction, so there is no change without an audit row.
            var history = statusChanged
                ? new RequestStatusHistory
                {
                    RequestId = id,
                    PreviousStatus = previousStatus,
                    NewStatus = dto.Status,
                    ChangedAt = now,
                    ChangedByUserId = dto.ChangedByUserId,
                }
                : null;

            if (!await _requestsRepository.TryUpdateAsync(request, dto.RowVersion, history, cancellationToken))
            {
                _logger.LogWarning("Concurrency conflict updating request {RequestId}", id);
                throw new ConflictException(ErrorMessages.StaleRequest);
            }

            _logger.LogInformation(
                "Request {RequestId} updated by user {UserId} (status {PreviousStatus} -> {NewStatus})",
                id, dto.ChangedByUserId, previousStatus, dto.Status);

            // The dashboard counts depend only on status and priority.
            if (statusChanged || priorityChanged)
            {
                InvalidateSummaryCache();
            }
        }

        return await GetAsync(id, cancellationToken);
    }

    public async Task<BulkStatusResultDto> BulkUpdateStatusAsync(
        BulkStatusUpdateDto dto, CancellationToken cancellationToken)
    {
        if (!await _requestsRepository.ActiveUserExistsAsync(dto.ChangedByUserId, cancellationToken))
        {
            throw new BusinessValidationException(nameof(dto.ChangedByUserId), ErrorMessages.InvalidActingUser);
        }

        var succeeded = new List<RequestListItemDto>();
        var failed = new List<BulkStatusFailureDto>();

        // Sequential on purpose: one DbContext, and each request is committed (or rejected) independently.
        foreach (var item in dto.Items)
        {
            var failure = await TryChangeStatusAsync(item, dto.Status, dto.ChangedByUserId, cancellationToken);
            if (failure is not null)
            {
                failed.Add(failure);
                continue;
            }

            succeeded.Add(await GetAsync(item.Id, cancellationToken));
        }

        if (succeeded.Count > 0)
        {
            InvalidateSummaryCache();
        }

        _logger.LogInformation(
            "Bulk status change to {NewStatus} by user {UserId}: {Succeeded} succeeded, {Failed} failed",
            dto.Status, dto.ChangedByUserId, succeeded.Count, failed.Count);

        return new BulkStatusResultDto(dto.Items.Count, succeeded.Count, failed.Count, succeeded, failed);
    }

    private async Task<BulkStatusFailureDto?> TryChangeStatusAsync(
        BulkStatusUpdateItemDto item, RequestStatus newStatus, int userId, CancellationToken cancellationToken)
    {
        var request = await _requestsRepository.GetForUpdateAsync(item.Id, cancellationToken);
        if (request is null)
        {
            return new BulkStatusFailureDto(item.Id, BulkFailureReason.NotFound, "לא נמצאה פניה מתאימה.");
        }

        if (!request.RowVersion.SequenceEqual(item.RowVersion))
        {
            return new BulkStatusFailureDto(item.Id, BulkFailureReason.Conflict, ErrorMessages.StaleRequest);
        }

        if (request.Status == newStatus)
        {
            return new BulkStatusFailureDto(item.Id, BulkFailureReason.AlreadyInStatus, "הפניה כבר נמצאת בסטטוס המבוקש.");
        }

        if (!StatusTransitions.IsAllowed(request.Status, newStatus))
        {
            return new BulkStatusFailureDto(item.Id, BulkFailureReason.InvalidTransition,
                ErrorMessages.TransitionNotAllowed(request.Status, newStatus));
        }

        var now = DateTime.UtcNow;
        var history = new RequestStatusHistory
        {
            RequestId = item.Id,
            PreviousStatus = request.Status,
            NewStatus = newStatus,
            ChangedAt = now,
            ChangedByUserId = userId,
        };
        request.Status = newStatus;
        request.UpdatedAt = now;

        if (!await _requestsRepository.TryUpdateAsync(request, item.RowVersion, history, cancellationToken))
        {
            _logger.LogWarning("Concurrency conflict in bulk update of request {RequestId}", item.Id);
            return new BulkStatusFailureDto(item.Id, BulkFailureReason.Conflict, ErrorMessages.StaleRequest);
        }

        return null;
    }

    public async Task<RequestsSummaryDto> GetSummaryAsync(CancellationToken cancellationToken)
    {
        return (await _cache.GetOrCreateAsync(SummaryCacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = SummaryCacheDuration;
            return await LoadSummaryAsync(cancellationToken);
        }))!;
    }

    public async Task<PagedResultDto<RequestListItemDto>> SearchAsync(
        RequestsQueryDto query, CancellationToken cancellationToken)
    {
        var criteria = _mapper.Map<RequestSearchCriteria>(query);
        var result = await _requestsRepository.SearchAsync(criteria, cancellationToken);

        var totalPages = (int)Math.Ceiling(result.TotalCount / (double)query.PageSize);
        return new PagedResultDto<RequestListItemDto>(
            _mapper.Map<List<RequestListItemDto>>(result.Items),
            query.Page,
            query.PageSize,
            result.TotalCount,
            totalPages);
    }

    private void InvalidateSummaryCache()
    {
        _cache.Remove(SummaryCacheKey);
    }

    private async Task<RequestsSummaryDto> LoadSummaryAsync(CancellationToken cancellationToken)
    {
        var byStatus = _mapper.Map<List<CountItemDto>>(
            await _requestsRepository.GetCountByStatusAsync(cancellationToken));
        var byPriority = _mapper.Map<List<CountItemDto>>(
            await _requestsRepository.GetCountByPriorityAsync(cancellationToken));

        return new RequestsSummaryDto(byStatus.Sum(x => x.Count), byStatus, byPriority);
    }
}
