using System;
using System.Linq;

var basketId = Guid.NewGuid();
Application.Execute(new CreateBasket(basketId));
Application.Execute(new AddItem(basketId, "iPhone"));
Application.Execute(new AddItem(basketId, "TV"));
Application.Execute(new AddItem(basketId, "Blender"));

public abstract record Command(Guid basketId);
public abstract record Event;

// Commands
public record CreateBasket(Guid basketId) : Command(basketId);
public record AddItem(Guid basketId, string name) : Command(basketId);

// Events
public record BasketCreated(Guid id) : Event;
public record ItemAdded(Guid basketId, string name) : Event;

// State
public record Basket(Guid id, string[] items) {
    override public string ToString() => $"Basket({id}, items: [{string.Join(", ", items)}])";
}

// Functions

public class Application
{
    public static IEnumerable<Event> Decide(Command command, Basket? state)
    {
        return command switch
        {
            CreateBasket c => new List<Event> { new BasketCreated(c.basketId) },
            AddItem c =>
                    Handle(c, state),
            _ => throw new NotImplementedException()
        };
    }

    private static List<Event> Handle(AddItem addItem, Basket? state)
    {
        if (state is null)
        {
            throw new InvalidOperationException();
        }
        return new List<Event> { new ItemAdded(addItem.basketId, addItem.name) };
    }

    public static Basket Build(Basket? state, Event evt)
    {
        return evt switch
        {
            BasketCreated e => new Basket(e.id, Array.Empty<string>()),
            ItemAdded e => state! with { items = state.items.Append(e.name).ToArray()},
            _ => throw new NotImplementedException()
        };
    }

    public static Dictionary<Guid, List<Event>> eventStore = new ();
    public static void PrintStore()
    {
        foreach (var (id, events) in eventStore)
        {
            Console.WriteLine($"{id}:");
            foreach (var evt in events)
            {
                Console.WriteLine($"  {evt}");
            }
        }
    }
    public static Basket Execute(Command command)
    {
        var events = eventStore.GetValueOrDefault(command.basketId, new List<Event>());
        var state = events.Aggregate(null as Basket, Build);
        var outcome = Decide(command, state);
        var newState = outcome.Aggregate(state, Build);
        eventStore[command.basketId] = events.AsEnumerable().Concat(outcome).ToList();
        PrintStore();
        Console.WriteLine("==> Result: " + newState);
        return newState;
    }
}
