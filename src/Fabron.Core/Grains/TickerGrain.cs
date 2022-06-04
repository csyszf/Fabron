using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Runtime;

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
        _tickReminder = await _runtime.ReminderRegistry.RegisterOrUpdateReminder(GrainContext.GrainId, Names.TickerReminder, dueTime, _interval);
        TickerLog.TickerRegistered(_logger, _key, dueTime);
    }

    protected async Task StopTicker()
    {
        int retry = 0;
        while (true)
        {
            if (_tickReminder is null)
            {
                _tickReminder = await _runtime.ReminderRegistry.GetReminder(GrainContext.GrainId, Names.TickerReminder);
            }
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
            EventId = 13100,
            Level = LogLevel.Information,
            Message = "[{key}]: Ticker registered with due time: {dueTime}")]
        public static partial void TickerRegistered(ILogger logger, string key, TimeSpan dueTime);

        [LoggerMessage(
            EventId = 13200,
            Level = LogLevel.Warning,
            Message = "[{key}]: Unexpected tick at {tickTime}, reason: {reason}")]
        public static partial void UnexpectedTick(ILogger logger, string key, string tickTime, string reason);

        public static void UnexpectedTick(ILogger logger, string key, DateTimeOffset? tickTime, string reason)
            => UnexpectedTick(logger, key, tickTime.HasValue ? tickTime.Value.ToString("o") : "unknown", reason);

        [LoggerMessage(
            EventId = 13300,
            Level = LogLevel.Warning,
            Message = "[{key}]: tick missed at {tickTime}")]
        public static partial void TickMissed(ILogger logger, string key, string tickTime);

        [LoggerMessage(
            EventId = 13400,
            Level = LogLevel.Error,
            Message = "[{key}]: error on ticking")]
        public static partial void ErrorOnTicking(ILogger logger, string key, Exception exception);

        [LoggerMessage(
            EventId = 13900,
            Level = LogLevel.Information,
            Message = "[{key}]: Ticker disposed")]
        public static partial void TickerDisposed(ILogger logger, string key);

        [LoggerMessage(
            EventId = 13910,
            Level = LogLevel.Warning,
            Message = "[{key}]: Unregister reminder failed, retry")]
        public static partial void RetryUnregisterReminder(ILogger logger, string key);
    }
}
