// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading.Tasks;
using Fabron.Models;

namespace Fabron
{
    public interface IJobQuerier
    {
        Task<IEnumerable<Job>> GetJobByLabel(string labelName, string labelValue);
        Task<IEnumerable<Job>> GetJobByLabels(IEnumerable<(string, string)> labels);
        Task<IEnumerable<CronJob>> GetCronJobByLabel(string labelName, string labelValue);
        Task<IEnumerable<CronJob>> GetCronJobByLabels(IEnumerable<(string, string)> labels);
    }

    public class NoopJobQuerier : IJobQuerier
    {
        public Task<IEnumerable<CronJob>> GetCronJobByLabel(string labelName, string labelValue) => throw new System.NotImplementedException();
        public Task<IEnumerable<CronJob>> GetCronJobByLabels(IEnumerable<(string, string)> labels) => throw new System.NotImplementedException();
        public Task<IEnumerable<Job>> GetJobByLabel(string labelName, string labelValue) => throw new System.NotImplementedException();
        public Task<IEnumerable<Job>> GetJobByLabels(IEnumerable<(string, string)> labels) => throw new System.NotImplementedException();
    }

}
