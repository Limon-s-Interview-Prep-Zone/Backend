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

    // =========================================================================
    // 1. FANOUT EXCHANGE: Broadcast to all interested subscribers
    // =========================================================================
    [HttpPost]
    [Route("publish-fanout")]
    public async Task<IActionResult> PublishFanout([FromBody] CreateOrderCommand command)
    {
        _logger.LogInformation("[FANOUT] Publishing OrderPlaced event for Order ID: {OrderId}", command.OrderId);

        await _publisher.Publish(new OrderPlaced(command.OrderId, command.UserName), context =>
        {
            context.Headers.Set("correlation-id", Guid.NewGuid().ToString());
            context.Headers.Set("event-source", "OrderService");
        });

        return Ok(new
        {
            Exchange = "Fanout",
            Message = "OrderPlaced broadcasted. Consumed by Order.Service, Inventory.Service, and Notification.Service",
            command.OrderId
        });
    }

    // =========================================================================
    // 2. DIRECT EXCHANGE: Exact routing key matching ("email" or "sms")
    // =========================================================================
    [HttpPost]
    [Route("publish-direct")]
    public async Task<IActionResult> PublishDirect(
        [FromQuery] string recipient,
        [FromQuery] string content,
        [FromQuery] string channel = "email") // "email" or "sms"
    {
        var normalizedChannel = channel.Trim().ToLowerInvariant();
        _logger.LogInformation("[DIRECT] Publishing notification to channel routing key: '{Channel}'", normalizedChannel);

        await _publisher.Publish(new SendNotificationEvent(Guid.NewGuid(), recipient, content, normalizedChannel), context =>
        {
            context.SetRoutingKey(normalizedChannel);
        });

        return Ok(new
        {
            Exchange = "Direct",
            RoutingKey = normalizedChannel,
            Message = $"Notification routed strictly to the {normalizedChannel} queue"
        });
    }

    // =========================================================================
    // 3. TOPIC EXCHANGE: Pattern matching with wildcards (* and #)
    // =========================================================================
    [HttpPost]
    [Route("publish-topic")]
    public async Task<IActionResult> PublishTopic(
        [FromQuery] int orderId,
        [FromQuery] decimal amount,
        [FromQuery] string method = "card",   // "card", "paypal", "crypto"
        [FromQuery] string status = "failed") // "success", "failed"
    {
        var routingKey = $"payment.{method.ToLowerInvariant()}.{status.ToLowerInvariant()}";
        _logger.LogInformation("[TOPIC] Publishing PaymentProcessedEvent with routing key: '{RoutingKey}'", routingKey);

        await _publisher.Publish(new PaymentProcessedEvent(Guid.NewGuid(), orderId, amount, method, status), context =>
        {
            context.SetRoutingKey(routingKey);
        });

        return Ok(new
        {
            Exchange = "Topic",
            RoutingKey = routingKey,
            Note = status.Equals("failed", StringComparison.OrdinalIgnoreCase)
                ? "Picked up by BOTH 'payment.*.failed' (FraudDetection) and 'payment.#' (Analytics)"
                : "Picked up ONLY by 'payment.#' (Analytics)"
        });
    }

    // =========================================================================
    // 4. HEADERS EXCHANGE: Attribute-based routing using AMQP message headers
    // =========================================================================
    [HttpPost]
    [Route("publish-headers")]
    public async Task<IActionResult> PublishHeaders(
        [FromQuery] string fileName,
        [FromQuery] string fileType = "pdf",
        [FromQuery] string tier = "enterprise") // "enterprise" or "standard"
    {
        _logger.LogInformation("[HEADERS] Publishing DocumentProcessedEvent with header tier: '{Tier}'", tier);

        await _publisher.Publish(new DocumentProcessedEvent(Guid.NewGuid(), fileName, fileType, "Finance"), context =>
        {
            context.Headers.Set("tier", tier.ToLowerInvariant());
        });

        return Ok(new
        {
            Exchange = "Headers",
            HeaderMatch = $"tier = {tier}",
            Note = tier.Equals("enterprise", StringComparison.OrdinalIgnoreCase)
                ? "Matched header 'tier=enterprise' -> Processed by EnterpriseDocumentConsumer"
                : "Did not match 'tier=enterprise' -> Skipped by EnterpriseDocumentConsumer"
        });
    }

    // =========================================================================
    // 5. POINT-TO-POINT COMMAND: Sent directly to a destination queue
    // =========================================================================
    [HttpPost]
    [Route("send-command")]
    public async Task<IActionResult> SendCommand([FromQuery] int orderId, [FromQuery] string product, [FromQuery] int qty)
    {
        _logger.LogInformation("[COMMAND] Sending UpdateInventoryStock directly to queue:inventory-stock-update");

        var sendEndpoint = await _sendEndpointProvider.GetSendEndpoint(new Uri("queue:inventory-stock-update"));
        await sendEndpoint.Send(new UpdateInventoryStock(orderId, product, qty));

        return Ok(new
        {
            Pattern = "Point-to-Point Command",
            Queue = "inventory-stock-update",
            orderId
        });
    }

    // =========================================================================
    // 6. REQUEST/RESPONSE: RPC over messaging bus
    // =========================================================================
    [HttpPost]
    [Route("check-order-status")]
    public async Task<ActionResult> CheckOrderStatus([FromQuery] int orderId, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("[RPC] Sending request to check order status for ID: {OrderId}", orderId);
            var response = await _client.GetResponse<OrderStatusResult>(
                new CheckOrderStatus(orderId),
                cancellationToken,
                timeout: RequestTimeout.After(s: 30));

            return Ok(response.Message);
        }
        catch (RequestTimeoutException ex)
        {
            _logger.LogError(ex, "Timeout waiting for response for ID: {OrderId}", orderId);
            return StatusCode(StatusCodes.Status504GatewayTimeout, "Timeout waiting for response");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking order status for ID: {OrderId}", orderId);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred");
        }
    }
}