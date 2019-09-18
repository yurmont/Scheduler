using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Quartz;
using Quartz.Impl;
using Quartzmin;
using System.Collections.Specialized;
using System.Threading;
using System.Threading.Tasks;

namespace AspNetCoreDocker
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var scheduler = GetScheduler().Result;
            //var scheduler = DemoScheduler.Create().Result;



            scheduler.Start();

            var host = WebHost.CreateDefaultBuilder(args).Configure(app => 
            {
                app.UseQuartzmin(new QuartzminOptions() { Scheduler = scheduler });

            }).ConfigureServices(services => 
            {
                services.AddQuartzmin();

            })
            .Build();

            host.Start();

            while (!scheduler.IsShutdown)
                Thread.Sleep(250);
        }

        private static async Task<IScheduler> GetScheduler()
        {
            var properties = new NameValueCollection
            {
                { "quartz.scheduler.instanceName", "QuartzRealPlaza" },
                { "quartz.scheduler.instanceId", "QuartzRealPlaza" },
                { "quartz.jobStore.type", "Quartz.Impl.AdoJobStore.JobStoreTX, Quartz" },
                { "quartz.plugin.recentHistory.type", "Quartz.Plugins.RecentHistory.ExecutionHistoryPlugin, Quartz.Plugins.RecentHistory" },
                { "quartz.plugin.recentHistory.storeType", "Quartz.Plugins.RecentHistory.Impl.InProcExecutionHistoryStore, Quartz.Plugins.RecentHistory" },
                { "quartz.jobStore.useProperties", "true" },
                { "quartz.jobStore.dataSource", "default" },
                { "quartz.jobStore.tablePrefix", "QRTZ_" },
                {
                    "quartz.dataSource.default.connectionString",
                    "server=localhost;database=scheduler;uid=root;pwd=password;SslMode=none;"
                },
                { "quartz.dataSource.default.provider", "MySql" },
                { "quartz.threadPool.threadCount", "1" },
                { "quartz.serializer.type", "json" },
            };
            var schedulerFactory = new StdSchedulerFactory(properties);
            var scheduler = await schedulerFactory.GetScheduler();

            return scheduler;
        }

    }
}

