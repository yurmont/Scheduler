namespace Scheduler.Job.Proxies
{
    public class IdentityResponse
    {
        public string access_token { get; set; }
        public string token_type { get; set; }
        public string refresh_token { get; set; }
        public int expires_in { get; set; }
        public string scope { get; set; }
        public string user_mother_lastname { get; set; }
        public string user_type { get; set; }
        public string user_names { get; set; }
        public string user_lastname { get; set; }
        public string user_document_number { get; set; }
        public string codigo_unico { get; set; }
        public string user_red { get; set; }
        public string jti { get; set; }
    }
}