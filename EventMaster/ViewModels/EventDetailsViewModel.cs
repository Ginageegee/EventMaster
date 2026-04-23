using System;
using System.Collections.Generic;
using EventMaster.Models;

namespace EventMaster.ViewModels;

public class EventDetailsViewModel
{
    public required Event Event { get; init; }
    public IReadOnlyList<TicketType> TicketTypes { get; init; } = Array.Empty<TicketType>();
}