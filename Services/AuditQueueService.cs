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
        private int _itemsProcessed = 0;
        private int _itemsFailed = 0;

        public AuditQueueService(IServiceProvider serviceProvider, ILogger<AuditQueueService> logger)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Adiciona um item à fila de auditoria
        /// </summary>
        public void EnqueueAudit(AuditQueueItem item)
        {
            if (item == null)
            {
                _logger.LogWarning("Tentativa de adicionar item nulo à fila de auditoria");
                return;
            }

            try
            {
                _auditQueue.Enqueue(item);
                _signal.Release();
                
                _logger.LogTrace("Item de auditoria adicionado à fila: {Action} - {Entity}", 
                    item.Action, item.EntityName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao adicionar item à fila de auditoria");
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("? Serviço de Auditoria em Background iniciado");

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        // Aguardar até que haja itens na fila ou cancelamento
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
                        _logger.LogInformation("Serviço de auditoria recebeu sinal de cancelamento");
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Erro no serviço de auditoria em background");
                        
                        // ? CORREÇÃO: Aguardar antes de tentar novamente para evitar loop infinito
                        try
                        {
                            await Task.Delay(1000, stoppingToken);
                        }
                        catch (OperationCanceledException)
                        {
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Erro crítico no serviço de auditoria");
            }
            finally
            {
                _logger.LogInformation(
                    "? Serviço de Auditoria em Background finalizado. " +
                    "Itens processados: {Processed}, Falhas: {Failed}, Pendentes: {Pending}",
                    _itemsProcessed, _itemsFailed, _auditQueue.Count);
            }
        }

        private async Task ProcessAuditItemAsync(AuditQueueItem item, CancellationToken cancellationToken)
        {
            if (item == null)
            {
                _logger.LogWarning("Item nulo encontrado na fila");
                return;
            }

            try
            {
                // ? CORREÇÃO: Criar um novo scope para cada item com timeout
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<Data.ApplicationDbContext>();

                // ? VALIDAÇÃO: Verificar se o usuário ainda existe
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                var userExists = await userManager.FindByIdAsync(item.UserId);
                
                if (userExists == null)
                {
                    _logger.LogWarning(
                        "Usuário {UserId} não encontrado ao processar auditoria. Ação: {Action}", 
                        item.UserId, item.Action);
                    // Continuar mesmo assim para não perder o log
                }

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
                
                // ? CORREÇÃO: Usar SaveChangesAsync com timeout
                using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
                
                await context.SaveChangesAsync(linkedCts.Token);

                _itemsProcessed++;
                
                _logger.LogDebug(
                    "? Log de auditoria processado: {Action} - {Entity} #{Id} (User: {UserId})", 
                    item.Action, item.EntityName, item.EntityId, item.UserId);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                _itemsFailed++;
                _logger.LogWarning(
                    "?? Timeout ao processar auditoria: {Action} - {Entity}. Item será descartado.", 
                    item.Action, item.EntityName);
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException dbEx)
            {
                _itemsFailed++;
                _logger.LogError(dbEx, 
                    "? Erro de banco de dados ao processar auditoria: {Action} - {Entity}", 
                    item.Action, item.EntityName);
            }
            catch (Exception ex)
            {
                _itemsFailed++;
                _logger.LogError(ex, 
                    "? Erro ao processar item de auditoria: {Action} - {Entity}", 
                    item.Action, item.EntityName);
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "?? Finalizando serviço de auditoria... " +
                "Processando {Count} itens restantes na fila", 
                _auditQueue.Count);

            // ? CORREÇÃO: Processar itens restantes com timeout global
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var maxShutdownTime = TimeSpan.FromSeconds(30);

            try
            {
                while (_auditQueue.TryDequeue(out var item) && stopwatch.Elapsed < maxShutdownTime)
                {
                    await ProcessAuditItemAsync(item, cancellationToken);
                }

                if (_auditQueue.Count > 0)
                {
                    _logger.LogWarning(
                        "?? {Count} itens de auditoria não foram processados devido ao shutdown", 
                        _auditQueue.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar itens restantes durante shutdown");
            }

            _logger.LogInformation(
                "? Shutdown do serviço de auditoria concluído. " +
                "Total processado: {Processed}, Falhas: {Failed}",
                _itemsProcessed, _itemsFailed);

            await base.StopAsync(cancellationToken);
        }

        // ? NOVO: Método para obter estatísticas (útil para diagnóstico)
        public (int Processed, int Failed, int Pending) GetStatistics()
        {
            return (_itemsProcessed, _itemsFailed, _auditQueue.Count);
        }
    }
}
