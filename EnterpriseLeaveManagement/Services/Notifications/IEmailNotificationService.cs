using Serilog;

namespace EnterpriseLeaveManagement.Services.Notifications;

public interface IEmailNotificationService
{
    Task SendAsync(string recipient, string subject, string body);
}

public class LogEmailNotificationService : IEmailNotificationService
{
    public Task SendAsync(string recipient, string subject, string body)
    {
        Log.Information("Mock email to {Recipient}: {Subject} - {Body}", recipient, subject, body);
        return Task.CompletedTask;
    }
}


