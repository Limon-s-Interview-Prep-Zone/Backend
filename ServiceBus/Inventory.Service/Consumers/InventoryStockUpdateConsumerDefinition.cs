using System;
using GreenPipes;
using MassTransit;
using MassTransit.ConsumeConfigurators;
using MassTransit.Definition;

namespace Inventory.Service.Consumers;

public class InventoryStockUpdateConsumerDefinition : ConsumerDefinition<InventoryStockUpdateConsumer>
{
    public InventoryStockUpdateConsumerDefinition()
    {
        // Explicit queue name for the command
        EndpointName = "inventory-stock-update";

        // High throughput & resource tuning
        ConcurrentMessageLimit = 8;
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<InventoryStockUpdateConsumer> consumerConfigurator)
    {
        endpointConfigurator.PrefetchCount = 16;

        endpointConfigurator.UseMessageRetry(r =>
        {
            r.Ignore<ArgumentException>();
            r.Interval(3, TimeSpan.FromSeconds(2));
        });
    }
}
