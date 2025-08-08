using GuardianCapitalLLC.Data;
using Mailjet.Client.Resources;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using System;
using System.Security.Claims;
using GuardianCapitalLLC.Models;
using Microsoft.AspNetCore.Identity;

namespace GuardianCapitalLLC.Services
{
    public class AuthSessionMiddleware
    {
        private readonly RequestDelegate _next;

        public AuthSessionMiddleware(RequestDelegate next) => _next = next;

        public async Task Invoke(HttpContext context, ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            var cookie = context.Request.Cookies["auth_session_id"];
            if (Guid.TryParse(cookie, out var sessionId))
            {
                var session = await db.UserSessions
                    .Include(s => s.User)
                    .FirstOrDefaultAsync(s => s.SessionId == sessionId.ToString() && s.IsActive && s.ExpiresAt > DateTime.UtcNow);

                if (session != null)
                {
                    var user = session.User;
                    session.LastAccessedAt = DateTime.UtcNow;
                    await db.SaveChangesAsync();

                    context.Items["User"] = user;

                    // If not authenticated yet, sign in the user
                    if (!context.User.Identity?.IsAuthenticated ?? true)
                    {
                        var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                        new Claim(ClaimTypes.Name, user.UserName ?? user.Email ?? "Unknown")
                    };

                        // ✅ Get Identity roles and add as claims
                        var roles = await userManager.GetRolesAsync(user);
                        foreach (var role in roles)
                        {
                            claims.Add(new Claim(ClaimTypes.Role, role));
                        }

                        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                        var principal = new ClaimsPrincipal(identity);

                        await context.SignInAsync(IdentityConstants.ApplicationScheme, principal);
                        context.User = principal;
                    }
                }
                else
                {
                    context.Response.Cookies.Delete("auth_session_id");

                    context.Response.Redirect("/Account/Logout");
                    return;
                }
            }

            await _next(context);
        }
    }
}
