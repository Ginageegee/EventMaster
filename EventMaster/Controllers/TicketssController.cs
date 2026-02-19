using EventMaster.Models;
using EventMaster.Services;
using EventMaster.ViewModels;
using EventMaster.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace EventMaster.Controllers;

public class TicketsController : Controller
{
    private const string ReceiptsKey = "TicketReceipts";
    private const string SessionEmailKey = "UserEmail";
    private const string SessionRoleKey = "UserRole";

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Purchase(int ticketTypeId, int quantity)
    {
        if (!IsAttendee())
            return RedirectToAction("Login", "Account");

        var buyerEmail = HttpContext.Session.GetString(SessionEmailKey) ?? "";

        var ticketType = InMemoryStore.GetTicketType(ticketTypeId);
        if (ticketType == null) return NotFound();

        var (ok, message, receipt) = InMemoryStore.Purchase(buyerEmail, ticketTypeId, quantity);

        if (!ok || receipt == null)
        {
            TempData["Error"] = message;
            return RedirectToAction("Details", "Events", new { id = ticketType.EventId });
        }

        var receipts = HttpContext.Session.GetJson<List<TicketReceipt>>(ReceiptsKey) ?? new List<TicketReceipt>();
        receipts.Add(receipt);
        HttpContext.Session.SetJson(ReceiptsKey, receipts);

        return View("Confirmation", receipt);
    }

    public IActionResult MyTickets()
    {
        if (!IsAttendee())
            return RedirectToAction("Login", "Account");

        var receipts = HttpContext.Session.GetJson<List<TicketReceipt>>(ReceiptsKey) ?? new List<TicketReceipt>();
        return View(receipts.OrderByDescending(r => r.PurchasedAtUtc).ToList());
    }

    private bool IsAttendee()
    {
        var role = HttpContext.Session.GetString(SessionRoleKey);
        var email = HttpContext.Session.GetString(SessionEmailKey);

        return role == Roles.Attendee && !string.IsNullOrWhiteSpace(email);
    }
}