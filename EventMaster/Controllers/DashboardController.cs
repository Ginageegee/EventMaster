using EventMaster.Data;
using EventMaster.Models;
using EventMaster.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text;

namespace EventMaster.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly ApplicationDbContext _context;

    public DashboardController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> CreateEvent()
    {
        Console.WriteLine("HIT: Dashboard/CreateEvent (GET)");

        var user = await GetCurrentUserAsync();
        if (user == null)
            return RedirectToAction("PostLogin", "Account");

        ViewBag.Venues = await _context.Venues.ToListAsync();

        var model = new Event
        {
            OrganizerId = user.UserId
        };

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> CreateEvent(
        Event model,
        List<TicketType> TicketTypes)
    {
        Console.WriteLine("HIT: Dashboard/CreateEvent (POST)");

        var user = await GetCurrentUserAsync();
        if (user == null)
            return RedirectToAction("PostLogin", "Account");

        if (!ModelState.IsValid)
        {
            ViewBag.Venues = await _context.Venues.ToListAsync();
            return View(model);
        }

        // Assign organizer
        model.OrganizerId = user.UserId;

        // Combine EventDate + EventTime into a single DateTime
        model.EventTime = model.EventDate.Date
            .AddHours(model.EventTime.Hour)
            .AddMinutes(model.EventTime.Minute);

        // Save event first so it gets an EventId
        _context.Events.Add(model);
        await _context.SaveChangesAsync();

        // Save all ticket types
        if (TicketTypes != null)
        {
            foreach (var tt in TicketTypes)
            {
                // Skip empty rows (user added then removed content)
                if (string.IsNullOrWhiteSpace(tt.Name))
                    continue;

                tt.EventId = model.EventId;
                _context.TicketTypes.Add(tt);
            }

            await _context.SaveChangesAsync();
        }

        return RedirectToAction("Index");
    }

    public async Task<IActionResult> Index()
    {
        Console.WriteLine("HIT: Dashboard/Index");

        var user = await GetCurrentUserAsync();
        if (user == null)
            return RedirectToAction("PostLogin", "Account");

        var myEvents = await _context.Events
            .Where(e => e.OrganizerId == user.UserId)
            .ToListAsync();

        var myTickets = await _context.Tickets
            .Include(t => t.Event)
            .Where(t => t.OwnerUserId == user.UserId)
            .ToListAsync();

        var viewModel = new DashboardViewModel
        {
            MyEvents = myEvents,
            MyTickets = myTickets
        };

        return View(viewModel);
    }
    
    public async Task<IActionResult> EditEvent(int id)
    {
        var user = await GetCurrentUserAsync();
        if (user == null)
            return RedirectToAction("PostLogin", "Account");

        var ev = await _context.Events
            .Include(e => e.TicketTypes)
            .FirstOrDefaultAsync(e => e.EventId == id && e.OrganizerId == user.UserId);

        if (ev == null)
            return NotFound();

        ViewBag.Venues = await _context.Venues.ToListAsync();

        return View(ev);
    }
    
    [HttpPost]
    public async Task<IActionResult> EditEvent(
        Event model,
        List<TicketType> TicketTypes)
    {
        var user = await GetCurrentUserAsync();
        if (user == null)
            return RedirectToAction("PostLogin", "Account");

        if (!ModelState.IsValid)
        {
            ViewBag.Venues = await _context.Venues.ToListAsync();
            return View(model);
        }

        // Load existing event
        var ev = await _context.Events
            .Include(e => e.TicketTypes)
            .FirstOrDefaultAsync(e => e.EventId == model.EventId && e.OrganizerId == user.UserId);

        if (ev == null)
            return NotFound();

        // Update event fields
        ev.EventName = model.EventName;
        ev.EventDescription = model.EventDescription;
        ev.VenueId = model.VenueId;

        ev.EventTime = model.EventDate.Date
            .AddHours(model.EventTime.Hour)
            .AddMinutes(model.EventTime.Minute);

        // --- Ticket Type Sync Logic ---
        // 1. Update existing ticket types
        foreach (var tt in TicketTypes.Where(t => t.TicketTypeId != 0))
        {
            var existing = ev.TicketTypes.FirstOrDefault(x => x.TicketTypeId == tt.TicketTypeId);
            if (existing != null)
            {
                existing.Name = tt.Name;
                existing.Price = tt.Price;
                existing.RequiresSeat = tt.RequiresSeat;
                existing.QuantityAvailable = tt.QuantityAvailable;
            }
        }

        // 2. Add new ticket types
        foreach (var tt in TicketTypes.Where(t => t.TicketTypeId == 0))
        {
            if (!string.IsNullOrWhiteSpace(tt.Name))
            {
                tt.EventId = ev.EventId;
                _context.TicketTypes.Add(tt);
            }
        }

        // 3. Remove deleted ticket types
        var postedIds = TicketTypes.Where(t => t.TicketTypeId != 0).Select(t => t.TicketTypeId).ToList();
        var toRemove = ev.TicketTypes.Where(t => !postedIds.Contains(t.TicketTypeId)).ToList();

        foreach (var remove in toRemove)
            _context.TicketTypes.Remove(remove);

        await _context.SaveChangesAsync();

        return RedirectToAction("Index");
    }
    
    public async Task<IActionResult> DeleteEvent(int id)
    {
        var user = await GetCurrentUserAsync();
        if (user == null)
            return RedirectToAction("PostLogin", "Account");

        var ev = await _context.Events
            .Include(e => e.TicketTypes)
            .Include(e => e.Tickets)
            .FirstOrDefaultAsync(e => e.EventId == id && e.OrganizerId == user.UserId);

        if (ev == null)
            return NotFound();

        // Calculate total refund for purchased tickets
        var totalRefund = ev.Tickets
            .Where(t => t.OrderId != null)
            .Sum(t => t.Price);

        ViewBag.TotalRefund = totalRefund;

        return View(ev);
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteEventConfirmed(int id, decimal expectedRefund, string refundConfirmation)
    {
        var user = await GetCurrentUserAsync();
        if (user == null)
            return RedirectToAction("PostLogin", "Account");

        var ev = await _context.Events
            .Include(e => e.TicketTypes)
            .Include(e => e.Tickets)
            .FirstOrDefaultAsync(e => e.EventId == id && e.OrganizerId == user.UserId);

        if (ev == null)
            return NotFound();

        var actualRefund = ev.Tickets
            .Where(t => t.OrderId != null)
            .Sum(t => t.Price);

        if (!decimal.TryParse(refundConfirmation, out var entered) || entered != actualRefund)
        {
            TempData["Error"] = "Refund amount does not match. Event was not deleted.";
            return RedirectToAction("DeleteEvent", new { id });
        }

        if (ev.Tickets != null)
            _context.Tickets.RemoveRange(ev.Tickets);

        if (ev.TicketTypes != null)
            _context.TicketTypes.RemoveRange(ev.TicketTypes);

        _context.Events.Remove(ev);

        await _context.SaveChangesAsync();

        return RedirectToAction("Index");
    }
    
    public async Task<IActionResult> Report(int id)
    {
        var user = await GetCurrentUserAsync();
        if (user == null)
            return RedirectToAction("PostLogin", "Account");

        var ev = await _context.Events
            .Include(e => e.Venue)
            .Include(e => e.TicketTypes)
            .Include(e => e.Tickets)
            .ThenInclude(t => t.OwnerUser)
            .Include(e => e.Tickets)
            .ThenInclude(t => t.Order)
            .FirstOrDefaultAsync(e => e.EventId == id && e.OrganizerId == user.UserId);

        if (ev == null)
            return NotFound();

        var soldTickets = ev.Tickets
            .Where(t => t.OrderId != null)
            .ToList();

        var ticketTypeReports = ev.TicketTypes
            .Select(tt =>
            {
                var ticketsForType = soldTickets
                    .Where(t => t.TicketTypeId == tt.TicketTypeId)
                    .ToList();

                return new TicketTypeReport
                {
                    Name = tt.Name,
                    CurrentPrice = tt.Price,
                    QuantityAvailable = tt.QuantityAvailable ?? 0,
                    QuantitySold = ticketsForType.Count,
                    Revenue = ticketsForType.Sum(t => t.Price)
                };
            })
            .ToList();

        var vm = new EventReportViewModel
        {
            Event = ev,
            SoldTickets = soldTickets,
            TotalTicketsSold = soldTickets.Count,
            TotalRevenue = soldTickets.Sum(t => t.Price),
            TicketTypeReports = ticketTypeReports
        };

        return View(vm);
    }
    
    public async Task<IActionResult> ExportEventCsv(int id)
    {
        var user = await GetCurrentUserAsync();
        if (user == null)
            return RedirectToAction("PostLogin", "Account");

        var ev = await _context.Events
            .Include(e => e.Tickets)
            .ThenInclude(t => t.OwnerUser)
            .Include(e => e.Tickets)
            .ThenInclude(t => t.Order)
            .Include(e => e.TicketTypes)
            .FirstOrDefaultAsync(e => e.EventId == id && e.OrganizerId == user.UserId);

        if (ev == null)
            return NotFound();

        var soldTickets = ev.Tickets
            .Where(t => t.OrderId != null)
            .ToList();

        var csv = new StringBuilder();

        csv.AppendLine("Buyer,Ticket Type,Price,Event");

        foreach (var t in soldTickets)
        {
            var buyer = t.OwnerUser == null
                ? "Unknown"
                : $"{t.OwnerUser.FirstName} {t.OwnerUser.LastName}".Trim();

            if (string.IsNullOrWhiteSpace(buyer) && t.OwnerUser != null)
                buyer = t.OwnerUser.Email ?? "Unknown";

            var ticketType = ev.TicketTypes
                .FirstOrDefault(tt => tt.TicketTypeId == t.TicketTypeId)?.Name ?? "Unknown";

            csv.AppendLine($"{buyer},{ticketType},{t.Price},{ev.EventName}");
        }

        var bytes = Encoding.UTF8.GetBytes(csv.ToString());
        var safeName = string.Join("_", ev.EventName.Split(Path.GetInvalidFileNameChars()));
        var fileName = $"{safeName}_Report.csv";

        return File(bytes, "text/csv", fileName);
    }
    
    private async Task<User?> GetCurrentUserAsync()
    {
        var auth0Id =
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
            User.FindFirst("sub")?.Value;

        Console.WriteLine("Auth0 ID: " + auth0Id);

        if (auth0Id == null)
            return null;

        return await _context.Users
            .FirstOrDefaultAsync(u => u.Auth0UserId == auth0Id);
    }
}