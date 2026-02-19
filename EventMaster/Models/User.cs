namespace EventMaster.Models;

public class User
{
    public int UserId { get; set; }
    
    public string FirstName { get; set; }
    public string LastName { get; set; }
    
    public string Email { get; set; }
    //Hash later
    //private string passwordHash { get; set; }
    public string Password { get; set; }
    public string Role { get; set; } = Roles.Attendee;
    
    public ICollection<Event> EventsCreated { get; set; }
    public ICollection<Order> Orders { get; set; } = new List<Order>();
    
    public ICollection<Cart> Carts { get; set; } = new List<Cart>();
    
}