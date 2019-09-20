
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Quartz;
using Scheduler.Api.Models;

namespace Scheduler.Api.Controllers
{
    [Route("api/triggers")]
    [ApiController]
    public class TriggerController : ControllerBase
    {
        private readonly IScheduler _scheduler;

        public TriggerController(IScheduler scheduler)
        {
            _scheduler = scheduler;
        }

        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] TriggerKeySearch triggerKeySearch)
        {
            if (!EnsureValidKey(triggerKeySearch.TriggerName, triggerKeySearch.ProjectName)) return BadRequest();

            var trigger = await _scheduler
                .GetTrigger(new TriggerKey(triggerKeySearch.TriggerName, triggerKeySearch.ProjectName))
                .ConfigureAwait(false);

            if (trigger == null)  return NotFound();

            var triggerType = trigger.GetTriggerType();
            TriggerViewModel model = null;

            switch (triggerType)
            {
                case TriggerType.Cron:
                    model = CronTriggerViewModel.FromTrigger((ICronTrigger) trigger);
                    break;
                case TriggerType.Simple:
                    model = SimpleTriggerViewModel.FromTrigger((ISimpleTrigger) trigger);
                    break;
                case TriggerType.Daily:
                    model = DailyTriggerViewModel.FromTrigger((IDailyTimeIntervalTrigger) trigger);
                    break;
                case TriggerType.Calendar:
                    model = CalendarTriggerViewModel.FromTrigger((ICalendarIntervalTrigger) trigger);
                    break;
                default:
                    throw new ApplicationException($"Trigger type {triggerType} does not exist.");
            }

            model.TriggerName = trigger.Key.Name;
            model.JobName = trigger.JobKey.Name;
            model.ProjectName = trigger.Key.Group;
            model.StartTimeUtc = trigger.StartTimeUtc.UtcDateTime.ToDefaultFormat();
            model.EndTimeUtc = trigger.EndTimeUtc?.UtcDateTime.ToDefaultFormat();
            model.CalendarName = trigger.CalendarName;
            model.Description = trigger.Description;
            model.Priority = trigger.Priority;
            model.MisfireInstruction = trigger.MisfireInstruction;
            model.JobDataMap = trigger.JobDataMap.WrappedMap;

            return Ok(model);
        }

        [HttpPost]
        [Route("cron")]
        public async Task<IActionResult> Create([FromBody] CronTriggerViewModel model)
        {
            return await CreateTrigger(model);
        }

        [HttpPost]
        [Route("simple")]
        public async Task<IActionResult> Create([FromBody] SimpleTriggerViewModel model)
        {
            return await CreateTrigger(model);
        }

        [HttpPost]
        [Route("daily")]
        public async Task<IActionResult> Create([FromBody] DailyTriggerViewModel model)
        {
            return await CreateTrigger(model);
        }
       
        [HttpPost]
        [Route("calendar")]
        public async Task<IActionResult> Create([FromBody] CalendarTriggerViewModel model)
        {
            return await CreateTrigger(model);
        }

        private async Task<IActionResult> CreateTrigger(TriggerViewModel model)
        {
            var triggerModel = model;

            var result = new ValidationResult();

            model.Validate(result.Errors);

            if (result.Success)
            {
                var jobDetail = await _scheduler.GetJobDetail(new JobKey(model.JobName, model.ProjectName))
                    .ConfigureAwait(false);
                if (jobDetail == null)
                {
                    var jobBuilder = JobBuilder.Create().StoreDurably()
                        .OfType(Type.GetType($"Scheduler.Job.{model.JobClassName},Scheduler.Job", true))
                        .WithIdentity(model.JobName, model.ProjectName)
                        .WithDescription(model.Description)
                        .SetJobData(new JobDataMap(model.JobDataMap))
                        .RequestRecovery(true)
                        .Build();

                    await _scheduler.AddJob(jobBuilder, replace: false);
                }

                var existingTrigger = await _scheduler
                    .GetTrigger(new TriggerKey(model.TriggerName, model.ProjectName))
                    .ConfigureAwait(false);

                if (existingTrigger != null)
                {
                    return Conflict($"Trigger with name {model.TriggerName} and project {model.ProjectName} already exists.");
                }

                var builder = triggerModel.CreateBuilder();

                var trigger = builder.Build();

                await _scheduler.ScheduleJob(trigger);
            }

            return Ok();
        }

        private bool EnsureValidKey(string name, string group) => !(string.IsNullOrEmpty(name) || string.IsNullOrEmpty(group));
    }
}
