﻿using System;
using System.Threading.Tasks;
using Fabron.Events;
using Fabron.Models;
using Microsoft.Toolkit.Diagnostics;
using Orleans;

namespace Fabron.Grains
{
    public partial class JobGrain
    {
        private EventLog CreateEventLog<TEvent>(TEvent @event, string type) where TEvent : class
            => EventLog.Create<TEvent>(
                this.GetPrimaryKeyString(),
                (_state?.Version ?? -1) + 1,
                DateTime.UtcNow,
                type,
                @event);

        private async Task CommitAsync(EventLog eventLog)
        {
            TransitionState(eventLog);
            await _eventStore.CommitEventLog(eventLog);
            _logger.EventRaised(eventLog);
            await _consumer.NotifyChanged(_offset, eventLog.Version);
        }

        private async Task RaiseAsync(JobScheduled jobScheduled)
        {
            EventLog eventLog = CreateEventLog(jobScheduled, nameof(JobScheduled));
            await CommitAsync(eventLog);
            MetricsHelper.JobCount_Scheduled.Inc();
        }

        private async Task RaiseAsync(JobExecutionStarted jobExecutionStarted)
        {
            EventLog eventLog = CreateEventLog(jobExecutionStarted, nameof(JobExecutionStarted));
            await CommitAsync(eventLog);
            MetricsHelper.JobCount_Running.Inc();
        }

        private async Task RaiseAsync(JobExecutionSucceed jobExecutionSucceed)
        {
            EventLog eventLog = CreateEventLog(jobExecutionSucceed, nameof(JobExecutionSucceed));
            await CommitAsync(eventLog);
            MetricsHelper.JobCount_RanToCompletion.Inc();
        }

        private async Task RaiseAsync(JobExecutionFailed jobExecutionFailed)
        {
            EventLog eventLog = CreateEventLog(jobExecutionFailed, nameof(JobExecutionFailed));
            await CommitAsync(eventLog);
            MetricsHelper.JobCount_Faulted.Inc();
        }

        private void TransitionState(EventLog eventlog)
        {
            var @event = IJobEvent.Get(eventlog);
            _state = @event switch
            {
                JobScheduled e => _state.Apply(e, this.GetPrimaryKeyString(), eventlog.Timestamp),
                JobExecutionStarted e => State.Apply(e, eventlog.Timestamp),
                JobExecutionSucceed e => State.Apply(e, eventlog.Timestamp),
                JobExecutionFailed e => State.Apply(e, eventlog.Timestamp),
                _ => ThrowHelper.ThrowInvalidEventName<Job>(eventlog.EntityId, eventlog.Version, eventlog.Type)
            };
            Guard.IsEqualTo(State.Version, eventlog.Version, nameof(State.Version));
        }
    }

    public static class JobEventsExtensions
    {
        public static Job Apply(this Job? state, JobScheduled @event, string id, DateTime timestamp)
            => new(
                new(id, timestamp, @event.Labels, @event.Annotations),
                new(@event.Schedule, @event.CommandName, @event.CommandData),
                JobStatus.Initial,
                0);

        public static Job Apply(this Job state, JobExecutionStarted @event, DateTime timestamp)
            => state with
            {
                Status = state.Status with
                {
                    StartedAt = timestamp,
                    ExecutionStatus = ExecutionStatus.Started
                },
                Version = state.Version + 1
            };

        public static Job Apply(this Job state, JobExecutionSucceed @event, DateTime timestamp)
            => state with
            {
                Status = state.Status with
                {
                    Result = @event.Result,
                    StartedAt = timestamp,
                    ExecutionStatus = ExecutionStatus.Succeed
                },
                Version = state.Version + 1
            };

        public static Job Apply(this Job state, JobExecutionFailed @event, DateTime timestamp)
            => state with
            {
                Status = state.Status with
                {
                    StartedAt = timestamp,
                    ExecutionStatus = ExecutionStatus.Faulted,
                    Reason = @event.Reason,
                },
                Version = state.Version + 1
            };

    }
}
