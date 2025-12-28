namespace EnterpriseLeaveManagement.Models;

public class LeaveStatusHistory
{
    public int Id { get; set; }

    public int LeaveId { get; set; }

    public string? PreviousStatus { get; set; }

    public string NewStatus { get; set; } = default!;

    public string ChangedBy { get; set; } = default!;

    public DateTime ChangedOn { get; set; } = DateTime.UtcNow;

    public string? Remarks { get; set; }

    public LeaveRequest? LeaveRequest { get; set; }
}



