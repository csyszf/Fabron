using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Runtime;
using Orleans.Timers;

namespace Fabron.Grains;

public abstract partial class TickerGrain : IRemindable
{
    private readonly IGrainRuntime _runtime;
    private readonly ILogger _logger;
    private readonly TimeSpan _interval;
    public TickerGrain(
        IGrainContext context,
        IGrainRuntime runtime,
        ILogger logger,
        TimeSpan interval)
    {
        GrainContext = context;
        _logger = logger;
        _interval = interval;
        _runtime = runtime;
    }

    protected string _key = default!;

    private IGrainReminder? _tickReminder;

    public IGrainContext GrainContext { get; }

    protected abstract Task Tick(DateTimeOffset? expectedTickTime = null);

    protected async Task TickAfter(TimeSpan dueTime)
    {
        if (_tickReminder is null)
            _tickReminder = await _runtime.ReminderRegistry.RegisterOrUpdateReminder(GrainContext.GrainId, Names.TickerReminder, dueTime, _interval);

        // ?
        if (dueTime != _interval)
        {
            _runtime.TimerRegistry.RegisterTimer(GrainContext, obj => Tick(), null, dueTime, TimeSpan.FromMilliseconds(-1));
        }

        TickerLog.TickerRegistered(_logger, _key, dueTime);
    }

    protected async Task StopTicker()
    {
        int retry = 0;
        while (true)
        {
            _tickReminder = await _runtime.ReminderRegistry.GetReminder(GrainContext.GrainId, Names.TickerReminder);
            if (_tickReminder is null) break;
            try
            {
                await _runtime.ReminderRegistry.UnregisterReminder(GrainContext.GrainId, _tickReminder);
                _tickReminder = null;
                TickerLog.TickerDisposed(_logger, _key);
                break;
            }
            catch (ReminderException)
            {
                if (retry++ < 3)
                {
                    TickerLog.RetryUnregisterReminder(_logger, _key);
                    continue;
                }
                throw;
            }
            catch (OperationCanceledException)
            {
                // ReminderService has been stopped
                // TODO: add log
                return;
            }
        }
    }

    async Task IRemindable.ReceiveReminder(string reminderName, TickStatus status)
    {
        if (_tickReminder is null)
        {
            _tickReminder = await _runtime.ReminderRegistry.GetReminder(GrainContext.GrainId, Names.TickerReminder);
        }
        await Tick(status.FirstTickTime);
    }

    public static partial class TickerLog
    {
        [LoggerMessage(
            EventId = 17001,
            Level = LogLevel.Debug,
            Message = "[{key}]: Ticker registered with due time: {dueTime}")]
        public static partial void TickerRegistered(ILogger logger, string key, TimeSpan dueTime);

        [LoggerMessage(
            EventId = 17002,
            Level = LogLevel.Debug,
            Message = "[{key}]: Ticker disposed")]
        public static partial void TickerDisposed(ILogger logger, string key);

        [LoggerMessage(
            EventId = 17003,
            Level = LogLevel.Warning,
            Message = "[{key}]: Unexpected tick at {tickTime}")]
        public static partial void UnexpectedTick(ILogger logger, string key, string tickTime);

        [LoggerMessage(
            EventId = 10004,
            Level = LogLevel.Warning,
            Message = "[{key}]: Unregister reminder failed, retry")]
        public static partial void RetryUnregisterReminder(ILogger logger, string key);
    }

}


