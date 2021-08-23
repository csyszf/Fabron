﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Fabron.Models
{
    public record JobItem(
        uint Index,
        string Uid,
        DateTime Schedule,
        ExecutionStatus Status
    );

    public record CronJobMetadata(
        string Uid,
        DateTime CreationTimestamp,
        Dictionary<string, string> Labels
    );

    public record CronJobSpec(
        string Schedule,
        string CommandName,
        string CommandData,
        DateTime? NotBefore,
        DateTime? ExpirationTime
    );

    public record CronJobStatus(
        List<JobItem> Jobs,
        uint LatestScheduleIndex = 0,
        DateTime? CompletionTimestamp = null,
        string? Reason = null,
        bool Finalized = false
    );

    public class CronJob
    {
        public CronJobMetadata Metadata { get; init; } = default!;
        public CronJobSpec Spec { get; init; } = default!;
        public CronJobStatus Status { get; set; } = default!;
        public ulong Version { get; set; }

        public IEnumerable<JobItem> RunningJobs => Status.Jobs.Where(item => item.Status == ExecutionStatus.Scheduled);
        public IEnumerable<JobItem> FinishedJobs => Status.Jobs.Where(item => item.Status is ExecutionStatus.Succeed or ExecutionStatus.Faulted);

        public JobItem? LatestItem => Status.Jobs.LastOrDefault();

        public bool HasRunningJobs => Status.Jobs.Any(item => item.Status == ExecutionStatus.Scheduled);

        public DateTime? GetNextSchedule()
        {
            Cronos.CronExpression cron = Cronos.CronExpression.Parse(Spec.Schedule);
            JobItem? lastedJob = LatestItem;
            DateTime notBefore = Spec.NotBefore ?? Metadata.CreationTimestamp;
            DateTime lastestScheduledAt = lastedJob is null ? notBefore : lastedJob.Schedule;
            DateTime? nextSchedule = cron.GetNextOccurrence(lastestScheduledAt, true);
            if (nextSchedule is null || nextSchedule.Value > Spec.ExpirationTime)
            {
                return null;
            }
            return nextSchedule;
        }

        public string GetChildJobIdByIndex(uint index) => Metadata.Uid + "-" + index;
    }
}
