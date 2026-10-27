using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using RequestsManagement.Dal.Entities;
using RequestsManagement.Dal.Models;

namespace RequestsManagement.Dal.Repositories;

internal sealed class RequestsRepository : IRequestsRepository
{
    private readonly AppDbContext _db;

    public RequestsRepository(AppDbContext db)
    {
        _db = db;
    }

    // GROUP BY runs in SQL Server (index IX_Requests_Status_CreatedAt); only one row per status is returned.
    public async Task<List<GroupCount<RequestStatus>>> GetCountByStatusAsync(
        CancellationToken cancellationToken)
    {
        return await _db.Requests
            .AsNoTracking()
            .GroupBy(r => r.Status)
            .Select(g => new GroupCount<RequestStatus>(g.Key, g.Count()))
            .ToListAsync(cancellationToken);
    }

    // GROUP BY runs in SQL Server (index IX_Requests_Priority_CreatedAt); only one row per priority is returned.
    public async Task<List<GroupCount<RequestPriority>>> GetCountByPriorityAsync(
        CancellationToken cancellationToken)
    {
        return await _db.Requests
            .AsNoTracking()
            .GroupBy(r => r.Priority)
            .Select(g => new GroupCount<RequestPriority>(g.Key, g.Count()))
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResult<RequestListItem>> SearchAsync(
        RequestSearchCriteria criteria, CancellationToken cancellationToken)
    {
        var filtered = ApplyFilters(_db.Requests.AsNoTracking(), criteria);

        var totalCount = await filtered.CountAsync(cancellationToken);

        var items = await ApplySort(filtered, criteria.SortBy, criteria.SortDirection)
            .Skip((criteria.Page - 1) * criteria.PageSize)
            .Take(criteria.PageSize)
            .Select(ToListItem)
            .ToListAsync(cancellationToken);

        return new PagedResult<RequestListItem>(items, totalCount);
    }

    public async Task<RequestListItem?> GetItemAsync(int id, CancellationToken cancellationToken)
    {
        return await _db.Requests
            .AsNoTracking()
            .Where(r => r.Id == id)
            .Select(ToListItem)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static readonly Expression<Func<Request, RequestListItem>> ToListItem = r => new RequestListItem(
        r.Id,
        r.Title,
        r.OrganizationName,
        r.Status,
        r.Priority,
        r.AssignedToUserId,
        r.AssignedToUser != null ? r.AssignedToUser.DisplayName : null,
        r.CreatedAt,
        r.UpdatedAt,
        r.RowVersion);

    public async Task<Request?> GetForUpdateAsync(int id, CancellationToken cancellationToken)
    {
        return await _db.Requests.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<bool> ActiveUserExistsAsync(int userId, CancellationToken cancellationToken)
    {
        return await _db.Users.AnyAsync(u => u.Id == userId && u.IsActive, cancellationToken);
    }

    public async Task<bool> ExistsAsync(int id, CancellationToken cancellationToken)
    {
        return await _db.Requests.AnyAsync(r => r.Id == id, cancellationToken);
    }

    // Id is the tie-breaker so entries with the same ChangedAt keep a stable order.
    public async Task<List<StatusHistoryItem>> GetHistoryAsync(int requestId, CancellationToken cancellationToken)
    {
        return await _db.RequestStatusHistory
            .AsNoTracking()
            .Where(h => h.RequestId == requestId)
            .OrderByDescending(h => h.ChangedAt)
            .ThenByDescending(h => h.Id)
            .Select(h => new StatusHistoryItem(
                h.Id,
                h.RequestId,
                h.PreviousStatus,
                h.NewStatus,
                h.ChangedAt,
                h.ChangedByUserId,
                h.ChangedByUser!.DisplayName))
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> TryUpdateAsync(
        Request request, byte[] expectedRowVersion, RequestStatusHistory? history, CancellationToken cancellationToken)
    {
        // EF puts the original RowVersion in the UPDATE's WHERE clause; 0 affected rows means a concurrent update won.
        _db.Entry(request).Property(r => r.RowVersion).OriginalValue = expectedRowVersion;
        if (history is not null)
        {
            _db.RequestStatusHistory.Add(history);
        }

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            // Drop the failed entity and its pending history row, so they are not retried by a later SaveChanges.
            _db.ChangeTracker.Clear();
            return false;
        }
    }

    private static IQueryable<Request> ApplyFilters(IQueryable<Request> query, RequestSearchCriteria c)
    {
        if (c.Status is { } status)
        {
            query = query.Where(r => r.Status == status);
        }

        if (c.Priority is { } priority)
        {
            query = query.Where(r => r.Priority == priority);
        }

        // Prefix match, so IX_Requests_OrganizationName_CreatedAt can be used for a seek.
        if (!string.IsNullOrWhiteSpace(c.OrganizationName))
        {
            var organization = c.OrganizationName.Trim();
            query = query.Where(r => r.OrganizationName.StartsWith(organization));
        }

        if (!string.IsNullOrWhiteSpace(c.AssignedTo))
        {
            var assignee = c.AssignedTo.Trim();
            query = query.Where(r => r.AssignedToUser != null &&
                (r.AssignedToUser.DisplayName.Contains(assignee) || r.AssignedToUser.UserName.Contains(assignee)));
        }

        if (c.CreatedFrom is { } from)
        {
            var fromDate = from.ToDateTime(TimeOnly.MinValue);
            query = query.Where(r => r.CreatedAt >= fromDate);
        }

        // Exclusive upper bound at the next midnight keeps the whole "to" day and stays index friendly.
        if (c.CreatedTo is { } to)
        {
            var beforeDate = to.AddDays(1).ToDateTime(TimeOnly.MinValue);
            query = query.Where(r => r.CreatedAt < beforeDate);
        }

        // Contains translates to LIKE '%text%' (parameterized and escaped by EF Core).
        if (!string.IsNullOrWhiteSpace(c.Search))
        {
            var text = c.Search.Trim();
            query = query.Where(r => r.Title.Contains(text) || r.OrganizationName.Contains(text));
        }

        return query;
    }

    // Whitelisted sort fields only (never a raw column name from the client). Id is the tie-breaker
    // so pages are stable when the sort values are equal.
    private static IOrderedQueryable<Request> ApplySort(
        IQueryable<Request> query, RequestSortField field, SortDirection direction)
    {
        var desc = direction == SortDirection.Desc;
        return field switch
        {
            RequestSortField.UpdatedAt => OrderBy(query, r => r.UpdatedAt, desc),
            RequestSortField.Title => OrderBy(query, r => r.Title, desc),
            RequestSortField.OrganizationName => OrderBy(query, r => r.OrganizationName, desc),
            RequestSortField.Status => OrderBy(query, r => r.Status, desc),
            RequestSortField.Priority => OrderBy(query, r => r.Priority, desc),
            _ => OrderBy(query, r => r.CreatedAt, desc),
        };
    }

    private static IOrderedQueryable<Request> OrderBy<TKey>(
        IQueryable<Request> query, Expression<Func<Request, TKey>> key, bool desc)
    {
        var ordered = desc ? query.OrderByDescending(key) : query.OrderBy(key);
        return ordered.ThenBy(r => r.Id);
    }
}
