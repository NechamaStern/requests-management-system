using RequestsManagement.Dal.Entities;

namespace RequestsManagement.Dal.Models;

// Projection read straight from SQL; the entity is never materialized.
public sealed record RequestListItem(
    int Id,
    string Title,
    string OrganizationName,
    RequestStatus Status,
    RequestPriority Priority,
    int? AssignedToUserId,
    string? AssignedTo,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    byte[] RowVersion);

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount);
