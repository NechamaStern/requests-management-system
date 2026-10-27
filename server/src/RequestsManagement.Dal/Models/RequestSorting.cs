namespace RequestsManagement.Dal.Models;

public enum RequestSortField
{
    CreatedAt = 0,
    UpdatedAt = 1,
    Title = 2,
    OrganizationName = 3,
    Status = 4,
    Priority = 5
}

public enum SortDirection
{
    Asc = 0,
    Desc = 1
}
