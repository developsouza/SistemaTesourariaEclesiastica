using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SistemaTesourariaEclesiastica.Models;
using SistemaTesourariaEclesiastica.Data;
using SistemaTesourariaEclesiastica.ViewModels;
using SistemaTesourariaEclesiastica.Attributes;
using SistemaTesourariaEclesiastica.Services;

namespace SistemaTesourariaEclesiastica.Controllers
{
    [Authorize] // Requer autenticação para todo o controller
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
                var user = await _userManager.GetUserAsync(User);
                var roles = await _userManager.GetRolesAsync(user);
                var primaryRole = roles.FirstOrDefault() ?? "Sem Perfil";

                // Log de acesso ao dashboard
                await _auditService.LogAsync("DASHBOARD_ACCESS", "Home",
                    $"Acesso ao dashboard por {user.NomeCompleto} ({primaryRole})");

                var hoje = DateTime.Now.Date;
                var inicioMes = new DateTime(hoje.Year, hoje.Month, 1);
                var fimMes = inicioMes.AddMonths(1).AddDays(-1);

                // Filtrar dados baseado no perfil do usuário
                IQueryable<Entrada> entradasQuery = _context.Entradas.AsQueryable();
                IQueryable<Saida> saidasQuery = _context.Saidas.AsQueryable();

                // Aplicar filtros baseados no centro de custo para usuários não-administrativos
                if (!User.IsInRole(Roles.Administrador) && !User.IsInRole(Roles.TesoureiroGeral))
                {
                    if (user.CentroCustoId.HasValue)
                    {
                        entradasQuery = entradasQuery.Where(e => e.CentroCustoId == user.CentroCustoId.Value);
                        saidasQuery = saidasQuery.Where(s => s.CentroCustoId == user.CentroCustoId.Value);
                    }
                    else
                    {
                        // Se não tem centro de custo definido, não mostra nenhum dado
                        entradasQuery = entradasQuery.Where(e => false);
                        saidasQuery = saidasQuery.Where(s => false);
                    }
                }

                // Estatísticas do mês atual
                var entradasMes = await entradasQuery
                    .Where(e => e.Data >= inicioMes && e.Data <= fimMes)
                    .SumAsync(e => e.Valor);

                var saidasMes = await saidasQuery
                    .Where(s => s.Data >= inicioMes && s.Data <= fimMes)
                    .SumAsync(s => s.Valor);

                // Saldo total baseado no perfil
                var totalEntradas = await entradasQuery.SumAsync(e => e.Valor);
                var totalSaidas = await saidasQuery.SumAsync(s => s.Valor);
                var saldoTotal = totalEntradas - totalSaidas;

                // Dízimos do mês (assumindo que existe um plano de contas específico para dízimos)
                var dizimosMes = await entradasQuery
                    .Include(e => e.PlanoDeContas)
                    .Where(e => e.Data >= inicioMes && e.Data <= fimMes &&
                               e.PlanoDeContas.Nome.ToLower().Contains("dízimo"))
                    .SumAsync(e => e.Valor);

                // Dados para gráfico de fluxo de caixa (últimos 6 meses)
                var fluxoCaixaData = new List<object>();

                for (int i = 5; i >= 0; i--)
                {
                    var mesInicio = hoje.AddMonths(-i).AddDays(-(hoje.Day - 1));
                    var mesFim = mesInicio.AddMonths(1).AddDays(-1);

                    var entradasMesGrafico = await entradasQuery
                        .Where(e => e.Data >= mesInicio && e.Data <= mesFim)
                        .SumAsync(e => e.Valor);

                    var saidasMesGrafico = await saidasQuery
                        .Where(s => s.Data >= mesInicio && s.Data <= mesFim)
                        .SumAsync(s => s.Valor);

                    fluxoCaixaData.Add(new
                    {
                        mes = mesInicio.ToString("MMM"),
                        entradas = entradasMesGrafico,
                        saidas = saidasMesGrafico,
                        saldo = entradasMesGrafico - saidasMesGrafico
                    });
                }

                // Estatísticas por categoria de despesa (últimos 3 meses)
                var tresMesesAtras = hoje.AddMonths(-3);
                var despesasPorCategoria = await saidasQuery
                    .Include(s => s.PlanoDeContas)
                    .Where(s => s.Data >= tresMesesAtras)
                    .GroupBy(s => s.PlanoDeContas.Nome)
                    .Select(g => new
                    {
                        categoria = g.Key,
                        valor = g.Sum(s => s.Valor)
                    })
                    .OrderByDescending(x => x.valor)
                    .Take(5)
                    .ToListAsync();

                // Definir dados da ViewBag baseados no perfil
                ViewBag.EntradasMes = entradasMes.ToString("C");
                ViewBag.SaidasMes = saidasMes.ToString("C");
                ViewBag.SaldoTotal = saldoTotal.ToString("C");
                ViewBag.Dizimo = dizimosMes.ToString("C");
                ViewBag.FluxoCaixaData = System.Text.Json.JsonSerializer.Serialize(fluxoCaixaData);
                ViewBag.DespesasData = System.Text.Json.JsonSerializer.Serialize(despesasPorCategoria);
                ViewBag.UserRole = primaryRole;
                ViewBag.UserName = user.NomeCompleto;
                ViewBag.CentroCusto = user.CentroCusto?.Nome ?? "Não definido";

                // Informações específicas por perfil
                ViewBag.ShowFullData = User.IsInRole(Roles.Administrador) || User.IsInRole(Roles.TesoureiroGeral);
                ViewBag.CanManageOperations = User.IsInRole(Roles.Administrador) ||
                                             User.IsInRole(Roles.TesoureiroGeral) ||
                                             User.IsInRole(Roles.TesoureiroLocal);
                ViewBag.CanApproveClosures = User.IsInRole(Roles.Administrador) || User.IsInRole(Roles.TesoureiroGeral);
                ViewBag.CanViewReports = true; // Todos os perfis podem ver relatórios

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao carregar dashboard para usuário {UserId}", _userManager.GetUserId(User));
                return View("Error", new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
            }
        }

        [AuthorizeRoles(Roles.TodosComAcessoRelatorios)]
        public async Task<IActionResult> Reports()
        {
            var user = await _userManager.GetUserAsync(User);
            await _auditService.LogAsync("REPORTS_ACCESS", "Home", "Acesso à página de relatórios");

            // Lógica específica para relatórios baseada no perfil
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

            // Estatísticas do sistema para administradores
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

        // Action para obter estatísticas via AJAX
        [HttpGet]
        public async Task<IActionResult> GetDashboardStats()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                var hoje = DateTime.Now.Date;
                var inicioMes = new DateTime(hoje.Year, hoje.Month, 1);

                // Aplicar filtros baseados no centro de custo
                IQueryable<Entrada> entradasQuery = _context.Entradas.AsQueryable();
                IQueryable<Saida> saidasQuery = _context.Saidas.AsQueryable();

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
                    entradasHoje = await entradasQuery.Where(e => e.Data.Date == hoje).SumAsync(e => e.Valor),
                    saidasHoje = await saidasQuery.Where(s => s.Data.Date == hoje).SumAsync(s => s.Valor),
                    entradasMes = await entradasQuery.Where(e => e.Data >= inicioMes).SumAsync(e => e.Valor),
                    saidasMes = await saidasQuery.Where(s => s.Data >= inicioMes).SumAsync(s => s.Valor)
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