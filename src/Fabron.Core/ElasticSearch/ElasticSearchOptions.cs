﻿
namespace Fabron.ElasticSearch
{
    public class ElasticSearchOptions
    {
        public string Server { get; set; } = default!;
        public string JobIndexName { get; set; } = default!;
        public string CronJobIndexName { get; set; } = default!;
    }
}
