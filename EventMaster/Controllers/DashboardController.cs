using EventMaster.Models;
using Microsoft.AspNetCore.Mvc;

namespace EventMaster.Controllers;

public class DashboardController : Controller
{
    [HttpPost]
    public IActionResult CreateEvent(Event model)
    {
        if (ModelState.IsValid)
        {
            // TODO: Save to DB
            return RedirectToAction("Index");
        }

        return View(model);
    }
    
    public IActionResult Index()
    {
        if (HttpContext.Session.GetString("UserRole") != Roles.Organizer)
            return RedirectToAction("Login", "Account");
        
        return View();
    }

}

