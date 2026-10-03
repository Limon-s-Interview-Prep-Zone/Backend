using System.Threading.Tasks;
using Contracts;
using Inventory.Service.Commands;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Inventory.Service.Controllers;

[ApiController]
[Route("[controller]")]
public class ProductsController : ControllerBase
{
    private readonly ILogger<ProductsController> _logger;
    private readonly IPublishEndpoint _publisher;

    public ProductsController(ILogger<ProductsController> logger, IPublishEndpoint publisher)
    {
        _logger = logger;
        _publisher = publisher;
    }

    [HttpPost]
    [Route("CreateProduct")]
    public async Task<IActionResult> CreateProductCommand([FromBody] CreateProductCommand command)
    {
        _logger.LogInformation("Publishing ProductCreationPlaced for product ID: {Id}", command.Id);

        await _publisher.Publish(new ProductCreationPlaced(command.Id, command.Code, command.ProductName));

        return Ok(new { Message = "ProductCreationPlaced event published successfully", command.Id });
    }
}