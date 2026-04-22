namespace EventMaster.ViewModels;

public class AddToCartViewModel
{
    public int TicketTypeId { get; set; }
    public int Quantity { get; set; } = 1;
    public int? SeatId { get; set; }
}