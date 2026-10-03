using System.Threading.Tasks;
using Contracts;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Notification.Service.Consumers;

/// <summary>
/// Direct Exchange Consumer
/// Bound to Direct Exchange with RoutingKey = "email"
/// </summary>
public class EmailNotificationConsumer : IConsumer<SendNotificationEvent>
{
    private readonly ILogger<EmailNotificationConsumer> _logger;

    public EmailNotificationConsumer(ILogger<EmailNotificationConsumer> logger)
    {
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<SendNotificationEvent> context)
    {
        var msg = context.Message;
        _logger.LogInformation("[DIRECT EXCHANGE -> EMAIL] Sending Email to {Recipient}. Content: {Content}",
            msg.Recipient, msg.Content);

        await Task.CompletedTask;
    }
}
