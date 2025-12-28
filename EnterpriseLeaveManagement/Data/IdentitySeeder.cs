using Microsoft.AspNetCore.Identity;

namespace EnterpriseLeaveManagement.Data;

public static class IdentitySeeder
{
    private static readonly string[] Roles = { "Employee", "Manager", "HR" };

    public static async Task SeedRolesAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        foreach (var role in Roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        var defaultUsers = new[]
        {
            new { Email = "employee1@company.com", FullName = "Default Employee", Role = "Employee" },
            new { Email = "manager1@company.com",  FullName = "Default Manager",  Role = "Manager" },
            new { Email = "hr1@company.com",       FullName = "Default HR",       Role = "HR" }
        };

        const string defaultPassword = "Password@123";

        foreach (var info in defaultUsers)
        {
            var user = await userManager.FindByEmailAsync(info.Email);
            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = info.Email,
                    Email = info.Email,
                    FullName = info.FullName,
                    EmailConfirmed = true
                };

                var createResult = await userManager.CreateAsync(user, defaultPassword);
                if (createResult.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, info.Role);
                }
            }
            else if (!await userManager.IsInRoleAsync(user, info.Role))
            {
                await userManager.AddToRoleAsync(user, info.Role);
            }
        }
    }
}

