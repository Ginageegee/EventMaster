namespace EventMaster.Models;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

[Index(nameof(Auth0UserId), IsUnique = true)]
public class User
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int UserId { get; set; }

    [Required]
    public string Auth0UserId { get; set; }

    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }

    public ICollection<Event> EventsCreated { get; set; } = new List<Event>();

    public ICollection<Order> Orders { get; set; } = new List<Order>();

    public ICollection<Cart> Carts { get; set; } = new List<Cart>();
}