using System.Collections.Generic;
using System.Configuration;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;

namespace Scheduler.Job.Proxies
{
    public class IdentityApiProxy
    {
        public IdentityResponse Authenticate()
        {
            IdentityResponse identityResponse = null;

            try
            {
                var client = new HttpClient();
                var basicAuthorization = ConfigurationManager.AppSettings.Get("AuthApiClientId") + ":" +
                                         ConfigurationManager.AppSettings.Get("AuthApiClientSecret");

                var authorization = "Basic " + Base64Encode(basicAuthorization);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Add("Authorization", authorization);

                var formContent = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("grant_type", ConfigurationManager.AppSettings.Get("GrantType")),
                    new KeyValuePair<string, string>("client_id", ConfigurationManager.AppSettings.Get("ClientId")),
                    new KeyValuePair<string, string>("client_secret", ConfigurationManager.AppSettings.Get("ClientSecret")),
                    new KeyValuePair<string, string>("username", ConfigurationManager.AppSettings.Get("UserName")),
                    new KeyValuePair<string, string>("password", ConfigurationManager.AppSettings.Get("Password")),
                });

                var task = client.PostAsync(ConfigurationManager.AppSettings.Get("IdentityApiUri"), formContent)
                    .ContinueWith((taskWithResponse) =>
                    {
                        var response = taskWithResponse.Result;
                        var jsonString = response.Content.ReadAsStringAsync();
                        identityResponse = JsonConvert.DeserializeObject<IdentityResponse>(jsonString.Result);

                        jsonString.Wait();
                    });

                task.Wait();
            }
            catch (System.Exception)
            {
                //log
            }

            return identityResponse;
        }

        public static string Base64Encode(string plainText)
        {
            var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }
    }
}
