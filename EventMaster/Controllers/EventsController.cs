using EventMaster.Data;
using EventMaster.Models;
using EventMaster.Services;
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
    
    public async Task<IActionResult> Index()
    {
        var events = await _context.Events
            .Where(e => e.EventTime >= DateTime.Now)
            .OrderBy(e => e.EventTime)
            .ToListAsync();
    
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