
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fabron.Models;
using Fabron.Store;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Runtime;

namespace Fabron.Grains;

public interface ISimpleScheduler : IGrainWithStringKey
{
    Task<SimpleSchedule> Register(
        SimpleScheduleSpec spec,
        Dictionary<string, string>? tags,
        string? owner
    );

    Task Unregister();
}

public class SimpleScheduler : TickerGrain, IGrainBase, ISimpleScheduler
{
    private readonly ILogger _logger;
    private readonly ISystemClock _clock;
    private readonly ISimpleScheduleStore _store;
    private readonly SimpleSchedulerOptions _options;

    public SimpleScheduler(
        IGrainContext context,
        IGrainRuntime runtime,
        ILogger<SimpleScheduler> logger,
        IOptions<SimpleSchedulerOptions> options,
        ISystemClock clock,
        ISimpleScheduleStore store) : base(context, runtime, logger, options.Value.TickerInterval)
    {
        _logger = logger;
        _clock = clock;
        _store = store;
        _options = options.Value;
    }

    private SimpleSchedule? _state;
    private string? _eTag;
    async Task IGrainBase.OnActivateAsync(CancellationToken cancellationToken)
    {
        _key = GrainContext.GrainReference.GetPrimaryKeyString();
        (_state, _eTag) = await _store.GetAsync(_key);
    }

    public async Task Unregister()
    {
        if (_state is not null)
        {
            await _store.RemoveAsync(_state.Metadata.Key, _eTag);
            await StopTicker();
        }
    }

    public async Task<SimpleSchedule> Register(
        SimpleScheduleSpec spec,
        Dictionary<string, string>? tags,
        string? owner)
    {
        var utcNow = _clock.UtcNow;
        var schedule_ = spec.Schedule is null || spec.Schedule.Value < utcNow ? utcNow : spec.Schedule.Value;
        await TickAfter(_options.TickerInterval);

        _state = new SimpleSchedule
        {
            Metadata = new()
            {
                Key = _key,
                CreationTimestamp = utcNow,
                Tags = tags,
                Owner = owner
            },
            Spec = spec
        };
        _eTag = await _store.SetAsync(_state, _eTag);

        utcNow = _clock.UtcNow;
        if (schedule_ <= utcNow)
        {
            await Tick(utcNow);
        }
        else
        {
            await TickAfter(schedule_ - utcNow);
        }
        return _state;
    }

    protected override async Task Tick(DateTimeOffset? expectedTickTime)
    {
        if (_state is null || _state.Metadata.DeletionTimestamp is not null)
        {
            TickerLog.UnexpectedTick(_logger, _key, expectedTickTime, "Schedule is not registered");
            await StopTicker();
            return;
        }

        // TODO: raise cloud event
    }
}

