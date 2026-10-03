using System.Threading.Tasks;
using Contracts;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Notification.Service.Consumers;

/// <summary>
/// Fanout Exchange Consumer
/// Automatically receives all OrderPlaced events broadcasted across microservices.
/// </summary>
public class OrderPlacedNotificationConsumer : IConsumer<OrderPlaced>
{
    private readonly ILogger<OrderPlacedNotificationConsumer> _logger;

    public OrderPlacedNotificationConsumer(ILogger<OrderPlacedNotificationConsumer> logger)
    {
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<OrderPlaced> context)
    {
        _logger.LogInformation("[FANOUT EXCHANGE -> NOTIFICATION] OrderPlaced received for OrderId: {OrderId}, User: {User}. Sending customer welcome email...",
            context.Message.OrderId, context.Message.UserName);

        await Task.CompletedTask;
    }
}
