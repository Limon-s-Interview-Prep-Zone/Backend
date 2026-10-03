using System.Threading.Tasks;
using Contracts;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Notification.Service.Consumers;

/// <summary>
/// Direct Exchange Consumer
/// Bound to Direct Exchange with RoutingKey = "sms"
/// </summary>
public class SmsNotificationConsumer : IConsumer<SendNotificationEvent>
{
    private readonly ILogger<SmsNotificationConsumer> _logger;

    public SmsNotificationConsumer(ILogger<SmsNotificationConsumer> logger)
    {
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<SendNotificationEvent> context)
    {
        var msg = context.Message;
        _logger.LogInformation("[DIRECT EXCHANGE -> SMS] Sending SMS to {Recipient}. Content: {Content}",
            msg.Recipient, msg.Content);

        await Task.CompletedTask;
    }
}
