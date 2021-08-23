// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Fabron.Models;

namespace Fabron.ElasticSearch
{
    public record JobDocument(
        string Id,
        JobMetadata Metadata,
        JobSpec Spec,
        JobStatus Status);
}
