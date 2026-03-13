using EventMaster.Data;
using EventMaster.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventMaster.Controllers;

[Authorize] // Require Auth0 login for all dashboard actions
public class DashboardController : Controller
{
    private readonly ApplicationDbContext _context;

    public DashboardController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: /Dashboard/CreateEvent
    public async Task<IActionResult> CreateEvent()
    {
        var user = await GetCurrentUserAsync();
        if (user == null)
            return RedirectToAction("Login", "Account");

        ViewBag.Venues = await _context.Venues.ToListAsync();

        var model = new Event
        {
            OrganizerId = user.UserId
        };

        return View(model);
    }

    // POST: /Dashboard/CreateEvent
    [HttpPost]
    public async Task<IActionResult> CreateEvent(Event model)
    {
        var user = await GetCurrentUserAsync();
        if (user == null)
            return RedirectToAction("Login", "Account");

        if (!ModelState.IsValid)
        {
            ViewBag.Venues = await _context.Venues.ToListAsync();
            return View(model);
        }

        // Assign organizer
        model.OrganizerId = user.UserId;

        // Merge date + time
        model.EventTime = model.EventDate.Date
            .AddHours(model.EventTime.Hour)
            .AddMinutes(model.EventTime.Minute);

        _context.Events.Add(model);
        await _context.SaveChangesAsync();

        return RedirectToAction("Index");
    }

    // GET: /Dashboard
    public async Task<IActionResult> Index()
    {
        var user = await GetCurrentUserAsync();
        if (user == null)
            return RedirectToAction("Login", "Account");

        var myEvents = await _context.Events
            .Where(e => e.OrganizerId == user.UserId)
            .ToListAsync();

        return View(myEvents);
    }

    // ⭐ Helper: Get the logged‑in Auth0 user from DB
    private async Task<User?> GetCurrentUserAsync()
    {
        var auth0Id = User.FindFirst("sub")?.Value;
        if (auth0Id == null)
            return null;

        return await _context.Users
            .FirstOrDefaultAsync(u => u.Auth0UserId == auth0Id);
    }
}