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
            // ✅ CORREÇÃO CRÍTICA: Aplicar headers de segurança PRIMEIRO
            // ANTES de qualquer operação que possa iniciar a resposta HTTP
            try
            {
                // Só aplica headers se a resposta ainda não foi iniciada
                if (!context.Response.HasStarted)
                {
                    ApplySecurityHeaders(context);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Erro ao aplicar headers de segurança (não crítico)");
            }

            // Adicionar claims personalizados se o usuário estiver autenticado
            if (context.User?.Identity?.IsAuthenticated == true)
            {
                try
                {
                    using var scope = context.RequestServices.CreateScope();
                    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                    
                    var user = await userManager.GetUserAsync(context.User);

                    // Se usuário não existe ou está inativo, fazer logout forçado
                    if (user == null || !user.Ativo)
                    {
                        _logger.LogWarning($"Usuário inativo ou inexistente tentou acessar o sistema: {context.User.Identity.Name}");

                        // ✅ CORREÇÃO: Verificar se a resposta já foi iniciada antes de redirecionar
                        if (!context.Response.HasStarted)
                        {
                            var signInManager = scope.ServiceProvider.GetRequiredService<SignInManager<ApplicationUser>>();
                            
                            // Fazer logout forçado
                            await signInManager.SignOutAsync();

                            // Redirecionar para login com mensagem
                            context.Response.Redirect("/Account/Login?message=inactive");
                        }
                        
                        return;
                    }

                    // Adicionar claims adicionais se não existirem
                    var identity = context.User.Identity as ClaimsIdentity;
                    
                    if (identity != null)
                    {
                        // ✅ CORREÇÃO: Verificar se o claim já existe antes de adicionar
                        var centroCustoValue = user.CentroCustoId?.ToString() ?? "";
                        if (!context.User.HasClaim(c => c.Type == "CentroCustoId" && c.Value == centroCustoValue))
                        {
                            identity.AddClaim(new Claim("CentroCustoId", centroCustoValue));
                        }

                        var nomeCompletoValue = user.NomeCompleto ?? "";
                        if (!context.User.HasClaim(c => c.Type == "NomeCompleto" && c.Value == nomeCompletoValue))
                        {
                            identity.AddClaim(new Claim("NomeCompleto", nomeCompletoValue));
                        }
                    }
                }
                catch (ObjectDisposedException)
                {
                    // ✅ Tratar caso o HttpContext já tenha sido descartado
                    _logger.LogDebug("HttpContext já foi descartado durante verificação de usuário");
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao processar controle de acesso do usuário");
                    // ✅ Continuar o pipeline mesmo com erro no middleware
                }
            }

            // Continuar o pipeline
            await _next(context);
        }

        private void ApplySecurityHeaders(HttpContext context)
        {
            try
            {
                // ✅ CORREÇÃO: Verificar se a resposta já foi iniciada
                if (context.Response.HasStarted)
                {
                    _logger.LogDebug("Resposta já iniciada, não é possível adicionar headers");
                    return;
                }

                var headers = context.Response.Headers;
                var isDevelopment = context.RequestServices
                    .GetRequiredService<IWebHostEnvironment>()
                    .IsDevelopment();

                // ✅ CORREÇÃO: Adicionar headers de forma segura, verificando se já existem
                if (!headers.ContainsKey("X-Content-Type-Options"))
                    headers["X-Content-Type-Options"] = "nosniff";
                
                if (!headers.ContainsKey("X-XSS-Protection"))
                    headers["X-XSS-Protection"] = "1; mode=block";
                
                if (!headers.ContainsKey("Referrer-Policy"))
                    headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

                // CSP com configuração diferente por ambiente
                if (!headers.ContainsKey("Content-Security-Policy"))
                {
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
            catch (Exception ex)
            {
                // ✅ CORREÇÃO: Log e continuar sem quebrar a aplicação
                _logger.LogWarning(ex, "Erro ao aplicar headers de segurança (operação não crítica)");
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