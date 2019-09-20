using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Quartz;
using Scheduler.Job.Proxies;

namespace Scheduler.Job
{
    public class PostRequestJob : IJob
    {
        private const string UriKey = "URI";
        private const string ApiKey = "APIKEY";
        private const string BodyKey = "BODY";

        public async Task Execute(IJobExecutionContext context)
        {
            Debug.WriteLine("PostRequestJob > " + context.Trigger);
            try
            {
                var requestUri = GetJobDataFieldValue(context, UriKey);
                var apiKey = GetJobDataFieldValue(context, ApiKey);
                var body = GetJobDataFieldValue(context, BodyKey);

                if(string.IsNullOrWhiteSpace(apiKey))
                     await PostRequestWithApiKey(requestUri, apiKey, body);

                 await PostRequestWithAccessToken(requestUri, apiKey, body);
            }
            catch (Exception)
            {
                // log message
            }
        }

        public async Task PostRequestWithApiKey(string requestUri, string apiKey, string body)
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Add("x-api-key", apiKey);

            var httpContent = new StringContent(body, Encoding.UTF8);
            httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            await client.PostAsync(requestUri, httpContent);
        }

        public async Task PostRequestWithAccessToken(string requestUri, string body, string body1)
        {
            var client = new HttpClient();
            var identityResponse = new IdentityApiProxy().Authenticate();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Add("Authorization bearer", identityResponse.access_token);

            var httpContent = new StringContent(body, Encoding.UTF8);
            httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            await client.PostAsync(requestUri, httpContent);
        }

        private string GetJobDataFieldValue(IJobExecutionContext context, string key)
        {
            var jobDataMap = context.JobDetail.JobDataMap;
            return jobDataMap.GetString(key);
        }
    }
}
