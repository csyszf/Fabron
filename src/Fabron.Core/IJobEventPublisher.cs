// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Fabron.Grains;
using Fabron.Models;
using Orleans;

namespace Fabron
{
    public interface IJobEventBus
    {
        Task OnJobStateChanged(Job jobState);

        Task OnJobFinalized(Job jobState);

        Task OnCronJobStateChanged(CronJob jobState);

        Task OnCronJobFinalized(CronJob jobState);
    }

    public class GrainBasedJobEventBus : IJobEventBus
    {
        private readonly IGrainFactory _factory;
        public GrainBasedJobEventBus(IGrainFactory factory) => _factory = factory;

        public async Task OnCronJobFinalized(CronJob jobState)
        {
            IJobReporterGrain grain = _factory.GetGrain<IJobReporterGrain>("cronjob/" + jobState.Metadata.Uid);
            await grain.OnCronJobFinalized(jobState);
        }

        public async Task OnCronJobStateChanged(CronJob jobState)
        {
            IJobReporterGrain grain = _factory.GetGrain<IJobReporterGrain>("cronjob/" + jobState.Metadata.Uid);
            await grain.OnCronJobStateChanged(jobState);
        }

        public async Task OnJobFinalized(Job jobState)
        {
            IJobReporterGrain grain = _factory.GetGrain<IJobReporterGrain>("job/" + jobState.Metadata.Uid);
            await grain.OnJobFinalized(jobState);
        }

        public async Task OnJobStateChanged(Job jobState)
        {
            IJobReporterGrain grain = _factory.GetGrain<IJobReporterGrain>("job/" + jobState.Metadata.Uid);
            await grain.OnJobStateChanged(jobState);
        }
    }
}
