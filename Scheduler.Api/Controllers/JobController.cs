
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Quartz;
using Scheduler.Api.Models;

namespace Scheduler.Api.Controllers
{
    [Route("api/jobs")]
    [ApiController]
    public class JobController : ControllerBase
    {
        private readonly IScheduler _scheduler;

        public JobController(IScheduler scheduler)
        {
            _scheduler = scheduler;
        }

        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] JobKeySearch jobKeySearch)
        {
            if (!EnsureValidKey(jobKeySearch.JobName, jobKeySearch.ProjectName)) return BadRequest();

            var jobDetail = await _scheduler.GetJobDetail(new JobKey(jobKeySearch.JobName, jobKeySearch.ProjectName))
                .ConfigureAwait(false);

            if (jobDetail == null) return NotFound();

            var model = new JobViewModel
            {
                JobName = jobDetail.Key.Name,
                ProjectName = jobDetail.Key.Group,
                Description = jobDetail.Description,
                Type = jobDetail.JobType.ToString(),
                JobDataMap = jobDetail.JobDataMap.WrappedMap
            };

            return Ok(model);
        }

        [HttpPost]
        public async void Create([FromBody] JobViewModel model)
        {
            var result = new ValidationResult();
            model.Validate(result.Errors);

            if (!result.Success) return;

            IJobDetail BuildJob(JobBuilder builder)
            {
                return builder
                    .OfType(Type.GetType(model.Type, true))
                    .WithIdentity(model.JobName, model.ProjectName)
                    .WithDescription(model.Description)
                    .SetJobData(new JobDataMap(model.JobDataMap))
                    .RequestRecovery(true)
                    .Build();
            }

            await _scheduler.AddJob(BuildJob(JobBuilder.Create().StoreDurably()), replace: false);

            if (model.CreateTrigger)
            {
                await _scheduler.TriggerJob(JobKey.Create(model.JobName, model.ProjectName));
            }
        }

        private bool EnsureValidKey(string name, string group) => !(string.IsNullOrEmpty(name) || string.IsNullOrEmpty(group));
    }
}
