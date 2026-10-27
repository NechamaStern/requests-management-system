using System.ComponentModel.DataAnnotations;
using RequestsManagement.Dal.Entities;

namespace RequestsManagement.Bll.Mapping.Dtos;

// Body of POST /api/requests/bulk-status. Every request carries the RowVersion the client last read.
public sealed class BulkStatusUpdateDto : IValidatableObject
{
    public const int MaxItems = 100;

    [EnumDataType(typeof(RequestStatus))]
    public RequestStatus Status { get; init; }

    [Required, MinLength(1), MaxLength(MaxItems)]
    public List<BulkStatusUpdateItemDto> Items { get; init; } = [];

    [Range(1, int.MaxValue)]
    public int ChangedByUserId { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Items.Select(i => i.Id).Distinct().Count() != Items.Count)
        {
            yield return new ValidationResult("Each request may appear only once.", [nameof(Items)]);
        }
    }
}

public sealed class BulkStatusUpdateItemDto
{
    [Range(1, int.MaxValue)]
    public int Id { get; init; }

    [Required]
    public byte[] RowVersion { get; init; } = [];
}
