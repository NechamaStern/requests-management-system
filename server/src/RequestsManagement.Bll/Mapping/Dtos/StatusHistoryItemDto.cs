namespace RequestsManagement.Bll.Mapping.Dtos;

public sealed record StatusHistoryItemDto(
    long Id,
    int RequestId,
    string PreviousStatus,
    string NewStatus,
    DateTime ChangedAt,
    int ChangedByUserId,
    string ChangedBy);
