using System;
using System.Linq;
using System.Threading.Tasks;
using EventMaster.Data;
using EventMaster.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventMaster.Controllers;

public class EventsController : Controller
{
    private readonly ApplicationDbContext _context;

    public EventsController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? searchTerm)
    {
        var query = _context.Events
            .Where(e => e.EventTime >= DateTime.Now);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(e => e.EventName.Contains(searchTerm));
        }

        var events = await query
            .OrderBy(e => e.EventTime)
            .ToListAsync();

        ViewBag.SearchTerm = searchTerm;

        return View(events);
    }

    public async Task<IActionResult> Details(int id)
    {
        var ev = await _context.Events
            .Include(e => e.Venue)
            .Include(e => e.Organizer)
            .FirstOrDefaultAsync(e => e.EventId == id);

        if (ev == null)
            return NotFound();

        var ticketTypes = await _context.TicketTypes
            .Where(t => t.EventId == id)
            .OrderBy(t => t.Price)
            .ToListAsync();

        var vm = new EventDetailsViewModel
        {
            Event = ev,
            TicketTypes = ticketTypes
        };

        return View(vm);
    }
}