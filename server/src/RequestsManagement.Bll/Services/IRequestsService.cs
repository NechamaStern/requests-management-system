using RequestsManagement.Bll.Mapping.Dtos;

namespace RequestsManagement.Bll.Services;

public interface IRequestsService
{
    /// <summary>Cached aggregations (by status and by priority) for the dashboard.</summary>
    Task<RequestsSummaryDto> GetSummaryAsync(CancellationToken cancellationToken);

    Task<PagedResultDto<RequestListItemDto>> SearchAsync(RequestsQueryDto query, CancellationToken cancellationToken);

    /// <summary>Status change history, newest first. Throws NotFoundException if the request does not exist.</summary>
    Task<IReadOnlyList<StatusHistoryItemDto>> GetHistoryAsync(int id, CancellationToken cancellationToken);

    /// <summary>Throws NotFoundException if the request does not exist.</summary>
    Task<RequestListItemDto> GetAsync(int id, CancellationToken cancellationToken);

    /// <summary>
    /// Updates status, priority and assignee with optimistic concurrency. Throws NotFoundException,
    /// ConflictException (stale RowVersion) or BusinessValidationException (invalid transition / user).
    /// </summary>
    Task<RequestListItemDto> UpdateAsync(int id, UpdateRequestDto dto, CancellationToken cancellationToken);

    /// <summary>
    /// Partial success: changes the status of up to 100 requests, each in its own transaction with the same rules as
    /// <see cref="UpdateAsync"/> (existence, RowVersion, transition, audit row). Per-item problems are reported in the
    /// result; only an invalid acting user throws BusinessValidationException (nothing is changed).
    /// </summary>
    Task<BulkStatusResultDto> BulkUpdateStatusAsync(BulkStatusUpdateDto dto, CancellationToken cancellationToken);
}
