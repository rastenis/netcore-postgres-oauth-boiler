using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Threading.Tasks;

namespace netcore_postgres_oauth_boiler.Policies
{
    public class AuthHandler : AuthorizationHandler<AuthRequirement>
    {
        IHttpContextAccessor _httpContextAccessor = null;

        public AuthHandler(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        protected async override Task HandleRequirementAsync(AuthorizationHandlerContext context,
                                                       AuthRequirement requirement)
        {
            var redirectContext = context.Resource as AuthorizationFilterContext;
            var session = _httpContextAccessor.HttpContext.Session;

            // loading session
            if (!session.IsAvailable)
            {
                await session.LoadAsync();
            }

            // Disallowing already logged-in users
            if (session.GetString("user") != null && requirement.authorized)
            {
                context.Succeed(requirement);
                return;
            }

            if (session.GetString("user") == null && !requirement.authorized)
            {
                context.Succeed(requirement);
                return;
            }

            context.Fail();
        }
    }
}
