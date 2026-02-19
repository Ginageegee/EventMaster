namespace EventMaster.ViewModels;

public class TicketReceipt
{
    public string ReceiptId { get; set; } = "";
    public DateTime PurchasedAtUtc { get; set; }

    public string BuyerEmail { get; set; } = "";

    public int EventId { get; set; }
    public string EventName { get; set; } = "";

    public int TicketTypeId { get; set; }
    public string TicketTypeName { get; set; } = "";

    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Total { get; set; }

    public List<string> TicketNumbers { get; set; } = new();
}