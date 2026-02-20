using EventMaster.Models;
using EventMaster.ViewModels;

namespace EventMaster.Services;

public static class InMemoryStore
{
    private static readonly object Sync = new();
    private static bool _initialized;

    private static readonly List<Event> Events = new();
    private static readonly List<TicketType> TicketTypes = new();

    private static int _nextEventId = 1;
    private static int _nextTicketTypeId = 1;

    public static IReadOnlyList<Event> GetUpcomingEvents()
    {
        EnsureInitialized();
        lock (Sync)
        {
            return Events.OrderBy(e => e.EventDate).ThenBy(e => e.EventTime).ToList();
        }
    }

    public static Event? GetEvent(int eventId)
    {
        EnsureInitialized();
        lock (Sync) return Events.FirstOrDefault(e => e.EventId == eventId);
    }

    public static IReadOnlyList<TicketType> GetTicketTypesForEvent(int eventId)
    {
        EnsureInitialized();
        lock (Sync)
        {
            return TicketTypes.Where(t => t.EventId == eventId).OrderBy(t => t.Price).ToList();
        }
    }

    public static TicketType? GetTicketType(int ticketTypeId)
    {
        EnsureInitialized();
        lock (Sync) return TicketTypes.FirstOrDefault(t => t.TicketTypeId == ticketTypeId);
    }

    public static (bool ok, string message, TicketReceipt? receipt) Purchase(string buyerEmail, int ticketTypeId, int quantity)
    {
        EnsureInitialized();

        if (string.IsNullOrWhiteSpace(buyerEmail))
            return (false, "Please log in and try again.", null);

        if (quantity is < 1 or > 10)
            return (false, "Quantity must be between 1 and 10.", null);

        lock (Sync)
        {
            var tt = TicketTypes.FirstOrDefault(t => t.TicketTypeId == ticketTypeId);
            if (tt == null) return (false, "Ticket type not found.", null);

            var ev = Events.FirstOrDefault(e => e.EventId == tt.EventId);
            if (ev == null) return (false, "Event not found.", null);

            var available = tt.QuantityAvailable ?? 0;
            if (available < quantity)
                return (false, $"Only {available} left for {tt.Name}.", null);

            tt.QuantityAvailable = available - quantity;

            var receipt = new TicketReceipt
            {
                ReceiptId = Guid.NewGuid().ToString("N"),
                PurchasedAtUtc = DateTime.UtcNow,
                BuyerEmail = buyerEmail.Trim(),
                EventId = ev.EventId,
                EventName = ev.EventName,
                TicketTypeId = tt.TicketTypeId,
                TicketTypeName = tt.Name,
                Quantity = quantity,
                UnitPrice = tt.Price,
                Total = tt.Price * quantity,
                TicketNumbers = Enumerable.Range(0, quantity)
                    .Select(_ => Guid.NewGuid().ToString("N")[..10].ToUpperInvariant())
                    .ToList()
            };

            return (true, "OK", receipt);
        }
    }

    private static void EnsureInitialized()
    {
        if (_initialized) return;

        lock (Sync)
        {
            if (_initialized) return;

            var organizer = new User
            {
                UserId = 1,
                FirstName = "Demo",
                LastName = "Organizer",
                Email = "organizer@eventmaster.local",
                Password = "demo",
                Role = Roles.Organizer
            };

            AddSeedEvent(organizer, "Toronto's HipHop Classic", "Hometown Artists with electric energy!", DateTime.Today.AddDays(7), new TimeSpan(19, 30, 0));
            AddSeedEvent(organizer, "DJ P House Music Tour", "Late-night set + lasers!", DateTime.Today.AddDays(12), new TimeSpan(22, 0, 0));
            AddSeedEvent(organizer, "Poetry Jazz Night", "Jazz, poetry, and great vibes!", DateTime.Today.AddDays(4), new TimeSpan(20, 0, 0));

            _initialized = true;
        }
    }

    private static void AddSeedEvent(User organizer, string name, string desc, DateTime date, TimeSpan time)
    {
        var ev = new Event
        {
            EventId = _nextEventId++,
            EventName = name,
            EventDescription = desc,
            EventDate = date,
            EventTime = DateTime.Today.Add(time),
            OrganizerId = organizer.UserId,
            Organizer = organizer
        };

        Events.Add(ev);

        TicketTypes.Add(new TicketType
        {
            TicketTypeId = _nextTicketTypeId++,
            EventId = ev.EventId,
            Event = ev,
            Name = "General Admission",
            Price = 25m,
            RequiresSeat = false,
            QuantityAvailable = 150
        });

        TicketTypes.Add(new TicketType
        {
            TicketTypeId = _nextTicketTypeId++,
            EventId = ev.EventId,
            Event = ev,
            Name = "VIP",
            Price = 60m,
            RequiresSeat = false,
            QuantityAvailable = 30
        });
    }
    
    public static IReadOnlyList<Event> GetEventsForOrganizer(int organizerId)
    {
        EnsureInitialized();
        lock (Sync)
        {
            return Events
                .Where(e => e.OrganizerId == organizerId)
                .OrderBy(e => e.EventDate)
                .ThenBy(e => e.EventTime)
                .ToList();
        }
    }
    
    public static int NextEventId()
    {
        EnsureInitialized();
        lock (Sync) return _nextEventId++;
    }

    public static int NextTicketTypeId()
    {
        EnsureInitialized();
        lock (Sync) return _nextTicketTypeId++;
    }

    public static void AddEvent(Event ev)
    {
        EnsureInitialized();
        lock (Sync) Events.Add(ev);
    }

    public static void AddTicketType(TicketType tt)
    {
        EnsureInitialized();
        lock (Sync) TicketTypes.Add(tt);
    }
}