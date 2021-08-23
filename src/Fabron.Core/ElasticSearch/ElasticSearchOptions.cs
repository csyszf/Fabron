// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Fabron.ElasticSearch
{
    public class ElasticSearchOptions
    {
        public string Server { get; set; } = default!;
        public string JobIndexName { get; set; } = default!;
        public string CronJobIndexName { get; set; } = default!;
    }
}
