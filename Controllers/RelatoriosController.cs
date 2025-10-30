using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using SistemaTesourariaEclesiastica.Attributes;
using SistemaTesourariaEclesiastica.Data;
using SistemaTesourariaEclesiastica.Enums;
using SistemaTesourariaEclesiastica.Models;
using SistemaTesourariaEclesiastica.Services;
using SistemaTesourariaEclesiastica.ViewModels;


namespace SistemaTesourariaEclesiastica.Controllers
{
    [Authorize]
    public class RelatoriosController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly AuditService _auditService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly BalanceteService _balanceteService;
        private readonly ILogger<RelatoriosController> _logger;

        public RelatoriosController(
            ApplicationDbContext context,
            AuditService auditService,
            UserManager<ApplicationUser> userManager,
            BalanceteService balanceteService,
            ILogger<RelatoriosController> logger)
        {
            _context = context;
            _auditService = auditService;
            _userManager = userManager;
            _balanceteService = balanceteService;
            _logger = logger;
        }

        // =====================================================
        // INDEX - PÁGINA PRINCIPAL DE RELATÓRIOS
        // =====================================================
        public async Task<IActionResult> Index()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                // Determinar centro de custo para filtro
                int? centroCustoFiltro = null;

                // ✅ CORRIGIDO: Administrador, TesoureiroGeral e Pastor podem ver TODOS os dados
                if (!User.IsInRole(Roles.Administrador) &&
                    !User.IsInRole(Roles.TesoureiroGeral) &&
                    !User.IsInRole(Roles.Pastor))
                {
                    // Apenas Tesoureiro Local precisa de filtro por centro de custo
                    if (user.CentroCustoId.HasValue)
                    {
                        centroCustoFiltro = user.CentroCustoId.Value;
                    }
                    else
                    {
                        ViewBag.TotalEntradas = "R$ 0,00";
                        ViewBag.TotalSaidas = "R$ 0,00";
                        ViewBag.SaldoAtual = "R$ 0,00";
                        ViewBag.EntradasPorCentroCustoLabels = new List<string>();
                        ViewBag.EntradasPorCentroCustoData = new List<decimal>();
                        ViewBag.SaidasPorPlanoContasLabels = new List<string>();
                        ViewBag.SaidasPorPlanoContasData = new List<decimal>();
                        return View();
                    }
                }
                // Administrador, TesoureiroGeral e Pastor: centroCustoFiltro = null (vê tudo)

                // Buscar IDs de entradas aprovadas
                var queryEntradasAprovadas = _context.Entradas.AsQueryable();

                if (centroCustoFiltro.HasValue)
                {
                    queryEntradasAprovadas = queryEntradasAprovadas
                        .Where(e => e.CentroCustoId == centroCustoFiltro.Value);
                }

                var idsEntradasAprovadas = await queryEntradasAprovadas
                    .Where(e => _context.FechamentosPeriodo.Any(f =>
                        f.CentroCustoId == e.CentroCustoId &&
                        f.Status == StatusFechamentoPeriodo.Aprovado &&
                        e.Data >= f.DataInicio &&
                        e.Data <= f.DataFim))
                    .Select(e => e.Id)
                    .ToListAsync();

                // Buscar IDs de saídas aprovadas
                var querySaidasAprovadas = _context.Saidas.AsQueryable();

                if (centroCustoFiltro.HasValue)
                {
                    querySaidasAprovadas = querySaidasAprovadas
                        .Where(s => s.CentroCustoId == centroCustoFiltro.Value);
                }

                var idsSaidasAprovadas = await querySaidasAprovadas
                    .Where(s => _context.FechamentosPeriodo.Any(f =>
                        f.CentroCustoId == s.CentroCustoId &&
                        f.Status == StatusFechamentoPeriodo.Aprovado &&
                        s.Data >= f.DataInicio &&
                        s.Data <= f.DataFim))
                    .Select(s => s.Id)
                    .ToListAsync();

                // Calcular totais
                var totalEntradas = idsEntradasAprovadas.Any()
                    ? await _context.Entradas
                        .Where(e => idsEntradasAprovadas.Contains(e.Id))
                        .SumAsync(e => (decimal?)e.Valor) ?? 0
                    : 0;

                var totalSaidas = idsSaidasAprovadas.Any()
                    ? await _context.Saidas
                        .Where(s => idsSaidasAprovadas.Contains(s.Id))
                        .SumAsync(s => (decimal?)s.Valor) ?? 0
                    : 0;

                var saldoAtual = totalEntradas - totalSaidas;

                ViewBag.TotalEntradas = totalEntradas.ToString("C2");
                ViewBag.TotalSaidas = totalSaidas.ToString("C2");
                ViewBag.SaldoAtual = saldoAtual.ToString("C2");

                // Dados para gráficos - Entradas por Centro de Custo (APROVADAS)
                var entradasPorCentroCusto = new List<(string CentroCusto, decimal Total)>();

                if (idsEntradasAprovadas.Any())
                {
                    entradasPorCentroCusto = await _context.Entradas
                        .Include(e => e.CentroCusto)
                        .Where(e => idsEntradasAprovadas.Contains(e.Id))
                        .GroupBy(e => new { e.CentroCustoId, e.CentroCusto!.Nome })
                        .Select(g => new
                        {
                            CentroCusto = g.Key.Nome ?? "Sem Centro de Custos",
                            Total = g.Sum(e => e.Valor)
                        })
                        .OrderByDescending(x => x.Total)
                        .ToListAsync()
                        .ContinueWith(task => task.Result.Select(x => (x.CentroCusto, x.Total)).ToList());
                }

                ViewBag.EntradasPorCentroCustoLabels = entradasPorCentroCusto.Select(e => e.CentroCusto).ToList();
                ViewBag.EntradasPorCentroCustoData = entradasPorCentroCusto.Select(e => e.Total).ToList();

                // Dados para gráficos - Saídas por Plano de Contas (APROVADAS)
                var saidasPorPlanoContas = new List<(string PlanoContas, decimal Total)>();

                if (idsSaidasAprovadas.Any())
                {
                    saidasPorPlanoContas = await _context.Saidas
                        .Include(s => s.PlanoDeContas)
                        .Where(s => idsSaidasAprovadas.Contains(s.Id))
                        .GroupBy(s => new { Nome = s.PlanoDeContas!.Nome ?? "Sem Plano de Contas" })
                        .Select(g => new { PlanoContas = g.Key.Nome, Total = g.Sum(s => s.Valor) })
                        .OrderByDescending(x => x.Total)
                        .ToListAsync()
                        .ContinueWith(task => task.Result.Select(x => (x.PlanoContas, x.Total)).ToList());
                }

                ViewBag.SaidasPorPlanoContasLabels = saidasPorPlanoContas.Select(s => s.PlanoContas).ToList();
                ViewBag.SaidasPorPlanoContasData = saidasPorPlanoContas.Select(s => s.Total).ToList();

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao carregar página de relatórios");
                TempData["Erro"] = "Erro ao carregar relatórios.";
                return View();
            }
        }

        // =====================================================
        // FLUXO DE CAIXA
        // =====================================================
        public async Task<IActionResult> FluxoDeCaixa(DateTime? dataInicio, DateTime? dataFim)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                if (!dataInicio.HasValue)
                {
                    dataInicio = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                }
                if (!dataFim.HasValue)
                {
                    dataFim = DateTime.Now.Date;
                }

                ViewBag.DataInicio = dataInicio.Value.ToString("yyyy-MM-dd");
                ViewBag.DataFim = dataFim.Value.ToString("yyyy-MM-dd");

                // Determinar centro de custo
                int? centroCustoFiltro = null;

                // ✅ CORRIGIDO: Administrador, TesoureiroGeral e Pastor veem TUDO
                if (!User.IsInRole(Roles.Administrador) &&
                    !User.IsInRole(Roles.TesoureiroGeral) &&
                    !User.IsInRole(Roles.Pastor))
                {
                    // Apenas Tesoureiro Local tem filtro
                    centroCustoFiltro = user.CentroCustoId;

                    if (!centroCustoFiltro.HasValue)
                    {
                        return View(new List<FluxoDeCaixaItem>());
                    }
                }
                // Administrador, TesoureiroGeral e Pastor: sem filtro (vê todos os centros de custo)

                // Buscar IDs de entradas aprovadas no período
                var queryEntradasAprovadas = _context.Entradas
                    .Where(e => e.Data >= dataInicio && e.Data <= dataFim);

                if (centroCustoFiltro.HasValue)
                {
                    queryEntradasAprovadas = queryEntradasAprovadas
                        .Where(e => e.CentroCustoId == centroCustoFiltro.Value);
                }

                var idsEntradasAprovadas = await queryEntradasAprovadas
                    .Where(e => _context.FechamentosPeriodo.Any(f =>
                        f.CentroCustoId == e.CentroCustoId &&
                        f.Status == StatusFechamentoPeriodo.Aprovado &&
                        e.Data >= f.DataInicio &&
                        e.Data <= f.DataFim))
                    .Select(e => e.Id)
                    .ToListAsync();

                // Buscar IDs de saídas aprovadas no período
                var querySaidasAprovadas = _context.Saidas
                    .Where(s => s.Data >= dataInicio && s.Data <= dataFim);

                if (centroCustoFiltro.HasValue)
                {
                    querySaidasAprovadas = querySaidasAprovadas
                        .Where(s => s.CentroCustoId == centroCustoFiltro.Value);
                }

                var idsSaidasAprovadas = await querySaidasAprovadas
                    .Where(s => _context.FechamentosPeriodo.Any(f =>
                        f.CentroCustoId == s.CentroCustoId &&
                        f.Status == StatusFechamentoPeriodo.Aprovado &&
                        s.Data >= f.DataInicio &&
                        s.Data <= f.DataFim))
                    .Select(s => s.Id)
                    .ToListAsync();

                // Buscar entradas e saídas aprovadas
                var entradas = idsEntradasAprovadas.Any()
                    ? await _context.Entradas
                        .Where(e => idsEntradasAprovadas.Contains(e.Id))
                        .OrderBy(e => e.Data)
                        .ToListAsync()
                    : new List<Entrada>();

                var saidas = idsSaidasAprovadas.Any()
                    ? await _context.Saidas
                        .Where(s => idsSaidasAprovadas.Contains(s.Id))
                        .OrderBy(s => s.Data)
                        .ToListAsync()
                    : new List<Saida>();

                var fluxoDeCaixa = new List<FluxoDeCaixaItem>();

                // Agrupar por data
                var entradasAgrupadas = entradas.GroupBy(e => e.Data.Date)
                                                .ToDictionary(g => g.Key, g => g.Sum(e => e.Valor));
                var saidasAgrupadas = saidas.GroupBy(s => s.Data.Date)
                                              .ToDictionary(g => g.Key, g => g.Sum(s => s.Valor));

                var todasAsDatas = entradasAgrupadas.Keys.Union(saidasAgrupadas.Keys).OrderBy(d => d).ToList();

                decimal saldoAcumulado = 0;
                foreach (var data in todasAsDatas)
                {
                    var totalEntradaDia = entradasAgrupadas.GetValueOrDefault(data, 0);
                    var totalSaidaDia = saidasAgrupadas.GetValueOrDefault(data, 0);
                    saldoAcumulado += totalEntradaDia - totalSaidaDia;

                    fluxoDeCaixa.Add(new FluxoDeCaixaItem
                    {
                        Data = data,
                        Entradas = totalEntradaDia,
                        Saidas = totalSaidaDia,
                        SaldoDia = totalEntradaDia - totalSaidaDia,
                        SaldoAcumulado = saldoAcumulado
                    });
                }

                return View(fluxoDeCaixa);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao gerar fluxo de caixa");
                TempData["Erro"] = "Erro ao gerar fluxo de caixa.";
                return View(new List<FluxoDeCaixaItem>());
            }
        }

        // =====================================================
        // ENTRADAS POR PERÍODO
        // =====================================================
        public async Task<IActionResult> EntradasPorPeriodo(DateTime? dataInicio, DateTime? dataFim)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                if (!dataInicio.HasValue)
                {
                    dataInicio = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                }
                if (!dataFim.HasValue)
                {
                    dataFim = DateTime.Now.Date;
                }

                ViewBag.DataInicio = dataInicio.Value.ToString("yyyy-MM-dd");
                ViewBag.DataFim = dataFim.Value.ToString("yyyy-MM-dd");

                // Determinar centro de custo
                int? centroCustoFiltro = null;

                // ✅ CORRIGIDO: Administrador, TesoureiroGeral e Pastor veem TODOS os dados
                if (!User.IsInRole(Roles.Administrador) &&
                    !User.IsInRole(Roles.TesoureiroGeral) &&
                    !User.IsInRole(Roles.Pastor))
                {
                    // Apenas Tesoureiro Local tem filtro
                    centroCustoFiltro = user.CentroCustoId;

                    if (!centroCustoFiltro.HasValue)
                    {
                        TempData["Aviso"] = "Seu usuário não possui um Centro de Custo definido.";
                        return View(new List<Entrada>());
                    }
                }
                // Administrador, TesoureiroGeral e Pastor: sem filtro

                // Buscar IDs de entradas aprovadas
                var queryEntradasAprovadas = _context.Entradas
                    .Where(e => e.Data >= dataInicio && e.Data <= dataFim);

                if (centroCustoFiltro.HasValue)
                {
                    queryEntradasAprovadas = queryEntradasAprovadas
                        .Where(e => e.CentroCustoId == centroCustoFiltro.Value);
                }

                var idsEntradasAprovadas = await queryEntradasAprovadas
                    .Where(e => _context.FechamentosPeriodo.Any(f =>
                        f.CentroCustoId == e.CentroCustoId &&
                        f.Status == StatusFechamentoPeriodo.Aprovado &&
                        e.Data >= f.DataInicio &&
                        e.Data <= f.DataFim))
                    .Select(e => e.Id)
                    .ToListAsync();

                if (!idsEntradasAprovadas.Any())
                {
                    TempData["Info"] = "Não há entradas aprovadas no período selecionado.";
                    return View(new List<Entrada>());
                }

                // Buscar entradas aprovadas com includes
                var entradas = await _context.Entradas
                    .Include(e => e.Membro)
                    .Include(e => e.CentroCusto)
                    .Include(e => e.MeioDePagamento)
                    .Include(e => e.PlanoDeContas)
                    .Where(e => idsEntradasAprovadas.Contains(e.Id))
                    .OrderBy(e => e.Data)
                    .ThenBy(e => e.Id)
                    .ToListAsync();

                // Calcular total
                var totalEntradas = entradas.Sum(e => e.Valor);
                ViewBag.TotalEntradas = totalEntradas.ToString("C");
                ViewBag.QuantidadeEntradas = entradas.Count;

                await _auditService.LogAsync("Visualização", "Relatório",
                    $"Relatório de Entradas - {dataInicio:dd/MM/yyyy} a {dataFim:dd/MM/yyyy} - {entradas.Count} registro(s)");

                return View(entradas);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao gerar relatório de entradas");
                TempData["Erro"] = "Erro ao processar o relatório de entradas.";
                return View(new List<Entrada>());
            }
        }

        // =====================================================
        // SAÍDAS POR PERÍODO
        // =====================================================
        public async Task<IActionResult> SaidasPorPeriodo(DateTime? dataInicio, DateTime? dataFim, int? centroCustoId, int? fornecedorId)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                if (!dataInicio.HasValue)
                {
                    dataInicio = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                }
                if (!dataFim.HasValue)
                {
                    dataFim = DateTime.Now.Date;
                }

                ViewBag.DataInicio = dataInicio.Value.ToString("yyyy-MM-dd");
                ViewBag.DataFim = dataFim.Value.ToString("yyyy-MM-dd");

                // Configurar dropdowns de filtros
                if (User.IsInRole(Roles.Administrador) ||
        User.IsInRole(Roles.TesoureiroGeral) ||
               User.IsInRole(Roles.Pastor))
                {
                    ViewBag.CentrosCusto = new SelectList(
                   await _context.CentrosCusto.Where(c => c.Ativo).OrderBy(c => c.Nome).ToListAsync(),
                     "Id", "Nome", centroCustoId);
                }
                else
                {
                    if (user.CentroCustoId.HasValue)
                    {
                        ViewBag.CentrosCusto = new SelectList(
                      await _context.CentrosCusto.Where(c => c.Ativo && c.Id == user.CentroCustoId.Value).ToListAsync(),
                        "Id", "Nome", centroCustoId);
                    }
                    else
                    {
                        ViewBag.CentrosCusto = new SelectList(Enumerable.Empty<CentroCusto>(), "Id", "Nome");
                    }
                }

                ViewBag.Fornecedores = new SelectList(
                await _context.Fornecedores.Where(f => f.Ativo).OrderBy(f => f.Nome).ToListAsync(),
                  "Id", "Nome", fornecedorId);

                // Determinar centro de custo para filtro de aprovação
                int? centroCustoParaAprovacao = null;

                // ✅ CORRIGIDO: Administrador, TesoureiroGeral e Pastor veem TODOS os dados
                if (!User.IsInRole(Roles.Administrador) &&
              !User.IsInRole(Roles.TesoureiroGeral) &&
                !User.IsInRole(Roles.Pastor))
                {
                    // Tesoureiro Local: filtro obrigatório
                    centroCustoParaAprovacao = user.CentroCustoId;

                    if (!centroCustoParaAprovacao.HasValue)
                    {
                        TempData["Aviso"] = "Seu usuário não possui um Centro de Custo definido.";
                        return View(new List<Saida>());
                    }
                }
                else
                {
                    // Administrador, TesoureiroGeral e Pastor: podem filtrar opcionalmente
                    centroCustoParaAprovacao = centroCustoId;
                }

                // Buscar IDs de saídas aprovadas
                var querySaidasAprovadas = _context.Saidas
                    .Where(s => s.Data >= dataInicio && s.Data <= dataFim);

                if (centroCustoParaAprovacao.HasValue)
                {
                    querySaidasAprovadas = querySaidasAprovadas
                        .Where(s => s.CentroCustoId == centroCustoParaAprovacao.Value);
                }

                var idsSaidasAprovadas = await querySaidasAprovadas
                    .Where(s => _context.FechamentosPeriodo.Any(f =>
                        f.CentroCustoId == s.CentroCustoId &&
                        f.Status == StatusFechamentoPeriodo.Aprovado &&
                        s.Data >= f.DataInicio &&
                        s.Data <= f.DataFim))
                    .Select(s => s.Id)
                    .ToListAsync();

                if (!idsSaidasAprovadas.Any())
                {
                    TempData["Info"] = "Não há saídas aprovadas no período selecionado.";
                    return View(new List<Saida>());
                }

                // Montar query principal com saídas aprovadas
                var query = _context.Saidas
                    .Include(s => s.Fornecedor)
                    .Include(s => s.CentroCusto)
                    .Include(s => s.MeioDePagamento)
                    .Include(s => s.PlanoDeContas)
                    .Where(s => idsSaidasAprovadas.Contains(s.Id));

                // Aplicar filtros adicionais
                if (User.IsInRole(Roles.Administrador) || User.IsInRole(Roles.TesoureiroGeral))
                {
                    if (centroCustoId.HasValue)
                    {
                        query = query.Where(s => s.CentroCustoId == centroCustoId.Value);
                    }
                }
                else
                {
                    if (user.CentroCustoId.HasValue)
                    {
                        query = query.Where(s => s.CentroCustoId == user.CentroCustoId.Value);
                    }
                }

                if (fornecedorId.HasValue)
                {
                    query = query.Where(s => s.FornecedorId == fornecedorId.Value);
                }

                var saidas = await query.OrderBy(s => s.Data).ThenBy(s => s.Id).ToListAsync();

                // Calcular total
                var totalSaidas = saidas.Sum(s => s.Valor);
                ViewBag.TotalSaidas = totalSaidas.ToString("C");
                ViewBag.QuantidadeSaidas = saidas.Count;

                await _auditService.LogAsync("Visualização", "Relatório",
                    $"Relatório de Saídas - {dataInicio:dd/MM/yyyy} a {dataFim:dd/MM/yyyy} - {saidas.Count} registro(s)");

                return View(saidas);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao gerar relatório de saídas");
                TempData["Erro"] = "Erro ao processar o relatório de saídas.";
                return View(new List<Saida>());
            }
        }

        // =====================================================
        // BALANCETE GERAL
        // =====================================================
        public async Task<IActionResult> BalanceteGeral(DateTime? dataInicio, DateTime? dataFim)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                if (!dataInicio.HasValue)
                {
                    dataInicio = new DateTime(DateTime.Now.Year, 1, 1);
                }
                if (!dataFim.HasValue)
                {
                    dataFim = DateTime.Now.Date;
                }

                ViewBag.DataInicio = dataInicio.Value.ToString("yyyy-MM-dd");
                ViewBag.DataFim = dataFim.Value.ToString("yyyy-MM-dd");

                // Determinar centro de custo
                int? centroCustoFiltro = null;

                // ✅ CORRIGIDO: Administrador, TesoureiroGeral e Pastor veem TODOS os dados
                if (!User.IsInRole(Roles.Administrador) &&
                    !User.IsInRole(Roles.TesoureiroGeral) &&
                    !User.IsInRole(Roles.Pastor))
                {
                    // Tesoureiro Local: filtro obrigatório
                    centroCustoFiltro = user.CentroCustoId;

                    if (!centroCustoFiltro.HasValue)
                    {
                        return View(new List<dynamic>());
                    }
                }
                // Administrador, TesoureiroGeral e Pastor: sem filtro (vê todos os centros)

                // Buscar IDs de entradas aprovadas
                var queryEntradasAprovadas = _context.Entradas
                    .Where(e => e.Data >= dataInicio && e.Data <= dataFim);

                if (centroCustoFiltro.HasValue)
                {
                    queryEntradasAprovadas = queryEntradasAprovadas
                        .Where(e => e.CentroCustoId == centroCustoFiltro.Value);
                }

                var idsEntradasAprovadas = await queryEntradasAprovadas
                    .Where(e => _context.FechamentosPeriodo.Any(f =>
                        f.CentroCustoId == e.CentroCustoId &&
                        f.Status == StatusFechamentoPeriodo.Aprovado &&
                        e.Data >= f.DataInicio &&
                        e.Data <= f.DataFim))
                    .Select(e => e.Id)
                    .ToListAsync();

                var entradas = new List<(int PlanoContasId, string PlanoContasNome, TipoPlanoContas Tipo, decimal TotalEntradas, decimal TotalSaidas)>();

                if (idsEntradasAprovadas.Any())
                {
                    entradas = await _context.Entradas
                        .Include(e => e.PlanoDeContas)
                        .Where(e => idsEntradasAprovadas.Contains(e.Id))
                        .GroupBy(e => new { e.PlanoDeContas!.Id, e.PlanoDeContas.Nome, e.PlanoDeContas.Tipo })
                        .Select(g => new
                        {
                            PlanoContasId = g.Key.Id,
                            PlanoContasNome = g.Key.Nome,
                            Tipo = g.Key.Tipo,
                            TotalEntradas = g.Sum(e => e.Valor),
                            TotalSaidas = 0m
                        })
                        .ToListAsync()
                        .ContinueWith(task => task.Result.Select(x => (x.PlanoContasId, x.PlanoContasNome, x.Tipo, x.TotalEntradas, x.TotalSaidas)).ToList());
                }

                // Buscar IDs de saídas aprovadas
                var querySaidasAprovadas = _context.Saidas
                    .Where(s => s.Data >= dataInicio && s.Data <= dataFim);

                if (centroCustoFiltro.HasValue)
                {
                    querySaidasAprovadas = querySaidasAprovadas
                        .Where(s => s.CentroCustoId == centroCustoFiltro.Value);
                }

                var idsSaidasAprovadas = await querySaidasAprovadas
                    .Where(s => _context.FechamentosPeriodo.Any(f =>
                        f.CentroCustoId == s.CentroCustoId &&
                        f.Status == StatusFechamentoPeriodo.Aprovado &&
                        s.Data >= f.DataInicio &&
                        s.Data <= f.DataFim))
                    .Select(s => s.Id)
                    .ToListAsync();

                var saidas = new List<(int PlanoContasId, string PlanoContasNome, TipoPlanoContas Tipo, decimal TotalEntradas, decimal TotalSaidas)>();

                if (idsSaidasAprovadas.Any())
                {
                    saidas = await _context.Saidas
                        .Include(s => s.PlanoDeContas)
                        .Where(s => idsSaidasAprovadas.Contains(s.Id))
                        .GroupBy(s => new { s.PlanoDeContas!.Id, s.PlanoDeContas.Nome, s.PlanoDeContas.Tipo })
                        .Select(g => new
                        {
                            PlanoContasId = g.Key.Id,
                            PlanoContasNome = g.Key.Nome,
                            Tipo = g.Key.Tipo,
                            TotalEntradas = 0m,
                            TotalSaidas = g.Sum(s => s.Valor)
                        })
                        .ToListAsync()
                        .ContinueWith(task => task.Result.Select(x => (x.PlanoContasId, x.PlanoContasNome, x.Tipo, x.TotalEntradas, x.TotalSaidas)).ToList());
                }

                // Combinar entradas e saídas
                var balanceteCompleto = entradas.Concat(saidas)
                    .GroupBy(x => new { x.PlanoContasId, x.PlanoContasNome, x.Tipo })
                    .Select(g => new
                    {
                        PlanoContasNome = g.Key.PlanoContasNome,
                        Tipo = g.Key.Tipo,
                        TotalEntradas = g.Sum(x => x.TotalEntradas),
                        TotalSaidas = g.Sum(x => x.TotalSaidas),
                        Saldo = g.Sum(x => x.TotalEntradas) - g.Sum(x => x.TotalSaidas)
                    })
                    .OrderBy(x => x.PlanoContasNome)
                    .ToList();

                return View(balanceteCompleto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao gerar balancete geral");
                TempData["Erro"] = "Erro ao gerar balancete geral.";
                return View(new List<dynamic>());
            }
        }

        // =====================================================
        // CONTRIBUIÇÕES POR MEMBRO
        // =====================================================
        public async Task<IActionResult> ContribuicoesPorMembro(DateTime? dataInicio, DateTime? dataFim, int? membroId)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                if (!dataInicio.HasValue)
                {
                    dataInicio = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                }
                if (!dataFim.HasValue)
                {
                    dataFim = DateTime.Now.Date;
                }

                ViewBag.DataInicio = dataInicio.Value.ToString("yyyy-MM-dd");
                ViewBag.DataFim = dataFim.Value.ToString("yyyy-MM-dd");

                ViewBag.Membros = new SelectList(
              await _context.Membros.Where(m => m.Ativo).OrderBy(m => m.NomeCompleto).ToListAsync(),
                "Id", "NomeCompleto", membroId);

                // Determinar centro de custo
                int? centroCustoFiltro = null;

                // ✅ CORRIGIDO: Administrador, TesoureiroGeral e Pastor veem TODOS os dados
                if (!User.IsInRole(Roles.Administrador) &&
            !User.IsInRole(Roles.TesoureiroGeral) &&
          !User.IsInRole(Roles.Pastor))
                {
                    // Tesoureiro Local: filtro obrigatório
                    centroCustoFiltro = user.CentroCustoId;

                    if (!centroCustoFiltro.HasValue)
                    {
                        ViewBag.ResumoPorMembro = new List<dynamic>();
                        return View(new List<Entrada>());
                    }
                }
                // Administrador, TesoureiroGeral e Pastor: sem filtro

                // Buscar IDs de entradas aprovadas
                var queryEntradasAprovadas = _context.Entradas
                            .Where(e => e.Data >= dataInicio && e.Data <= dataFim && e.MembroId != null);

                if (centroCustoFiltro.HasValue)
                {
                    queryEntradasAprovadas = queryEntradasAprovadas
                        .Where(e => e.CentroCustoId == centroCustoFiltro.Value);
                }

                if (membroId.HasValue)
                {
                    queryEntradasAprovadas = queryEntradasAprovadas
                        .Where(e => e.MembroId == membroId.Value);
                }

                var idsEntradasAprovadas = await queryEntradasAprovadas
                    .Where(e => _context.FechamentosPeriodo.Any(f =>
                        f.CentroCustoId == e.CentroCustoId &&
                        f.Status == StatusFechamentoPeriodo.Aprovado &&
                        e.Data >= f.DataInicio &&
                        e.Data <= f.DataFim))
                    .Select(e => e.Id)
                    .ToListAsync();

                var contribuicoes = idsEntradasAprovadas.Any()
                    ? await _context.Entradas
                        .Include(e => e.Membro)
                        .Include(e => e.PlanoDeContas)
                        .Include(e => e.CentroCusto)
                        .Include(e => e.MeioDePagamento)
                        .Where(e => idsEntradasAprovadas.Contains(e.Id))
                        .OrderBy(e => e.Membro!.NomeCompleto)
                        .ThenBy(e => e.Data)
                        .ToListAsync()
                    : new List<Entrada>();

                // Resumo por membro
                var resumoPorMembro = contribuicoes
                    .GroupBy(e => new { e.MembroId, e.Membro!.NomeCompleto })
                    .Select(g => new
                    {
                        MembroNome = g.Key.NomeCompleto,
                        TotalContribuicoes = g.Sum(e => e.Valor),
                        QuantidadeContribuicoes = g.Count(),
                        UltimaContribuicao = g.Max(e => e.Data)
                    })
                    .OrderByDescending(x => x.TotalContribuicoes)
                    .ToList();

                ViewBag.ResumoPorMembro = resumoPorMembro;

                return View(contribuicoes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao gerar relatório de contribuições");
                TempData["Erro"] = "Erro ao processar relatório de contribuições.";
                ViewBag.ResumoPorMembro = new List<dynamic>();
                return View(new List<Entrada>());
            }
        }

        // =====================================================
        // DESPESAS POR CENTRO DE CUSTO
        // =====================================================
        public async Task<IActionResult> DespesasPorCentroCusto(DateTime? dataInicio, DateTime? dataFim, int? centroCustoId)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                if (!dataInicio.HasValue)
                {
                    dataInicio = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                }
                if (!dataFim.HasValue)
                {
                    dataFim = DateTime.Now.Date;
                }

                ViewBag.DataInicio = dataInicio.Value.ToString("yyyy-MM-dd");
                ViewBag.DataFim = dataFim.Value.ToString("yyyy-MM-dd");

                // Configurar dropdown
                if (User.IsInRole(Roles.Administrador) ||
               User.IsInRole(Roles.TesoureiroGeral) ||
                 User.IsInRole(Roles.Pastor))
                {
                    ViewBag.CentrosCusto = new SelectList(
                          await _context.CentrosCusto.Where(c => c.Ativo).OrderBy(c => c.Nome).ToListAsync(),
                          "Id", "Nome", centroCustoId);
                }
                else
                {
                    if (user.CentroCustoId.HasValue)
                    {
                        ViewBag.CentrosCusto = new SelectList(
                             await _context.CentrosCusto.Where(c => c.Ativo && c.Id == user.CentroCustoId.Value).ToListAsync(),
                        "Id", "Nome", centroCustoId);
                    }
                    else
                    {
                        ViewBag.CentrosCusto = new SelectList(Enumerable.Empty<CentroCusto>(), "Id", "Nome");
                    }
                }

                // Determinar centro de custo
                int? centroCustoFiltro = null;

                // ✅ CORRIGIDO: Administrador, TesoureiroGeral e Pastor veem TODOS os dados
                if (!User.IsInRole(Roles.Administrador) &&
           !User.IsInRole(Roles.TesoureiroGeral) &&
             !User.IsInRole(Roles.Pastor))
                {
                    // Tesoureiro Local: filtro obrigatório
                    centroCustoFiltro = user.CentroCustoId;

                    if (!centroCustoFiltro.HasValue)
                    {
                        ViewBag.ResumoPorCentroCusto = new List<dynamic>();
                        return View(new List<Saida>());
                    }
                }
                else
                {
                    // Administrador, TesoureiroGeral e Pastor: podem filtrar opcionalmente
                    centroCustoFiltro = centroCustoId;
                }

                // Buscar IDs de saídas aprovadas
                var querySaidasAprovadas = _context.Saidas
                    .Where(s => s.Data >= dataInicio && s.Data <= dataFim);

                if (centroCustoFiltro.HasValue)
                {
                    querySaidasAprovadas = querySaidasAprovadas
                        .Where(s => s.CentroCustoId == centroCustoFiltro.Value);
                }

                var idsSaidasAprovadas = await querySaidasAprovadas
                    .Where(s => _context.FechamentosPeriodo.Any(f =>
                        f.CentroCustoId == s.CentroCustoId &&
                        f.Status == StatusFechamentoPeriodo.Aprovado &&
                        s.Data >= f.DataInicio &&
                        s.Data <= f.DataFim))
                    .Select(s => s.Id)
                    .ToListAsync();

                var despesas = idsSaidasAprovadas.Any()
                    ? await _context.Saidas
                        .Include(s => s.CentroCusto)
                        .Include(s => s.PlanoDeContas)
                        .Include(s => s.Fornecedor)
                        .Include(s => s.MeioDePagamento)
                        .Where(s => idsSaidasAprovadas.Contains(s.Id))
                        .OrderBy(s => s.Data)
                        .ToListAsync()
                    : new List<Saida>();

                // Resumo por centro de custo
                var resumoPorCentroCusto = despesas
                    .GroupBy(s => new { s.CentroCustoId, s.CentroCusto!.Nome })
                    .Select(g => new
                    {
                        CentroCustoNome = g.Key.Nome,
                        TotalDespesas = g.Sum(s => s.Valor),
                        QuantidadeDespesas = g.Count()
                    })
                    .OrderByDescending(x => x.TotalDespesas)
                    .ToList();

                ViewBag.ResumoPorCentroCusto = resumoPorCentroCusto;

                return View(despesas);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao gerar relatório de despesas por centro de custo");
                TempData["Erro"] = "Erro ao processar relatório de despesas.";
                ViewBag.ResumoPorCentroCusto = new List<dynamic>();
                return View(new List<Saida>());
            }
        }

        // =====================================================
        // BALANCETE MENSAL
        // =====================================================
        [HttpGet]
        [Authorize(Policy = "Relatorios")]
        public async Task<IActionResult> BalanceteMensal(
            int? centroCustoId,
            DateTime? dataInicio,
            DateTime? dataFim)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                if (!dataInicio.HasValue)
                {
                    dataInicio = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                }
                if (!dataFim.HasValue)
                {
                    dataFim = dataInicio.Value.AddMonths(1).AddDays(-1);
                }

                int selectedCentroCustoId;

                if (User.IsInRole(Roles.Administrador) || User.IsInRole(Roles.TesoureiroGeral))
                {
                    if (centroCustoId.HasValue)
                    {
                        selectedCentroCustoId = centroCustoId.Value;
                    }
                    else
                    {
                        var sede = await _context.CentrosCusto
                            .Where(c => c.Nome.Contains("Sede") || c.Nome.Contains("SEDE"))
                            .FirstOrDefaultAsync();

                        if (sede == null)
                        {
                            TempData["ErrorMessage"] = "Nenhum centro de custo encontrado.";
                            return RedirectToAction("Index");
                        }

                        selectedCentroCustoId = sede.Id;
                    }

                    ViewBag.CentrosCusto = new SelectList(
                        await _context.CentrosCusto.Where(c => c.Ativo).OrderBy(c => c.Nome).ToListAsync(),
                        "Id", "Nome", selectedCentroCustoId);
                }
                else
                {
                    if (!user.CentroCustoId.HasValue)
                    {
                        TempData["ErrorMessage"] = "Você não está vinculado a nenhum centro de custo.";
                        return RedirectToAction("Index");
                    }

                    selectedCentroCustoId = user.CentroCustoId.Value;
                    ViewBag.CentrosCusto = null;
                }

                var balancete = await _balanceteService.GerarBalanceteMensalAsync(
                    selectedCentroCustoId,
                    dataInicio.Value,
                    dataFim.Value);

                ViewBag.DataInicio = dataInicio.Value.ToString("yyyy-MM-dd");
                ViewBag.DataFim = dataFim.Value.ToString("yyyy-MM-dd");
                ViewBag.CentroCustoId = selectedCentroCustoId;
                ViewBag.PodeEscolherCentroCusto = User.IsInRole(Roles.Administrador) ||
                                                  User.IsInRole(Roles.TesoureiroGeral);

                await _auditService.LogAsync("Visualização", "Relatório",
                    $"Balancete mensal: {balancete.CentroCustoNome} - {balancete.Periodo}");

                return View(balancete);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao gerar balancete mensal");
                TempData["ErrorMessage"] = $"Erro ao gerar balancete: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        [HttpGet]
        [Authorize(Policy = "Relatorios")]
        public async Task<IActionResult> BalanceteMensalPdf(int centroCustoId, DateTime dataInicio, DateTime dataFim)
        {
            try
            {
                var balancete = await _balanceteService.GerarBalanceteMensalAsync(
                    centroCustoId,
                    dataInicio,
                    dataFim);

                var pdfBytes = SistemaTesourariaEclesiastica.Helpers.BalancetePdfHelper.GerarPdfBalanceteMensal(balancete);

                var nomeArquivo = $"Balancete_Mensal_{balancete.CentroCustoNome.Replace(" ", "_")}_{dataInicio:yyyyMM}.pdf";

                await _auditService.LogAsync("Exportação", "Relatório",
                    $"Balancete mensal PDF: {balancete.CentroCustoNome} - {balancete.Periodo}");

                return File(pdfBytes, "application/pdf", nomeArquivo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao exportar balancete para PDF");
                TempData["ErrorMessage"] = $"Erro ao exportar PDF: {ex.Message}";
                return RedirectToAction("BalanceteMensal", new { centroCustoId, dataInicio, dataFim });
            }
        }

        // =====================================================
        // EXPORTAÇÃO PARA EXCEL - ENTRADAS
        // =====================================================
        public async Task<IActionResult> ExportarEntradasExcel(DateTime? dataInicio, DateTime? dataFim)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                if (!dataInicio.HasValue)
                {
                    dataInicio = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                }
                if (!dataFim.HasValue)
                {
                    dataFim = DateTime.Now.Date;
                }

                // Determinar centro de custo
                int? centroCustoFiltro = null;

                // ✅ CORRIGIDO: Administrador, TesoureiroGeral e Pastor veem TODOS os dados
                if (!User.IsInRole(Roles.Administrador) &&
              !User.IsInRole(Roles.TesoureiroGeral) &&
             !User.IsInRole(Roles.Pastor))
                {
                    // Tesoureiro Local: filtro obrigatório
                    centroCustoFiltro = user.CentroCustoId;

                    if (!centroCustoFiltro.HasValue)
                    {
                        TempData["Erro"] = "Usuário sem centro de custo definido.";
                        return RedirectToAction("EntradasPorPeriodo");
                    }
                }
                // Administrador, TesoureiroGeral e Pastor: sem filtro (exporta tudo)

                // Buscar IDs de entradas aprovadas
                var queryEntradasAprovadas = _context.Entradas
                    .Where(e => e.Data >= dataInicio && e.Data <= dataFim);

                if (centroCustoFiltro.HasValue)
                {
                    queryEntradasAprovadas = queryEntradasAprovadas
                        .Where(e => e.CentroCustoId == centroCustoFiltro.Value);
                }

                var idsEntradasAprovadas = await queryEntradasAprovadas
                    .Where(e => _context.FechamentosPeriodo.Any(f =>
                        f.CentroCustoId == e.CentroCustoId &&
                        f.Status == StatusFechamentoPeriodo.Aprovado &&
                        e.Data >= f.DataInicio &&
                        e.Data <= f.DataFim))
                    .Select(e => e.Id)
                    .ToListAsync();

                var entradas = idsEntradasAprovadas.Any()
                    ? await _context.Entradas
                        .Include(e => e.Membro)
                        .Include(e => e.CentroCusto)
                        .Include(e => e.MeioDePagamento)
                        .Include(e => e.PlanoDeContas)
                        .Where(e => idsEntradasAprovadas.Contains(e.Id))
                        .OrderBy(e => e.Data)
                        .ToListAsync()
                    : new List<Entrada>();

                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("Entradas Aprovadas");

                // Cabeçalhos
                worksheet.Cells[1, 1].Value = "Data";
                worksheet.Cells[1, 2].Value = "Valor";
                worksheet.Cells[1, 3].Value = "Descrição";
                worksheet.Cells[1, 4].Value = "Membro";
                worksheet.Cells[1, 5].Value = "Centro de Custo";
                worksheet.Cells[1, 6].Value = "Plano de Contas";
                worksheet.Cells[1, 7].Value = "Meio de Pagamento";
                worksheet.Cells[1, 8].Value = "Observações";

                using (var range = worksheet.Cells[1, 1, 1, 8])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                }

                for (int i = 0; i < entradas.Count; i++)
                {
                    var entrada = entradas[i];
                    var row = i + 2;

                    worksheet.Cells[row, 1].Value = entrada.Data.ToString("dd/MM/yyyy");
                    worksheet.Cells[row, 2].Value = entrada.Valor;
                    worksheet.Cells[row, 3].Value = entrada.Descricao;
                    worksheet.Cells[row, 4].Value = entrada.Membro?.NomeCompleto ?? "";
                    worksheet.Cells[row, 5].Value = entrada.CentroCusto?.Nome ?? "";
                    worksheet.Cells[row, 6].Value = entrada.PlanoDeContas?.Nome ?? "";
                    worksheet.Cells[row, 7].Value = entrada.MeioDePagamento?.Nome ?? "";
                    worksheet.Cells[row, 8].Value = entrada.Observacoes ?? "";
                }

                worksheet.Column(2).Style.Numberformat.Format = "R$ #,##0.00";
                worksheet.Cells.AutoFitColumns();

                var stream = new MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;

                var fileName = $"Entradas_Aprovadas_{dataInicio:yyyyMMdd}_{dataFim:yyyyMMdd}.xlsx";
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao exportar entradas para Excel");
                TempData["Erro"] = "Erro ao exportar entradas.";
                return RedirectToAction("EntradasPorPeriodo");
            }
        }

        // =====================================================
        // EXPORTAÇÃO PARA EXCEL - SAÍDAS
        // =====================================================
        public async Task<IActionResult> ExportarSaidasExcel(DateTime? dataInicio, DateTime? dataFim)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                if (!dataInicio.HasValue)
                {
                    dataInicio = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                }
                if (!dataFim.HasValue)
                {
                    dataFim = DateTime.Now.Date;
                }

                // Determinar centro de custo
                int? centroCustoFiltro = null;

                // ✅ CORRIGIDO: Administrador, TesoureiroGeral e Pastor veem TODOS os dados
                if (!User.IsInRole(Roles.Administrador) &&
           !User.IsInRole(Roles.TesoureiroGeral) &&
              !User.IsInRole(Roles.Pastor))
                {
                    // Tesoureiro Local: filtro obrigatório
                    centroCustoFiltro = user.CentroCustoId;

                    if (!centroCustoFiltro.HasValue)
                    {
                        TempData["Erro"] = "Usuário sem centro de custo definido.";
                        return RedirectToAction("SaidasPorPeriodo");
                    }
                }
                // Administrador, TesoureiroGeral e Pastor: sem filtro (exporta tudo)

                // Buscar IDs de saídas aprovadas
                var querySaidasAprovadas = _context.Saidas
                    .Where(s => s.Data >= dataInicio && s.Data <= dataFim);

                if (centroCustoFiltro.HasValue)
                {
                    querySaidasAprovadas = querySaidasAprovadas
                        .Where(s => s.CentroCustoId == centroCustoFiltro.Value);
                }

                var idsSaidasAprovadas = await querySaidasAprovadas
                    .Where(s => _context.FechamentosPeriodo.Any(f =>
                        f.CentroCustoId == s.CentroCustoId &&
                        f.Status == StatusFechamentoPeriodo.Aprovado &&
                        s.Data >= f.DataInicio &&
                        s.Data <= f.DataFim))
                    .Select(s => s.Id)
                    .ToListAsync();

                var saidas = idsSaidasAprovadas.Any()
                    ? await _context.Saidas
                        .Include(s => s.Fornecedor)
                        .Include(s => s.CentroCusto)
                        .Include(s => s.MeioDePagamento)
                        .Include(s => s.PlanoDeContas)
                        .Where(s => idsSaidasAprovadas.Contains(s.Id))
                        .OrderBy(s => s.Data)
                        .ToListAsync()
                    : new List<Saida>();

                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("Saídas Aprovadas");

                // Cabeçalhos
                worksheet.Cells[1, 1].Value = "Data";
                worksheet.Cells[1, 2].Value = "Valor";
                worksheet.Cells[1, 3].Value = "Descrição";
                worksheet.Cells[1, 4].Value = "Fornecedor";
                worksheet.Cells[1, 5].Value = "Centro de Custo";
                worksheet.Cells[1, 6].Value = "Plano de Contas";
                worksheet.Cells[1, 7].Value = "Meio de Pagamento";
                worksheet.Cells[1, 8].Value = "Observações";

                using (var range = worksheet.Cells[1, 1, 1, 8])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                }

                for (int i = 0; i < saidas.Count; i++)
                {
                    var saida = saidas[i];
                    var row = i + 2;

                    worksheet.Cells[row, 1].Value = saida.Data.ToString("dd/MM/yyyy");
                    worksheet.Cells[row, 2].Value = saida.Valor;
                    worksheet.Cells[row, 3].Value = saida.Descricao;
                    worksheet.Cells[row, 4].Value = saida.Fornecedor?.Nome ?? "";
                    worksheet.Cells[row, 5].Value = saida.CentroCusto?.Nome ?? "";
                    worksheet.Cells[row, 6].Value = saida.PlanoDeContas?.Nome ?? "";
                    worksheet.Cells[row, 7].Value = saida.MeioDePagamento?.Nome ?? "";
                    worksheet.Cells[row, 8].Value = saida.Observacoes ?? "";
                }

                worksheet.Column(2).Style.Numberformat.Format = "R$ #,##0.00";
                worksheet.Cells.AutoFitColumns();

                var stream = new MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;

                var fileName = $"Saidas_Aprovadas_{dataInicio:yyyyMMdd}_{dataFim:yyyyMMdd}.xlsx";
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao exportar saídas para Excel");
                TempData["Erro"] = "Erro ao exportar saídas.";
                return RedirectToAction("SaidasPorPeriodo");
            }
        }
    }
}