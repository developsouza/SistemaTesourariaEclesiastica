using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaTesourariaEclesiastica.Attributes;
using SistemaTesourariaEclesiastica.Data;
using SistemaTesourariaEclesiastica.Enums;
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
        private readonly LancamentoAprovadoService _lancamentoAprovadoService;


        public HomeController(
            ILogger<HomeController> logger,
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            AuditService auditService,
            LancamentoAprovadoService lancamentoAprovadoService)
        {
            _logger = logger;
            _context = context;
            _userManager = userManager;
            _auditService = auditService;
            _lancamentoAprovadoService = lancamentoAprovadoService;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                // =====================================================
                // 1. VERIFICAR USUÁRIO E AUTENTICAÇÃO
                // =====================================================
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    _logger.LogWarning("Usuário não encontrado, redirecionando para login");
                    return RedirectToAction("Login", "Account");
                }

                var roles = await _userManager.GetRolesAsync(user);
                var primaryRole = roles.FirstOrDefault() ?? "Sem Perfil";

                _logger.LogInformation($"Dashboard acessado por: {user.NomeCompleto} ({primaryRole})");

                // =====================================================
                // 2. LOG DE AUDITORIA (com proteção contra erro)
                // =====================================================
                try
                {
                    await _auditService.LogAsync("DASHBOARD_ACCESS", "Home",
                        $"Acesso ao dashboard por {user.NomeCompleto} ({primaryRole})");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Erro ao registrar auditoria (não crítico)");
                }

                // =====================================================
                // 3. VERIFICAR SE AS TABELAS EXISTEM
                // =====================================================
                var tabelasExistem = await VerificarTabelas();
                if (!tabelasExistem)
                {
                    _logger.LogWarning("Tabelas do banco não existem, redirecionando para configuração");
                    TempData["Aviso"] = "O banco de dados não foi inicializado. Execute: dotnet ef database update";
                    return View(await ObterDadosVazios(user, primaryRole));
                }

                // =====================================================
                // 4. CONFIGURAR PERÍODO E DATAS
                // =====================================================
                var hoje = DateTime.Now.Date;
                var inicioMes = new DateTime(hoje.Year, hoje.Month, 1);
                var fimMes = inicioMes.AddMonths(1).AddDays(-1);

                // =====================================================
                // 5. DETERMINAR CENTRO DE CUSTO BASEADO NO PERFIL
                // =====================================================
                int? centroCustoFiltro = null;

                if (!User.IsInRole(Roles.Administrador) && !User.IsInRole(Roles.TesoureiroGeral))
                {
                    if (user.CentroCustoId.HasValue)
                    {
                        centroCustoFiltro = user.CentroCustoId.Value;
                    }
                    else
                    {
                        _logger.LogWarning($"Usuário {user.NomeCompleto} sem Centro de Custo definido");
                        // Usuário sem centro de custo verá dados vazios
                        ViewBag.EntradasMes = "R$ 0,00";
                        ViewBag.SaidasMes = "R$ 0,00";
                        ViewBag.SaldoTotal = "R$ 0,00";
                        ViewBag.DizimosMes = "R$ 0,00";
                        ViewBag.FluxoCaixaData = new List<object>();
                        ViewBag.DespesasData = new List<object>();
                        ViewBag.UserRole = primaryRole;
                        ViewBag.UserName = user.NomeCompleto;
                        ViewBag.CentroCusto = "Não definido";
                        ViewBag.ShowFullData = false;
                        ViewBag.CanManageOperations = false;
                        ViewBag.CanApproveClosures = false;
                        ViewBag.CanViewReports = true;
                        ViewBag.Alertas = new List<object>();
                        ViewBag.AtividadesRecentes = new List<object>();

                        TempData["Aviso"] = "Seu usuário não possui um Centro de Custo definido. Entre em contato com o administrador.";
                        return View();
                    }
                }

                // =====================================================
                // 6. BUSCAR IDs DE LANÇAMENTOS APROVADOS (MÊS ATUAL)
                // =====================================================
                List<int> idsEntradasAprovadasMes = new List<int>();
                List<int> idsSaidasAprovadasMes = new List<int>();

                try
                {
                    // Buscar entradas que estão em fechamentos aprovados do mês
                    var queryEntradasMes = _context.Entradas
                        .Where(e => e.Data >= inicioMes && e.Data <= fimMes);

                    if (centroCustoFiltro.HasValue)
                    {
                        queryEntradasMes = queryEntradasMes.Where(e => e.CentroCustoId == centroCustoFiltro.Value);
                    }

                    idsEntradasAprovadasMes = await queryEntradasMes
                        .Where(e => _context.FechamentosPeriodo.Any(f =>
                            f.CentroCustoId == e.CentroCustoId &&
                            f.Status == StatusFechamentoPeriodo.Aprovado &&
                            e.Data >= f.DataInicio &&
                            e.Data <= f.DataFim))
                        .Select(e => e.Id)
                        .ToListAsync();

                    // Buscar saídas que estão em fechamentos aprovados do mês
                    var querySaidasMes = _context.Saidas
                        .Where(s => s.Data >= inicioMes && s.Data <= fimMes);

                    if (centroCustoFiltro.HasValue)
                    {
                        querySaidasMes = querySaidasMes.Where(s => s.CentroCustoId == centroCustoFiltro.Value);
                    }

                    idsSaidasAprovadasMes = await querySaidasMes
                        .Where(s => _context.FechamentosPeriodo.Any(f =>
                            f.CentroCustoId == s.CentroCustoId &&
                            f.Status == StatusFechamentoPeriodo.Aprovado &&
                            s.Data >= f.DataInicio &&
                            s.Data <= f.DataFim))
                        .Select(s => s.Id)
                        .ToListAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao buscar IDs de lançamentos aprovados");
                }

                // =====================================================
                // 7. CALCULAR ESTATÍSTICAS DO MÊS (APENAS APROVADOS)
                // =====================================================
                decimal entradasMes = 0, saidasMes = 0, dizimosMes = 0;

                try
                {
                    // Total de entradas APROVADAS do mês
                    if (idsEntradasAprovadasMes.Any())
                    {
                        entradasMes = await _context.Entradas
                            .Where(e => idsEntradasAprovadasMes.Contains(e.Id))
                            .SumAsync(e => (decimal?)e.Valor) ?? 0;

                        // Dízimos APROVADOS do mês
                        dizimosMes = await _context.Entradas
                            .Include(e => e.PlanoDeContas)
                            .Where(e => idsEntradasAprovadasMes.Contains(e.Id) &&
                                       e.PlanoDeContas != null &&
                                       e.PlanoDeContas.Nome.ToLower().Contains("dízimo"))
                            .SumAsync(e => (decimal?)e.Valor) ?? 0;
                    }

                    // Total de saídas APROVADAS do mês
                    if (idsSaidasAprovadasMes.Any())
                    {
                        saidasMes = await _context.Saidas
                            .Where(s => idsSaidasAprovadasMes.Contains(s.Id))
                            .SumAsync(s => (decimal?)s.Valor) ?? 0;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao calcular estatísticas do mês");
                }

                // =====================================================
                // 8. CALCULAR SALDO TOTAL (APROVADOS DE TODOS OS TEMPOS)
                // =====================================================
                decimal totalEntradas = 0, totalSaidas = 0;

                try
                {
                    // Buscar TODAS as entradas aprovadas (histórico completo)
                    var queryTodasEntradas = _context.Entradas.AsQueryable();

                    if (centroCustoFiltro.HasValue)
                    {
                        queryTodasEntradas = queryTodasEntradas.Where(e => e.CentroCustoId == centroCustoFiltro.Value);
                    }

                    var idsTodasEntradasAprovadas = await queryTodasEntradas
                        .Where(e => _context.FechamentosPeriodo.Any(f =>
                            f.CentroCustoId == e.CentroCustoId &&
                            f.Status == StatusFechamentoPeriodo.Aprovado &&
                            e.Data >= f.DataInicio &&
                            e.Data <= f.DataFim))
                        .Select(e => e.Id)
                        .ToListAsync();

                    if (idsTodasEntradasAprovadas.Any())
                    {
                        totalEntradas = await _context.Entradas
                            .Where(e => idsTodasEntradasAprovadas.Contains(e.Id))
                            .SumAsync(e => (decimal?)e.Valor) ?? 0;
                    }

                    // Buscar TODAS as saídas aprovadas (histórico completo)
                    var queryTodasSaidas = _context.Saidas.AsQueryable();

                    if (centroCustoFiltro.HasValue)
                    {
                        queryTodasSaidas = queryTodasSaidas.Where(s => s.CentroCustoId == centroCustoFiltro.Value);
                    }

                    var idsTodasSaidasAprovadas = await queryTodasSaidas
                        .Where(s => _context.FechamentosPeriodo.Any(f =>
                            f.CentroCustoId == s.CentroCustoId &&
                            f.Status == StatusFechamentoPeriodo.Aprovado &&
                            s.Data >= f.DataInicio &&
                            s.Data <= f.DataFim))
                        .Select(s => s.Id)
                        .ToListAsync();

                    if (idsTodasSaidasAprovadas.Any())
                    {
                        totalSaidas = await _context.Saidas
                            .Where(s => idsTodasSaidasAprovadas.Contains(s.Id))
                            .SumAsync(s => (decimal?)s.Valor) ?? 0;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao calcular totais históricos");
                }

                var saldoTotal = totalEntradas - totalSaidas;

                // =====================================================
                // 9. DADOS PARA GRÁFICO DE FLUXO DE CAIXA (ÚLTIMOS 6 MESES - APROVADOS)
                // =====================================================
                var fluxoCaixaData = new List<object>();

                try
                {
                    for (int i = 5; i >= 0; i--)
                    {
                        var mesInicio = hoje.AddMonths(-i).AddDays(-(hoje.Day - 1));
                        var mesFim = mesInicio.AddMonths(1).AddDays(-1);

                        // Buscar entradas aprovadas deste mês específico
                        var queryEntradasGrafico = _context.Entradas
                            .Where(e => e.Data >= mesInicio && e.Data <= mesFim);

                        if (centroCustoFiltro.HasValue)
                        {
                            queryEntradasGrafico = queryEntradasGrafico.Where(e => e.CentroCustoId == centroCustoFiltro.Value);
                        }

                        var idsEntradasMesGrafico = await queryEntradasGrafico
                            .Where(e => _context.FechamentosPeriodo.Any(f =>
                                f.CentroCustoId == e.CentroCustoId &&
                                f.Status == StatusFechamentoPeriodo.Aprovado &&
                                e.Data >= f.DataInicio &&
                                e.Data <= f.DataFim))
                            .Select(e => e.Id)
                            .ToListAsync();

                        var entradasMesGrafico = idsEntradasMesGrafico.Any()
                            ? await _context.Entradas
                                .Where(e => idsEntradasMesGrafico.Contains(e.Id))
                                .SumAsync(e => (decimal?)e.Valor) ?? 0
                            : 0;

                        // Buscar saídas aprovadas deste mês específico
                        var querySaidasGrafico = _context.Saidas
                            .Where(s => s.Data >= mesInicio && s.Data <= mesFim);

                        if (centroCustoFiltro.HasValue)
                        {
                            querySaidasGrafico = querySaidasGrafico.Where(s => s.CentroCustoId == centroCustoFiltro.Value);
                        }

                        var idsSaidasMesGrafico = await querySaidasGrafico
                            .Where(s => _context.FechamentosPeriodo.Any(f =>
                                f.CentroCustoId == s.CentroCustoId &&
                                f.Status == StatusFechamentoPeriodo.Aprovado &&
                                s.Data >= f.DataInicio &&
                                s.Data <= f.DataFim))
                            .Select(s => s.Id)
                            .ToListAsync();

                        var saidasMesGrafico = idsSaidasMesGrafico.Any()
                            ? await _context.Saidas
                                .Where(s => idsSaidasMesGrafico.Contains(s.Id))
                                .SumAsync(s => (decimal?)s.Valor) ?? 0
                            : 0;

                        fluxoCaixaData.Add(new
                        {
                            mes = mesInicio.ToString("MMM", new System.Globalization.CultureInfo("pt-BR")),
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

                // =====================================================
                // 10. DESPESAS POR CATEGORIA (ÚLTIMOS 3 MESES - APROVADAS)
                // =====================================================
                var despesasPorCategoria = new List<object>();

                try
                {
                    var tresMesesAtras = hoje.AddMonths(-3);

                    var querySaidasCategoria = _context.Saidas
                        .Where(s => s.Data >= tresMesesAtras);

                    if (centroCustoFiltro.HasValue)
                    {
                        querySaidasCategoria = querySaidasCategoria.Where(s => s.CentroCustoId == centroCustoFiltro.Value);
                    }

                    var idsSaidasCategoria = await querySaidasCategoria
                        .Where(s => _context.FechamentosPeriodo.Any(f =>
                            f.CentroCustoId == s.CentroCustoId &&
                            f.Status == StatusFechamentoPeriodo.Aprovado &&
                            s.Data >= f.DataInicio &&
                            s.Data <= f.DataFim))
                        .Select(s => s.Id)
                        .ToListAsync();

                    if (idsSaidasCategoria.Any())
                    {
                        despesasPorCategoria = await _context.Saidas
                            .Include(s => s.PlanoDeContas)
                            .Where(s => idsSaidasCategoria.Contains(s.Id) && s.PlanoDeContas != null)
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
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao gerar dados de despesas por categoria");
                }

                // =====================================================
                // 11. PREENCHER VIEWBAG COM TODOS OS DADOS
                // =====================================================
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

                // Listas vazias (para compatibilidade com a view)
                ViewBag.Alertas = new List<object>();
                ViewBag.AtividadesRecentes = new List<object>();

                _logger.LogInformation($"Dashboard carregado com sucesso para {user.NomeCompleto}");

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
            ViewBag.UserCentroCustoId = user?.CentroCustoId;

            // Carregar opções de centros de custo se o usuário pode ver todos os dados
            if (canViewAllData)
            {
                try
                {
                    var centrosCusto = await _context.CentrosCusto
                        .Where(c => c.Ativo)
                        .OrderBy(c => c.Nome)
                        .Select(c => new { c.Id, c.Nome })
                        .ToListAsync();

                    ViewBag.CentrosCusto = centrosCusto;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao carregar centros de custo");
                    ViewBag.CentrosCusto = new List<object>();
                }
            }

            return View();
        }

        [AuthorizeRoles(Roles.AdminOnly)]
        public async Task<IActionResult> SystemManagement()
        {
            var user = await _userManager.GetUserAsync(User);
            await _auditService.LogAsync("SYSTEM_MANAGEMENT_ACCESS", "Home",
                "Acesso ao painel de administração do sistema");

            try
            {
                var totalUsuarios = await _userManager.Users.CountAsync();
                var usuariosAtivos = await _userManager.Users.Where(u => u.Ativo).CountAsync();
                var totalCentrosCusto = await _context.CentrosCusto.CountAsync();

                // Estatísticas adicionais
                var totalEntradas = await _context.Entradas.CountAsync();
                var totalSaidas = await _context.Saidas.CountAsync();
                var totalFechamentos = await _context.FechamentosPeriodo.CountAsync();
                var ultimoBackup = "Nunca realizado"; // Implementar quando tiver backup
                var versaoSistema = "1.0.0";

                ViewBag.TotalUsuarios = totalUsuarios;
                ViewBag.UsuariosAtivos = usuariosAtivos;
                ViewBag.TotalCentrosCusto = totalCentrosCusto;
                ViewBag.TotalEntradas = totalEntradas;
                ViewBag.TotalSaidas = totalSaidas;
                ViewBag.TotalFechamentos = totalFechamentos;
                ViewBag.UltimoBackup = ultimoBackup;
                ViewBag.VersaoSistema = versaoSistema;
                ViewBag.ServidorNome = Environment.MachineName;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao carregar dados de administração");
                TempData["ErrorMessage"] = "Erro ao carregar estatísticas do sistema";

                // Valores padrão em caso de erro
                ViewBag.TotalUsuarios = 0;
                ViewBag.UsuariosAtivos = 0;
                ViewBag.TotalCentrosCusto = 0;
            }

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