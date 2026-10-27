using System.ComponentModel.DataAnnotations;
using RequestsManagement.Dal.Entities;

namespace RequestsManagement.Bll.Mapping.Dtos;

// Body of PUT /api/requests/{id}. The full editable state is sent, so the update is idempotent.
public sealed class UpdateRequestDto
{
    [EnumDataType(typeof(RequestStatus))]
    public RequestStatus Status { get; init; }

    [EnumDataType(typeof(RequestPriority))]
    public RequestPriority Priority { get; init; }

    /// <summary>The assignee (a user id). Null means "unassigned".</summary>
    public int? AssignedToUserId { get; init; }

    /// <summary>The RowVersion the client last read; the update succeeds only if it is still current.</summary>
    [Required]
    public byte[] RowVersion { get; init; } = [];

    /// <summary>Written to the audit history. There is no authentication yet, so the client supplies it.</summary>
    [Range(1, int.MaxValue)]
    public int ChangedByUserId { get; init; }
}
