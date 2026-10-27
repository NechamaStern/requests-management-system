using RequestsManagement.Dal.Entities;
using RequestsManagement.Dal.Models;
using RequestsManagement.Dal.Repositories;

namespace RequestsManagement.Tests;

// In-memory stand-in for the repository. It counts the summary queries (the cached read)
// and mimics the RowVersion check that the database does on update.
internal sealed class FakeRequestsRepository : IRequestsRepository
{
    public const int ValidUserId = 1;

    private readonly Dictionary<int, Request> _requests = [];

    public int SummaryQueries { get; private set; }

    public bool RejectNextUpdate { get; set; }

    public Request Add(int id, RequestStatus status, RequestPriority priority = RequestPriority.Medium)
    {
        var request = new Request
        {
            Id = id,
            Title = $"Request {id}",
            OrganizationName = "Org",
            Status = status,
            Priority = priority,
            RowVersion = [1],
        };
        _requests[id] = request;
        return request;
    }

    public Task<List<GroupCount<RequestStatus>>> GetCountByStatusAsync(CancellationToken cancellationToken)
    {
        SummaryQueries++;
        var counts = _requests.Values.GroupBy(r => r.Status)
            .Select(g => new GroupCount<RequestStatus>(g.Key, g.Count())).ToList();
        return Task.FromResult(counts);
    }

    public Task<List<GroupCount<RequestPriority>>> GetCountByPriorityAsync(CancellationToken cancellationToken)
    {
        var counts = _requests.Values.GroupBy(r => r.Priority)
            .Select(g => new GroupCount<RequestPriority>(g.Key, g.Count())).ToList();
        return Task.FromResult(counts);
    }

    public Task<RequestListItem?> GetItemAsync(int id, CancellationToken cancellationToken)
    {
        RequestListItem? item = _requests.TryGetValue(id, out var r)
            ? new RequestListItem(r.Id, r.Title, r.OrganizationName, r.Status, r.Priority,
                r.AssignedToUserId, null, r.CreatedAt, r.UpdatedAt, r.RowVersion)
            : null;
        return Task.FromResult(item);
    }

    public Task<Request?> GetForUpdateAsync(int id, CancellationToken cancellationToken) =>
        Task.FromResult(_requests.GetValueOrDefault(id));

    public Task<bool> ActiveUserExistsAsync(int userId, CancellationToken cancellationToken) =>
        Task.FromResult(userId == ValidUserId);

    public Task<bool> ExistsAsync(int id, CancellationToken cancellationToken) =>
        Task.FromResult(_requests.ContainsKey(id));

    public Task<bool> TryUpdateAsync(
        Request request, byte[] expectedRowVersion, RequestStatusHistory? history, CancellationToken cancellationToken)
    {
        if (RejectNextUpdate)
        {
            RejectNextUpdate = false;
            return Task.FromResult(false);
        }

        request.RowVersion = [(byte)(request.RowVersion[0] + 1)];
        return Task.FromResult(true);
    }

    public Task<PagedResult<RequestListItem>> SearchAsync(
        RequestSearchCriteria criteria, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public Task<List<StatusHistoryItem>> GetHistoryAsync(int requestId, CancellationToken cancellationToken) =>
        throw new NotSupportedException();
}
