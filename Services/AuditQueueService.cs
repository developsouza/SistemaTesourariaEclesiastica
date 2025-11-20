using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using SistemaTesourariaEclesiastica.Models;

namespace SistemaTesourariaEclesiastica.Services
{
    /// <summary>
    /// Item de auditoria na fila
    /// </summary>
    public class AuditQueueItem
    {
        public string UserId { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string EntityName { get; set; } = string.Empty;
        public string EntityId { get; set; } = string.Empty;
        public string? Details { get; set; }
        public DateTime Timestamp { get; set; }
        public string? IPAddress { get; set; }
        public string? UserAgent { get; set; }
    }

    /// <summary>
    /// Serviço de fila para processar logs de auditoria em background
    /// </summary>
    public class AuditQueueService : BackgroundService
    {
        private readonly ConcurrentQueue<AuditQueueItem> _auditQueue = new();
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AuditQueueService> _logger;
        private readonly SemaphoreSlim _signal = new(0);

        public AuditQueueService(IServiceProvider serviceProvider, ILogger<AuditQueueService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        /// <summary>
        /// Adiciona um item à fila de auditoria
        /// </summary>
        public void EnqueueAudit(AuditQueueItem item)
        {
            if (item == null) return;

            _auditQueue.Enqueue(item);
            _signal.Release();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Serviço de Auditoria em Background iniciado");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Aguardar até que haja itens na fila
                    await _signal.WaitAsync(stoppingToken);

                    // Processar todos os itens disponíveis
                    while (_auditQueue.TryDequeue(out var item))
                    {
                        await ProcessAuditItemAsync(item, stoppingToken);
                    }
                }
                catch (OperationCanceledException)
                {
                    // Normal durante o shutdown
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro no serviço de auditoria em background");
                    await Task.Delay(1000, stoppingToken); // Aguardar antes de tentar novamente
                }
            }

            _logger.LogInformation("Serviço de Auditoria em Background finalizado");
        }

        private async Task ProcessAuditItemAsync(AuditQueueItem item, CancellationToken cancellationToken)
        {
            try
            {
                // Criar um novo scope para cada item
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<Data.ApplicationDbContext>();

                var log = new LogAuditoria
                {
                    UsuarioId = item.UserId,
                    Acao = item.Action,
                    Entidade = item.EntityName,
                    EntidadeId = item.EntityId,
                    DataHora = item.Timestamp,
                    Detalhes = item.Details,
                    EnderecoIP = item.IPAddress,
                    UserAgent = item.UserAgent
                };

                context.LogsAuditoria.Add(log);
                await context.SaveChangesAsync(cancellationToken);

                _logger.LogDebug("Log de auditoria processado: {Action} - {Entity} #{Id}", 
                    item.Action, item.EntityName, item.EntityId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar item de auditoria: {Action} - {Entity}", 
                    item.Action, item.EntityName);
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Finalizando serviço de auditoria... Processando itens restantes");

            // Processar itens restantes na fila antes de finalizar
            while (_auditQueue.TryDequeue(out var item))
            {
                await ProcessAuditItemAsync(item, cancellationToken);
            }

            await base.StopAsync(cancellationToken);
        }
    }
}
