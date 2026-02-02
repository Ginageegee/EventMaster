using EventMaster.Enums;

namespace EventMaster.Models;

public class Ticket
{
    public int TicketId { get; set; }
    
    public int EventId { get; set; }
    public Event Event { get; set; }
    
    public int TicketTypeId { get; set; }
    public TicketType TicketType { get; set; }
    
    public int? SeatId { get; set; }
    public Seat? Seat { get; set; }
    
    public string QrCode { get; set; }           
    public  TicketStatus Status { get; set; }  
    
    public int? OrderId { get; set; }
    public Order? Order { get; set; }
}