using Fabron.Models;

namespace Fabron.Store;

public interface ISimpleScheduleStore : IStateStore2<TimedEvent>
{ }

public interface ICronScheduleStore : IStateStore2<CronEvent>
{ }

public class InMemorySimpleSchedule : InMemoryStateStore2<TimedEvent>, ISimpleScheduleStore
{
    protected override string GetStateKey(TimedEvent state)
        => state.Metadata.Key;
}

public class InMemoryCronScheduleStore : InMemoryStateStore2<CronEvent>, ICronScheduleStore
{
    protected override string GetStateKey(CronEvent state)
        => state.Metadata.Key;
}
