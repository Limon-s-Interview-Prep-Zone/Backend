using System.Threading.Tasks;
using Contracts;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Notification.Service.Consumers;

/// <summary>
/// Topic Exchange Consumer
/// Bound with wildcard RoutingKey = "payment.#"
/// Consumes all payment events regardless of provider or status for analytics.
/// </summary>
public class AnalyticsPaymentConsumer : IConsumer<PaymentProcessedEvent>
{
    private readonly ILogger<AnalyticsPaymentConsumer> _logger;

    public AnalyticsPaymentConsumer(ILogger<AnalyticsPaymentConsumer> logger)
    {
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<PaymentProcessedEvent> context)
    {
        var msg = context.Message;
        _logger.LogInformation("[TOPIC EXCHANGE -> ANALYTICS] Recording payment metrics for OrderId: {OrderId}, Method: {Method}, Status: {Status}",
            msg.OrderId, msg.PaymentMethod, msg.Status);

        await Task.CompletedTask;
    }
}
