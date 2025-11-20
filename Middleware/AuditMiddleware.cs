using Microsoft.AspNetCore.Identity;
using SistemaTesourariaEclesiastica.Models;
using SistemaTesourariaEclesiastica.Services;
using System.Text.Json;

namespace SistemaTesourariaEclesiastica.Middleware
{
    public class AuditMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<AuditMiddleware> _logger;

        public AuditMiddleware(RequestDelegate next, ILogger<AuditMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // ✅ CORREÇÃO: Capturar informações ANTES da execução da requisição
            var startTime = DateTime.UtcNow;
            var shouldAudit = context.User.Identity?.IsAuthenticated == true && ShouldAudit(context);
            var actionInfo = ExtractActionInfo(context);
            
            try
            {
                // Executar a próxima requisição
                await _next(context);

                // ✅ CORREÇÃO: Apenas fazer log se a resposta NÃO tiver começado
                if (shouldAudit && !context.Response.HasStarted)
                {
                    await LogRequestAsync(context, actionInfo, startTime);
                }
            }
            catch (Exception ex)
            {
                // Log de erro
                _logger.LogError(ex, $"Erro durante execução da requisição {context.Request.Method} {context.Request.Path}");

                // Log de auditoria para erros (apenas se resposta não começou)
                if (context.User.Identity?.IsAuthenticated == true && !context.Response.HasStarted)
                {
                    await LogErrorAsync(context, ex);
                }

                throw;
            }
        }

        private static bool ShouldAudit(HttpContext context)
        {
            var path = context.Request.Path.Value?.ToLower();
            var method = context.Request.Method.ToUpper();

            if (string.IsNullOrEmpty(path)) return false;

            // Não auditar requests de assets estáticos
            var staticExtensions = new[] { ".css", ".js", ".png", ".jpg", ".jpeg", ".gif", ".ico", ".svg", ".woff", ".woff2", ".ttf" };
            if (staticExtensions.Any(ext => path.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            // Não auditar requests de health check
            if (path.Contains("/health", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // Auditar apenas operações importantes
            var auditPaths = new[]
            {
                "/entradas",
                "/saidas",
                "/despesasrecorrentes",
                "/fechamentos",
                "/fechamentoperiodo",
                "/usuarios",
                "/planos",
                "/membros",
                "/fornecedores",
                "/transferencias"
            };

            var shouldAuditPath = auditPaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase));

            // Auditar todas as operações de escrita (POST, PUT, DELETE) e algumas de leitura importantes
            var shouldAuditMethod = method is "POST" or "PUT" or "DELETE" or "PATCH";

            return shouldAuditPath && (shouldAuditMethod || path.Contains("/details", StringComparison.OrdinalIgnoreCase));
        }

        private async Task LogRequestAsync(HttpContext context, (string Controller, string Action, string? EntityId) actionInfo, DateTime startTime)
        {
            try
            {
                // ✅ VERIFICAÇÃO ADICIONAL: Não tentar fazer log se resposta já começou
                if (context.Response.HasStarted)
                {
                    _logger.LogWarning("Tentativa de log de auditoria após resposta iniciada. Path: {Path}", context.Request.Path);
                    return;
                }

                using var scope = context.RequestServices.CreateScope();
                var auditService = scope.ServiceProvider.GetService<AuditService>();
                var userManager = scope.ServiceProvider.GetService<UserManager<ApplicationUser>>();

                if (auditService == null || userManager == null) return;

                var user = await userManager.GetUserAsync(context.User);
                if (user == null) return;

                var details = await ExtractRequestDetailsAsync(context, startTime);

                await auditService.LogAuditAsync(
                    user.Id,
                    $"{context.Request.Method} {actionInfo.Action}",
                    actionInfo.Controller,
                    actionInfo.EntityId ?? "0",
                    details
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao registrar log de auditoria");
            }
        }

        private async Task LogErrorAsync(HttpContext context, Exception exception)
        {
            try
            {
                // ✅ VERIFICAÇÃO ADICIONAL: Não tentar fazer log se resposta já começou
                if (context.Response.HasStarted)
                {
                    _logger.LogWarning("Tentativa de log de erro após resposta iniciada. Path: {Path}", context.Request.Path);
                    return;
                }

                using var scope = context.RequestServices.CreateScope();
                var auditService = scope.ServiceProvider.GetService<AuditService>();
                var userManager = scope.ServiceProvider.GetService<UserManager<ApplicationUser>>();

                if (auditService == null || userManager == null) return;

                var user = await userManager.GetUserAsync(context.User);
                if (user == null) return;

                var details = JsonSerializer.Serialize(new
                {
                    Error = exception.Message,
                    StackTrace = exception.StackTrace?.Take(1000), // Limitar o tamanho
                    RequestPath = context.Request.Path.Value,
                    RequestMethod = context.Request.Method,
                    StatusCode = context.Response.StatusCode
                });

                await auditService.LogAuditAsync(
                    user.Id,
                    "ERROR",
                    "System",
                    "0",
                    details
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao registrar log de erro de auditoria");
            }
        }

        private static (string Controller, string Action, string? EntityId) ExtractActionInfo(HttpContext context)
        {
            var routeValues = context.Request.RouteValues;
            var controller = routeValues.GetValueOrDefault("controller")?.ToString() ?? "Unknown";
            var action = routeValues.GetValueOrDefault("action")?.ToString() ?? "Unknown";
            var entityId = routeValues.GetValueOrDefault("id")?.ToString();

            return (controller, action, entityId);
        }

        private async Task<string?> ExtractRequestDetailsAsync(HttpContext context, DateTime startTime)
        {
            try
            {
                var details = new Dictionary<string, object>();

                // Adicionar informações da requisição
                details["UserAgent"] = context.Request.Headers["User-Agent"].ToString();
                details["IPAddress"] = context.Connection.RemoteIpAddress?.ToString();
                details["RequestTime"] = startTime;
                details["ResponseTime"] = DateTime.UtcNow;
                details["Duration"] = (DateTime.UtcNow - startTime).TotalMilliseconds;
                details["StatusCode"] = context.Response.StatusCode;

                // Adicionar parâmetros da query string (exceto dados sensíveis)
                if (context.Request.Query.Any())
                {
                    var queryParams = new Dictionary<string, string>();
                    foreach (var param in context.Request.Query)
                    {
                        if (!IsSensitiveParameter(param.Key))
                        {
                            queryParams[param.Key] = param.Value.ToString();
                        }
                    }
                    if (queryParams.Any())
                    {
                        details["QueryParameters"] = queryParams;
                    }
                }

                // ✅ CORREÇÃO: Não tentar ler Form se resposta já começou ou se não há FormContentType
                if (context.Request.Method is "POST" or "PUT" or "PATCH" &&
                    context.Request.HasFormContentType &&
                    !context.Response.HasStarted &&
                    context.Request.ContentLength < 10000) // Limitar tamanho
                {
                    try
                    {
                        var formData = new Dictionary<string, string>();
                        foreach (var field in context.Request.Form)
                        {
                            if (!IsSensitiveParameter(field.Key))
                            {
                                formData[field.Key] = field.Value.ToString();
                            }
                        }
                        if (formData.Any())
                        {
                            details["FormData"] = formData;
                        }
                    }
                    catch (InvalidOperationException)
                    {
                        // Form já foi lido - ignorar
                        _logger.LogDebug("Form já foi lido anteriormente");
                    }
                }

                return JsonSerializer.Serialize(details, new JsonSerializerOptions
                {
                    WriteIndented = false,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao extrair detalhes da requisição");
                return $"Erro ao extrair detalhes: {ex.Message}";
            }
        }

        private static bool IsSensitiveParameter(string parameterName)
        {
            var sensitiveParams = new[]
            {
                "password", "senha", "token", "secret", "key", "cpf", "cnpj",
                "email", "telefone", "celular", "__requestverificationtoken"
            };

            return sensitiveParams.Any(param =>
                parameterName.Contains(param, StringComparison.OrdinalIgnoreCase));
        }
    }

    public static class AuditMiddlewareExtensions
    {
        public static IApplicationBuilder UseAuditMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<AuditMiddleware>();
        }
    }
}