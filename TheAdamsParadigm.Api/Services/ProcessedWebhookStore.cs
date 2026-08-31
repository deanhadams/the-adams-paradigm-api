using System.Collections.Concurrent;

namespace TheAdamsParadigm.Api.Services;

public class ProcessedWebhookStore
{
    private readonly ConcurrentDictionary<string, DateTime> _processedEvents = new();

    public bool HasBeenProcessed(string eventId)
    {
        return _processedEvents.ContainsKey(eventId);
    }

    public bool TryMarkAsProcessed(string eventId)
    {
        return _processedEvents.TryAdd(eventId, DateTime.UtcNow);
    }

    public int Count => _processedEvents.Count;
}
