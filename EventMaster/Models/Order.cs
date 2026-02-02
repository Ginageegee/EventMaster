using EventMaster.Enums;

namespace EventMaster.Models;

public class Order
{
    public int OrderId { get; set; }
    
    public int BuyerUserId { get; set; }
    public User BuyerUser { get; set; }

    public OrderStatus Status { get; set; }
    public decimal TotalAmount { get; set; }
    
    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();

}