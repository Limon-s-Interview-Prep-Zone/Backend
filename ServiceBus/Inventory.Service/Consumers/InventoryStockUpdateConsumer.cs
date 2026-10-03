using System.Threading.Tasks;
using Contracts;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Inventory.Service.Consumers
{
    public class InventoryStockUpdateConsumer : IConsumer<UpdateInventoryStock>
    {
        private readonly ILogger<InventoryStockUpdateConsumer> _logger;

        public InventoryStockUpdateConsumer(ILogger<InventoryStockUpdateConsumer> logger)
        {
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<UpdateInventoryStock> context)
        {
            var message = context.Message;
            _logger.LogInformation("Processing command: UpdateInventoryStock for Order ID: {OrderId}, Product: {Product}, Quantity: {Qty}",
                message.OrderId, message.ProductName, message.Quantity);

            // Simulating stock update logic
            await Task.Delay(50);

            _logger.LogInformation("Stock successfully updated for Order ID: {OrderId}", message.OrderId);
        }
    }
}
