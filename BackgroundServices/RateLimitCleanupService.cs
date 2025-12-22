using SistemaTesourariaEclesiastica.Services;

namespace SistemaTesourariaEclesiastica.BackgroundServices
{
    /// <summary>
    /// Serviço em background para limpar tentativas antigas de acesso à transparência
    /// Executa automaticamente a cada 24 horas
    /// </summary>
    public class RateLimitCleanupService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<RateLimitCleanupService> _logger;
        private readonly TimeSpan _intervalo = TimeSpan.FromHours(24); // Executa 1x por dia

        public RateLimitCleanupService(
            IServiceProvider serviceProvider,
            ILogger<RateLimitCleanupService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("RateLimitCleanupService iniciado");

            // Aguardar 1 hora após inicialização para executar pela primeira vez
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Executando limpeza de tentativas antigas de acesso...");

                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var rateLimitService = scope.ServiceProvider.GetRequiredService<RateLimitService>();
                        var removidos = await rateLimitService.LimparTentativasAntigasAsync();

                        _logger.LogInformation($"Limpeza concluída: {removidos} tentativa(s) antiga(s) removida(s)");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao executar limpeza de tentativas antigas");
                }

                // Aguardar intervalo antes da próxima execução
                await Task.Delay(_intervalo, stoppingToken);
            }

            _logger.LogInformation("RateLimitCleanupService finalizado");
        }
    }
}
