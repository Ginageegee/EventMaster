using EventMaster.Data;
using EventMaster.Models;
using EventMaster.Models.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

public class AccountController : Controller
{
    private readonly ApplicationDbContext _context;

    public AccountController(ApplicationDbContext context)
    {
        _context = context;
    }

    public IActionResult Login(string returnUrl = "/Dashboard/Index")
    {
        Console.WriteLine("HIT: Account/Login");

        if (User.Identity?.IsAuthenticated == true)
        {
            return Redirect(returnUrl);
        }

        var props = new AuthenticationProperties
        {
            RedirectUri = Url.Action("PostLogin", "Account", new { returnUrl })
        };

        return Challenge(props, "Auth0");
    }

    [Authorize]
    public async Task<IActionResult> PostLogin(string returnUrl = "/Dashboard/Index")
    {
        Console.WriteLine("HIT: Account/PostLogin");

        var user = await EnsureLocalUserAsync();

        if (user == null)
        {
            return Content("Authenticated successfully, but the local user could not be created.");
        }

        if (string.IsNullOrWhiteSpace(user.FirstName) ||
            string.IsNullOrWhiteSpace(user.LastName))
        {
            Console.WriteLine("REDIRECT: Missing names → CompleteProfile");
            return RedirectToAction("CompleteProfile");
        }

        if (string.IsNullOrWhiteSpace(returnUrl) || !Url.IsLocalUrl(returnUrl))
        {
            Console.WriteLine("REDIRECT: Invalid returnUrl → Dashboard");
            return RedirectToAction("Index", "Dashboard");
        }

        Console.WriteLine($"REDIRECT: returnUrl → {returnUrl}");
        return Redirect(returnUrl);
    }

    public IActionResult Logout()
    {
        Console.WriteLine("HIT: Account/Logout");

        var props = new AuthenticationProperties
        {
            RedirectUri = "/"
        };

        return SignOut(props, "Auth0", "Cookies");
    }

    [Authorize]
    public async Task<IActionResult> Profile()
    {
        Console.WriteLine("HIT: Account/Profile (GET)");

        var user = await EnsureLocalUserAsync();

        if (user == null)
        {
            return RedirectToAction("Login");
        }

        return View(user);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(User model)
    {
        Console.WriteLine("HIT: Account/Profile (POST)");

        var user = await EnsureLocalUserAsync();

        if (user == null)
        {
            return RedirectToAction("Login");
        }

        user.Email = model.Email?.Trim();
        user.FirstName = model.FirstName?.Trim() ?? "";
        user.LastName = model.LastName?.Trim() ?? "";

        await _context.SaveChangesAsync();

        return RedirectToAction("Profile");
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> CompleteProfile()
    {
        Console.WriteLine("HIT: Account/CompleteProfile (GET)");

        var user = await EnsureLocalUserAsync();

        if (user == null)
            return RedirectToAction("Login");

        if (!string.IsNullOrWhiteSpace(user.FirstName) &&
            !string.IsNullOrWhiteSpace(user.LastName))
        {
            Console.WriteLine("REDIRECT: Profile complete → Dashboard");
            return RedirectToAction("Index", "Dashboard");
        }

        var vm = new CompleteProfileViewModel
        {
            FirstName = user.FirstName ?? "",
            LastName = user.LastName ?? ""
        };

        return View(vm);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CompleteProfile(CompleteProfileViewModel vm)
    {
        Console.WriteLine("HIT: Account/CompleteProfile (POST)");

        if (!ModelState.IsValid)
            return View(vm);

        var user = await EnsureLocalUserAsync();

        if (user == null)
            return RedirectToAction("Login");

        user.FirstName = vm.FirstName.Trim();
        user.LastName = vm.LastName.Trim();

        await _context.SaveChangesAsync();

        Console.WriteLine("REDIRECT: Profile saved → Dashboard");
        return RedirectToAction("Index", "Dashboard");
    }

    private async Task<User?> EnsureLocalUserAsync()
    {
        var auth0Id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        Console.WriteLine($"EnsureLocalUserAsync: Auth0Id={auth0Id}");

        if (string.IsNullOrWhiteSpace(auth0Id))
        {
            Console.WriteLine("EnsureLocalUserAsync: No Auth0Id");
            return null;
        }

        var email =
            User.FindFirst(ClaimTypes.Email)?.Value ??
            User.FindFirst("email")?.Value;

        var firstName =
            User.FindFirst(ClaimTypes.GivenName)?.Value ??
            User.FindFirst("given_name")?.Value ?? "";

        var lastName =
            User.FindFirst(ClaimTypes.Surname)?.Value ??
            User.FindFirst("family_name")?.Value ?? "";

        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Auth0UserId == auth0Id);

        if (existingUser != null)
        {
            bool updated = false;

            if (!string.IsNullOrWhiteSpace(email) && existingUser.Email != email)
            {
                existingUser.Email = email;
                updated = true;
            }

            if (!string.IsNullOrWhiteSpace(firstName) && existingUser.FirstName != firstName)
            {
                existingUser.FirstName = firstName;
                updated = true;
            }

            if (!string.IsNullOrWhiteSpace(lastName) && existingUser.LastName != lastName)
            {
                existingUser.LastName = lastName;
                updated = true;
            }

            if (updated)
            {
                await _context.SaveChangesAsync();
            }

            Console.WriteLine($"EnsureLocalUserAsync: Returning existing user {existingUser.UserId}");
            return existingUser;
        }

        var newUser = new User
        {
            Auth0UserId = auth0Id,
            Email = email,
            FirstName = firstName,
            LastName = lastName
        };

        _context.Users.Add(newUser);
        await _context.SaveChangesAsync();

        Console.WriteLine($"EnsureLocalUserAsync: Created new user {newUser.UserId}");
        return newUser;
    }
}