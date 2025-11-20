using Microsoft.AspNetCore.Identity;
using SistemaTesourariaEclesiastica.Models;
using System.Security.Claims;

namespace SistemaTesourariaEclesiastica.Middleware
{
    public class AccessControlMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<AccessControlMiddleware> _logger;

        public AccessControlMiddleware(RequestDelegate next, ILogger<AccessControlMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Adicionar claims personalizados se o usuário estiver autenticado
            if (context.User.Identity.IsAuthenticated)
            {
                using var scope = context.RequestServices.CreateScope();
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                var signInManager = scope.ServiceProvider.GetRequiredService<SignInManager<ApplicationUser>>();

                var user = await userManager.GetUserAsync(context.User);

                // ✅ CORREÇÃO: Se usuário não existe ou está inativo, fazer logout forçado
                if (user == null || !user.Ativo)
                {
                    _logger.LogWarning($"Usuário inativo ou inexistente tentou acessar o sistema: {context.User.Identity.Name}");

                    // Fazer logout forçado
                    await signInManager.SignOutAsync();

                    // Redirecionar para login com mensagem
                    context.Response.Redirect("/Account/Login?message=inactive");
                    return; // ✅ IMPORTANTE: Retornar aqui sem chamar ApplySecurityHeaders
                }

                // Adicionar claims adicionais se não existirem
                var identity = (ClaimsIdentity)context.User.Identity;

                if (!context.User.HasClaim("CentroCustoId", user.CentroCustoId?.ToString() ?? ""))
                {
                    identity.AddClaim(new Claim("CentroCustoId", user.CentroCustoId?.ToString() ?? ""));
                }

                if (!context.User.HasClaim("NomeCompleto", user.NomeCompleto ?? ""))
                {
                    identity.AddClaim(new Claim("NomeCompleto", user.NomeCompleto ?? ""));
                }
            }

            // ✅ CORREÇÃO: Aplicar headers de segurança APENAS antes de chamar _next
            // e SOMENTE se a resposta ainda não foi iniciada
            if (!context.Response.HasStarted)
            {
                ApplySecurityHeaders(context);
            }

            await _next(context);
        }

        private static bool ShouldAuditPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;

            var auditPaths = new[]
            {
                "/entradas",
                "/saidas",
                "/fechamentos",
                "/usuarios",
                "/auditoria",
                "/relatorios"
            };

            return auditPaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase));
        }

        private void ApplySecurityHeaders(HttpContext context)
        {
            var headers = context.Response.Headers;
            var isDevelopment = context.RequestServices
                .GetRequiredService<IWebHostEnvironment>()
                .IsDevelopment();

            headers["X-Content-Type-Options"] = "nosniff";
            headers["X-XSS-Protection"] = "1; mode=block";
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

            // CSP com configuração diferente por ambiente
            if (isDevelopment)
            {
                // CSP mais permissivo para desenvolvimento
                headers["Content-Security-Policy"] =
                    "default-src 'self'; " +
                    "script-src 'self' 'unsafe-inline' 'unsafe-eval' https://cdn.jsdelivr.net https://cdnjs.cloudflare.com https://cdn.datatables.net; " +
                    "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://cdnjs.cloudflare.com https://cdn.datatables.net https://fonts.googleapis.com; " +
                    "font-src 'self' https://fonts.gstatic.com https://cdnjs.cloudflare.com https://cdn.jsdelivr.net; " +
                    "img-src 'self' data: https: blob:; " +
                    "connect-src 'self' https://cdn.jsdelivr.net https://cdnjs.cloudflare.com https://cdn.datatables.net ws://localhost:* http://localhost:*; " +
                    "frame-ancestors 'none'; " +
                    "base-uri 'self'; " +
                    "form-action 'self';";
            }
            else
            {
                // CSP mais restritivo para produção
                headers["Content-Security-Policy"] =
                    "default-src 'self'; " +
                    "script-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://cdnjs.cloudflare.com https://cdn.datatables.net; " +
                    "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://cdnjs.cloudflare.com https://cdn.datatables.net https://fonts.googleapis.com; " +
                    "font-src 'self' https://fonts.gstatic.com https://cdnjs.cloudflare.com https://cdn.jsdelivr.net; " +
                    "img-src 'self' data: https:; " +
                    "connect-src 'self' https://cdn.jsdelivr.net https://cdnjs.cloudflare.com https://cdn.datatables.net; " +
                    "frame-ancestors 'none'; " +
                    "base-uri 'self'; " +
                    "form-action 'self';";
            }
        }
    }

    public static class AccessControlMiddlewareExtensions
    {
        public static IApplicationBuilder UseAccessControl(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<AccessControlMiddleware>();
        }
    }
}