using System.Threading.Tasks;
using Fabron.Models;

namespace Fabron.Core.CloudEvents;

public interface IEventRouter
{
    bool Matches(ScheduleMetadata metadata, CloudEventEnvelop envelop);
    ValueTask DispatchAsync(ScheduleMetadata metadata, CloudEventEnvelop envelop);
}
