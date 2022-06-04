using Fabron.Models;

namespace Fabron.Store;

public interface ISimpleScheduleStore : IStateStore2<SimpleSchedule>
{ }

public interface ICronScheduleStore : IStateStore2<CronSchedule>
{ }

public class InMemorySimpleSchedule : InMemoryStateStore2<SimpleSchedule>, ISimpleScheduleStore
{
    protected override string GetStateKey(SimpleSchedule state)
        => state.Metadata.Key;
}

public class InMemoryCronScheduleStore : InMemoryStateStore2<CronSchedule>, ICronScheduleStore
{
    protected override string GetStateKey(CronSchedule state)
        => state.Metadata.Key;
}
