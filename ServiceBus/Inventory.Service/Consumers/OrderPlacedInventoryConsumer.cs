using System.Threading.Tasks;
using Contracts;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Inventory.Service.Consumers
{
    public class OrderPlacedInventoryConsumer : IConsumer<OrderPlaced>
    {
        private readonly ILogger<OrderPlacedInventoryConsumer> _logger;

        public OrderPlacedInventoryConsumer(ILogger<OrderPlacedInventoryConsumer> logger)
        {
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<OrderPlaced> context)
        {
            _logger.LogInformation("Inventory.Service received OrderPlaced event! Order ID: {OrderId}, User: {User}. Reserving inventory...",
                context.Message.OrderId, context.Message.UserName);

            await Task.Delay(50);

            _logger.LogInformation("Inventory reserved successfully for Order ID: {OrderId}", context.Message.OrderId);
        }
    }
}
