using RequestsManagement.Dal.Entities;
using RequestsManagement.Dal.Models;

namespace RequestsManagement.Dal.Repositories;

public interface IRequestsRepository
{
    /// <summary>Number of requests per status. Statuses with no requests are omitted.</summary>
    Task<List<GroupCount<RequestStatus>>> GetCountByStatusAsync(CancellationToken cancellationToken);

    /// <summary>Number of requests per priority. Priorities with no requests are omitted.</summary>
    Task<List<GroupCount<RequestPriority>>> GetCountByPriorityAsync(CancellationToken cancellationToken);

    /// <summary>Filtered, sorted page of requests. Filtering, sorting and paging all run in the database.</summary>
    Task<PagedResult<RequestListItem>> SearchAsync(RequestSearchCriteria criteria, CancellationToken cancellationToken);

    Task<RequestListItem?> GetItemAsync(int id, CancellationToken cancellationToken);

    /// <summary>Tracked entity, to be saved with <see cref="TryUpdateAsync"/>.</summary>
    Task<Request?> GetForUpdateAsync(int id, CancellationToken cancellationToken);

    Task<bool> ActiveUserExistsAsync(int userId, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken);

    /// <summary>Status changes of one request, newest first (served by IX_RequestStatusHistory_RequestId_ChangedAt).</summary>
    Task<List<StatusHistoryItem>> GetHistoryAsync(int requestId, CancellationToken cancellationToken);

    /// <summary>
    /// Saves the tracked request and the optional history row in one transaction. The UPDATE is guarded by
    /// <paramref name="expectedRowVersion"/> in its WHERE clause. Returns false if another update won (no changes saved).
    /// </summary>
    Task<bool> TryUpdateAsync(
        Request request, byte[] expectedRowVersion, RequestStatusHistory? history, CancellationToken cancellationToken);
}
