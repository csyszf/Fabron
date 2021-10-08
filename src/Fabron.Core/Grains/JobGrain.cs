
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fabron.Events;
using Fabron.Mando;
using Fabron.Models;
using Fabron.Stores;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Toolkit.Diagnostics;
using Orleans;
using Orleans.Concurrency;
using Orleans.Runtime;

namespace Fabron.Grains
{
    public interface IJobGrain : IGrainWithStringKey
    {
        [ReadOnly]
        Task<Job?> GetState();

        [ReadOnly]
        Task<ExecutionStatus> GetStatus();

        Task<Job> Schedule(
            string commandName,
            string commandData,
            DateTime? schedule,
            Dictionary<string, string>? labels,
            Dictionary<string, string>? annotations);

        Task Delete();

        [AlwaysInterleave]
        Task CommitOffset(long version);

        Task Purge();

        [ReadOnly]
        Task WaitEventsConsumed(int timeoutSeconds);
    }

    public partial class JobGrain : Grain, IJobGrain, IRemindable
    {
        private readonly TimeSpan _defaultTickPeriod = TimeSpan.FromMinutes(2);
        private readonly ILogger _logger;
        private readonly JobOptions _options;
        private readonly IMediator _mediator;
        private readonly IJobEventStore _eventStore;
        private IGrainReminder? _tickReminder;

        public JobGrain(
            ILogger<JobGrain> logger,
            IOptions<JobOptions> options,
            IMediator mediator,
            IJobEventStore store)
        {
            _logger = logger;
            _options = options.Value;
            _mediator = mediator;
            _eventStore = store;
        }

        public override async Task OnActivateAsync()
        {
            _key = this.GetPrimaryKeyString();
            _consumer = GrainFactory.GetGrain<IJobEventConsumer>(_key);

            _logger.LogInformation("[{Key}]: Loading state", _key);

            var getConsumerOffsetTask = _eventStore.GetConsumerOffset(_key);

            List<EventLog> eventLogs = await _eventStore.GetEventLogs(_key, 0);
            foreach (EventLog? eventLog in eventLogs)
            {
                TransitionState(eventLog);
            }
            _consumerOffset = await getConsumerOffsetTask;

            _logger.LogInformation("[{Key}]: Loaded state", _key);
        }

        private string _key = default!;
        private IJobEventConsumer _consumer = default!;
        private long _consumerOffset;
        private Job? _state;
        private TaskCompletionSource<bool>? _consumingCompletionSource;

        private Job State
        {
            get
            {
                Guard.IsNotNull(_state, nameof(State));
                return _state;
            }
        }
        private bool ConsumerNotFollowedUp => _state is not null && _state.Version != _consumerOffset;
        private bool Deleted => _state is null || _state.Status.Deleted;
        private bool Purged => _state is null && _consumerOffset == -1;
        private bool DeletedButNotPurged => Deleted && !Purged;

        public Task<Job?> GetState() => Task.FromResult(_state);

        public Task<ExecutionStatus> GetStatus()
        {
            return Task.FromResult(_state is not null ? State.Status.ExecutionStatus : ExecutionStatus.NotScheduled);
        }

        public async Task Purge()
        {
            if (ConsumerNotFollowedUp)
            {
                await NotifyConsumer();
                await WaitEventsConsumed(10);
                return;
            }

            if (_state != null)
            {
                await _eventStore.ClearEventLogs(_key, long.MaxValue);
                _state = null;
            }
            if (_consumerOffset != -1)
            {
                await _eventStore.ClearConsumerOffset(_key);
                await _consumer.Reset();
                _consumerOffset = -1;
            }
            await StopTicker();
            _logger.Purged(_key);
        }

        public async Task Delete()
        {
            if (Deleted)
            {
                return;
            }
            JobDeleted? @event = new JobDeleted();
            await RaiseAsync(@event, nameof(JobDeleted));
        }

        private async Task Complete() => await StopTicker();

        public async Task<Job> Schedule(
            string commandName,
            string commandData,
            DateTime? schedule,
            Dictionary<string, string>? labels,
            Dictionary<string, string>? annotations)
        {
            _logger.LogInformation("[{Key}]: Scheduling", _key);

            DateTime utcNow = DateTime.UtcNow;
            DateTime schedule_ = schedule is null || schedule.Value < utcNow ? utcNow : (DateTime)schedule;
            await EnsureTicker(TimeSpan.FromMinutes(2));
            _logger.LogInformation("[{Key}]: Ticker registered (1)", _key);

            JobScheduled jobScheduled = new JobScheduled(
                labels ?? new Dictionary<string, string>(),
                annotations ?? new Dictionary<string, string>(),
                schedule_,
                commandName,
                commandData
            );
            await RaiseAsync(jobScheduled);

            utcNow = DateTime.UtcNow;
            if (schedule_ <= utcNow)
            {
                _ = Task.Factory.StartNew(Tick).Unwrap();
            }
            else
            {
                await TickAfter(schedule_ - utcNow);
                _logger.LogInformation("[{Key}]: Ticker registered (2)", _key);
            }
            return State;
        }

        private async Task Tick()
        {
            if (Deleted)
            {
                if (!Purged)
                    await Purge();
                return;
            }

            Task next = State.Status switch
            {
                { ExecutionStatus: ExecutionStatus.Scheduled } => Start(),
                { ExecutionStatus: ExecutionStatus.Started } => Execute(),
                { ExecutionStatus: ExecutionStatus.Succeed or ExecutionStatus.Faulted } => Complete(),
                _ => Task.FromException(ThrowHelper.CreateInvalidJobExecutionState(State.Status.ExecutionStatus))
            };
            await next;
        }

        private async Task Start()
        {
            _logger.StartingJobExecution(State.Metadata.Key);
            MetricsHelper.JobScheduleTardiness.Observe(State.Tardiness.TotalSeconds);

            JobExecutionStarted jobExecutionStarted = new JobExecutionStarted();
            await RaiseAsync(jobExecutionStarted);

            await Tick();
        }

        private async Task Execute()
        {
            string? result;
            try
            {
                result = await _mediator.Handle(State.Spec.CommandName, State.Spec.CommandData);
                Guard.IsNotNull(result, nameof(result));
            }
            catch (Exception e)
            {
                if (e is not TaskCanceledException || State.Status.ExecutionStatus != ExecutionStatus.Canceled)
                {
                    JobExecutionFailed jobExecutionFailed = new JobExecutionFailed(e.Message);
                    await RaiseAsync(jobExecutionFailed);
                }

                await Tick();
                return;
            }

            JobExecutionSucceed jobExecutionSucceed = new(result);
            await RaiseAsync(jobExecutionSucceed);
            MetricsHelper.JobCount_RanToCompletion.Inc();

            await Tick();
        }

        private async Task TickAfter(TimeSpan dueTime)
        {
            await EnsureTicker(dueTime);
            _logger.TickerRegistered(_key, dueTime);
        }

        private async Task EnsureTicker(TimeSpan dueTime)
            => _tickReminder = await RegisterOrUpdateReminder(Names.TickerReminder, dueTime, _defaultTickPeriod);

        private async Task StopTicker()
        {
            int retry = 0;
            while (true)
            {
                _tickReminder = await GetReminder(Names.TickerReminder);
                if (_tickReminder is null) break;
                try
                {
                    await UnregisterReminder(_tickReminder);
                    _tickReminder = null;
                    _logger.TickerStopped(_key);
                    break;
                }
                catch (Orleans.Runtime.ReminderException)
                {
                    if (retry++ < 3)
                    {
                        _logger.RetryUnregisterReminder(_key);
                        continue;
                    }
                    throw;
                }
                catch (OperationCanceledException)
                {
                    // ReminderService has been stopped
                    return;
                }
            }
        }

        public async Task ReceiveReminder(string reminderName, TickStatus status)
        {
            if (_tickReminder is null)
            {
                _tickReminder = await GetReminder(Names.TickerReminder);
            }
            await Tick();
        }

        public async Task CommitOffset(long offset)
        {
            long currentVersion = State.Version;
            if (offset < _consumerOffset || offset > currentVersion)
            {
                _logger.ConsumerOffsetInvalid(_key, offset, _consumerOffset, currentVersion);
                return;
            }
            if (offset == _consumerOffset || offset == currentVersion)
            {
                return;
            }

            await _eventStore.SaveConsumerOffset(State.Metadata.Key, offset);
            _consumerOffset = offset;
            _logger.ConsumerOffsetUpdated(_key, _consumerOffset);

            if (_consumingCompletionSource != null && _consumerOffset == State.Version)
            {
                _consumingCompletionSource.SetResult(true);
            }
        }

        public async Task WaitEventsConsumed(int timeoutSeconds)
        {
            if (ConsumerNotFollowedUp)
            {
                _consumingCompletionSource = new TaskCompletionSource<bool>();
                try
                {
                    await _consumingCompletionSource.Task.WaitAsync(TimeSpan.FromSeconds(timeoutSeconds));
                }
                catch (TimeoutException)
                {
                }
                if (ConsumerNotFollowedUp)
                {
                    ThrowHelper.ThrowConsumerNotFollowedUp(_key, State.Version, _consumerOffset);
                }
            }
        }
    }
}
