using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaTesourariaEclesiastica.Attributes;
using SistemaTesourariaEclesiastica.Data;
using SistemaTesourariaEclesiastica.Models;
using SistemaTesourariaEclesiastica.Services;
using System.Diagnostics;

namespace SistemaTesourariaEclesiastica.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AuditService _auditService;

        public HomeController(
            ILogger<HomeController> logger,
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            AuditService auditService)
        {
            _logger = logger;
            _context = context;
            _userManager = userManager;
            _auditService = auditService;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                // 1. Verificar usuário
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    _logger.LogWarning("Usuário não encontrado, redirecionando para login");
                    return RedirectToAction("Login", "Account");
                }

                var roles = await _userManager.GetRolesAsync(user);
                var primaryRole = roles.FirstOrDefault() ?? "Sem Perfil";

                _logger.LogInformation($"Dashboard acessado por: {user.NomeCompleto} ({primaryRole})");

                // 2. Log de auditoria (com proteção contra erro)
                try
                {
                    await _auditService.LogAsync("DASHBOARD_ACCESS", "Home",
                        $"Acesso ao dashboard por {user.NomeCompleto} ({primaryRole})");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Erro ao registrar auditoria (não crítico)");
                }

                // 3. Verificar se as tabelas existem
                var tabelasExistem = await VerificarTabelas();
                if (!tabelasExistem)
                {
                    _logger.LogWarning("Tabelas do banco não existem, redirecionando para configuração");
                    TempData["Aviso"] = "O banco de dados não foi inicializado. Execute: dotnet ef database update";
                    return View(await ObterDadosVazios(user, primaryRole));
                }

                var hoje = DateTime.Now.Date;
                var inicioMes = new DateTime(hoje.Year, hoje.Month, 1);
                var fimMes = inicioMes.AddMonths(1).AddDays(-1);

                // 4. Buscar dados com segurança
                IQueryable<Entrada> entradasQuery = _context.Entradas;
                IQueryable<Saida> saidasQuery = _context.Saidas;

                // Aplicar filtros baseados no perfil
                if (!User.IsInRole(Roles.Administrador) && !User.IsInRole(Roles.TesoureiroGeral))
                {
                    if (user.CentroCustoId.HasValue)
                    {
                        entradasQuery = entradasQuery.Where(e => e.CentroCustoId == user.CentroCustoId.Value);
                        saidasQuery = saidasQuery.Where(s => s.CentroCustoId == user.CentroCustoId.Value);
                    }
                    else
                    {
                        _logger.LogWarning($"Usuário {user.NomeCompleto} sem Centro de Custo definido");
                        entradasQuery = entradasQuery.Where(e => false);
                        saidasQuery = saidasQuery.Where(s => false);
                    }
                }

                // 5. Estatísticas do mês (com proteção)
                decimal entradasMes = 0, saidasMes = 0, totalEntradas = 0, totalSaidas = 0, dizimosMes = 0;

                try
                {
                    entradasMes = await entradasQuery
                        .Where(e => e.Data >= inicioMes && e.Data <= fimMes)
                        .SumAsync(e => (decimal?)e.Valor) ?? 0;

                    saidasMes = await saidasQuery
                        .Where(s => s.Data >= inicioMes && s.Data <= fimMes)
                        .SumAsync(s => (decimal?)s.Valor) ?? 0;

                    totalEntradas = await entradasQuery.SumAsync(e => (decimal?)e.Valor) ?? 0;
                    totalSaidas = await saidasQuery.SumAsync(s => (decimal?)s.Valor) ?? 0;

                    // Dízimos (com proteção adicional)
                    dizimosMes = await entradasQuery
                        .Include(e => e.PlanoDeContas)
                        .Where(e => e.Data >= inicioMes && e.Data <= fimMes &&
                                   e.PlanoDeContas != null &&
                                   e.PlanoDeContas.Nome.ToLower().Contains("dízimo"))
                        .SumAsync(e => (decimal?)e.Valor) ?? 0;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao calcular estatísticas do mês");
                }

                var saldoTotal = totalEntradas - totalSaidas;

                // 6. Dados para gráfico de fluxo de caixa
                var fluxoCaixaData = new List<object>();

                try
                {
                    for (int i = 5; i >= 0; i--)
                    {
                        var mesInicio = hoje.AddMonths(-i).AddDays(-(hoje.Day - 1));
                        var mesFim = mesInicio.AddMonths(1).AddDays(-1);

                        var entradasMesGrafico = await entradasQuery
                            .Where(e => e.Data >= mesInicio && e.Data <= mesFim)
                            .SumAsync(e => (decimal?)e.Valor) ?? 0;

                        var saidasMesGrafico = await saidasQuery
                            .Where(s => s.Data >= mesInicio && s.Data <= mesFim)
                            .SumAsync(s => (decimal?)s.Valor) ?? 0;

                        fluxoCaixaData.Add(new
                        {
                            mes = mesInicio.ToString("MMM"),
                            entradas = entradasMesGrafico,
                            saidas = saidasMesGrafico,
                            saldo = entradasMesGrafico - saidasMesGrafico
                        });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao gerar dados de fluxo de caixa");
                }

                // 7. Despesas por categoria
                var despesasPorCategoria = new List<object>();

                try
                {
                    var tresMesesAtras = hoje.AddMonths(-3);
                    despesasPorCategoria = await saidasQuery
                        .Include(s => s.PlanoDeContas)
                        .Where(s => s.Data >= tresMesesAtras && s.PlanoDeContas != null)
                        .GroupBy(s => s.PlanoDeContas.Nome)
                        .Select(g => new
                        {
                            categoria = g.Key ?? "Sem Categoria",
                            valor = g.Sum(s => s.Valor)
                        })
                        .OrderByDescending(x => x.valor)
                        .Take(5)
                        .ToListAsync<object>();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao gerar dados de despesas por categoria");
                }

                // 8. Preencher ViewBag
                ViewBag.EntradasMes = entradasMes.ToString("C");
                ViewBag.SaidasMes = saidasMes.ToString("C");
                ViewBag.SaldoTotal = saldoTotal.ToString("C");
                ViewBag.DizimosMes = dizimosMes.ToString("C");
                ViewBag.FluxoCaixaData = fluxoCaixaData;
                ViewBag.DespesasData = despesasPorCategoria;
                ViewBag.UserRole = primaryRole;
                ViewBag.UserName = user.NomeCompleto;
                ViewBag.CentroCusto = user.CentroCusto?.Nome ?? "Não definido";

                // Permissões
                ViewBag.ShowFullData = User.IsInRole(Roles.Administrador) || User.IsInRole(Roles.TesoureiroGeral);
                ViewBag.CanManageOperations = User.IsInRole(Roles.Administrador) ||
                                             User.IsInRole(Roles.TesoureiroGeral) ||
                                             User.IsInRole(Roles.TesoureiroLocal);
                ViewBag.CanApproveClosures = User.IsInRole(Roles.Administrador) || User.IsInRole(Roles.TesoureiroGeral);
                ViewBag.CanViewReports = true;

                // Listas vazias
                ViewBag.Alertas = new List<object>();
                ViewBag.AtividadesRecentes = new List<object>();

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERRO CRÍTICO ao carregar dashboard");
                _logger.LogError($"Mensagem: {ex.Message}");
                _logger.LogError($"StackTrace: {ex.StackTrace}");

                if (ex.InnerException != null)
                {
                    _logger.LogError($"InnerException: {ex.InnerException.Message}");
                }

                TempData["Erro"] = $"Erro ao carregar o dashboard: {ex.Message}. Verifique os logs para mais detalhes.";

                return View("Error", new ErrorViewModel
                {
                    RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
                });
            }
        }

        private async Task<bool> VerificarTabelas()
        {
            try
            {
                // Tentar fazer uma consulta simples para verificar se as tabelas existem
                await _context.Entradas.AnyAsync();
                await _context.Saidas.AnyAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Tabelas não existem ou banco não foi criado");
                return false;
            }
        }

        private async Task<object> ObterDadosVazios(ApplicationUser user, string primaryRole)
        {
            ViewBag.EntradasMes = "R$ 0,00";
            ViewBag.SaidasMes = "R$ 0,00";
            ViewBag.SaldoTotal = "R$ 0,00";
            ViewBag.DizimosMes = "R$ 0,00";
            ViewBag.FluxoCaixaData = new List<object>();
            ViewBag.DespesasData = new List<object>();
            ViewBag.UserRole = primaryRole;
            ViewBag.UserName = user.NomeCompleto;
            ViewBag.CentroCusto = user.CentroCusto?.Nome ?? "Não definido";
            ViewBag.ShowFullData = User.IsInRole(Roles.Administrador) || User.IsInRole(Roles.TesoureiroGeral);
            ViewBag.CanManageOperations = User.IsInRole(Roles.Administrador) ||
                                         User.IsInRole(Roles.TesoureiroGeral) ||
                                         User.IsInRole(Roles.TesoureiroLocal);
            ViewBag.CanApproveClosures = User.IsInRole(Roles.Administrador) || User.IsInRole(Roles.TesoureiroGeral);
            ViewBag.CanViewReports = true;
            ViewBag.Alertas = new List<object>();
            ViewBag.AtividadesRecentes = new List<object>();

            return new { };
        }

        [AuthorizeRoles(Roles.TodosComAcessoRelatorios)]
        public async Task<IActionResult> Reports()
        {
            var user = await _userManager.GetUserAsync(User);
            await _auditService.LogAsync("REPORTS_ACCESS", "Home", "Acesso à página de relatórios");

            var canViewAllData = User.IsInRole(Roles.Administrador) || User.IsInRole(Roles.TesoureiroGeral);

            ViewBag.CanViewAllData = canViewAllData;
            ViewBag.UserCentroCustoId = user.CentroCustoId;

            return View();
        }

        [AuthorizeRoles(Roles.AdminOnly)]
        public async Task<IActionResult> SystemManagement()
        {
            var user = await _userManager.GetUserAsync(User);
            await _auditService.LogAsync("SYSTEM_MANAGEMENT_ACCESS", "Home",
                "Acesso ao painel de administração do sistema");

            var totalUsuarios = await _userManager.Users.CountAsync();
            var usuariosAtivos = await _userManager.Users.Where(u => u.Ativo).CountAsync();
            var totalCentrosCusto = await _context.CentrosCusto.CountAsync();

            ViewBag.TotalUsuarios = totalUsuarios;
            ViewBag.UsuariosAtivos = usuariosAtivos;
            ViewBag.TotalCentrosCusto = totalCentrosCusto;

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpGet]
        public async Task<IActionResult> GetDashboardStats()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                var hoje = DateTime.Now.Date;
                var inicioMes = new DateTime(hoje.Year, hoje.Month, 1);

                IQueryable<Entrada> entradasQuery = _context.Entradas;
                IQueryable<Saida> saidasQuery = _context.Saidas;

                if (!User.IsInRole(Roles.Administrador) && !User.IsInRole(Roles.TesoureiroGeral))
                {
                    if (user.CentroCustoId.HasValue)
                    {
                        entradasQuery = entradasQuery.Where(e => e.CentroCustoId == user.CentroCustoId.Value);
                        saidasQuery = saidasQuery.Where(s => s.CentroCustoId == user.CentroCustoId.Value);
                    }
                }

                var stats = new
                {
                    entradasHoje = await entradasQuery.Where(e => e.Data.Date == hoje).SumAsync(e => (decimal?)e.Valor) ?? 0,
                    saidasHoje = await saidasQuery.Where(s => s.Data.Date == hoje).SumAsync(s => (decimal?)s.Valor) ?? 0,
                    entradasMes = await entradasQuery.Where(e => e.Data >= inicioMes).SumAsync(e => (decimal?)e.Valor) ?? 0,
                    saidasMes = await saidasQuery.Where(s => s.Data >= inicioMes).SumAsync(s => (decimal?)s.Valor) ?? 0
                };

                return Json(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter estatísticas do dashboard");
                return Json(new { error = "Erro ao carregar dados" });
            }
        }
    }
}