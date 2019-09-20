using System;
using System.Globalization;
using Quartz;
using Scheduler.Api.Models;

namespace Scheduler.Api
{
    internal static class Extensions
    {
        public static TriggerBuilder ForJob(this TriggerBuilder builder, string jobKey)
        {
            var parts = jobKey.Split('.');
            return builder.ForJob(new JobKey(parts[1], parts[0]));
        }

        public static TimeOfDay ToTimeOfDay(this TimeSpan timeSpan)
        {
            return new TimeOfDay(timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds);
        }

        public static string ToDefaultFormat(this DateTime date)
        {
            return date.ToString(DateTimeSettings.DefaultDateFormat + " " + DateTimeSettings.DefaultTimeFormat, CultureInfo.InvariantCulture);
        }

        public static TriggerType GetTriggerType(this ITrigger trigger)
        {
            if (trigger is ICronTrigger)
                return TriggerType.Cron;
            if (trigger is IDailyTimeIntervalTrigger)
                return TriggerType.Daily;
            if (trigger is ISimpleTrigger)
                return TriggerType.Simple;
            if (trigger is ICalendarIntervalTrigger)
                return TriggerType.Calendar;

            return TriggerType.Unknown;
        }

        public static TimeSpan ToTimeSpan(this TimeOfDay timeOfDay)
        {
            return TimeSpan.FromSeconds(timeOfDay.Second + timeOfDay.Minute * 60 + timeOfDay.Hour * 3600);
        }
    }
}
