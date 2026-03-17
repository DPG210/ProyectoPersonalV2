using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace ProyectoPersonal.Policies
{
    public class SerAdminRequirement:
        AuthorizationHandler<SerAdminRequirement>,
        IAuthorizationRequirement
    {
        protected override Task
            HandleRequirementAsync(AuthorizationHandlerContext context,
            SerAdminRequirement requirement)
        {
            if (context.User.HasClaim(x => x.Type == ClaimTypes.Role) == false)
            {
                context.Fail();
            }
            else
            {
                string data =
                    context.User.FindFirstValue(ClaimTypes.Role);
                if ( data.ToUpper() == "ADMIN")
                {
                    context.Succeed(requirement);
                }
                else
                {
                    context.Fail();
                }
            }
            return Task.CompletedTask;
        }
    }
}

