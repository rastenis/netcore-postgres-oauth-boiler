using Microsoft.AspNetCore.Authorization;

namespace netcore_postgres_oauth_boiler.Policies
{
    public class AuthRequirement : IAuthorizationRequirement
    {
        public bool authorized { get; }

        public AuthRequirement(bool authorized)
        {
            this.authorized = authorized;
        }
    }
}
