using Microsoft.AspNetCore.Identity;

namespace EnterpriseLeaveManagement.Data;

public class ApplicationUser : IdentityUser
{
    public string? FullName { get; set; }
}


