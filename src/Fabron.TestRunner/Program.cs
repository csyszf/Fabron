// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Fabron;
using Fabron.Events;
using Fabron.TestRunner.Scenarios;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;


// await new ScheduleCronJob().PlayAsync();
// await new LabelBasedQuery().PlayAsync();
// await new ErrorOnListeners().PlayAsync();
await new PgsqlScheduleCronJob().PlayAsync();
