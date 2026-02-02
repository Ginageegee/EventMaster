namespace EventMaster.Models;

public class CartItem
{
    public int CartItemId { get; set; }

    public int CartId { get; set; }
    public Cart Cart { get; set; }

    public int TicketTypeId { get; set; }
    public TicketType TicketType { get; set; }
    
    public int Quantity { get; set; } = 1;
    
    public int? SeatId { get; set; }
    public Seat? Seat { get; set; }
    
    
}