using EventMaster.Models;
using Microsoft.AspNetCore.Mvc;

namespace EventMaster.Controllers
{
    public class AccountController : Controller
    {
        // TEMP storage until DB (resets whenever the app restarts)
        private static readonly List<User> Users = new();

        // Adds demo accounts once so you can always log in after a restart.
        private static void EnsureSeedUsers()
        {
            if (Users.Count > 0) return;

            Users.Add(new User
            {
                UserId = 1,
                FirstName = "Test",
                LastName = "Attendee",
                Email = "attendee@eventmaster.com",
                Password = "test",
                Role = Roles.Attendee,
                EventsCreated = new List<Event>()
            });

            Users.Add(new User
            {
                UserId = 2,
                FirstName = "Test",
                LastName = "Organizer",
                Email = "organizer@eventmaster.com",
                Password = "test",
                Role = Roles.Organizer,
                EventsCreated = new List<Event>()
            });
        }

        public IActionResult Index()
        {
            var role = HttpContext.Session.GetString("UserRole");

            if (role == Roles.Organizer)
                return RedirectToAction("Index", "Dashboard");

            if (role == Roles.Attendee)
                return RedirectToAction("Index", "Home");

            return RedirectToAction("Login", "Account");
        }

        public IActionResult Register()
        {
            EnsureSeedUsers();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Register(User model)
        {
            EnsureSeedUsers();

            if (!ModelState.IsValid)
                return View(model);

            model.Email = (model.Email ?? string.Empty).Trim();

            if (Users.Any(u => u.Email.Equals(model.Email, StringComparison.OrdinalIgnoreCase)))
            {
                ModelState.AddModelError(nameof(model.Email), "That email is already registered.");
                return View(model);
            }

            if (string.IsNullOrWhiteSpace(model.Role))
                model.Role = Roles.Attendee;

            // Prevent null collection warnings / future issues
            model.EventsCreated ??= new List<Event>();

            Users.Add(model);
            return RedirectToAction("Login");
        }

        public IActionResult Login()
        {
            EnsureSeedUsers();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(string email, string password)
        {
            EnsureSeedUsers();

            email = (email ?? string.Empty).Trim();

            var user = Users.FirstOrDefault(u =>
                u.Email.Equals(email, StringComparison.OrdinalIgnoreCase) &&
                u.Password == password);

            if (user == null)
            {
                ViewBag.Error = "Invalid login.";
                return View();
            }

            // Store user in session
            HttpContext.Session.SetString("UserEmail", user.Email);
            HttpContext.Session.SetString("UserRole", user.Role);
            HttpContext.Session.SetString("UserId", user.UserId.ToString());
            
            return user.Role == Roles.Organizer
                ? RedirectToAction("Index", "Dashboard")
                : RedirectToAction("Index", "Home");
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Remove("UserEmail");
            HttpContext.Session.Remove("UserRole");
            return RedirectToAction("Index", "Home");
        }
    }
}