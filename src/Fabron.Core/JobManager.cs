// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Fabron.Mando;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Orleans;

namespace Fabron
{
    public partial class JobManager : IJobManager
    {
        private readonly ILogger _logger;
        private readonly CommandRegistry _registry;
        private readonly IClusterClient _client;
        private readonly IJobQuerier _querier;

        public JobManager(ILogger<JobManager> logger,
            IOptions<CommandRegistry> options,
            IClusterClient client,
            IJobQuerier querier)
        {
            _logger = logger;
            _registry = options.Value;
            _client = client;
            _querier = querier;
        }
    }
}
