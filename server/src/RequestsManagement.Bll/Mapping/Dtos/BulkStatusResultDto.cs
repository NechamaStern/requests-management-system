namespace RequestsManagement.Bll.Mapping.Dtos;

public enum BulkFailureReason
{
    NotFound,
    Conflict,
    InvalidTransition,
    AlreadyInStatus,
}

public sealed record BulkStatusFailureDto(int Id, BulkFailureReason Reason, string Message);

/// <summary>Partial-success result: the request itself is valid (200), each item succeeded or failed on its own.</summary>
public sealed record BulkStatusResultDto(
    int RequestedCount,
    int SucceededCount,
    int FailedCount,
    IReadOnlyList<RequestListItemDto> Succeeded,
    IReadOnlyList<BulkStatusFailureDto> Failed);
