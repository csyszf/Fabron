// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

using Nest;

namespace Fabron.ElasticSearch
{
    public class ElasticSearchJobQuerier : IJobQuerier
    {
        private readonly ElasticSearchOptions _options;
        private readonly IElasticClient _esClient;

        public ElasticSearchJobQuerier(IOptions<ElasticSearchOptions> options, IElasticClient esClient)
        {
            _options = options.Value;
            _esClient = esClient;
        }


        public async Task<IEnumerable<Models.Job>> GetJobByLabel(string labelName, string labelValue)
        {
            ISearchResponse<JobDocument> res = await SearchByLabelAsync<JobDocument>(_options.JobIndexName, labelName, labelValue);
            return res.Documents.Select(job => new Models.Job
            {
                Metadata = job.Metadata,
                Spec = job.Spec,
                Status = job.Status
            }).ToList();
        }

        public async Task<IEnumerable<Models.Job>> GetJobByLabels(IEnumerable<(string, string)> labels)
        {
            ISearchResponse<JobDocument> res = await SearchByLabelsAsync<JobDocument>(_options.JobIndexName, labels);
            return res.Documents.Select(job => new Models.Job
            {
                Metadata = job.Metadata,
                Spec = job.Spec,
                Status = job.Status
            }).ToList();
        }

        public async Task<IEnumerable<Models.CronJob>> GetCronJobByLabel(string labelName, string labelValue)
        {
            ISearchResponse<CronJobDocument> res = await SearchByLabelAsync<CronJobDocument>(_options.CronJobIndexName, labelName, labelValue);
            return res.Documents.Select(job => new Models.CronJob
            {
                Metadata = job.Metadata,
                Spec = job.Spec,
                Status = job.Status
            }).ToList();
        }

        public async Task<IEnumerable<Models.CronJob>> GetCronJobByLabels(IEnumerable<(string, string)> labels)
        {
            ISearchResponse<CronJobDocument> res = await SearchByLabelsAsync<CronJobDocument>(_options.JobIndexName, labels);
            return res.Documents.Select(job => new Models.CronJob
            {
                Metadata = job.Metadata,
                Spec = job.Spec,
                Status = job.Status
            }).ToList();
        }

        private async Task<ISearchResponse<TDocument>> SearchByLabelAsync<TDocument>(string indexName, string labelName, string labelValue)
            where TDocument : class
        {
            ISearchResponse<TDocument> res = await _esClient.SearchAsync<TDocument>(s => s
                .Index(indexName)
                .Query(q => q
                    .Match(m => m.Field($"metadata.labels.{labelName}").Query(labelValue))
                )
            );
            return res;
        }

        private async Task<ISearchResponse<TDocument>> SearchByLabelsAsync<TDocument>(string indexName, IEnumerable<(string, string)> labels)
            where TDocument : class
        {
            ISearchResponse<TDocument> res = await _esClient.SearchAsync<TDocument>(s => s
                .Index(_options.JobIndexName)
                .Query(q => q
                    .Match(m =>
                    {
                        MatchQueryDescriptor<TDocument> q = m;
                        foreach ((string labelName, string labelValue) in labels)
                        {
                            q = q.Field($"metadata.labels.{labelName}").Query(labelValue);
                        }
                        return q;
                    })
                )
            );
            return res;
        }
    }
}
