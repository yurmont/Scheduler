using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Quartz;

namespace Scheduler.Job
{
    public class DummyJob : IJob
    {
        private static readonly Random Random = new Random();

        public async Task Execute(IJobExecutionContext context)
        {
            Debug.WriteLine("DummyJob > " + context.Trigger.ToString());

            await Task.Delay(TimeSpan.FromSeconds(Random.Next(1, 1)));
        }
    }
}
