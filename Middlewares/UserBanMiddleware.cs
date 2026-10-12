using Microsoft.AspNetCore.Identity;
using VzOverFlow.Models;

namespace VzOverFlow.Middlewares
{
    public class UserBanMiddleware
    {
        private readonly RequestDelegate _next;

        public UserBanMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, UserManager<User> userManager)
        {
            if (context.User.Identity?.IsAuthenticated == true)
            {
                var user = await userManager.GetUserAsync(context.User);
                
                if (user != null && user.IsBanned)
                {
                    var path = context.Request.Path;
                    
                    if (path.HasValue && 
                        (path.Value.Contains(".") || path.Value.StartsWith("/lib") || path.Value.StartsWith("/static")))
                    {
                        await _next(context);
                        return;
                    }

                    bool isAllowedPath = path.StartsWithSegments("/users/banned", StringComparison.OrdinalIgnoreCase) || 
                                       path.StartsWithSegments("/users/logout", StringComparison.OrdinalIgnoreCase);

                    if (!isAllowedPath)
                    {
                        context.Response.Redirect("/users/banned");
                        return;
                    }
                }
            }

            await _next(context);
        }
    }

    public static class UserBanMiddlewareExtensions
    {
        public static IApplicationBuilder UseUserBanEnforcement(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<UserBanMiddleware>();
        }
    }
}