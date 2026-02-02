namespace EventMaster.Models;

public class TicketType
{
    public int TicketTypeId { get; set; }

    public int EventId { get; set; }
    public Event Event { get; set; }

    public string Name { get; set; }          
    public decimal Price { get; set; }

    public bool RequiresSeat { get; set; }    
    
    public int? QuantityAvailable { get; set; }
    
}