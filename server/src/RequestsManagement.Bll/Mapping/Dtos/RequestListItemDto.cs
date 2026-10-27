namespace RequestsManagement.Bll.Mapping.Dtos;

public sealed record RequestListItemDto(
    int Id,
    string Title,
    string OrganizationName,
    string Status,
    string Priority,
    int? AssignedToUserId,
    string? AssignedTo,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    byte[] RowVersion);

public sealed record PagedResultDto<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);
