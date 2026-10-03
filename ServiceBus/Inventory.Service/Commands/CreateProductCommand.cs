namespace Inventory.Service.Commands;

public record CreateProductCommand(int Id, string Code, string ProductName);