using System.ComponentModel.DataAnnotations;

namespace EnterpriseLeaveManagement.ViewModels.Leave;

public class LeaveApplyViewModel
{
    [Required]
    [Display(Name = "Leave Type")]
    public string LeaveType { get; set; } = default!;

    [Required]
    [DataType(DataType.Date)]
    public DateTime StartDate { get; set; }

    [Required]
    [DataType(DataType.Date)]
    public DateTime EndDate { get; set; }

    [StringLength(1000)]
    public string? Reason { get; set; }
}



