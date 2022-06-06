using System.Collections.Generic;

namespace Fabron.Core.CloudEvents;

public class SimpleEventRouterOptions
{
    public List<SimpleEventRouter.Route> Routes { get; init; } = new();
}
