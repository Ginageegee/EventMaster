namespace EventMaster.Models;

public class Event
{
    public int EventId { get; set; }
    
    public string EventName { get; set; }
    public string EventDescription { get; set; }
    
    public DateTime EventDate { get; set; }
    public DateTime EventTime { get; set; }
    
    public int OrganizerId { get; set; }
    public User Organizer { get; set; }
    
    public int? VenueId { get; set; }
    public Venue Venue { get; set; }
    
}