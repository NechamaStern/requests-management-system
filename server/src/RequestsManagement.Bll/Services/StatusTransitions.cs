using RequestsManagement.Dal.Entities;

namespace RequestsManagement.Bll.Services;

// The single source of truth for the allowed status workflow. Completed is final.
internal static class StatusTransitions
{
    private static readonly Dictionary<RequestStatus, RequestStatus[]> Allowed = new()
    {
        [RequestStatus.New] = [RequestStatus.InProgress],
        [RequestStatus.InProgress] = [RequestStatus.Waiting, RequestStatus.Completed],
        [RequestStatus.Waiting] = [RequestStatus.InProgress, RequestStatus.Completed],
        [RequestStatus.Completed] = [],
    };

    public static bool IsAllowed(RequestStatus from, RequestStatus to) => Allowed[from].Contains(to);
}
