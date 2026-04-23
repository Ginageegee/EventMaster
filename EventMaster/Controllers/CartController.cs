using System;
using System.Linq;
using EventMaster.Data;
using EventMaster.Models;
using EventMaster.ViewModels;
using EventMaster.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Threading.Tasks;

namespace EventMaster.Controllers;

[Authorize]
public class CartController : Controller
{
    private readonly ApplicationDbContext _context;

    public CartController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var user = await GetCurrentUserAsync();
        if (user == null)
            return RedirectToAction("PostLogin", "Account");

        var cart = await GetOrCreateCartAsync(user.UserId);

        cart = await _context.Carts
            .Include(c => c.Items)
                .ThenInclude(i => i.TicketType)
            .Include(c => c.Items)
                .ThenInclude(i => i.Seat)
            .FirstOrDefaultAsync(c => c.CartId == cart.CartId);

        return View(cart);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddToCart(AddToCartViewModel model)
    {
        var user = await GetCurrentUserAsync();
        if (user == null)
            return RedirectToAction("PostLogin", "Account");

        if (!ModelState.IsValid)
            return RedirectToAction("Index", "Events");

        var cart = await GetOrCreateCartAsync(user.UserId);

        var existingItem = await _context.CartItems.FirstOrDefaultAsync(ci =>
            ci.CartId == cart.CartId &&
            ci.TicketTypeId == model.TicketTypeId &&
            ci.SeatId == model.SeatId);

        if (existingItem != null)
        {
            existingItem.Quantity += model.Quantity;
        }
        else
        {
            var newItem = new CartItem
            {
                CartId = cart.CartId,
                TicketTypeId = model.TicketTypeId,
                Quantity = model.Quantity,
                SeatId = model.SeatId
            };

            _context.CartItems.Add(newItem);
        }

        cart.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateQuantity(int cartItemId, int quantity)
    {
        var user = await GetCurrentUserAsync();
        if (user == null)
            return RedirectToAction("PostLogin", "Account");

        var cartItem = await _context.CartItems
            .Include(ci => ci.Cart)
            .FirstOrDefaultAsync(ci => ci.CartItemId == cartItemId);

        if (cartItem == null || cartItem.Cart.UserId != user.UserId)
            return NotFound();

        if (quantity <= 0)
        {
            _context.CartItems.Remove(cartItem);
        }
        else
        {
            cartItem.Quantity = quantity;
            cartItem.Cart.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveItem(int cartItemId)
    {
        var user = await GetCurrentUserAsync();
        if (user == null)
            return RedirectToAction("PostLogin", "Account");

        var cartItem = await _context.CartItems
            .Include(ci => ci.Cart)
            .FirstOrDefaultAsync(ci => ci.CartItemId == cartItemId);

        if (cartItem == null || cartItem.Cart.UserId != user.UserId)
            return NotFound();

        _context.CartItems.Remove(cartItem);
        await _context.SaveChangesAsync();

        return RedirectToAction("Index");
    }

    // ✅ NEW CHECKOUT METHOD
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout()
    {
        var user = await GetCurrentUserAsync();
        if (user == null)
            return RedirectToAction("PostLogin", "Account");

        var cart = await _context.Carts
            .Include(c => c.Items)
                .ThenInclude(i => i.TicketType)
            .Include(c => c.Items)
                .ThenInclude(i => i.Seat)
            .FirstOrDefaultAsync(c => c.UserId == user.UserId);

        if (cart == null || cart.Items == null || !cart.Items.Any())
        {
            TempData["Error"] = "Your cart is empty.";
            return RedirectToAction("Index");
        }

        decimal totalAmount = 0;

        // ✅ Validate inventory
        foreach (var item in cart.Items)
        {
            if (item.TicketType == null)
            {
                TempData["Error"] = "A ticket in your cart is invalid.";
                return RedirectToAction("Index");
            }

            if (item.TicketType.QuantityAvailable.HasValue &&
                item.TicketType.QuantityAvailable.Value < item.Quantity)
            {
                TempData["Error"] = $"Not enough tickets for {item.TicketType.Name}.";
                return RedirectToAction("Index");
            }

            totalAmount += item.TicketType.Price * item.Quantity;
        }

        // ✅ Create order
        var order = new Order
        {
            BuyerUserId = user.UserId,
            Status = OrderStatus.Completed,
            TotalAmount = totalAmount
        };

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        // ✅ Create tickets
        foreach (var item in cart.Items)
        {
            if (item.TicketType == null)
                continue;

            // reduce inventory
            if (item.TicketType.QuantityAvailable.HasValue)
            {
                item.TicketType.QuantityAvailable -= item.Quantity;
            }

            for (int i = 0; i < item.Quantity; i++)
            {
                var ticket = new Ticket
                {
                    EventId = item.TicketType.EventId,
                    TicketTypeId = item.TicketTypeId,
                    SeatId = item.SeatId,
                    OrderId = order.OrderId,
                    OwnerUserId = user.UserId,
                    Price = item.TicketType.Price,
                    QrCode = Guid.NewGuid().ToString(),
                    Status = TicketStatus.Active,
                    IsListedForSale = false
                };

                _context.Tickets.Add(ticket);
            }
        }

        // ✅ Clear cart
        _context.CartItems.RemoveRange(cart.Items);
        cart.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        TempData["Success"] = "Checkout successful! Your tickets have been created.";

        return RedirectToAction("Index", "Dashboard");
    }

    private async Task<Cart> GetOrCreateCartAsync(int userId)
    {
        var cart = await _context.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart == null)
        {
            cart = new Cart
            {
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Carts.Add(cart);
            await _context.SaveChangesAsync();
        }

        return cart;
    }

    private async Task<User?> GetCurrentUserAsync()
    {
        var auth0Id =
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
            User.FindFirst("sub")?.Value;

        if (auth0Id == null)
            return null;

        return await _context.Users
            .FirstOrDefaultAsync(u => u.Auth0UserId == auth0Id);
    }
}