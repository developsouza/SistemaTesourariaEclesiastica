using Microsoft.EntityFrameworkCore;
using SistemaTesourariaEclesiastica.Data;
using SistemaTesourariaEclesiastica.Models;

namespace SistemaTesourariaEclesiastica.Services
{
    /// <summary>
    /// Serviço para controlar Rate Limiting no Portal de Transparência
    /// Limite: 3 tentativas falhadas a cada 3 horas por CPF
    /// </summary>
    public class RateLimitService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<RateLimitService> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        // Configurações de Rate Limiting
        private const int MAX_TENTATIVAS = 3;
        private const int JANELA_TEMPO_HORAS = 3;

        public RateLimitService(
            ApplicationDbContext context,
            ILogger<RateLimitService> logger,
            IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Verifica se o CPF está bloqueado por exceder o limite de tentativas
        /// </summary>
        public async Task<(bool Bloqueado, DateTime? DataDesbloqueio, int TentativasRestantes)> VerificarBloqueioAsync(string cpf)
        {
            if (string.IsNullOrWhiteSpace(cpf))
                return (false, null, MAX_TENTATIVAS);

            var limiteDataHora = DateTime.Now.AddHours(-JANELA_TEMPO_HORAS);

            // Buscar tentativas falhadas nas últimas 3 horas
            var tentativasFalhadas = await _context.TentativasAcessoTransparencia
                .Where(t => t.CPF == cpf && 
                           !t.Sucesso && 
                           t.DataHoraTentativa >= limiteDataHora)
                .OrderByDescending(t => t.DataHoraTentativa)
                .ToListAsync();

            var quantidadeTentativas = tentativasFalhadas.Count;
            var tentativasRestantes = Math.Max(0, MAX_TENTATIVAS - quantidadeTentativas);

            if (quantidadeTentativas >= MAX_TENTATIVAS)
            {
                // CPF está bloqueado
                var primeiraTentativa = tentativasFalhadas.LastOrDefault();
                var dataDesbloqueio = primeiraTentativa?.DataHoraTentativa.AddHours(JANELA_TEMPO_HORAS);

                _logger.LogWarning($"CPF bloqueado por exceder limite de tentativas: {cpf.Substring(0, 3)}***");

                return (true, dataDesbloqueio, 0);
            }

            return (false, null, tentativasRestantes);
        }

        /// <summary>
        /// Registra uma tentativa de acesso (sucesso ou falha)
        /// </summary>
        public async Task RegistrarTentativaAsync(string cpf, bool sucesso, string? mensagem = null)
        {
            if (string.IsNullOrWhiteSpace(cpf))
                return;

            var httpContext = _httpContextAccessor.HttpContext;
            var enderecoIP = ObterEnderecoIP(httpContext);
            var userAgent = httpContext?.Request.Headers["User-Agent"].ToString();

            var tentativa = new TentativaAcessoTransparencia
            {
                CPF = cpf,
                DataHoraTentativa = DateTime.Now,
                Sucesso = sucesso,
                EnderecoIP = enderecoIP,
                UserAgent = userAgent?.Length > 500 ? userAgent.Substring(0, 500) : userAgent,
                Mensagem = mensagem?.Length > 200 ? mensagem.Substring(0, 200) : mensagem
            };

            _context.TentativasAcessoTransparencia.Add(tentativa);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                $"Tentativa de acesso registrada: CPF={cpf.Substring(0, 3)}***, " +
                $"Sucesso={sucesso}, IP={enderecoIP}");

            // Se foi sucesso, limpar tentativas falhadas antigas do mesmo CPF
            if (sucesso)
            {
                await LimparTentativasFalhadasAsync(cpf);
            }
        }

        /// <summary>
        /// Limpa tentativas falhadas após um acesso bem-sucedido
        /// </summary>
        private async Task LimparTentativasFalhadasAsync(string cpf)
        {
            var tentativasParaRemover = await _context.TentativasAcessoTransparencia
                .Where(t => t.CPF == cpf && !t.Sucesso)
                .ToListAsync();

            if (tentativasParaRemover.Any())
            {
                _context.TentativasAcessoTransparencia.RemoveRange(tentativasParaRemover);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Tentativas falhadas limpas para CPF: {cpf.Substring(0, 3)}***");
            }
        }

        /// <summary>
        /// Remove tentativas antigas (job de limpeza - executar periodicamente)
        /// Remove registros com mais de 7 dias
        /// </summary>
        public async Task<int> LimparTentativasAntigasAsync()
        {
            var dataLimite = DateTime.Now.AddDays(-7);

            var tentativasAntigas = await _context.TentativasAcessoTransparencia
                .Where(t => t.DataHoraTentativa < dataLimite)
                .ToListAsync();

            if (tentativasAntigas.Any())
            {
                _context.TentativasAcessoTransparencia.RemoveRange(tentativasAntigas);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Limpeza automática: {tentativasAntigas.Count} tentativas antigas removidas");

                return tentativasAntigas.Count;
            }

            return 0;
        }

        /// <summary>
        /// Obtém estatísticas de rate limiting
        /// </summary>
        public async Task<Dictionary<string, int>> ObterEstatisticasAsync()
        {
            var limiteDataHora = DateTime.Now.AddHours(-JANELA_TEMPO_HORAS);

            var stats = new Dictionary<string, int>
            {
                ["TotalTentativasRecentes"] = await _context.TentativasAcessoTransparencia
                    .Where(t => t.DataHoraTentativa >= limiteDataHora)
                    .CountAsync(),

                ["TentativasSucesso"] = await _context.TentativasAcessoTransparencia
                    .Where(t => t.DataHoraTentativa >= limiteDataHora && t.Sucesso)
                    .CountAsync(),

                ["TentativasFalhadas"] = await _context.TentativasAcessoTransparencia
                    .Where(t => t.DataHoraTentativa >= limiteDataHora && !t.Sucesso)
                    .CountAsync(),

                ["CPFsBloqueados"] = await _context.TentativasAcessoTransparencia
                    .Where(t => t.DataHoraTentativa >= limiteDataHora && !t.Sucesso)
                    .GroupBy(t => t.CPF)
                    .Where(g => g.Count() >= MAX_TENTATIVAS)
                    .CountAsync()
            };

            return stats;
        }

        /// <summary>
        /// Obtém o endereço IP real do cliente (considera proxies e load balancers)
        /// </summary>
        private string? ObterEnderecoIP(HttpContext? context)
        {
            if (context == null)
                return null;

            // Tenta obter IP real de trás de proxies/load balancers
            var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                var ips = forwardedFor.Split(',', StringSplitOptions.RemoveEmptyEntries);
                return ips.FirstOrDefault()?.Trim();
            }

            var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
            if (!string.IsNullOrEmpty(realIp))
                return realIp.Trim();

            return context.Connection.RemoteIpAddress?.ToString();
        }

        /// <summary>
        /// Desbloqueia manualmente um CPF (uso administrativo)
        /// </summary>
        public async Task<bool> DesbloquearCPFAsync(string cpf)
        {
            if (string.IsNullOrWhiteSpace(cpf))
                return false;

            var tentativas = await _context.TentativasAcessoTransparencia
                .Where(t => t.CPF == cpf && !t.Sucesso)
                .ToListAsync();

            if (tentativas.Any())
            {
                _context.TentativasAcessoTransparencia.RemoveRange(tentativas);
                await _context.SaveChangesAsync();

                _logger.LogWarning($"CPF desbloqueado manualmente: {cpf.Substring(0, 3)}***");
                return true;
            }

            return false;
        }
    }
}
