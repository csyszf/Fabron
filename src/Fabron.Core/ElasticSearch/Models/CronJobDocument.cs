// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Fabron.Models;

namespace Fabron.ElasticSearch
{
    public record CronJobDocument(
        string Id,
        CronJobMetadata Metadata,
        CronJobSpec Spec,
        CronJobStatus Status);
}
