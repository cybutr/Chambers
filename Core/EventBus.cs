// EventBus — subscribe to any event with EventBus.Subscribe<T>(handler)
//
// Available events:
//   DayStartedEvent(int DayCount, double Season)          — each day rollover
//   SunriseEvent(int DayCount)                            — TimeOfDay crosses SunriseTime
//   SunsetEvent(int DayCount)                             — TimeOfDay crosses SunsetTime
//   SeasonChangedEvent(int Season)                        — 0=spring 1=summer 2=autumn 3=winter
//   WeatherChangedEvent(WeatherType Previous, WeatherType Current)
//   MapGeneratedEvent(int Seed, int Width, int Height)    — after Generate() completes

public record DayStartedEvent(int DayCount, double Season);
public record SunriseEvent(int DayCount);
public record SunsetEvent(int DayCount);
public record SeasonChangedEvent(int Season);
public record WeatherChangedEvent(WeatherType Previous, WeatherType Current);
public record MapGeneratedEvent(int Seed, int Width, int Height);

public static class EventBus
{
    private static readonly Dictionary<Type, List<Delegate>> _handlers = [];
    private static readonly object _lock = new();

    public static void Subscribe<T>(Action<T> handler)
    {
        lock (_lock)
        {
            if (!_handlers.TryGetValue(typeof(T), out var list))
                _handlers[typeof(T)] = list = [];
            list.Add(handler);
        }
    }

    public static void Unsubscribe<T>(Action<T> handler)
    {
        lock (_lock)
        {
            if (_handlers.TryGetValue(typeof(T), out var list))
                list.Remove(handler);
        }
    }

    public static void Emit<T>(T evt)
    {
        List<Delegate> snapshot;
        lock (_lock)
        {
            if (!_handlers.TryGetValue(typeof(T), out var list)) return;
            snapshot = [..list];
        }
        foreach (var h in snapshot)
            ((Action<T>)h)(evt);
    }
}
