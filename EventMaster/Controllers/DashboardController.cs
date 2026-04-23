using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EventMaster.Data;
using EventMaster.Models;
using EventMaster.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

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
[ValidateAntiForgeryToken]
public async Task<IActionResult> CreateEvent(
    Event model,
    List<TicketType> TicketTypes,
    IFormFile? MediaFile)
{
    Console.WriteLine("HIT: Dashboard/CreateEvent (POST)");

    var user = await GetCurrentUserAsync();
    if (user == null)
        return RedirectToAction("PostLogin", "Account");

    if (!ModelState.IsValid)
    {
        Console.WriteLine("ModelState is invalid in CreateEvent.");

        foreach (var entry in ModelState)
        {
            foreach (var error in entry.Value.Errors)
            {
                Console.WriteLine($"FIELD: {entry.Key} | ERROR: {error.ErrorMessage}");
            }
        }

        ViewBag.Venues = await _context.Venues.ToListAsync();
        return View(model);
    }

    try
    {
        var eventToSave = new Event
        {
            OrganizerId = user.UserId,
            VenueId = model.VenueId,
            EventName = model.EventName,
            EventDescription = model.EventDescription,
            EventDate = model.EventDate,
            EventTime = model.EventDate.Date
                .AddHours(model.EventTime.Hour)
                .AddMinutes(model.EventTime.Minute)
        };

        if (MediaFile != null && MediaFile.Length > 0)
        {
            Console.WriteLine($"Uploading media: {MediaFile.FileName} | {MediaFile.ContentType}");

            var mediaResult = await SaveMediaFileAsync(MediaFile);
            eventToSave.MediaPath = mediaResult.MediaPath;
            eventToSave.MediaType = mediaResult.MediaType;

            Console.WriteLine($"Media saved: {eventToSave.MediaPath} | {eventToSave.MediaType}");
        }

        _context.Events.Add(eventToSave);
        await _context.SaveChangesAsync();

        Console.WriteLine($"Event saved successfully. EventId = {eventToSave.EventId}");

        if (TicketTypes != null)
        {
            foreach (var tt in TicketTypes)
            {
                if (string.IsNullOrWhiteSpace(tt.Name))
                    continue;

                var ticketType = new TicketType
                {
                    EventId = eventToSave.EventId,
                    Name = tt.Name,
                    Price = tt.Price,
                    QuantityAvailable = tt.QuantityAvailable,
                    RequiresSeat = tt.RequiresSeat
                };

                _context.TicketTypes.Add(ticketType);
            }

            await _context.SaveChangesAsync();
            Console.WriteLine("Ticket types saved successfully.");
        }

        TempData["Success"] = "Event created successfully.";
        return RedirectToAction("Index", "Dashboard");
    }
    catch (Exception ex)
    {
        Console.WriteLine("CREATE EVENT ERROR: " + ex.Message);
        Console.WriteLine(ex.StackTrace);

        TempData["Error"] = ex.Message;
        ViewBag.Venues = await _context.Venues.ToListAsync();
        return View(model);
    }
}

    public async Task<IActionResult> Index()
    {
        Console.WriteLine("HIT: Dashboard/Index");

        var user = await GetCurrentUserAsync();
        if (user == null)
            return RedirectToAction("PostLogin", "Account");

        var myEvents = await _context.Events
            .Where(e => e.OrganizerId == user.UserId)
            .Include(e => e.Venue)
            .OrderBy(e => e.EventDate)
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
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditEvent(
        Event model,
        List<TicketType> TicketTypes,
        IFormFile? MediaFile)
    {
        var user = await GetCurrentUserAsync();
        if (user == null)
            return RedirectToAction("PostLogin", "Account");

        if (!ModelState.IsValid)
        {
            ViewBag.Venues = await _context.Venues.ToListAsync();
            return View(model);
        }

        var ev = await _context.Events
            .Include(e => e.TicketTypes)
            .FirstOrDefaultAsync(e => e.EventId == model.EventId && e.OrganizerId == user.UserId);

        if (ev == null)
            return NotFound();

        ev.EventName = model.EventName;
        ev.EventDescription = model.EventDescription;
        ev.VenueId = model.VenueId;
        ev.EventDate = model.EventDate;
        ev.EventTime = model.EventDate.Date
            .AddHours(model.EventTime.Hour)
            .AddMinutes(model.EventTime.Minute);

        if (MediaFile != null && MediaFile.Length > 0)
        {
            DeletePhysicalFile(ev.MediaPath);

            var mediaResult = await SaveMediaFileAsync(MediaFile);
            ev.MediaPath = mediaResult.MediaPath;
            ev.MediaType = mediaResult.MediaType;
        }

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

        foreach (var tt in TicketTypes.Where(t => t.TicketTypeId == 0))
        {
            if (!string.IsNullOrWhiteSpace(tt.Name))
            {
                tt.EventId = ev.EventId;
                _context.TicketTypes.Add(tt);
            }
        }

        var postedIds = TicketTypes
            .Where(t => t.TicketTypeId != 0)
            .Select(t => t.TicketTypeId)
            .ToList();

        var toRemove = ev.TicketTypes
            .Where(t => !postedIds.Contains(t.TicketTypeId))
            .ToList();

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

        DeletePhysicalFile(ev.MediaPath);

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

    public async Task<IActionResult> Venues()
    {
        var user = await GetCurrentUserAsync();
        if (user == null)
            return RedirectToAction("PostLogin", "Account");

        var venues = await _context.Venues.ToListAsync();
        return View(venues);
    }

    public IActionResult AddVenue()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddVenue(Venue venue)
    {
        if (!ModelState.IsValid)
            return View(venue);

        _context.Venues.Add(venue);
        await _context.SaveChangesAsync();

        return RedirectToAction("Venues");
    }

    public async Task<IActionResult> DeleteVenue(int id)
    {
        var venue = await _context.Venues
            .Include(v => v.Sections)
            .FirstOrDefaultAsync(v => v.VenueId == id);

        if (venue == null)
            return NotFound();

        bool hasEvents = await _context.Events.AnyAsync(e => e.VenueId == id);
        if (hasEvents)
        {
            TempData["Error"] = "This venue cannot be deleted because it is assigned to one or more events.";
            return RedirectToAction("Venues");
        }

        return View(venue);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteVenueConfirmed(int venueId)
    {
        var venue = await _context.Venues.FindAsync(venueId);
        if (venue == null)
            return NotFound();

        _context.Venues.Remove(venue);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Venue deleted successfully.";
        return RedirectToAction("Venues");
    }

    private async Task<(string MediaPath, string MediaType)> SaveMediaFileAsync(IFormFile mediaFile)
    {
        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "events");

        if (!Directory.Exists(uploadsFolder))
        {
            Directory.CreateDirectory(uploadsFolder);
        }

        var extension = Path.GetExtension(mediaFile.FileName).ToLowerInvariant();
        var uniqueFileName = $"{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await mediaFile.CopyToAsync(stream);
        }

        var mediaPath = $"/uploads/events/{uniqueFileName}";
        var mediaType = mediaFile.ContentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase)
            ? "video"
            : "image";

        return (mediaPath, mediaType);
    }

    private void DeletePhysicalFile(string? mediaPath)
    {
        if (string.IsNullOrWhiteSpace(mediaPath))
            return;

        var relativePath = mediaPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", relativePath);

        if (System.IO.File.Exists(fullPath))
        {
            System.IO.File.Delete(fullPath);
        }
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