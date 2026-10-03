using System.Threading.Tasks;
using Contracts;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Notification.Service.Consumers;

/// <summary>
/// Topic Exchange Consumer
/// Bound with wildcard RoutingKey = "payment.*.failed"
/// Intercepts any failed payment across any payment provider.
/// </summary>
public class FraudDetectionConsumer : IConsumer<PaymentProcessedEvent>
{
    private readonly ILogger<FraudDetectionConsumer> _logger;

    public FraudDetectionConsumer(ILogger<FraudDetectionConsumer> logger)
    {
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<PaymentProcessedEvent> context)
    {
        var msg = context.Message;
        _logger.LogWarning("[TOPIC EXCHANGE -> FRAUD ALERT] Detected failed payment! OrderId: {OrderId}, Method: {Method}, Amount: {Amount}",
            msg.OrderId, msg.PaymentMethod, msg.Amount);

        await Task.CompletedTask;
    }
}
