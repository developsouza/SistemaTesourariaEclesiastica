using Microsoft.AspNetCore.Identity;
using SistemaTesourariaEclesiastica.Attributes;
using SistemaTesourariaEclesiastica.Models;
using SistemaTesourariaEclesiastica.Services;
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
            // Aplicar headers de segurança
            ApplySecurityHeaders(context);

            // Adicionar claims personalizados se o usuário estiver autenticado
            if (context.User.Identity.IsAuthenticated)
            {
                using var scope = context.RequestServices.CreateScope();
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                var auditService = scope.ServiceProvider.GetRequiredService<AuditService>();

                var user = await userManager.GetUserAsync(context.User);
                if (user != null && user.Ativo)
                {
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

                    // Log de auditoria para rotas importantes
                    //if (ShouldAuditPath(context.Request.Path.Value))
                    //{
                    //    await auditService.LogAcessoAsync(
                    //        user.Id,
                    //        $"{context.Request.Method} {context.Request.Path}",
                    //        context.Connection.RemoteIpAddress?.ToString()
                    //    );
                    //}
                }
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
                    "script-src 'self' 'unsafe-inline' 'unsafe-eval' https://cdn.jsdelivr.net https://cdnjs.cloudflare.com; " +
                    "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://cdnjs.cloudflare.com https://fonts.googleapis.com; " +
                    "font-src 'self' https://fonts.gstatic.com https://cdnjs.cloudflare.com https://cdn.jsdelivr.net; " +
                    "img-src 'self' data: https: blob:; " +
                    "connect-src 'self' https://cdn.jsdelivr.net https://cdnjs.cloudflare.com ws://localhost:* http://localhost:*; " +
                    "frame-ancestors 'none'; " +
                    "base-uri 'self'; " +
                    "form-action 'self';";
            }
            else
            {
                // CSP mais restritivo para produção
                headers["Content-Security-Policy"] =
                    "default-src 'self'; " +
                    "script-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://cdnjs.cloudflare.com; " +
                    "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://cdnjs.cloudflare.com https://fonts.googleapis.com; " +
                    "font-src 'self' https://fonts.gstatic.com https://cdnjs.cloudflare.com https://cdn.jsdelivr.net; " +
                    "img-src 'self' data: https:; " +
                    "connect-src 'self' https://cdn.jsdelivr.net https://cdnjs.cloudflare.com; " +
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

    /// <summary>
    /// Middleware para controle de centro de custo
    /// Garante que usuários locais só acessem dados do seu centro de custo
    /// </summary>
    public class CentroCustoAccessMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<CentroCustoAccessMiddleware> _logger;

        public CentroCustoAccessMiddleware(RequestDelegate next, ILogger<CentroCustoAccessMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.User.Identity.IsAuthenticated)
            {
                var path = context.Request.Path.Value?.ToLower();
                var method = context.Request.Method;

                // Verificar se é uma operação que requer verificação de centro de custo
                if (RequiresCentroCustoCheck(path, method))
                {
                    var isAuthorized = await CheckCentroCustoAccess(context);
                    if (!isAuthorized)
                    {
                        _logger.LogWarning($"Acesso negado por centro de custo para usuário {context.User.Identity.Name} em {path}");
                        context.Response.StatusCode = 403;
                        await context.Response.WriteAsync("Acesso negado: você só pode acessar dados do seu centro de custo.");
                        return;
                    }
                }
            }

            await _next(context);
        }

        private static bool RequiresCentroCustoCheck(string path, string method)
        {
            if (string.IsNullOrEmpty(path)) return false;

            var restrictedPaths = new[]
            {
                "/entradas/edit/",
                "/entradas/delete/",
                "/saidas/edit/",
                "/saidas/delete/",
                "/fechamentos/"
            };

            return method != "GET" && restrictedPaths.Any(p => path.StartsWith(p));
        }

        private static async Task<bool> CheckCentroCustoAccess(HttpContext context)
        {
            // Administradores e Tesoureiros Gerais têm acesso total
            if (context.User.IsInRole(Roles.Administrador) || context.User.IsInRole(Roles.TesoureiroGeral))
            {
                return true;
            }

            // Para outros usuários, verificar se estão acessando dados do seu centro de custo
            var userCentroCustoId = context.User.FindFirst("CentroCustoId")?.Value;
            if (string.IsNullOrEmpty(userCentroCustoId))
            {
                return false;
            }

            // Extrair centro de custo da requisição
            var requestCentroCustoId = ExtractCentroCustoFromRequest(context);

            return string.IsNullOrEmpty(requestCentroCustoId) || userCentroCustoId == requestCentroCustoId;
        }

        private static string ExtractCentroCustoFromRequest(HttpContext context)
        {
            // Tentar extrair de route params
            if (context.Request.RouteValues.TryGetValue("centroCustoId", out var routeValue))
            {
                return routeValue?.ToString();
            }

            // Tentar extrair de query string
            if (context.Request.Query.TryGetValue("centroCustoId", out var queryValue))
            {
                return queryValue.FirstOrDefault();
            }

            // Para POST/PUT, tentar extrair do form data
            if (context.Request.HasFormContentType)
            {
                if (context.Request.Form.TryGetValue("CentroCustoId", out var formValue))
                {
                    return formValue.FirstOrDefault();
                }
            }

            return null;
        }
    }

    public static class CentroCustoAccessMiddlewareExtensions
    {
        public static IApplicationBuilder UseCentroCustoAccess(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<CentroCustoAccessMiddleware>();
        }
    }
}