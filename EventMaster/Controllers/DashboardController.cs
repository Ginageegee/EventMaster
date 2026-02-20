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

        var model = new Event
        {
            OrganizerId = int.Parse(HttpContext.Session.GetString("UserId")),
            VenueId = null // or default venue if you want
        };

        return View(model);

    }

    // POST: /Dashboard/CreateEvent
    [HttpPost]
    public IActionResult CreateEvent(Event model)
    {
        if (!ModelState.IsValid)
            return View(model);

        model.OrganizerId = int.Parse(HttpContext.Session.GetString("UserId"));
        model.EventTime = model.EventDate.Date
            .AddHours(model.EventTime.Hour)
            .AddMinutes(model.EventTime.Minute);
        
        InMemoryStore.AddEvent(model);

        return RedirectToAction("Index");
    }

    public IActionResult Index()
    {
        if (HttpContext.Session.GetString("UserRole") != Roles.Organizer)
            return RedirectToAction("Login", "Account");
    
        var userIdString = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userIdString))
            return RedirectToAction("Login", "Account");

        int organizerId = int.Parse(userIdString);
        var myEvents = InMemoryStore.GetEventsForOrganizer(organizerId);

        return View(myEvents);
    }
}