namespace Order.Service.Commands;

public record CreateOrderCommand(int OrderId, string UserName, string ProductName);