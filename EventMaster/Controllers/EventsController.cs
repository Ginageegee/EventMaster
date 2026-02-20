using EventMaster.Models;
using EventMaster.Services;
using EventMaster.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace EventMaster.Controllers;

public class EventsController : Controller
{
    public IActionResult Index()
    {
        var events = InMemoryStore.GetUpcomingEvents();
        return View(events);
    }

    public IActionResult Details(int id)
    {
        var ev = InMemoryStore.GetEvent(id);
        if (ev == null) return NotFound();

        var vm = new EventDetailsViewModel
        {
            Event = ev,
            TicketTypes = InMemoryStore.GetTicketTypesForEvent(id)
        };

        return View(vm);
    }
}