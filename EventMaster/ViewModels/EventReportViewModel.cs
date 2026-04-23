using System;
using System.Collections.Generic;
using EventMaster.Models;

namespace EventMaster.Models.ViewModels;

public class EventReportViewModel
{
    public required Event Event { get; init; }

    public int TotalTicketsSold { get; init; }
    public decimal TotalRevenue { get; init; }

    public IReadOnlyList<TicketTypeReport> TicketTypeReports { get; init; } 
        = Array.Empty<TicketTypeReport>();

    public IReadOnlyList<Ticket> SoldTickets { get; init; } 
        = Array.Empty<Ticket>();
}

public class TicketTypeReport
{
    public required string Name { get; init; }

    // Optional: current configured price on the ticket type
    public decimal CurrentPrice { get; init; }

    public int QuantityAvailable { get; init; }
    public int QuantitySold { get; init; }

    // Revenue from actual ticket prices (supports historical pricing)
    public decimal Revenue { get; init; }
}