namespace Contracts;

public class OrderStatusResult
{
    public int OrderId { get; set; }
    public int StatusCode { get; set; }
    public string StatusText { get; set; }

    public OrderStatusResult(int orderId, int statusCode, string statusText)
    {
        OrderId = orderId;
        StatusCode = statusCode;
        StatusText = statusText;
    }
}