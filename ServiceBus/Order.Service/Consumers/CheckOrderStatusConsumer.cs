using System.Threading.Tasks;
using Contracts;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Order.Service.Consumers;

public class CheckOrderStatusConsumer : IConsumer<CheckOrderStatus>
{
    private readonly ILogger<CheckOrderStatusConsumer> _logger;

    public CheckOrderStatusConsumer(ILogger<CheckOrderStatusConsumer> logger)
    {
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<CheckOrderStatus> context)
    {
        _logger.LogInformation("Received CheckOrderStatus request: {OrderId}", context.Message.OrderId);

        // Simulating data fetching logic
        var result = new OrderStatusResult(context.Message.OrderId, 1, "Pending for Shipment");

        _logger.LogInformation("Responding with OrderStatusResult: {OrderId}", result.OrderId);

        await context.RespondAsync(result);
    }
}