
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fabron.Core.CloudEvents;
using Fabron.Models;
using Fabron.Store;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Toolkit.Diagnostics;
using Orleans;
using Orleans.Concurrency;
using Orleans.Runtime;

namespace Fabron.Grains;

public interface ICronEventScheduler : IGrainWithStringKey
{
    [ReadOnly]
    ValueTask<CronEvent?> GetState();

    Task<CronEvent> Schedule(
        CronEventSpec spec,
        Dictionary<string, string>? labels,
        Dictionary<string, string>? annotations,
        string? owner
    );

    Task Unregister();
}

public class CronEventScheduler : TickerGrain, IGrainBase, ICronEventScheduler
{
    private readonly ILogger _logger;
    private readonly ISystemClock _clock;
    private readonly ICronEventStore _store;
    private readonly CronSchedulerOptions _options;
    private readonly IEventDispatcher _dispatcher;

    public CronEventScheduler(
        IGrainContext context,
        IGrainRuntime runtime,
        ILogger<CronEventScheduler> logger,
        IOptions<CronSchedulerOptions> options,
        ISystemClock clock,
        ICronEventStore store,
        IEventDispatcher dispatcher) : base(context, runtime, logger, options.Value.TickerInterval)
    {
        _logger = logger;
        _clock = clock;
        _store = store;
        _options = options.Value;
        _dispatcher = dispatcher;
    }

    private CronEvent? _state;
    private string? _eTag;
    private DateTimeOffset? _lastSchedule;

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

    public ValueTask<CronEvent?> GetState() => new(_state);

    public async Task<CronEvent> Schedule(
        CronEventSpec spec,
        Dictionary<string, string>? labels,
        Dictionary<string, string>? annotations,
        string? owner)
    {
        var utcNow = _clock.UtcNow;
        _state = new CronEvent
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

        if (!_state.Spec.Suspend && (_state.Spec.NotBefore is null || _state.Spec.NotBefore.Value <= utcNow))
        {
            await Tick(utcNow);
        }
        return _state;
    }

    protected override async Task Tick(DateTimeOffset? expectedTickTime)
    {
        if (_state is null || _state.Metadata.DeletionTimestamp is not null)
        {
            TickerLog.UnexpectedTick(_logger, _key, expectedTickTime, "NotRegistered");
            await StopTicker();
            return;
        }

        if (_state.Spec.Suspend)
        {
            TickerLog.UnexpectedTick(_logger, _key, expectedTickTime, "Suspended");
            await StopTicker();
            return;
        }

        DateTimeOffset now = _clock.UtcNow;
        if (now > _state.Spec.ExpirationTime)
        {
            TickerLog.UnexpectedTick(_logger, _key, expectedTickTime, "Expired");
            await StopTicker();
            return;
        }

        if (_state.Spec.NotBefore.HasValue && now < _state.Spec.NotBefore.Value)
        {
            TickerLog.UnexpectedTick(_logger, _key, expectedTickTime, "NotStarted");
            await TickAfter(_state.Spec.NotBefore.Value.Subtract(now));
            return;
        }

        Cronos.CronExpression cron = Cronos.CronExpression.Parse(_state.Spec.Schedule, _options.CronFormat);

        DateTimeOffset from = now.AddSeconds(-10);
        if (_lastSchedule.HasValue && _lastSchedule.Value > from)
        {
            from = _lastSchedule.Value;
        }
        var to = now.AddSeconds(10);
        if (to > _state.Spec.ExpirationTime)
        {
            to = _state.Spec.ExpirationTime.Value;
        }

        var schedules = cron.GetOccurrences(from, to, _options.TimeZone, fromInclusive: false, toInclusive: true);
        if (schedules.Any())
        {
            var scheduleTasks = schedules.Select(DispatchNew);
            await Task.WhenAll(scheduleTasks);
            _lastSchedule = schedules.Last();
        }
        from = to;
        var nextTick = cron.GetNextOccurrence(from, _options.TimeZone);
        if (!nextTick.HasValue || (_state.Spec.ExpirationTime.HasValue && nextTick.Value > _state.Spec.ExpirationTime.Value))
        {
            // no more next tick
            await StopTicker();
            return;
        }
        await TickAfter(nextTick.Value.Subtract(now));
    }

    private async Task DispatchNew(DateTimeOffset schedule)
    {
        Guard.IsNotNull(_state, nameof(_state));
        try
        {
            await _dispatcher.DispatchAsync(_state.Metadata, _state.ToCloudEvent(schedule, _options.JsonSerializerOptions));
        }
        catch (Exception e)
        {
            TickerLog.ErrorOnTicking(_logger, _key, e);
            return;
        }
    }
}

