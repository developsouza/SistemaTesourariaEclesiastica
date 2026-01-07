using SistemaTesourariaEclesiastica.Services;

namespace SistemaTesourariaEclesiastica.BackgroundServices
{
    /// <summary>
    /// Serviço em background para executar diagnóstico de consistência automaticamente
    /// </summary>
    public class ConsistenciaBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ConsistenciaBackgroundService> _logger;
        private readonly TimeSpan _intervalo = TimeSpan.FromHours(24); // Executar diariamente

        public ConsistenciaBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<ConsistenciaBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Serviço de Consistência Automática iniciado");

            // Aguardar 5 minutos após inicialização antes da primeira execução
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ExecutarDiagnosticoAutomatico(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao executar diagnóstico automático de consistência");
                }

                // Aguardar até a próxima execução
                await Task.Delay(_intervalo, stoppingToken);
            }

            _logger.LogInformation("Serviço de Consistência Automática finalizado");
        }

        private async Task ExecutarDiagnosticoAutomatico(CancellationToken stoppingToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var consistenciaService = scope.ServiceProvider.GetRequiredService<ConsistenciaService>();
            var auditService = scope.ServiceProvider.GetRequiredService<AuditService>();

            _logger.LogInformation("Iniciando diagnóstico automático de consistência");

            var relatorio = await consistenciaService.ExecutarDiagnosticoCompleto();

            // Registrar nos logs
            _logger.LogInformation(
                $"Diagnóstico automático concluído: {relatorio.TotalInconsistencias} inconsistências encontradas " +
                $"({relatorio.InconsistenciasCriticas} críticas, {relatorio.InconsistenciasAvisos} avisos, " +
                $"{relatorio.InconsistenciasInformacoes} informações). Saúde do sistema: {relatorio.PercentualSaude:F1}%");

            // Se houver inconsistências críticas, gerar alerta
            if (relatorio.InconsistenciasCriticas > 0)
            {
                _logger.LogWarning(
                    $"ATENÇÃO: {relatorio.InconsistenciasCriticas} inconsistências CRÍTICAS detectadas! " +
                    $"Acesse o painel de consistência para mais detalhes.");

                await auditService.LogAsync(
                    "Alerta",
                    "Consistencia",
                    $"Diagnóstico automático detectou {relatorio.InconsistenciasCriticas} inconsistências CRÍTICAS");
            }

            // Registrar auditoria
            await auditService.LogAsync(
                "Diagnóstico Automático",
                "Consistencia",
                $"Diagnóstico executado: {relatorio.TotalInconsistencias} inconsistências " +
                $"({relatorio.ClassificacaoSaude}, {relatorio.PercentualSaude:F1}%)");
        }
    }
}
