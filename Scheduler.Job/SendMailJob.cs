using System;
using System.Diagnostics;
using System.Net.Mail;
using System.Threading.Tasks;
using Quartz;

namespace Scheduler.Job
{
    public class SendMailJob : IJob
    {
        private static readonly Random Random = new Random();
        private const string EmailsKey = "EMAILS";
        private const string SubjectKey = "SUBJECT";
        private const string BodyKey = "BODY";

        public async Task Execute(IJobExecutionContext context)
        {
            Debug.WriteLine("SendMailJob > " + context.Trigger);

            try
            {
                var emails = GetJobDataFieldValue(context, EmailsKey);
                var subject = GetJobDataFieldValue(context, SubjectKey);
                var body = GetJobDataFieldValue(context, BodyKey);

                await SendEmail(emails, subject, body);
            }
            catch (Exception)
            {
                // log message
            }
        }

        public async Task SendEmail(string emails, string subject, string body)
        {
            var message = new MailMessage();
            message.To.Add(emails);

            message.Subject = subject;
            message.Body = body;

            using (var smtpClient = new SmtpClient())
            {
                await smtpClient.SendMailAsync(message);
            }
        }

        private string GetJobDataFieldValue(IJobExecutionContext context, string key)
        {
            var jobDataMap = context.JobDetail.JobDataMap;
            return jobDataMap.GetString(key);
        }
    }
}
