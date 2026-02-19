using Microsoft.AspNetCore.Mvc;

namespace EventMaster.Controllers;

public class TicketsController : Controller
{
    
    public IActionResult Buy(int id)
    {
        if (HttpContext.Session.GetString("UserRole") != "Client")
            return RedirectToAction("Login", "Account");
        
        // Fake ticket number
        var ticketNumber = Guid.NewGuid().ToString().Substring(0, 8);

        ViewBag.EventId = id;
        ViewBag.TicketNumber = ticketNumber;

        return View();
    }
}