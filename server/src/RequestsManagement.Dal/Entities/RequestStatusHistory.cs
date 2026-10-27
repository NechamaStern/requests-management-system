namespace RequestsManagement.Dal.Entities;

public class RequestStatusHistory
{
    public long Id { get; set; }
    public int RequestId { get; set; }
    public RequestStatus PreviousStatus { get; set; }
    public RequestStatus NewStatus { get; set; }
    public DateTime ChangedAt { get; set; }
    public int ChangedByUserId { get; set; }
    public User? ChangedByUser { get; set; }
}

