using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;
using System.Text.Json;
using SistemaTesourariaEclesiastica.Services;

namespace SistemaTesourariaEclesiastica.Filters
{
    /// <summary>
    /// Action Filter para Auditoria - Alternativa ao Middleware
    /// 
    /// Este filtro é executado APÓS o MVC processar a action, evitando
    /// conflitos com cookies, headers e o pipeline HTTP.
    /// 
    /// Muito mais simples e confiável que middleware para auditoria.
    /// </summary>
    public class AuditActionFilter : IAsyncActionFilter
    {
        private readonly AuditQueueService _auditQueue;
        private readonly ILogger<AuditActionFilter> _logger;

        public AuditActionFilter(
            AuditQueueService auditQueue,
            ILogger<AuditActionFilter> logger)
        {
            _auditQueue = auditQueue;
            _logger = logger;
        }

        public async Task OnActionExecutionAsync(
            ActionExecutingContext context,
            ActionExecutionDelegate next)
        {
            // Verificar se deve auditar
            if (!ShouldAudit(context))
            {
                await next();
                return;
            }

            // Capturar informações ANTES da execução
            var timestamp = DateTime.UtcNow;
            var userId = context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userName = context.HttpContext.User.Identity?.Name ?? "Unknown";
            var ipAddress = context.HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = context.HttpContext.Request.Headers["User-Agent"].ToString();
            var method = context.HttpContext.Request.Method;
            var controller = context.RouteData.Values["controller"]?.ToString() ?? "Unknown";
            var action = context.RouteData.Values["action"]?.ToString() ?? "Unknown";
            var entityId = context.RouteData.Values["id"]?.ToString() ?? "0";
            var path = context.HttpContext.Request.Path.Value ?? "";

            // Executar a action
            var executedContext = await next();

            // Capturar status code DEPOIS da execução
            var statusCode = context.HttpContext.Response.StatusCode;

            // Adicionar à fila de auditoria (não bloqueia a resposta)
            if (!string.IsNullOrEmpty(userId))
            {
                try
                {
                    var details = JsonSerializer.Serialize(new
                    {
                        Method = method,
                        Path = path,
                        Controller = controller,
                        Action = action,
                        EntityId = entityId,
                        Timestamp = timestamp,
                        StatusCode = statusCode,
                        Success = executedContext.Exception == null
                    }, new JsonSerializerOptions
                    {
                        WriteIndented = false,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });

                    _auditQueue.EnqueueAudit(new AuditQueueItem
                    {
                        UserId = userId,
                        Action = $"{method} {action}",
                        EntityName = controller,
                        EntityId = entityId,
                        Details = details,
                        Timestamp = timestamp,
                        IPAddress = ipAddress,
                        UserAgent = userAgent
                    });

                    _logger.LogDebug("Auditoria enfileirada: {User} -> {Method} {Controller}/{Action}", 
                        userName, method, controller, action);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Erro ao enfileirar auditoria (não crítico)");
                }
            }
        }

        private static bool ShouldAudit(ActionExecutingContext context)
        {
            try
            {
                // Só auditar usuários autenticados
                if (context.HttpContext.User?.Identity?.IsAuthenticated != true)
                    return false;

                var controller = context.RouteData.Values["controller"]?.ToString()?.ToLower() ?? "";
                var method = context.HttpContext.Request.Method.ToUpper();

                // Controllers que devem ser auditados
                var auditControllers = new[]
                {
                    "entradas",
                    "saidas",
                    "despesasrecorrentes",
                    "fechamentos",
                    "fechamentoperiodo",
                    "usuarios",
                    "planos",
                    "membros",
                    "fornecedores",
                    "transferencias",
                    "regrasrateio",
                    "emprestimos"
                };

                if (!auditControllers.Contains(controller))
                    return false;

                // Auditar operações de escrita
                return method is "POST" or "PUT" or "DELETE" or "PATCH";
            }
            catch
            {
                return false;
            }
        }
    }
}
