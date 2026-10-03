namespace Contracts;

public class CheckOrderStatus
{
    public int OrderId { get; set; }

    public CheckOrderStatus(int orderId)
    {
        OrderId = orderId;
    }
}