namespace Contracts;

public record UpdateInventoryStock(int OrderId, string ProductName, int Quantity);
