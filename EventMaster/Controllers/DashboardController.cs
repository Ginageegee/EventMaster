using EventMaster.Data;
using EventMaster.Models;
using EventMaster.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

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
    public async Task<IActionResult> CreateEvent(Event model)
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

        model.OrganizerId = user.UserId;

        model.EventTime = model.EventDate.Date
            .AddHours(model.EventTime.Hour)
            .AddMinutes(model.EventTime.Minute);

        _context.Events.Add(model);
        await _context.SaveChangesAsync();

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