using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace EventMaster.Controllers;

[Authorize]
public class CartController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;

    public CartController(ApplicationDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
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

        var stripeSecretKey = _configuration["Stripe:SecretKey"];
        if (string.IsNullOrWhiteSpace(stripeSecretKey))
        {
            TempData["Error"] = "Stripe is not configured yet.";
            return RedirectToAction("Index");
        }

        StripeConfiguration.ApiKey = stripeSecretKey;

        var order = new Order
        {
            BuyerUserId = user.UserId,
            Status = OrderStatus.InProgress,
            TotalAmount = totalAmount
        };

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        var payment = new Payment
        {
            OrderId = order.OrderId,
            Amount = totalAmount,
            Status = PaymentStatus.Pending
        };

        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        var successUrl = Url.Action(
            "Success",
            "Cart",
            null,
            Request.Scheme) + "?session_id={CHECKOUT_SESSION_ID}";

        var cancelUrl = Url.Action(
            "Cancel",
            "Cart",
            new { orderId = order.OrderId },
            Request.Scheme);

        var lineItems = new List<SessionLineItemOptions>();

        foreach (var item in cart.Items)
        {
            if (item.TicketType == null) continue;

            var ticketName = item.TicketType.Name;

            if (item.Seat != null && !string.IsNullOrWhiteSpace(item.Seat.SeatNumber))
            {
                ticketName += $" (Seat {item.Seat.SeatNumber})";
            }

            lineItems.Add(new SessionLineItemOptions
            {
                Quantity = item.Quantity,
                PriceData = new SessionLineItemPriceDataOptions
                {
                    Currency = "cad",
                    UnitAmount = (long)(item.TicketType.Price * 100),
                    ProductData = new SessionLineItemPriceDataProductDataOptions
                    {
                        Name = ticketName
                    }
                }
            });
        }

        var options = new SessionCreateOptions
        {
            Mode = "payment",
            SuccessUrl = successUrl,
            CancelUrl = cancelUrl,
            ClientReferenceId = order.OrderId.ToString(),
            Metadata = new Dictionary<string, string>
            {
                ["orderId"] = order.OrderId.ToString(),
                ["cartId"] = cart.CartId.ToString(),
                ["buyerUserId"] = user.UserId.ToString()
            },
            LineItems = lineItems
        };

        var service = new SessionService();
        var session = await service.CreateAsync(options);

        return Redirect(session.Url);
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Success(string session_id)
    {
        if (string.IsNullOrWhiteSpace(session_id))
            return RedirectToAction("Index");

        var stripeSecretKey = _configuration["Stripe:SecretKey"];
        if (string.IsNullOrWhiteSpace(stripeSecretKey))
            return RedirectToAction("Index");

        StripeConfiguration.ApiKey = stripeSecretKey;

        var service = new SessionService();
        var session = await service.GetAsync(session_id);

        ViewBag.OrderId = session.ClientReferenceId;
        ViewBag.AmountTotal = session.AmountTotal.HasValue
            ? session.AmountTotal.Value / 100.0m
            : 0m;

        return View();
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Cancel(int orderId)
    {
        var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderId == orderId);
        var payment = await _context.Payments.FirstOrDefaultAsync(p => p.OrderId == orderId);

        if (order != null && order.Status == OrderStatus.InProgress)
            order.Status = OrderStatus.Cancelled;

        if (payment != null && payment.Status == PaymentStatus.Pending)
            payment.Status = PaymentStatus.Cancelled;

        await _context.SaveChangesAsync();

        ViewBag.OrderId = orderId;
        return View();
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> MyTickets()
    {
        var user = await GetCurrentUserAsync();
        if (user == null)
            return RedirectToAction("PostLogin", "Account");

        var tickets = await _context.Tickets
            .Include(t => t.Event)
            .Include(t => t.TicketType)
            .Where(t => t.OwnerUserId == user.UserId)
            .OrderByDescending(t => t.TicketId)
            .ToListAsync();

        return View(tickets);
    }

    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    [HttpPost]
    public async Task<IActionResult> StripeWebhook()
    {
        var stripeSecretKey = _configuration["Stripe:SecretKey"];
        var webhookSecret = _configuration["Stripe:WebhookSecret"];

        if (string.IsNullOrWhiteSpace(stripeSecretKey) || string.IsNullOrWhiteSpace(webhookSecret))
            return BadRequest();

        StripeConfiguration.ApiKey = stripeSecretKey;

        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();

        try
        {
            var stripeEvent = EventUtility.ConstructEvent(
                json,
                Request.Headers["Stripe-Signature"],
                webhookSecret
            );

            if (stripeEvent.Type == "checkout.session.completed")
            {
                var session = stripeEvent.Data.Object as Session;
                if (session != null)
                    await HandleCompletedCheckoutSession(session);
            }

            if (stripeEvent.Type == "checkout.session.expired")
            {
                var session = stripeEvent.Data.Object as Session;
                if (session != null)
                    await HandleExpiredCheckoutSession(session);
            }

            return Ok();
        }
        catch
        {
            return BadRequest();
        }
    }

    private async Task HandleCompletedCheckoutSession(Session session)
    {
        var orderIdRaw = session.ClientReferenceId ?? session.Metadata["orderId"];
        var cartIdRaw = session.Metadata["cartId"];
        var buyerUserIdRaw = session.Metadata["buyerUserId"];

        if (!int.TryParse(orderIdRaw, out var orderId) ||
            !int.TryParse(cartIdRaw, out var cartId) ||
            !int.TryParse(buyerUserIdRaw, out var buyerUserId))
        {
            return;
        }

        var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderId == orderId);
        var payment = await _context.Payments.FirstOrDefaultAsync(p => p.OrderId == orderId);
        var cart = await _context.Carts
            .Include(c => c.Items)
                .ThenInclude(i => i.TicketType)
            .Include(c => c.Items)
                .ThenInclude(i => i.Seat)
            .FirstOrDefaultAsync(c => c.CartId == cartId && c.UserId == buyerUserId);

        if (order == null || payment == null || cart == null || cart.Items == null || !cart.Items.Any())
            return;

        if (payment.Status == PaymentStatus.Succeeded)
            return;

        foreach (var item in cart.Items)
        {
            if (item.TicketType == null)
            {
                order.Status = OrderStatus.Cancelled;
                payment.Status = PaymentStatus.Failed;
                await _context.SaveChangesAsync();
                return;
            }

            var available = item.TicketType.QuantityAvailable ?? 0;
            if (available < item.Quantity)
            {
                order.Status = OrderStatus.Cancelled;
                payment.Status = PaymentStatus.Failed;
                await _context.SaveChangesAsync();
                return;
            }
        }

        foreach (var item in cart.Items)
        {
            if (item.TicketType == null) continue;

            item.TicketType.QuantityAvailable = (item.TicketType.QuantityAvailable ?? 0) - item.Quantity;

            for (int i = 0; i < item.Quantity; i++)
            {
                var ticket = new Ticket
                {
                    EventId = item.TicketType.EventId,
                    TicketTypeId = item.TicketTypeId,
                    SeatId = item.SeatId,
                    OrderId = order.OrderId,
                    OwnerUserId = buyerUserId,
                    Price = item.TicketType.Price,
                    QrCode = Guid.NewGuid().ToString("N"),
                    Status = TicketStatus.Active,
                    IsListedForSale = false
                };

                _context.Tickets.Add(ticket);
            }
        }

        order.Status = OrderStatus.Completed;
        payment.Status = PaymentStatus.Succeeded;

        _context.CartItems.RemoveRange(cart.Items);
        cart.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    private async Task HandleExpiredCheckoutSession(Session session)
    {
        var orderIdRaw = session.ClientReferenceId ?? session.Metadata["orderId"];
        if (!int.TryParse(orderIdRaw, out var orderId))
            return;

        var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderId == orderId);
        var payment = await _context.Payments.FirstOrDefaultAsync(p => p.OrderId == orderId);

        if (order != null && order.Status == OrderStatus.InProgress)
            order.Status = OrderStatus.Cancelled;

        if (payment != null && payment.Status == PaymentStatus.Pending)
            payment.Status = PaymentStatus.Cancelled;

        await _context.SaveChangesAsync();
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