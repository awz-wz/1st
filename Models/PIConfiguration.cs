namespace PIWebAPIApp.Models
{
    public class PIConfiguration
    {
        public string BaseUrl { get; set; }
        public string DataServerWebId { get; set; }
        public int TimeoutSeconds { get; set; } = 30;
        public bool UseDefaultCredentials { get; set; } = true;

        public PIConfiguration()
        {
            BaseUrl = "https://your-pi-server/piwebapi";
            DataServerWebId = "your-default-webid";
        }

        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(BaseUrl) && 
                   !string.IsNullOrWhiteSpace(DataServerWebId);
        }
    }
}