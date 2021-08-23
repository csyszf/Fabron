// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Fabron.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Fabron.ElasticSearch
{
    public class ElasticSearchJobReporter : IJobReporter
    {
        private readonly ILogger<ElasticSearchJobReporter> _logger;
        private readonly ElasticSearchOptions _options;
        private readonly Nest.IElasticClient _esClient;

        public ElasticSearchJobReporter(ILogger<ElasticSearchJobReporter> logger, IOptions<ElasticSearchOptions> options, Nest.IElasticClient esClient)
        {
            _logger = logger;
            _options = options.Value;
            _esClient = esClient;
        }

        public async Task Report(string jobId, long version, Job job)
        {
            JobDocument doc = new(jobId,
                job.Metadata,
                job.Spec,
                job.Status);
            Nest.IndexResponse res = await _esClient.IndexAsync(doc, idx => idx.Index(_options.JobIndexName));
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug(res.DebugInformation);
            }
            if (res.Result == Nest.Result.Error)
            {
                _logger.LogError($"Failed to index doc: {res.DebugInformation}");
            }
        }
    }
}
