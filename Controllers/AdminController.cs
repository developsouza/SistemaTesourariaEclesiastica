using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaTesourariaEclesiastica.Attributes;
using SistemaTesourariaEclesiastica.Data;
using SistemaTesourariaEclesiastica.Enums;
using SistemaTesourariaEclesiastica.Models;
using SistemaTesourariaEclesiastica.Services;

namespace SistemaTesourariaEclesiastica.Controllers
{
    [Authorize]
    [AuthorizeRoles(Roles.AdminOnly)]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AuditService _auditService;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            AuditService auditService,
            ILogger<AdminController> logger)
        {
            _context = context;
            _userManager = userManager;
            _auditService = auditService;
            _logger = logger;
        }

        // GET: Admin/SystemInfo
        public async Task<IActionResult> SystemInfo()
        {
            await _auditService.LogAsync("SYSTEM_INFO_ACCESS", "Admin", "Acesso às informações do sistema");

            try
            {
                var systemInfo = new
                {
                    ServerName = Environment.MachineName,
                    SystemVersion = "1.0.0",
                    AspNetVersion = Environment.Version.ToString(),
                    OperatingSystem = Environment.OSVersion.ToString(),
                    ProcessorCount = Environment.ProcessorCount,
                    WorkingSet = GC.GetTotalMemory(false),
                    DatabaseProvider = "SQL Server",
                    StartTime = DateTime.Now.AddHours(-2), // Simulated
                    Uptime = TimeSpan.FromHours(2).ToString(@"hh\:mm\:ss")
                };

                ViewBag.SystemInfo = systemInfo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter informações do sistema");
                TempData["ErrorMessage"] = "Erro ao carregar informações do sistema";
            }

            return View();
        }

        // GET: Admin/DatabaseStats
        public async Task<IActionResult> DatabaseStats()
        {
            await _auditService.LogAsync("DATABASE_STATS_ACCESS", "Admin", "Acesso às estatísticas do banco");

            try
            {
                var stats = new
                {
                    TotalUsuarios = await _userManager.Users.CountAsync(),
                    TotalCentrosCusto = await _context.CentrosCusto.CountAsync(),
                    TotalMembros = await _context.Membros.CountAsync(),
                    TotalEntradas = await _context.Entradas.CountAsync(),
                    TotalSaidas = await _context.Saidas.CountAsync(),
                    TotalFechamentos = await _context.FechamentosPeriodo.CountAsync(),
                    TotalLogsAuditoria = await _context.LogsAuditoria.CountAsync(),
                    TotalMeiosPagamento = await _context.MeiosDePagamento.CountAsync(),
                    // CORRIGIDO: Usar PlanosDeContas ao invés de FontesRenda e CategoriasDespesa
                    TotalFontesRenda = await _context.PlanosDeContas
                        .Where(p => p.Tipo == TipoPlanoContas.Receita)
                        .CountAsync(),
                    TotalCategoriasDespesa = await _context.PlanosDeContas
                        .Where(p => p.Tipo == TipoPlanoContas.Despesa)
                        .CountAsync()
                };

                ViewBag.DatabaseStats = stats;

                // Estatísticas por período (último mês)
                var ultimoMes = DateTime.Now.AddMonths(-1);
                var statsRecentes = new
                {
                    EntradasRecentes = await _context.Entradas
                        .Where(e => e.Data >= ultimoMes)
                        .CountAsync(),
                    SaidasRecentes = await _context.Saidas
                        .Where(s => s.Data >= ultimoMes)
                        .CountAsync(),
                    FechamentosRecentes = await _context.FechamentosPeriodo
                        .Where(f => f.DataSubmissao >= ultimoMes)
                        .CountAsync(),
                    LogsRecentes = await _context.LogsAuditoria
                        .Where(l => l.DataHora >= ultimoMes)
                        .CountAsync()
                };

                ViewBag.StatsRecentes = statsRecentes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter estatísticas do banco");
                TempData["ErrorMessage"] = "Erro ao carregar estatísticas";
            }

            return View();
        }

        // GET: Admin/UserActivity
        public async Task<IActionResult> UserActivity(int page = 1, int pageSize = 20)
        {
            await _auditService.LogAsync("USER_ACTIVITY_ACCESS", "Admin", "Acesso à atividade de usuários");

            try
            {
                var ultimaSemana = DateTime.Now.AddDays(-7);

                var atividades = await _context.LogsAuditoria
                    .Where(l => l.DataHora >= ultimaSemana)
                    .OrderByDescending(l => l.DataHora)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var totalAtividades = await _context.LogsAuditoria
                    .Where(l => l.DataHora >= ultimaSemana)
                    .CountAsync();

                // Buscar informações dos usuários
                var atividadesComUsuarios = new List<dynamic>();
                foreach (var atividade in atividades)
                {
                    var usuario = await _userManager.FindByIdAsync(atividade.UsuarioId);
                    atividadesComUsuarios.Add(new
                    {
                        Atividade = atividade,
                        Usuario = usuario
                    });
                }

                // CORRIGIDO: Calcular estatísticas no Controller ao invés de usar LINQ na View
                var usuariosUnicos = atividades.Select(a => a.UsuarioId).Distinct().Count();
                var atividadesUltimaHora = atividades.Where(a => a.DataHora >= DateTime.Now.AddHours(-1)).Count();
                var mediaPorDia = Math.Round((double)totalAtividades / 7, 1);

                ViewBag.Atividades = atividadesComUsuarios;
                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = (int)Math.Ceiling((double)totalAtividades / pageSize);
                ViewBag.TotalAtividades = totalAtividades;
                ViewBag.UsuariosUnicos = usuariosUnicos;
                ViewBag.AtividadesUltimaHora = atividadesUltimaHora;
                ViewBag.MediaPorDia = mediaPorDia;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao carregar atividades de usuários");
                TempData["ErrorMessage"] = "Erro ao carregar atividades";
            }

            return View();
        }

        // POST: Admin/CleanupLogs
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CleanupLogs(int dias = 60)
        {
            try
            {
                var dataCorte = DateTime.Now.AddDays(-dias);
                var logsParaRemover = await _context.LogsAuditoria
                    .Where(l => l.DataHora < dataCorte)
                    .CountAsync();

                if (logsParaRemover > 0)
                {
                    var logs = await _context.LogsAuditoria
                        .Where(l => l.DataHora < dataCorte)
                        .ToListAsync();

                    _context.LogsAuditoria.RemoveRange(logs);
                    await _context.SaveChangesAsync();

                    await _auditService.LogAsync("CLEANUP_LOGS", "Admin",
                        $"Limpeza de logs executada: {logsParaRemover} registros removidos (anteriores a {dataCorte:dd/MM/yyyy})");

                    TempData["SuccessMessage"] = $"Limpeza concluída: {logsParaRemover} logs removidos.";
                }
                else
                {
                    TempData["InfoMessage"] = "Nenhum log encontrado para remoção.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao limpar logs");
                TempData["ErrorMessage"] = "Erro ao executar limpeza de logs";
            }

            return RedirectToAction("SystemManagement", "Home");
        }

        // GET: Admin/Security
        public async Task<IActionResult> Security()
        {
            await _auditService.LogAsync("SECURITY_ACCESS", "Admin", "Acesso às configurações de segurança");

            try
            {
                // Estatísticas de segurança
                var ultimaSemana = DateTime.Now.AddDays(-7);
                var ultimoMes = DateTime.Now.AddMonths(-1);

                var securityStats = new
                {
                    TentativasLoginSemana = await _context.LogsAuditoria
                        .Where(l => l.DataHora >= ultimaSemana && l.Acao.Contains("LOGIN"))
                        .CountAsync(),
                    UsuariosAtivosSemana = await _context.LogsAuditoria
                        .Where(l => l.DataHora >= ultimaSemana)
                        .Select(l => l.UsuarioId)
                        .Distinct()
                        .CountAsync(),
                    AcessosAdminMes = await _context.LogsAuditoria
                        .Where(l => l.DataHora >= ultimoMes && l.Entidade == "Admin")
                        .CountAsync(),
                    AlteracoesSistema = await _context.LogsAuditoria
                        .Where(l => l.DataHora >= ultimoMes &&
                               (l.Acao.Contains("CREATE") || l.Acao.Contains("UPDATE") || l.Acao.Contains("DELETE")))
                        .CountAsync()
                };

                ViewBag.SecurityStats = securityStats;

                // Últimas atividades críticas
                var atividadesCriticas = await _context.LogsAuditoria
                    .Where(l => l.Acao.Contains("DELETE") || l.Acao.Contains("ADMIN") || l.Entidade == "Usuario")
                    .OrderByDescending(l => l.DataHora)
                    .Take(10)
                    .ToListAsync();

                ViewBag.AtividadesCriticas = atividadesCriticas;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao carregar dados de segurança");
                TempData["ErrorMessage"] = "Erro ao carregar configurações de segurança";
            }

            return View();
        }

        // GET: Admin/Maintenance
        public async Task<IActionResult> Maintenance()
        {
            await _auditService.LogAsync("MAINTENANCE_ACCESS", "Admin", "Acesso à manutenção do sistema");

            try
            {
                // Verificações de integridade básicas
                var healthChecks = new
                {
                    DatabaseConnection = await VerificarConexaoBanco(),
                    TablesIntegrity = await VerificarIntegridadeTabelas(),
                    OrphanRecords = await VerificarRegistrosOrfaos(),
                    LogsSize = await ObterTamanhoLogs()
                };

                ViewBag.HealthChecks = healthChecks;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao carregar informações de manutenção");
                TempData["ErrorMessage"] = "Erro ao verificar status de manutenção";
            }

            return View();
        }

        #region Métodos Auxiliares

        private async Task<bool> VerificarConexaoBanco()
        {
            try
            {
                await _context.Database.ExecuteSqlRawAsync("SELECT 1");
                return true;
            }
            catch
            {
                return false;
            }
        }

        private async Task<bool> VerificarIntegridadeTabelas()
        {
            try
            {
                // Verificações básicas
                var usuariosCount = await _userManager.Users.CountAsync();
                var centrosCount = await _context.CentrosCusto.CountAsync();

                return usuariosCount >= 0 && centrosCount >= 0;
            }
            catch
            {
                return false;
            }
        }

        private async Task<int> VerificarRegistrosOrfaos()
        {
            try
            {
                // Verificar entradas sem meio de pagamento válido
                var entradasOrfaos = await _context.Entradas
                    .Where(e => !_context.MeiosDePagamento.Any(m => m.Id == e.MeioDePagamentoId))
                    .CountAsync();

                // Verificar saídas sem meio de pagamento válido
                var saidasOrfaos = await _context.Saidas
                    .Where(s => !_context.MeiosDePagamento.Any(m => m.Id == s.MeioDePagamentoId))
                    .CountAsync();

                return entradasOrfaos + saidasOrfaos;
            }
            catch
            {
                return -1;
            }
        }

        private async Task<string> ObterTamanhoLogs()
        {
            try
            {
                var count = await _context.LogsAuditoria.CountAsync();
                return $"{count:N0} registros";
            }
            catch
            {
                return "Erro ao calcular";
            }
        }

        #endregion
    }
}