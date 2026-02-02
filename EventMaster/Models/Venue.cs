namespace EventMaster.Models;

public class Venue
{
    public int VenueId { get; set; }
    public string Name { get; set; }
    public string Address { get; set; }
    public string Capacity { get; set; }
    public string Phonenumber { get; set; }
    public string VenueDescription { get; set; }
    
    public ICollection<Section> Sections { get; set; } = new List<Section>();

}