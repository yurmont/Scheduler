
using System;
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
            TriggerViewModel triggerViewModel = null;

            switch (triggerType)
            {
                case TriggerType.Cron:
                    triggerViewModel = CronTriggerViewModel.FromTrigger((ICronTrigger) trigger);
                    break;
                case TriggerType.Simple:
                    triggerViewModel = SimpleTriggerViewModel.FromTrigger((ISimpleTrigger) trigger);
                    break;
                case TriggerType.Daily:
                    triggerViewModel = DailyTriggerViewModel.FromTrigger((IDailyTimeIntervalTrigger) trigger);
                    break;
                case TriggerType.Calendar:
                    triggerViewModel = CalendarTriggerViewModel.FromTrigger((ICalendarIntervalTrigger) trigger);
                    break;
                default:
                    throw new ApplicationException($"Trigger type {triggerType} does not exist.");
            }

            triggerViewModel.TriggerName = trigger.Key.Name;
            triggerViewModel.JobName = trigger.JobKey.Name;
            triggerViewModel.ProjectName = trigger.Key.Group;
            triggerViewModel.StartTimeUtc = trigger.StartTimeUtc.UtcDateTime.ToDefaultFormat();
            triggerViewModel.EndTimeUtc = trigger.EndTimeUtc?.UtcDateTime.ToDefaultFormat();
            triggerViewModel.CalendarName = trigger.CalendarName;
            triggerViewModel.Description = trigger.Description;
            triggerViewModel.Priority = trigger.Priority;
            triggerViewModel.MisfireInstruction = trigger.MisfireInstruction;

            return Ok(triggerViewModel);
        }

        [HttpPost]
        [Route("cron")]
        public async void Create([FromBody] CronTriggerViewModel model)
        {
            await CreateTrigger(model);
        }

        [HttpPost]
        [Route("simple")]
        public async void Create([FromBody] SimpleTriggerViewModel model)
        {
            await CreateTrigger(model);
        }

        [HttpPost]
        [Route("daily")]
        public async void Create([FromBody] DailyTriggerViewModel model)
        {
            await CreateTrigger(model);
        }
       
        [HttpPost]
        [Route("calendar")]
        public async void Create([FromBody] CalendarTriggerViewModel model)
        {
            await CreateTrigger(model);
        }

        private async Task CreateTrigger(TriggerViewModel model)
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

                var builder = triggerModel.CreateBuilder();

                var trigger = builder.Build();

                await _scheduler.ScheduleJob(trigger);
            }
        }

        private bool EnsureValidKey(string name, string group) => !(string.IsNullOrEmpty(name) || string.IsNullOrEmpty(group));
    }
}
