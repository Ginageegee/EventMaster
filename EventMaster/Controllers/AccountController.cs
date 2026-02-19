using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc;
using EventMaster.Models;

namespace EventMaster.Controllers
{
    public class AccountController : Controller
    {
        private static List<User> Users = new List<User>(); // TEMP storage until DB

        public IActionResult Index()
        {
            if (HttpContext.Session.GetString("UserRole") != "Staff")
                return RedirectToAction("Login", "Account");

            return View();
        }
        
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Register(User model)
        {
            if (ModelState.IsValid)
            {
                // Save user (later replace with DB)
                Users.Add(model);

                return RedirectToAction("Login");
            }

            return View(model);
        }

        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(string email, string password)
        {
            var user = Users.FirstOrDefault(u => u.Email == email && u.Password == password);

            if (user != null)
            {
                // Store user in session
                HttpContext.Session.SetString("UserEmail", user.Email);
                HttpContext.Session.SetString("UserRole", user.Role);

                if (user.Role == Roles.Organizer)
                    return RedirectToAction("Index", "Dashboard");


                return RedirectToAction("Index", "Home");
            }

            ViewBag.Error = "Invalid login.";
            return View();
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }
    }
}