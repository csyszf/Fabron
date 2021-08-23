// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Fabron.Models;
using Microsoft.Extensions.Logging;

using Orleans;
using Orleans.Concurrency;

namespace Fabron.Grains
{
    public interface IJobReporterGrain : IGrainWithStringKey
    {
        [OneWay]
        Task OnJobStateChanged(Job jobState);

        Task OnJobFinalized(Job jobState);

        [OneWay]
        Task OnCronJobStateChanged(CronJob jobState);

        Task OnCronJobFinalized(CronJob jobState);
    }

    public class JobReporterGrain : Grain, IJobReporterGrain
    {
        private readonly ILogger<JobReporterGrain> _logger;
        private readonly IJobReporter _reporter;

        public JobReporterGrain(ILogger<JobReporterGrain> logger, IJobReporter reporter)
        {
            _logger = logger;
            _reporter = reporter;
        }

        public async Task OnJobStateChanged(Job jobState)
        {
            await _reporter.Report(this.GetPrimaryKeyString(), jobState.Metadata.ResourceVersion, jobState);
            return;
        }

        public async Task OnJobFinalized(Job jobState)
        {
            await _reporter.Report(this.GetPrimaryKeyString(), jobState.Metadata.ResourceVersion, jobState);
            DeactivateOnIdle();
            return;
        }

        public Task OnCronJobStateChanged(CronJob jobState) => throw new System.NotImplementedException();
        public Task OnCronJobFinalized(CronJob jobState) => throw new System.NotImplementedException();
    }
}
