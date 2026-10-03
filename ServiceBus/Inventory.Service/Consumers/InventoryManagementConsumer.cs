using System.Text.Json;
using System.Threading.Tasks;
using Contracts;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Inventory.Service.Consumers;

public class InventoryManagementConsumer : IConsumer<ProductCreationPlaced>
{
    private readonly ILogger<InventoryManagementConsumer> _logger;

    public InventoryManagementConsumer(ILogger<InventoryManagementConsumer> logger)
    {
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ProductCreationPlaced> context)
    {
        _logger.LogInformation("New product created: {Message}", JsonSerializer.Serialize(context.Message));
        await Task.CompletedTask;
    }
}