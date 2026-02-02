namespace EventMaster.Models;

public class Seat
{
    public int SeatId { get; set; }
    
    public int SectionId { get; set; }
    public Section Section { get; set; }
    
    public string SeatNumber { get; set; }
    public string Row { get; set; }
}