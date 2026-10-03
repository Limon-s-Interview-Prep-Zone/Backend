using System;
using System.Threading;
using System.Threading.Tasks;
using Contracts;
using MassTransit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Order.Service.Commands;

namespace Order.Service.Controllers;

[ApiController]
[Route("[controller]")]
public class OrdersController : ControllerBase
{
    private readonly ILogger<OrdersController> _logger;
    private readonly IPublishEndpoint _publisher;
    private readonly ISendEndpointProvider _sendEndpointProvider;
    private readonly IRequestClient<CheckOrderStatus> _client;

    public OrdersController(
        ILogger<OrdersController> logger,
        IPublishEndpoint publisher,
        ISendEndpointProvider sendEndpointProvider,
        IRequestClient<CheckOrderStatus> client)
    {
        _logger = logger;
        _publisher = publisher;
        _sendEndpointProvider = sendEndpointProvider;
        _client = client;
    }

    /// <summary>
    /// Demonstrates 1-to-many Event Publishing (Pub/Sub)
    /// Both Order.Service and Inventory.Service can subscribe to OrderPlaced.
    /// </summary>
    [HttpPost]
    [Route("CreateOrder")]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderCommand command)
    {
        _logger.LogInformation("Publishing OrderPlaced event for Order ID: {OrderId}", command.OrderId);

        await _publisher.Publish(new OrderPlaced(command.OrderId, command.UserName), context =>
        {
            context.Headers.Set("correlation-id", Guid.NewGuid().ToString());
            context.Headers.Set("event-source", "OrderService");
        });

        return Ok(new { Message = "OrderPlaced event published successfully", command.OrderId });
    }

    /// <summary>
    /// Demonstrates 1-to-1 Command Sending (Point-to-Point)
    /// Directly targets a specific queue on the consumer service.
    /// </summary>
    [HttpPost]
    [Route("SendInventoryUpdate")]
    public async Task<IActionResult> SendInventoryUpdate([FromQuery] int orderId, [FromQuery] string product, [FromQuery] int qty)
    {
        _logger.LogInformation("Sending UpdateInventoryStock command to queue for Order ID: {OrderId}", orderId);

        var sendEndpoint = await _sendEndpointProvider.GetSendEndpoint(new Uri("queue:inventory-stock-update"));
        await sendEndpoint.Send(new UpdateInventoryStock(orderId, product, qty));

        return Ok(new { Message = "UpdateInventoryStock command sent successfully", orderId });
    }

    /// <summary>
    /// Demonstrates Request/Response pattern (RPC over Messaging)
    /// Sends a request and waits for OrderStatusResult.
    /// </summary>
    [HttpPost]
    [Route("check-order-status")]
    public async Task<ActionResult> CheckOrderStatus([FromQuery] int orderId, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Sending request to check order status for order ID: {OrderId}", orderId);
            var response = await _client.GetResponse<OrderStatusResult>(
                new CheckOrderStatus(orderId),
                cancellationToken,
                timeout: RequestTimeout.After(s: 30));

            return Ok(response.Message);
        }
        catch (RequestTimeoutException ex)
        {
            _logger.LogError(ex, "Timeout waiting for response for order ID: {OrderId}", orderId);
            return StatusCode(StatusCodes.Status504GatewayTimeout, "Timeout waiting for response");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking order status for order ID: {OrderId}", orderId);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while checking order status");
        }
    }
}