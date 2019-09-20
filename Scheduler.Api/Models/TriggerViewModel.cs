using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Newtonsoft.Json;
using Quartz;
using Quartz.Impl.Triggers;
using ModelValidator = Scheduler.Api.Helpers.ModelValidator;

namespace Scheduler.Api.Models
{
    public abstract class TriggerViewModel
    {
        [Required]
        public string JobName { get; set; }

        [Required]
        public string ProjectName { get; set; }

        [Required]
        public string TriggerName { get; set; }

        [Required]
        public int? Priority { get; set; }

        [Required]
        public int? MisfireInstruction { get; set; }

        public string Description { get; set; }

        public string StartTimeUtc { get; set; }

        public string EndTimeUtc { get; set; }

        public DateTime? GetStartTimeUtc() => ParseDateTime(StartTimeUtc);

        public DateTime? GetEndTimeUtc() => ParseDateTime(EndTimeUtc);

        DateTime? ParseDateTime(string value)
        {
            if (string.IsNullOrEmpty(value))
                return null;

            return null;
        }

        public string CalendarName { get; set; }

        public int PriorityOrDefault => Priority ?? 5;

        public IDictionary<string, object> JobDataMap { get; set; }

        public string JobClassName { get; set; }

        public TriggerBuilder CreateBuilder()
        {
            var builder = TriggerBuilder.Create()
                .WithIdentity(new TriggerKey(TriggerName, ProjectName))
                .ForJob(jobKey: $"{ProjectName}.{JobName}")
                .UsingJobData(new JobDataMap(JobDataMap))
                .WithDescription(Description)
                .WithPriority(PriorityOrDefault);

            builder.StartAt(GetStartTimeUtc() ?? DateTime.UtcNow);
            builder.EndAt(GetEndTimeUtc());

            if (!string.IsNullOrEmpty(CalendarName))
                builder.ModifiedByCalendar(CalendarName);

            Apply(builder, this);

            return builder;
        }

        public abstract void Validate(ICollection<ValidationError> errors);

        public abstract void Apply(TriggerBuilder builder, TriggerViewModel model);
    }

    public class CronTriggerViewModel : TriggerViewModel, IHasValidation
    {
        [Required]
        public string Expression { get; set; }
        public string TimeZone { get; set; }

        public override void Validate(ICollection<ValidationError> errors)
        {
            ModelValidator.ValidateObject(this, errors, nameof(CronTriggerViewModel), nameof(CronTriggerViewModel));
        }

        public override void Apply(TriggerBuilder builder, TriggerViewModel model)
        {
            builder.WithCronSchedule(Expression, x =>
            {
                 if (!string.IsNullOrEmpty(TimeZone))
                     x.InTimeZone(TimeZoneInfo.FindSystemTimeZoneById(TimeZone));

                 switch (model.MisfireInstruction)
                 {
                     case Quartz.MisfireInstruction.InstructionNotSet:
                         break;
                     case Quartz.MisfireInstruction.IgnoreMisfirePolicy:
                         x.WithMisfireHandlingInstructionIgnoreMisfires();
                         break;
                     case Quartz.MisfireInstruction.CronTrigger.DoNothing:
                         x.WithMisfireHandlingInstructionDoNothing();
                         break;
                     case Quartz.MisfireInstruction.CronTrigger.FireOnceNow:
                         x.WithMisfireHandlingInstructionFireAndProceed();
                         break;
                     default:
                         throw new ArgumentException("Invalid value: " + model.MisfireInstruction, nameof(model.MisfireInstruction));
                 }
            });
        }

        public static CronTriggerViewModel FromTrigger(ICronTrigger trigger)
        {
            return new CronTriggerViewModel()
            {
                Expression = trigger.CronExpressionString,
                TimeZone = trigger.TimeZone.Id,
            };
        }
    }

    public class SimpleTriggerViewModel : TriggerViewModel, IHasValidation
    {
        [Required]
        public int? RepeatInterval { get; set; }

        [Required]
        public IntervalUnit RepeatUnit { get; set; }

        public int? RepeatCount { get; set; }

        public bool RepeatForever { get; set; }

        public override void Validate(ICollection<ValidationError> errors)
        {
            ModelValidator.ValidateObject(this, errors, nameof(SimpleTriggerViewModel), nameof(SimpleTriggerViewModel));

            if (RepeatForever == false && RepeatCount == null)
                errors.Add(ValidationError.EmptyField("trigger[simple.repeatCount]"));
        }

        TimeSpan GetRepeatIntervalTimeSpan()
        {
            switch (RepeatUnit)
            {
                case IntervalUnit.Millisecond:
                    return TimeSpan.FromMilliseconds(RepeatInterval.Value);
                case IntervalUnit.Second:
                    return TimeSpan.FromSeconds(RepeatInterval.Value);
                case IntervalUnit.Minute:
                    return TimeSpan.FromMinutes(RepeatInterval.Value);
                case IntervalUnit.Hour:
                    return TimeSpan.FromHours(RepeatInterval.Value);
                case IntervalUnit.Day:
                    return TimeSpan.FromDays(RepeatInterval.Value);
                default:
                    throw new ArgumentException("Invalid value: " + RepeatUnit, nameof(RepeatUnit));
            }
        }

        public override void Apply(TriggerBuilder builder, TriggerViewModel model)
        {
            builder.WithSimpleSchedule(x =>
            {
                x.WithInterval(GetRepeatIntervalTimeSpan());

                if (RepeatForever)
                    x.RepeatForever();
                else
                    x.WithRepeatCount(RepeatCount.Value);

                switch (model.MisfireInstruction)
                {
                    case Quartz.MisfireInstruction.InstructionNotSet:
                        break;
                    case Quartz.MisfireInstruction.IgnoreMisfirePolicy:
                        x.WithMisfireHandlingInstructionIgnoreMisfires();
                        break;
                    case Quartz.MisfireInstruction.SimpleTrigger.FireNow:
                        x.WithMisfireHandlingInstructionFireNow();
                        break;
                    case Quartz.MisfireInstruction.SimpleTrigger.RescheduleNowWithExistingRepeatCount:
                        x.WithMisfireHandlingInstructionNowWithExistingCount();
                        break;
                    case Quartz.MisfireInstruction.SimpleTrigger.RescheduleNowWithRemainingRepeatCount:
                        x.WithMisfireHandlingInstructionNowWithRemainingCount();
                        break;
                    case Quartz.MisfireInstruction.SimpleTrigger.RescheduleNextWithRemainingCount:
                        x.WithMisfireHandlingInstructionNextWithExistingCount();
                        break;
                    default:
                        throw new ArgumentException("Invalid value: " + model.MisfireInstruction, nameof(model.MisfireInstruction));
                }
            });
        }

        public static SimpleTriggerViewModel FromTrigger(ISimpleTrigger trigger)
        {
            var model = new SimpleTriggerViewModel()
            {
                RepeatCount = trigger.RepeatCount,
                RepeatForever = trigger.RepeatCount == SimpleTriggerImpl.RepeatIndefinitely,
                RepeatInterval = (int)trigger.RepeatInterval.TotalMilliseconds,
                RepeatUnit = IntervalUnit.Millisecond,
            };

            if (model.RepeatCount == -1)
                model.RepeatCount = null;

            if (trigger.RepeatInterval.Milliseconds == 0 && model.RepeatInterval > 0)
            {
                model.RepeatInterval = (int)trigger.RepeatInterval.TotalSeconds;
                model.RepeatUnit = IntervalUnit.Second;
                if (trigger.RepeatInterval.Seconds != 0) return model;

                model.RepeatInterval = (int)trigger.RepeatInterval.TotalMinutes;
                model.RepeatUnit = IntervalUnit.Minute;

                if (trigger.RepeatInterval.Minutes != 0) return model;

                model.RepeatInterval = (int)trigger.RepeatInterval.TotalHours;
                model.RepeatUnit = IntervalUnit.Hour;

                if (trigger.RepeatInterval.Hours != 0) return model;

                model.RepeatInterval = (int)trigger.RepeatInterval.TotalDays;
                model.RepeatUnit = IntervalUnit.Day;
            }

            return model;
        }
    }

    public class DailyTriggerViewModel : TriggerViewModel, IHasValidation
    {
        public DaysOfWeekViewModel DaysOfWeek { get; set; } = new DaysOfWeekViewModel();

        [Required]
        public int? RepeatInterval { get; set; }

        [Required]
        public IntervalUnit RepeatUnit { get; set; }

        public int? RepeatCount { get; set; }

        public bool RepeatForever { get; set; }

        [Required]
        public TimeSpan? StartTime { get; set; }

        [Required]
        public TimeSpan? EndTime { get; set; }

        public string TimeZone { get; set; }

        public override void Validate(ICollection<ValidationError> errors)
        {
            ModelValidator.ValidateObject(this, errors, nameof(DailyTriggerViewModel), nameof(DailyTriggerViewModel));

            if (RepeatForever == false && RepeatCount == null)
                errors.Add(ValidationError.EmptyField("trigger[daily.repeatCount]"));
        }

        public override void Apply(TriggerBuilder builder, TriggerViewModel model)
        {
            builder.WithDailyTimeIntervalSchedule(x =>
            {
                if (!string.IsNullOrEmpty(TimeZone))
                    x.InTimeZone(TimeZoneInfo.FindSystemTimeZoneById(TimeZone));

                if (!RepeatForever)
                    x.WithRepeatCount(RepeatCount.Value);

                x.WithInterval(RepeatInterval.Value, RepeatUnit);
                x.StartingDailyAt(StartTime.Value.ToTimeOfDay());
                x.EndingDailyAt(EndTime.Value.ToTimeOfDay());
                x.OnDaysOfTheWeek(DaysOfWeek.GetSelected().ToArray());

                switch (model.MisfireInstruction)
                {
                    case Quartz.MisfireInstruction.InstructionNotSet:
                        break;
                    case Quartz.MisfireInstruction.IgnoreMisfirePolicy:
                        x.WithMisfireHandlingInstructionIgnoreMisfires();
                        break;
                    case Quartz.MisfireInstruction.DailyTimeIntervalTrigger.DoNothing:
                        x.WithMisfireHandlingInstructionDoNothing();
                        break;
                    case Quartz.MisfireInstruction.DailyTimeIntervalTrigger.FireOnceNow:
                        x.WithMisfireHandlingInstructionFireAndProceed();
                        break;
                    default:
                        throw new ArgumentException("Invalid value: " + model.MisfireInstruction, nameof(model.MisfireInstruction));
                }
            });
        }

        public static DailyTriggerViewModel FromTrigger(IDailyTimeIntervalTrigger trigger)
        {
            var model = new DailyTriggerViewModel()
            {
                RepeatCount = trigger.RepeatCount,
                RepeatInterval = trigger.RepeatInterval,
                RepeatUnit = trigger.RepeatIntervalUnit,
                StartTime = trigger.StartTimeOfDay.ToTimeSpan(),
                EndTime = trigger.EndTimeOfDay.ToTimeSpan(),
                DaysOfWeek = DaysOfWeekViewModel.Create(trigger.DaysOfWeek),
                TimeZone = trigger.TimeZone.Id,
            };

            if (model.RepeatCount == -1)
            {
                model.RepeatCount = null;
                model.RepeatForever = true;
            }

            return model;
        }
    }

    public class CalendarTriggerViewModel : TriggerViewModel, IHasValidation
    {
        [Required]
        public int? RepeatInterval { get; set; } // nullable to validate missing value

        [Required]
        public IntervalUnit RepeatUnit { get; set; }

        public string TimeZone { get; set; }

        public bool PreserveHourAcrossDst { get; set; }

        public bool SkipDayIfHourDoesNotExist { get; set; }

        public override void Validate(ICollection<ValidationError> errors)
        {
            ModelValidator.ValidateObject(this, errors, nameof(CalendarTriggerViewModel), nameof(CalendarTriggerViewModel));
        }

        public override void Apply(TriggerBuilder builder, TriggerViewModel model)
        {
            builder.WithCalendarIntervalSchedule(x =>
            {
                if (!string.IsNullOrEmpty(TimeZone))
                    x.InTimeZone(TimeZoneInfo.FindSystemTimeZoneById(TimeZone));

                x.WithInterval(RepeatInterval.Value, RepeatUnit);
                x.PreserveHourOfDayAcrossDaylightSavings(PreserveHourAcrossDst);
                x.SkipDayIfHourDoesNotExist(SkipDayIfHourDoesNotExist);

                switch (model.MisfireInstruction)
                {
                    case Quartz.MisfireInstruction.InstructionNotSet:
                        break;
                    case Quartz.MisfireInstruction.IgnoreMisfirePolicy:
                        x.WithMisfireHandlingInstructionIgnoreMisfires();
                        break;
                    case Quartz.MisfireInstruction.CalendarIntervalTrigger.DoNothing:
                        x.WithMisfireHandlingInstructionDoNothing();
                        break;
                    case Quartz.MisfireInstruction.CalendarIntervalTrigger.FireOnceNow:
                        x.WithMisfireHandlingInstructionFireAndProceed();
                        break;
                    default:
                        throw new ArgumentException("Invalid value: " + model.MisfireInstruction, nameof(model.MisfireInstruction));
                }
            });
        }

        public static CalendarTriggerViewModel FromTrigger(ICalendarIntervalTrigger trigger)
        {
            return new CalendarTriggerViewModel()
            {
                RepeatInterval = trigger.RepeatInterval,
                RepeatUnit = trigger.RepeatIntervalUnit,
                PreserveHourAcrossDst = trigger.PreserveHourOfDayAcrossDaylightSavings,
                SkipDayIfHourDoesNotExist = trigger.SkipDayIfHourDoesNotExist,
                TimeZone = trigger.TimeZone.Id,
            };
        }
    }

    public class DaysOfWeekViewModel
    {
        public bool Monday { get; set; }
        public bool Tuesday { get; set; }
        public bool Wednesday { get; set; }
        public bool Thursday { get; set; }
        public bool Friday { get; set; }
        public bool Saturday { get; set; }
        public bool Sunday { get; set; }

        public void AllOn()
        {
            Monday = true;
            Tuesday = true;
            Wednesday = true;
            Thursday = true;
            Friday = true;
            Saturday = true;
            Sunday = true;
        }

        public static DaysOfWeekViewModel Create(IEnumerable<DayOfWeek> list)
        {
            var model = new DaysOfWeekViewModel();
            foreach (var item in list)
            {
                if (item == DayOfWeek.Sunday)
                    model.Sunday = true;
                if (item == DayOfWeek.Monday)
                    model.Monday = true;
                if (item == DayOfWeek.Tuesday)
                    model.Tuesday = true;
                if (item == DayOfWeek.Wednesday)
                    model.Wednesday = true;
                if (item == DayOfWeek.Thursday)
                    model.Thursday = true;
                if (item == DayOfWeek.Friday)
                    model.Friday = true;
                if (item == DayOfWeek.Saturday)
                    model.Saturday = true;
            }
            return model;
        }

        public IEnumerable<DayOfWeek> GetSelected()
        {
            if (Monday) yield return DayOfWeek.Monday;
            if (Tuesday) yield return DayOfWeek.Tuesday;
            if (Wednesday) yield return DayOfWeek.Wednesday;
            if (Thursday) yield return DayOfWeek.Thursday;
            if (Friday) yield return DayOfWeek.Friday;
            if (Saturday) yield return DayOfWeek.Saturday;
            if (Sunday) yield return DayOfWeek.Sunday;
        }
    }

    public enum TriggerType
    {
        Unknown = 0,
        Cron,
        Simple,
        Daily,
        Calendar,
    }
}