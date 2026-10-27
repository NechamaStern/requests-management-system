using RequestsManagement.Dal.Entities;

namespace RequestsManagement.Dal.Models;

// All filters are optional and combined with AND. Dates are inclusive (whole days).
public sealed class RequestSearchCriteria
{
    public string? Search { get; init; }
    public RequestStatus? Status { get; init; }
    public RequestPriority? Priority { get; init; }
    public string? OrganizationName { get; init; }
    public string? AssignedTo { get; init; }
    public DateOnly? CreatedFrom { get; init; }
    public DateOnly? CreatedTo { get; init; }
    public RequestSortField SortBy { get; init; } = RequestSortField.CreatedAt;
    public SortDirection SortDirection { get; init; } = SortDirection.Desc;
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
