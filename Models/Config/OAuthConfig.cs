using System;

namespace netcore_postgres_oauth_boiler.Models.Config
{
    public class OAuthConfig
    {
        public PlatformConfig Google { get; set; }
        public PlatformConfig Github { get; set; }
        public PlatformConfig Linkedin { get; set; }

    }
    public class PlatformConfig
    {
        public string client_id { get; set; }
        public string client_secret { get; set; }
    }
}
