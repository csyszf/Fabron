using System;

namespace Fabron
{
    public static class KeyUtils
    {
        public static string BuildJobKey(string name, string @namespace)
        {
            return $"/registry/jobs/{@namespace}/{name}";
        }

        public static (string name, string @namespace) ParseJobKey(string key)
        {
            string[] seqs = key.Split('/');
            return (seqs[4], seqs[3]);
        }

        public static string BuildCronJobKey(string name, string @namespace)
        {
            return $"/registry/cronjobs/{@namespace}/{name}";
        }

        public static (string name, string @namespace) ParseCronJobKey(string key)
        {
            string[] seqs = key.Split('/');
            return (seqs[4], seqs[3]);
        }

        public static string BuildCronJobItemKey(string name, string @namespace, DateTimeOffset schedule)
        {
            string childJobName = $"fabron-cronjob-{name}-{schedule.ToUnixTimeSeconds()}";
            return $"/registry/jobs/{@namespace}/{childJobName}";
        }

    }
}
