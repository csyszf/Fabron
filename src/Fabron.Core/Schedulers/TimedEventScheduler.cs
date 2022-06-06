
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fabron.Core.CloudEvents;
using Fabron.Models;
using Fabron.Store;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Concurrency;
using Orleans.Runtime;

namespace Fabron.Grains;

public interface ITimedEventScheduler : IGrainWithStringKey
{
    Task<TimedEvent> Schedule(
        TimedEventSpec spec,
        Dictionary<string, string>? labels,
        Dictionary<string, string>? annotations,
        string? owner
    );

    [ReadOnly]
    ValueTask<TimedEvent?> GetState();

    Task Unregister();
}

public class TimedEventScheduler : TickerGrain, IGrainBase, ITimedEventScheduler
{
    private readonly ILogger _logger;
    private readonly ISystemClock _clock;
    private readonly ITimedEventStore _store;
    private readonly SimpleSchedulerOptions _options;
    private readonly IEventDispatcher _mediator;

    public TimedEventScheduler(
        IGrainContext context,
        IGrainRuntime runtime,
        ILogger<TimedEventScheduler> logger,
        IOptions<SimpleSchedulerOptions> options,
        ISystemClock clock,
        ITimedEventStore store,
        IEventDispatcher mediator) : base(context, runtime, logger, options.Value.TickerInterval)
    {
        _logger = logger;
        _clock = clock;
        _store = store;
        _options = options.Value;
        _mediator = mediator;
    }

    private TimedEvent? _state;
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

    public ValueTask<TimedEvent?> GetState() => ValueTask.FromResult(_state);

    public async Task<TimedEvent> Schedule(
        TimedEventSpec spec,
        Dictionary<string, string>? labels,
        Dictionary<string, string>? annotations,
        string? owner)
    {
        var utcNow = _clock.UtcNow;
        var schedule_ = spec.Schedule;
        await TickAfter(_options.TickerInterval);

        _state = new TimedEvent
        {
            Metadata = new()
            {
                Key = _key,
                CreationTimestamp = utcNow,
                Labels = labels,
                Annotations = annotations,
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

        try
        {
            var envelop = _state.ToCloudEvent(_state.Spec.Schedule, _options.JsonSerializerOptions);
            await _mediator.DispatchAsync(_state.Metadata, envelop);
        }
        catch (Exception e)
        {
            TickerLog.ErrorOnTicking(_logger, _key, e);
            return;
        }
    }
}

