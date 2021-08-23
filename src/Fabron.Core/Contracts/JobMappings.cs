// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text.Json;
using Fabron.Contracts;
using Fabron.Mando;

namespace Fabron
{
    public static class JobMappings
    {
        public static Job<TCommand, TResult> Map<TCommand, TResult>(this Models.Job job)
            where TCommand : ICommand<TResult>
        {
            TCommand? cmdData = JsonSerializer.Deserialize<TCommand>(job.Spec.CommandData);
            if (cmdData is null)
            {
                throw new InvalidOperationException();
            }

            TResult? cmdResult = job.Status.Result is null
                    ? default
                    : JsonSerializer.Deserialize<TResult>(job.Status.Result);

            Job<TCommand, TResult> typedJob = new(
                job.Metadata,
                new JobSpec<TCommand>(
                    job.Spec.Schedule,
                    job.Spec.CommandName,
                    cmdData),
                new JobStatus<TResult>(
                    job.Status.ExecutionStatus,
                    job.Status.StartedAt,
                    job.Status.FinishedAt,
                    cmdResult,
                    job.Status.Reason,
                    job.Status.Finalized)
            );
            return typedJob;
        }

        public static CronJob<TCommand> Map<TCommand>(this Models.CronJob cronJob)
            where TCommand : ICommand
        {
            TCommand? cmdData = JsonSerializer.Deserialize<TCommand>(cronJob.Spec.CommandData);
            if (cmdData is null)
            {
                throw new InvalidOperationException();
            }

            CronJob<TCommand> typedCronJob = new(
                cronJob.Metadata,
                new TypedCronJobSpec<TCommand>(
                    cronJob.Spec.Schedule,
                    cronJob.Spec.CommandName,
                    cmdData,
                    cronJob.Spec.StartTimestamp,
                    cronJob.Spec.EndTimeStamp),
                cronJob.Status
            );
            return typedCronJob;
        }
    }
}
