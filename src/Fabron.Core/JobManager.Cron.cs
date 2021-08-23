﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

using Fabron.Contracts;
using Fabron.Grains;
using Fabron.Mando;
using Fabron.Models;
using Microsoft.Extensions.Logging;

namespace Fabron
{
    public partial class JobManager
    {
        public async Task<CronJob<TCommand>> ScheduleCronJob<TCommand>(string jobId, string cronExp, TCommand command, DateTime? notBefore = null, DateTime? expirationTime = null, Dictionary<string, string>? labels = null)
            where TCommand : ICommand
        {
            string commandName = _registry.CommandNameRegistrations[typeof(TCommand)];
            string commandData = JsonSerializer.Serialize(command);

            CronJob state = await ScheduleCronJob(jobId, cronExp, commandName, commandData, notBefore, expirationTime, labels);
            return state.Map<TCommand>();
        }

        private async Task<CronJob> ScheduleCronJob(string jobId, string cronExp, string commandName, string commandData, DateTime? notBefore, DateTime? expirationTime, Dictionary<string, string>? labels)
        {
            _logger.LogDebug($"Creating CronJob[{jobId}]");
            ICronJobGrain grain = _client.GetGrain<ICronJobGrain>(jobId);
            await grain.Schedule(cronExp, commandName, commandData, notBefore, expirationTime, labels);
            _logger.LogDebug($"CronJob[{jobId}] Created");

            return await grain.GetState();
        }


        public async Task<CronJob<TCommand>?> GetCronJobById<TCommand>(string jobId)
            where TCommand : ICommand
        {
            ICronJobGrain grain = _client.GetGrain<ICronJobGrain>(jobId);
            CronJob? jobState = await grain.GetState();
            if (jobState is null)
            {
                return null;
            }

            return jobState.Map<TCommand>();
        }

        public async Task<IEnumerable<CronJob<TJobCommand>>> GetCronJobByLabel<TJobCommand>(string labelName, string labelValue)
            where TJobCommand : ICommand
        {
            IEnumerable<CronJob> jobs = await _querier.GetCronJobByLabel(labelName, labelValue);
            return jobs.Select(job => job.Map<TJobCommand>());
        }

        public async Task<IEnumerable<CronJob<TJobCommand>>> GetCronJobByLabels<TJobCommand>(params (string, string)[] labels)
            where TJobCommand : ICommand
        {
            IEnumerable<CronJob> jobs = await _querier.GetCronJobByLabels(labels);
            return jobs.Select(job => job.Map<TJobCommand>());
        }

    }
}
