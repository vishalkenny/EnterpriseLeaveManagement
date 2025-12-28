using EnterpriseLeaveManagement.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseLeaveManagement.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();
    public DbSet<LeaveStatusHistory> LeaveStatusHistories => Set<LeaveStatusHistory>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<LeaveRequest>(entity =>
        {
            entity.HasKey(l => l.LeaveId);

            entity.Property(l => l.LeaveType)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(l => l.Reason)
                .HasMaxLength(1000);

            entity.Property(l => l.Status)
                .HasMaxLength(50)
                .IsRequired();

            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(l => l.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<LeaveStatusHistory>(entity =>
        {
            entity.HasKey(h => h.Id);

            entity.Property(h => h.PreviousStatus)
                .HasMaxLength(50);

            entity.Property(h => h.NewStatus)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(h => h.Remarks)
                .HasMaxLength(1000);
        });
    }
}



