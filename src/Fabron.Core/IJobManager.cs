// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Fabron.Contracts;
using Fabron.Mando;

namespace Fabron
{
    public interface IJobManager
    {
        // transient
        Task<Job<TCommand, TResult>> ScheduleJob<TCommand, TResult>(string jobId, TCommand command, DateTime? scheduledAt = null, Dictionary<string, string>? labels = null) where TCommand : ICommand<TResult>;

        Task<Job<TJobCommand, TResult>?> GetJobById<TJobCommand, TResult>(string jobId) where TJobCommand : ICommand<TResult>;

        Task<IEnumerable<Job<TJobCommand, TResult>>> GetJobByLabel<TJobCommand, TResult>(string labelName, string labelValue) where TJobCommand : ICommand<TResult>;

        Task<IEnumerable<Job<TJobCommand, TResult>>> GetJobByLabels<TJobCommand, TResult>(params (string, string)[] labels) where TJobCommand : ICommand<TResult>;

        Task<IEnumerable<Job<TJobCommand, TResult>>> GetJobByCron<TJobCommand, TResult>(string cronJobId) where TJobCommand : ICommand<TResult>;

        // cron
        Task<CronJob<TCommand>> ScheduleCronJob<TCommand>(string cronJobId, string cronExp, TCommand command, Dictionary<string, string>? labels) where TCommand : ICommand;

        Task<CronJob<TCommand>?> GetCronJobById<TCommand>(string cronJobId) where TCommand : ICommand;

        Task<IEnumerable<CronJob<TJobCommand>>> GetCronJobByLabel<TJobCommand>(string labelName, string labelValue) where TJobCommand : ICommand;

        Task<IEnumerable<CronJob<TJobCommand>>> GetCronJobByLabels<TJobCommand>(params (string, string)[] labels) where TJobCommand : ICommand;

    }
}
