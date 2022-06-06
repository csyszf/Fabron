
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabron.Models;

namespace Fabron.Core.CloudEvents;

public interface IEventDispatcher
{
    List<IEventRouter> Routers { get; }

    ValueTask DispatchAsync(ScheduleMetadata metadata, CloudEventEnvelop envelop);
}

public class EventDispatcher : IEventDispatcher
{
    public EventDispatcher(IEnumerable<IEventRouter> routers)
    {
        Routers = routers.ToList();
    }

    public List<IEventRouter> Routers { get; }

    public ValueTask DispatchAsync(ScheduleMetadata metadata, CloudEventEnvelop envelop)
    {
        foreach (var router in Routers)
        {
            if (router.Matches(metadata, envelop))
            {
                return router.DispatchAsync(metadata, envelop);
            }
        }
        return ValueTask.CompletedTask;
    }
}
