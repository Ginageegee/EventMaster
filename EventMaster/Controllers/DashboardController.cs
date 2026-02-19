namespace EventMaster.Controllers;

public class DashboardController
{
    
}

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