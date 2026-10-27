using RequestsManagement.Dal.Entities;

namespace RequestsManagement.Dal.Models;

// Projection read straight from SQL; ChangedBy is the display name of the user who made the change.
public sealed record StatusHistoryItem(
    long Id,
    int RequestId,
    RequestStatus PreviousStatus,
    RequestStatus NewStatus,
    DateTime ChangedAt,
    int ChangedByUserId,
    string ChangedBy);
