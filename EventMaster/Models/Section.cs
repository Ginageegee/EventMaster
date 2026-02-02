namespace EventMaster.Models;

public class Section
{
    public int SectionId { get; set; }
    
    public int VenueId { get; set; }
    public Venue Venue { get; set; }
    
    public string SectionDescription { get; set; }
    
    public ICollection<Seat> Seats { get; set; }
    
}