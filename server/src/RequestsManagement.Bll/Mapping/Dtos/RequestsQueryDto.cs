using System.ComponentModel.DataAnnotations;
using RequestsManagement.Dal.Entities;
using RequestsManagement.Dal.Models;

namespace RequestsManagement.Bll.Mapping.Dtos;

// Bound from the query string of GET /api/requests. Invalid values produce a 400 ProblemDetails.
public sealed class RequestsQueryDto : IValidatableObject
{
    public const int MaxPageSize = 100;
    public const int DefaultPageSize = 20;

    [MaxLength(200)]
    public string? Search { get; init; }
    public RequestStatus? Status { get; init; }
    public RequestPriority? Priority { get; init; }

    [MaxLength(200)]
    public string? OrganizationName { get; init; }

    [MaxLength(200)]
    public string? AssignedTo { get; init; }
    public DateOnly? CreatedFrom { get; init; }
    public DateOnly? CreatedTo { get; init; }

    public RequestSortField SortBy { get; init; } = RequestSortField.CreatedAt;
    public SortDirection SortDirection { get; init; } = SortDirection.Desc;

    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, MaxPageSize)]
    public int PageSize { get; init; } = DefaultPageSize;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (CreatedFrom > CreatedTo)
        {
            yield return new ValidationResult(
                "CreatedFrom must not be later than CreatedTo.",
                [nameof(CreatedFrom), nameof(CreatedTo)]);
        }
    }
}
