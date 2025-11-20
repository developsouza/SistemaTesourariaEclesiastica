using Microsoft.AspNetCore.Identity;
using SistemaTesourariaEclesiastica.Models;
using SistemaTesourariaEclesiastica.Services;
using System.Text.Json;

namespace SistemaTesourariaEclesiastica.Middleware
{
    /// <summary>
    /// Middleware de Auditoria usando Fila em Background
    /// 
    /// Esta implementação usa um serviço de fila (AuditQueueService) que processa
    /// logs de auditoria de forma completamente isolada do pipeline HTTP.
    /// 
    /// O middleware captura informações ANTES de executar a requisição e adiciona
    /// à fila. O processamento real acontece em background sem bloquear a resposta.
    /// </summary>
    public class AuditMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<AuditMiddleware> _logger;
        private readonly AuditQueueService _auditQueue;

        public AuditMiddleware(
            RequestDelegate next, 
            ILogger<AuditMiddleware> logger,
            AuditQueueService auditQueue)
        {
            _next = next;
            _logger = logger;
            _auditQueue = auditQueue;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var shouldAudit = context.User.Identity?.IsAuthenticated == true && ShouldAudit(context);
            
            if (!shouldAudit)
            {
                await _next(context);
                return;
            }

            // Capturar TODAS as informações necessárias ANTES de executar a requisição
            var timestamp = DateTime.UtcNow;
            var userId = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var actionInfo = ExtractActionInfo(context);
            var ipAddress = context.Connection.RemoteIpAddress?.ToString();
            var userAgent = context.Request.Headers["User-Agent"].ToString();
            var method = context.Request.Method;
            var path = context.Request.Path.Value ?? "";
            var queryParams = CaptureQueryParameters(context);

            try
            {
                // Executar a requisição
                await _next(context);

                // ✅ SOLUÇÃO: Adicionar à fila SEM acessar HttpContext
                if (!string.IsNullOrEmpty(userId))
                {
                    var details = JsonSerializer.Serialize(new
                    {
                        Method = method,
                        Path = path,
                        QueryParameters = queryParams,
                        Timestamp = timestamp,
                        StatusCode = context.Response.StatusCode
                    }, new JsonSerializerOptions
                    {
                        WriteIndented = false,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });

                    _auditQueue.EnqueueAudit(new AuditQueueItem
                    {
                        UserId = userId,
                        Action = $"{method} {actionInfo.Action}",
                        EntityName = actionInfo.Controller,
                        EntityId = actionInfo.EntityId ?? "0",
                        Details = details,
                        Timestamp = timestamp,
                        IPAddress = ipAddress,
                        UserAgent = userAgent
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "Erro durante execução da requisição {Method} {Path}. User: {User}", 
                    method, path, context.User?.Identity?.Name ?? "Anonymous");

                // Registrar erro na fila
                if (!string.IsNullOrEmpty(userId))
                {
                    var errorDetails = JsonSerializer.Serialize(new
                    {
                        Error = ex.Message,
                        StackTrace = ex.StackTrace?.Substring(0, Math.Min(ex.StackTrace?.Length ?? 0, 500)),
                        Method = method,
                        Path = path
                    });

                    _auditQueue.EnqueueAudit(new AuditQueueItem
                    {
                        UserId = userId,
                        Action = "ERROR",
                        EntityName = "System",
                        EntityId = "0",
                        Details = errorDetails,
                        Timestamp = timestamp,
                        IPAddress = ipAddress,
                        UserAgent = userAgent
                    });
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

        private static Dictionary<string, string> CaptureQueryParameters(HttpContext context)
        {
            var queryParams = new Dictionary<string, string>();

            try
            {
                if (context.Request.Query.Any())
                {
                    foreach (var param in context.Request.Query)
                    {
                        if (!IsSensitiveParameter(param.Key))
                        {
                            queryParams[param.Key] = param.Value.ToString();
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Ignorar erros ao capturar query params
            }

            return queryParams;
        }

        private static (string Controller, string Action, string? EntityId) ExtractActionInfo(HttpContext context)
        {
            var routeValues = context.Request.RouteValues;
            var controller = routeValues.GetValueOrDefault("controller")?.ToString() ?? "Unknown";
            var action = routeValues.GetValueOrDefault("action")?.ToString() ?? "Unknown";
            var entityId = routeValues.GetValueOrDefault("id")?.ToString();

            return (controller, action, entityId);
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