using System.Collections.Generic;

namespace EventMaster.Models.ViewModels;

public class DashboardViewModel
{
    public List<Event> MyEvents { get; set; } = new();
    public List<Ticket> MyTickets { get; set; } = new();
}