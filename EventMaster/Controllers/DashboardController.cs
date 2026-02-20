using EventMaster.Models;
using EventMaster.Services;
using Microsoft.AspNetCore.Mvc;

namespace EventMaster.Controllers;

public class DashboardController : Controller
{
    // GET: /Dashboard/CreateEvent
    public IActionResult CreateEvent()
    {
        if (HttpContext.Session.GetString("UserRole") != Roles.Organizer)
            return RedirectToAction("Login", "Account");

        return View();
    }

    // POST: /Dashboard/CreateEvent
    [HttpPost]
    public IActionResult CreateEvent(Event model)
    {
        if (!ModelState.IsValid)
            return View(model);

        model.EventId = InMemoryStore.NextEventId();
        model.OrganizerId = int.Parse(HttpContext.Session.GetString("UserId"));

        InMemoryStore.AddEvent(model);

        // Add default ticket type
        var ticketType = new TicketType
        {
            TicketTypeId = InMemoryStore.NextTicketTypeId(),
            EventId = model.EventId,
            Name = "General Admission",
            Price = 25,
            QuantityAvailable = 100
        };

        InMemoryStore.AddTicketType(ticketType);

        return RedirectToAction("Index");
    }

    public IActionResult Index()
    {
        if (HttpContext.Session.GetString("UserRole") != Roles.Organizer)
            return RedirectToAction("Login", "Account");

        return View();
    }
}