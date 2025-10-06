using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using SistemaTesourariaEclesiastica.Attributes;
using SistemaTesourariaEclesiastica.Data;
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

        public RelatoriosController(ApplicationDbContext context, AuditService auditService, UserManager<ApplicationUser> userManager, BalanceteService balanceteService)
        {
            _context = context;
            _auditService = auditService;
            _userManager = userManager;
            _balanceteService = balanceteService;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            // Query para entradas com filtro de centro de custo
            var entradasQuery = _context.Entradas.AsQueryable();

            // Aplicar filtro de centro de custo para Tesoureiros Locais e Pastores
            if (!User.IsInRole(Roles.Administrador) && !User.IsInRole(Roles.TesoureiroGeral))
            {
                if (user.CentroCustoId.HasValue)
                {
                    entradasQuery = entradasQuery.Where(e => e.CentroCustoId == user.CentroCustoId.Value);
                }
                else
                {
                    entradasQuery = entradasQuery.Where(e => false);
                }
            }

            // Query para saídas com filtro de centro de custo
            var saidasQuery = _context.Saidas.AsQueryable();

            // Aplicar filtro de centro de custo para Tesoureiros Locais e Pastores
            if (!User.IsInRole(Roles.Administrador) && !User.IsInRole(Roles.TesoureiroGeral))
            {
                if (user.CentroCustoId.HasValue)
                {
                    saidasQuery = saidasQuery.Where(s => s.CentroCustoId == user.CentroCustoId.Value);
                }
                else
                {
                    saidasQuery = saidasQuery.Where(s => false);
                }
            }

            var totalEntradas = await entradasQuery.Select(e => e.Valor).SumAsync();
            var totalSaidas = await saidasQuery.Select(s => s.Valor).SumAsync();
            var saldoAtual = totalEntradas - totalSaidas;

            ViewBag.TotalEntradas = totalEntradas.ToString("C2");
            ViewBag.TotalSaidas = totalSaidas.ToString("C2");
            ViewBag.SaldoAtual = saldoAtual.ToString("C2");

            // Dados para gráficos - Entradas por Centro de Custo
            var entradasPorCentroCusto = await entradasQuery
                .GroupBy(e => new { e.CentroCustoId, e.CentroCusto.Nome })
                .Select(g => new
                {
                    CentroCusto = g.Key.Nome ?? "Sem Centro de Custo",
                    Total = g.Sum(e => e.Valor)
                })
                .ToListAsync();

            entradasPorCentroCusto = entradasPorCentroCusto
                .OrderByDescending(x => x.Total)
                .ToList();

            ViewBag.EntradasPorCentroCustoLabels = entradasPorCentroCusto.Select(x => x.CentroCusto).ToList();
            ViewBag.EntradasPorCentroCustoData = entradasPorCentroCusto.Select(x => x.Total).ToList();

            // Dados para gráficos - Saídas por Plano de Contas
            var saidasPorPlanoContas = await saidasQuery
                .Include(s => s.PlanoDeContas)
                .GroupBy(s => new { Nome = s.PlanoDeContas.Nome ?? "Sem Plano de Contas" })
                .Select(g => new { PlanoContas = g.Key.Nome, Total = g.Sum(s => s.Valor) })
                .ToListAsync();

            saidasPorPlanoContas = saidasPorPlanoContas
                .OrderByDescending(x => x.Total)
                .ToList();

            ViewBag.SaidasPorPlanoContasLabels = saidasPorPlanoContas.Select(x => x.PlanoContas).ToList();
            ViewBag.SaidasPorPlanoContasData = saidasPorPlanoContas.Select(x => x.Total).ToList();

            return View();
        }

        // GET: Relatorios/FluxoDeCaixa
        public async Task<IActionResult> FluxoDeCaixa(DateTime? dataInicio, DateTime? dataFim)
        {
            var user = await _userManager.GetUserAsync(User);

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

            // Query de entradas com filtro de centro de custo
            var entradasQuery = _context.Entradas
                .Where(e => e.Data >= dataInicio && e.Data <= dataFim);

            // Aplicar filtro de centro de custo para Tesoureiros Locais e Pastores
            if (!User.IsInRole(Roles.Administrador) && !User.IsInRole(Roles.TesoureiroGeral))
            {
                if (user.CentroCustoId.HasValue)
                {
                    entradasQuery = entradasQuery.Where(e => e.CentroCustoId == user.CentroCustoId.Value);
                }
                else
                {
                    entradasQuery = entradasQuery.Where(e => false);
                }
            }

            // Query de saídas com filtro de centro de custo
            var saidasQuery = _context.Saidas
                .Where(s => s.Data >= dataInicio && s.Data <= dataFim);

            // Aplicar filtro de centro de custo para Tesoureiros Locais e Pastores
            if (!User.IsInRole(Roles.Administrador) && !User.IsInRole(Roles.TesoureiroGeral))
            {
                if (user.CentroCustoId.HasValue)
                {
                    saidasQuery = saidasQuery.Where(s => s.CentroCustoId == user.CentroCustoId.Value);
                }
                else
                {
                    saidasQuery = saidasQuery.Where(s => false);
                }
            }

            var entradas = await entradasQuery.OrderBy(e => e.Data).ToListAsync();
            var saidas = await saidasQuery.OrderBy(s => s.Data).ToListAsync();

            var fluxoDeCaixa = new List<FluxoDeCaixaItem>();

            // Agrupar por data para um relatório diário/mensal
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

        // GET: Relatorios/EntradasPorPeriodo
        public async Task<IActionResult> EntradasPorPeriodo(DateTime? dataInicio, DateTime? dataFim)
        {
            var user = await _userManager.GetUserAsync(User);

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

            var query = _context.Entradas
                .Include(e => e.Membro)
                .Include(e => e.CentroCusto)
                .Include(e => e.MeioDePagamento)
                .Include(e => e.PlanoDeContas)
                .Where(e => e.Data >= dataInicio && e.Data <= dataFim);

            // Aplicar filtro de centro de custo para Tesoureiros Locais e Pastores
            if (!User.IsInRole(Roles.Administrador) && !User.IsInRole(Roles.TesoureiroGeral))
            {
                if (user.CentroCustoId.HasValue)
                {
                    query = query.Where(e => e.CentroCustoId == user.CentroCustoId.Value);
                }
                else
                {
                    query = query.Where(e => false);
                }
            }

            var entradas = await query.OrderBy(e => e.Data).ToListAsync();

            return View(entradas);
        }

        // GET: Relatorios/SaidasPorPeriodo
        public async Task<IActionResult> SaidasPorPeriodo(DateTime? dataInicio, DateTime? dataFim, int? centroCustoId, int? fornecedorId)
        {
            var user = await _userManager.GetUserAsync(User);

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

            // Filtros para dropdowns - CORRIGIDO para respeitar centro de custo
            if (User.IsInRole(Roles.Administrador) || User.IsInRole(Roles.TesoureiroGeral))
            {
                ViewBag.CentrosCusto = new SelectList(await _context.CentrosCusto.Where(c => c.Ativo).ToListAsync(), "Id", "Nome", centroCustoId);
            }
            else
            {
                // Tesoureiros Locais só veem seu próprio centro de custo
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

            ViewBag.Fornecedores = new SelectList(await _context.Fornecedores.Where(f => f.Ativo).ToListAsync(), "Id", "Nome", fornecedorId);

            var query = _context.Saidas
                .Include(s => s.Fornecedor)
                .Include(s => s.CentroCusto)
                .Include(s => s.MeioDePagamento)
                .Include(s => s.PlanoDeContas)
                .Where(s => s.Data >= dataInicio && s.Data <= dataFim);

            // Aplicar filtro de centro de custo para Tesoureiros Locais e Pastores
            if (!User.IsInRole(Roles.Administrador) && !User.IsInRole(Roles.TesoureiroGeral))
            {
                if (user.CentroCustoId.HasValue)
                {
                    query = query.Where(s => s.CentroCustoId == user.CentroCustoId.Value);
                }
                else
                {
                    query = query.Where(s => false);
                }
            }
            else
            {
                // Admin e Tesoureiro Geral podem filtrar por centro de custo
                if (centroCustoId.HasValue)
                {
                    query = query.Where(s => s.CentroCustoId == centroCustoId.Value);
                }
            }

            if (fornecedorId.HasValue)
            {
                query = query.Where(s => s.FornecedorId == fornecedorId.Value);
            }

            var saidas = await query.OrderBy(s => s.Data).ToListAsync();

            return View(saidas);
        }

        // GET: Relatorios/BalanceteGeral
        public async Task<IActionResult> BalanceteGeral(DateTime? dataInicio, DateTime? dataFim)
        {
            var user = await _userManager.GetUserAsync(User);

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

            // Query de entradas com filtro de centro de custo
            var entradasQuery = _context.Entradas
                .Include(e => e.PlanoDeContas)
                .Where(e => e.Data >= dataInicio && e.Data <= dataFim);

            // Aplicar filtro de centro de custo para Tesoureiros Locais e Pastores
            if (!User.IsInRole(Roles.Administrador) && !User.IsInRole(Roles.TesoureiroGeral))
            {
                if (user.CentroCustoId.HasValue)
                {
                    entradasQuery = entradasQuery.Where(e => e.CentroCustoId == user.CentroCustoId.Value);
                }
                else
                {
                    entradasQuery = entradasQuery.Where(e => false);
                }
            }

            var entradas = await entradasQuery
                .GroupBy(e => new { e.PlanoDeContas.Id, e.PlanoDeContas.Nome, e.PlanoDeContas.Tipo })
                .Select(g => new
                {
                    PlanoContasId = g.Key.Id,
                    PlanoContasNome = g.Key.Nome,
                    Tipo = g.Key.Tipo,
                    TotalEntradas = g.Sum(e => e.Valor),
                    TotalSaidas = 0m
                })
                .ToListAsync();

            // Query de saídas com filtro de centro de custo
            var saidasQuery = _context.Saidas
                .Include(s => s.PlanoDeContas)
                .Where(s => s.Data >= dataInicio && s.Data <= dataFim);

            // Aplicar filtro de centro de custo para Tesoureiros Locais e Pastores
            if (!User.IsInRole(Roles.Administrador) && !User.IsInRole(Roles.TesoureiroGeral))
            {
                if (user.CentroCustoId.HasValue)
                {
                    saidasQuery = saidasQuery.Where(s => s.CentroCustoId == user.CentroCustoId.Value);
                }
                else
                {
                    saidasQuery = saidasQuery.Where(s => false);
                }
            }

            var saidas = await saidasQuery
                .GroupBy(s => new { s.PlanoDeContas.Id, s.PlanoDeContas.Nome, s.PlanoDeContas.Tipo })
                .Select(g => new
                {
                    PlanoContasId = g.Key.Id,
                    PlanoContasNome = g.Key.Nome,
                    Tipo = g.Key.Tipo,
                    TotalEntradas = 0m,
                    TotalSaidas = g.Sum(s => s.Valor)
                })
                .ToListAsync();

            // Combinar entradas e saídas (Ordenação em memória)
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

        // GET: Relatorios/ContribuicoesPorMembro
        public async Task<IActionResult> ContribuicoesPorMembro(DateTime? dataInicio, DateTime? dataFim, int? membroId)
        {
            var user = await _userManager.GetUserAsync(User);

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

            // Filtro para dropdown de membros
            ViewBag.Membros = new SelectList(await _context.Membros.Where(m => m.Ativo).OrderBy(m => m.NomeCompleto).ToListAsync(), "Id", "Nome", membroId);

            var query = _context.Entradas
                .Include(e => e.Membro)
                .Include(e => e.PlanoDeContas)
                .Include(e => e.CentroCusto)
                .Include(e => e.MeioDePagamento)
                .Where(e => e.Data >= dataInicio && e.Data <= dataFim && e.MembroId != null);

            // Aplicar filtro de centro de custo para Tesoureiros Locais e Pastores
            if (!User.IsInRole(Roles.Administrador) && !User.IsInRole(Roles.TesoureiroGeral))
            {
                if (user.CentroCustoId.HasValue)
                {
                    query = query.Where(e => e.CentroCustoId == user.CentroCustoId.Value);
                }
                else
                {
                    query = query.Where(e => false);
                }
            }

            if (membroId.HasValue)
            {
                query = query.Where(e => e.MembroId == membroId.Value);
            }

            var contribuicoes = await query
                .OrderBy(e => e.Membro.NomeCompleto)
                .ThenBy(e => e.Data)
                .ToListAsync();

            // Resumo por membro (em memória)
            var resumoPorMembro = contribuicoes
                .GroupBy(e => new { e.MembroId, e.Membro.NomeCompleto })
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

        // GET: Relatorios/DespesasPorCentroCusto
        public async Task<IActionResult> DespesasPorCentroCusto(DateTime? dataInicio, DateTime? dataFim, int? centroCustoId)
        {
            var user = await _userManager.GetUserAsync(User);

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

            // Filtro para dropdown de centros de custo - CORRIGIDO
            if (User.IsInRole(Roles.Administrador) || User.IsInRole(Roles.TesoureiroGeral))
            {
                ViewBag.CentrosCusto = new SelectList(await _context.CentrosCusto.Where(c => c.Ativo).ToListAsync(), "Id", "Nome", centroCustoId);
            }
            else
            {
                // Tesoureiros Locais só veem seu próprio centro de custo
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

            var query = _context.Saidas
                .Include(s => s.CentroCusto)
                .Include(s => s.PlanoDeContas)
                .Include(s => s.Fornecedor)
                .Include(s => s.MeioDePagamento)
                .Where(s => s.Data >= dataInicio && s.Data <= dataFim);

            // Aplicar filtro de centro de custo para Tesoureiros Locais e Pastores
            if (!User.IsInRole(Roles.Administrador) && !User.IsInRole(Roles.TesoureiroGeral))
            {
                if (user.CentroCustoId.HasValue)
                {
                    query = query.Where(s => s.CentroCustoId == user.CentroCustoId.Value);
                }
                else
                {
                    query = query.Where(s => false);
                }
            }
            else
            {
                // Admin e Tesoureiro Geral podem filtrar por centro de custo
                if (centroCustoId.HasValue)
                {
                    query = query.Where(s => s.CentroCustoId == centroCustoId.Value);
                }
            }

            var despesas = await query.OrderBy(s => s.Data).ToListAsync();

            // Resumo por centro de custo (em memória)
            var resumoPorCentroCusto = despesas
                .GroupBy(s => new { s.CentroCustoId, s.CentroCusto.Nome })
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

        // ==========================================
        // NOVA ACTION: BALANCETE MENSAL
        // ==========================================

        /// <summary>
        /// GET: Relatorios/BalanceteMensal
        /// Gera o relatório de balancete mensal conforme especificação técnica
        /// </summary>
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

                // Definir período padrão (mês atual)
                if (!dataInicio.HasValue)
                {
                    dataInicio = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                }
                if (!dataFim.HasValue)
                {
                    dataFim = dataInicio.Value.AddMonths(1).AddDays(-1);
                }

                // Determinar centro de custo
                int selectedCentroCustoId;

                if (User.IsInRole(Roles.Administrador) || User.IsInRole(Roles.TesoureiroGeral))
                {
                    // Admin e Tesoureiro Geral podem escolher qualquer centro de custo
                    if (centroCustoId.HasValue)
                    {
                        selectedCentroCustoId = centroCustoId.Value;
                    }
                    else
                    {
                        // Se não especificado, pegar a Sede
                        var sede = await _context.CentrosCusto
                            .Where(c => c.Nome.Contains("Sede") || c.Nome.Contains("SEDE"))
                            .FirstOrDefaultAsync();

                        if (sede == null)
                        {
                            TempData["ErrorMessage"] = "Nenhum centro de custo encontrado. Configure a Sede primeiro.";
                            return RedirectToAction("Index");
                        }

                        selectedCentroCustoId = sede.Id;
                    }

                    // Preencher dropdown de centros de custo para filtro
                    ViewBag.CentrosCusto = new SelectList(
                        await _context.CentrosCusto
                            .Where(c => c.Ativo)
                            .OrderBy(c => c.Nome)
                            .ToListAsync(),
                        "Id",
                        "Nome",
                        selectedCentroCustoId);
                }
                else
                {
                    // Tesoureiro Local e Pastor só veem seu próprio centro de custo
                    if (!user.CentroCustoId.HasValue)
                    {
                        TempData["ErrorMessage"] = "Você não está vinculado a nenhum centro de custo.";
                        return RedirectToAction("Index");
                    }

                    selectedCentroCustoId = user.CentroCustoId.Value;
                    ViewBag.CentrosCusto = null; // Não mostrar dropdown
                }

                // Gerar o balancete usando o serviço
                var balancete = await _balanceteService.GerarBalanceteMensalAsync(
                    selectedCentroCustoId,
                    dataInicio.Value,
                    dataFim.Value);

                // Passar informações para a view
                ViewBag.DataInicio = dataInicio.Value.ToString("yyyy-MM-dd");
                ViewBag.DataFim = dataFim.Value.ToString("yyyy-MM-dd");
                ViewBag.CentroCustoId = selectedCentroCustoId;
                ViewBag.PodeEscolherCentroCusto = User.IsInRole(Roles.Administrador) ||
                                                  User.IsInRole(Roles.TesoureiroGeral);

                // Registrar auditoria
                await _auditService.LogAsync(
                    "Visualização",
                    "Relatório",
                    $"Balancete mensal visualizado: {balancete.CentroCustoNome} - {balancete.Periodo}");

                return View(balancete);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Erro ao gerar balancete: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// GET: Relatorios/BalanceteMensalPdf
        /// Exporta o balancete mensal para PDF
        /// </summary>
        [HttpGet]
        [Authorize(Policy = "Relatorios")]
        public async Task<IActionResult> BalanceteMensalPdf(
            int centroCustoId,
            DateTime dataInicio,
            DateTime dataFim)
        {
            try
            {
                // Gerar o balancete
                var balancete = await _balanceteService.GerarBalanceteMensalAsync(
                    centroCustoId,
                    dataInicio,
                    dataFim);

                // Aqui você pode implementar a geração de PDF usando o PdfService
                // ou uma biblioteca como IronPDF, iTextSharp, etc.

                // Por enquanto, retornar um placeholder
                TempData["InfoMessage"] = "Funcionalidade de exportação para PDF em desenvolvimento.";
                return RedirectToAction("BalanceteMensal", new { centroCustoId, dataInicio, dataFim });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Erro ao exportar PDF: {ex.Message}";
                return RedirectToAction("BalanceteMensal", new { centroCustoId, dataInicio, dataFim });
            }
        }

        // Exportação para Excel - Entradas
        public async Task<IActionResult> ExportarEntradasExcel(DateTime? dataInicio, DateTime? dataFim)
        {
            var user = await _userManager.GetUserAsync(User);

            if (!dataInicio.HasValue)
            {
                dataInicio = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            }
            if (!dataFim.HasValue)
            {
                dataFim = DateTime.Now.Date;
            }

            var query = _context.Entradas
                .Include(e => e.Membro)
                .Include(e => e.CentroCusto)
                .Include(e => e.MeioDePagamento)
                .Include(e => e.PlanoDeContas)
                .Where(e => e.Data >= dataInicio && e.Data <= dataFim);

            // Aplicar filtro de centro de custo para Tesoureiros Locais e Pastores
            if (!User.IsInRole(Roles.Administrador) && !User.IsInRole(Roles.TesoureiroGeral))
            {
                if (user.CentroCustoId.HasValue)
                {
                    query = query.Where(e => e.CentroCustoId == user.CentroCustoId.Value);
                }
                else
                {
                    query = query.Where(e => false);
                }
            }

            var entradas = await query.OrderBy(e => e.Data).ToListAsync();

            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Entradas");

            // Cabeçalhos
            worksheet.Cells[1, 1].Value = "Data";
            worksheet.Cells[1, 2].Value = "Valor";
            worksheet.Cells[1, 3].Value = "Descrição";
            worksheet.Cells[1, 4].Value = "Membro";
            worksheet.Cells[1, 5].Value = "Centro de Custo";
            worksheet.Cells[1, 6].Value = "Plano de Contas";
            worksheet.Cells[1, 7].Value = "Meio de Pagamento";
            worksheet.Cells[1, 8].Value = "Observações";

            // Estilo do cabeçalho
            using (var range = worksheet.Cells[1, 1, 1, 8])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
            }

            // Dados
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

            // Formatação da coluna de valor
            worksheet.Column(2).Style.Numberformat.Format = "R$ #,##0.00";
            worksheet.Cells.AutoFitColumns();

            var stream = new MemoryStream();
            package.SaveAs(stream);
            stream.Position = 0;

            var fileName = $"Entradas_{dataInicio:yyyyMMdd}_{dataFim:yyyyMMdd}.xlsx";
            return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        // Exportação para Excel - Saídas
        public async Task<IActionResult> ExportarSaidasExcel(DateTime? dataInicio, DateTime? dataFim)
        {
            var user = await _userManager.GetUserAsync(User);

            if (!dataInicio.HasValue)
            {
                dataInicio = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            }
            if (!dataFim.HasValue)
            {
                dataFim = DateTime.Now.Date;
            }

            var query = _context.Saidas
                .Include(s => s.Fornecedor)
                .Include(s => s.CentroCusto)
                .Include(s => s.MeioDePagamento)
                .Include(s => s.PlanoDeContas)
                .Where(s => s.Data >= dataInicio && s.Data <= dataFim);

            // Aplicar filtro de centro de custo para Tesoureiros Locais e Pastores
            if (!User.IsInRole(Roles.Administrador) && !User.IsInRole(Roles.TesoureiroGeral))
            {
                if (user.CentroCustoId.HasValue)
                {
                    query = query.Where(s => s.CentroCustoId == user.CentroCustoId.Value);
                }
                else
                {
                    query = query.Where(s => false);
                }
            }

            var saidas = await query.OrderBy(s => s.Data).ToListAsync();

            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Saídas");

            // Cabeçalhos
            worksheet.Cells[1, 1].Value = "Data";
            worksheet.Cells[1, 2].Value = "Valor";
            worksheet.Cells[1, 3].Value = "Descrição";
            worksheet.Cells[1, 4].Value = "Fornecedor";
            worksheet.Cells[1, 5].Value = "Centro de Custo";
            worksheet.Cells[1, 6].Value = "Plano de Contas";
            worksheet.Cells[1, 7].Value = "Meio de Pagamento";
            worksheet.Cells[1, 8].Value = "Observações";

            // Estilo do cabeçalho
            using (var range = worksheet.Cells[1, 1, 1, 8])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
            }

            // Dados
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

            // Formatação da coluna de valor
            worksheet.Column(2).Style.Numberformat.Format = "R$ #,##0.00";
            worksheet.Cells.AutoFitColumns();

            var stream = new MemoryStream();
            package.SaveAs(stream);
            stream.Position = 0;

            var fileName = $"Saidas_{dataInicio:yyyyMMdd}_{dataFim:yyyyMMdd}.xlsx";
            return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
    }
}