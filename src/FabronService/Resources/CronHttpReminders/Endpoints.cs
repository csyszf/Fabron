// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabron;
using Fabron.Contracts;
using FabronService.Commands;
using FabronService.Resources.CronHttpReminders.Models;
using FabronService.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace FabronService.Resources.CronHttpReminders
{
    [ApiController]
    [Route("CronHttpReminders")]
    public class Endpoints : ControllerBase
    {
        private readonly ILogger<Endpoints> _logger;
        private readonly IJobManager _jobManager;
        private readonly IResourceLocator _resourceLocator;

        public Endpoints(ILogger<Endpoints> logger,
            IJobManager jobManager,
            IResourceLocator resourceLocator)
        {
            _logger = logger;
            _jobManager = jobManager;
            _resourceLocator = resourceLocator;
        }

        [HttpPost(Name = "CronHttpReminders_Register")]
        public async Task<IActionResult> Create(RegisterCronHttpReminderRequest req)
        {
            string? tenantId = HttpContext.User.Identity!.Name!;
            string? resourceUri = $"tenants/{tenantId}/CronHttpReminders/{req.Name}";
            string? resourceId = await _resourceLocator.GetOrCreateResourceId(resourceUri);
            Fabron.Contracts.CronJob<RequestWebAPI>? job = await _jobManager.ScheduleCronJob<RequestWebAPI>(resourceId, req.Schedule, req.Command, new Dictionary<string, string> { { "tenant", tenantId } });
            CronHttpReminder? reminder = job.ToResource(req.Name);
            return CreatedAtRoute("CronHttpReminders_Get", new { name = reminder.Name }, reminder);
        }

        [HttpGet("{name}", Name = "CronHttpReminders_Get")]
        public async Task<IActionResult> Get(string name)
        {
            string? tenantId = HttpContext.User.Identity!.Name;
            string? resourceUri = $"tenants/{tenantId}/CronHttpReminders/{name}";
            string? resourceId = await _resourceLocator.GetResourceId(resourceUri);
            if (resourceId == null)
            {
                return NotFound();
            }

            CronJob<RequestWebAPI>? job = await _jobManager.GetCronJobById<RequestWebAPI>(resourceId);
            return job is null ? NotFound() : Ok(job.ToResource(name));
        }

        [HttpGet("{name}/items", Name = "CronHttpReminders_GetItems")]
        public async Task<IActionResult> GetItems(string name)
        {
            string? tenantId = HttpContext.User.Identity!.Name;
            string? resourceUri = $"tenants/{tenantId}/CronHttpReminders/{name}";
            string? resourceId = await _resourceLocator.GetResourceId(resourceUri);
            if (resourceId == null)
            {
                return NotFound();
            }

            IEnumerable<Job<RequestWebAPI, int>>? jobs = await _jobManager.GetJobByCron<RequestWebAPI, int>(resourceId);
            return Ok(jobs.Select(job => job.ToResource()));
        }
    }
}
