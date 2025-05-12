using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Geography4Geek_1.Middleware
{
    public class UserActivityMiddleware
    {
        private readonly RequestDelegate _next;

        public UserActivityMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Versione semplificata che non fa nulla
            // Rimuove ogni riferimento a LastActivityDate
            await _next(context);
        }
    }
}