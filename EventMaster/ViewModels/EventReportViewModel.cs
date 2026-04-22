namespace EventMaster.Models.ViewModels
{
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
        public decimal Price { get; init; }
        public int QuantityAvailable { get; init; }
        public int QuantitySold { get; init; }
        public decimal Revenue => QuantitySold * Price;
    }
}