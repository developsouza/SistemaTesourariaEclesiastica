using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using OfficeOpenXml.Style;
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

        public RelatoriosController(ApplicationDbContext context, AuditService auditService, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _auditService = auditService;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            // Cálculo otimizado dos totais
            var totaisTask = _context.Entradas
                .Select(e => e.Valor)
                .SumAsync();

            var totalSaidasTask = _context.Saidas
                .Select(s => s.Valor)
                .SumAsync();

            await Task.WhenAll(totaisTask, totalSaidasTask);

            var totalEntradas = await totaisTask;
            var totalSaidas = await totalSaidasTask;
            var saldoAtual = totalEntradas - totalSaidas;

            ViewBag.TotalEntradas = totalEntradas.ToString("C2");
            ViewBag.TotalSaidas = totalSaidas.ToString("C2");
            ViewBag.SaldoAtual = saldoAtual.ToString("C2");

            // Dados para gráficos - Entradas por Centro de Custo (CORRIGIDO)
            var entradasPorCentroCusto = await _context.Entradas
                .GroupBy(e => new { e.CentroCustoId, e.CentroCusto.Nome })
                .Select(g => new
                {
                    CentroCusto = g.Key.Nome ?? "Sem Centro de Custo",
                    Total = g.Sum(e => e.Valor)
                })
                .ToListAsync(); // Materializa primeiro

            // Ordena em memória (LINQ to Objects)
            entradasPorCentroCusto = entradasPorCentroCusto
                .OrderByDescending(x => x.Total)
                .ToList();

            ViewBag.EntradasPorCentroCustoLabels = entradasPorCentroCusto.Select(x => x.CentroCusto).ToList();
            ViewBag.EntradasPorCentroCustoData = entradasPorCentroCusto.Select(x => x.Total).ToList();

            // Dados para gráficos - Saídas por Plano de Contas (CORRIGIDO)
            var saidasPorPlanoContas = await _context.Saidas
                .Include(s => s.PlanoDeContas)
                .GroupBy(s => new { Nome = s.PlanoDeContas.Nome ?? "Sem Plano de Contas" })
                .Select(g => new { PlanoContas = g.Key.Nome, Total = g.Sum(s => s.Valor) })
                .ToListAsync(); // Materializa primeiro

            // Ordena em memória (LINQ to Objects)
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

            var entradas = await _context.Entradas
                .Where(e => e.Data >= dataInicio && e.Data <= dataFim)
                .OrderBy(e => e.Data)
                .ToListAsync();

            var saidas = await _context.Saidas
                .Where(s => s.Data >= dataInicio && s.Data <= dataFim)
                .OrderBy(s => s.Data)
                .ToListAsync();

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

            var entradas = await _context.Entradas
                .Include(e => e.Membro)
                .Include(e => e.CentroCusto)
                .Include(e => e.MeioDePagamento)
                .Include(e => e.PlanoDeContas)
                .Where(e => e.Data >= dataInicio && e.Data <= dataFim)
                .OrderBy(e => e.Data)
                .ToListAsync();

            return View(entradas);
        }

        // GET: Relatorios/SaidasPorPeriodo
        public async Task<IActionResult> SaidasPorPeriodo(DateTime? dataInicio, DateTime? dataFim, int? centroCustoId, int? fornecedorId)
        {
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

            // Filtros para dropdowns
            ViewBag.CentrosCusto = new SelectList(await _context.CentrosCusto.Where(c => c.Ativo).ToListAsync(), "Id", "Nome", centroCustoId);
            ViewBag.Fornecedores = new SelectList(await _context.Fornecedores.Where(f => f.Ativo).ToListAsync(), "Id", "Nome", fornecedorId);

            var query = _context.Saidas
                .Include(s => s.Fornecedor)
                .Include(s => s.CentroCusto)
                .Include(s => s.MeioDePagamento)
                .Include(s => s.PlanoDeContas)
                .Where(s => s.Data >= dataInicio && s.Data <= dataFim);

            if (centroCustoId.HasValue)
            {
                query = query.Where(s => s.CentroCustoId == centroCustoId.Value);
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

            // Entradas por Plano de Contas (CORRIGIDO)
            var entradas = await _context.Entradas
                .Include(e => e.PlanoDeContas)
                .Where(e => e.Data >= dataInicio && e.Data <= dataFim)
                .GroupBy(e => new { e.PlanoDeContas.Id, e.PlanoDeContas.Nome, e.PlanoDeContas.Tipo })
                .Select(g => new
                {
                    PlanoContasId = g.Key.Id,
                    PlanoContasNome = g.Key.Nome,
                    Tipo = g.Key.Tipo,
                    TotalEntradas = g.Sum(e => e.Valor),
                    TotalSaidas = 0m
                })
                .ToListAsync(); // Materializa primeiro

            // Saídas por Plano de Contas (CORRIGIDO)
            var saidas = await _context.Saidas
                .Include(s => s.PlanoDeContas)
                .Where(s => s.Data >= dataInicio && s.Data <= dataFim)
                .GroupBy(s => new { s.PlanoDeContas.Id, s.PlanoDeContas.Nome, s.PlanoDeContas.Tipo })
                .Select(g => new
                {
                    PlanoContasId = g.Key.Id,
                    PlanoContasNome = g.Key.Nome,
                    Tipo = g.Key.Tipo,
                    TotalEntradas = 0m,
                    TotalSaidas = g.Sum(s => s.Valor)
                })
                .ToListAsync(); // Materializa primeiro

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

            if (membroId.HasValue)
            {
                query = query.Where(e => e.MembroId == membroId.Value);
            }

            var contribuicoes = await query
                .OrderBy(e => e.Membro.NomeCompleto) // Mudança aqui: Nome -> NomeCompleto
                .ThenBy(e => e.Data)
                .ToListAsync();

            // Resumo por membro (em memória - CORRIGIDO)
            var resumoPorMembro = contribuicoes
                .GroupBy(e => new { e.MembroId, e.Membro.NomeCompleto }) // Mudança aqui também
                .Select(g => new
                {
                    MembroNome = g.Key.NomeCompleto, // E aqui
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

            // Filtro para dropdown de centros de custo
            ViewBag.CentrosCusto = new SelectList(await _context.CentrosCusto.Where(c => c.Ativo).ToListAsync(), "Id", "Nome", centroCustoId);

            var query = _context.Saidas
                .Include(s => s.CentroCusto)
                .Include(s => s.PlanoDeContas)
                .Include(s => s.Fornecedor)
                .Include(s => s.MeioDePagamento)
                .Where(s => s.Data >= dataInicio && s.Data <= dataFim);

            if (centroCustoId.HasValue)
            {
                query = query.Where(s => s.CentroCustoId == centroCustoId.Value);
            }

            var despesas = await query.OrderBy(s => s.Data).ToListAsync();

            // Resumo por centro de custo (em memória - CORRIGIDO)
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

        // Exportação para Excel - Entradas
        public async Task<IActionResult> ExportarEntradasExcel(DateTime? dataInicio, DateTime? dataFim)
        {
            if (!dataInicio.HasValue)
            {
                dataInicio = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            }
            if (!dataFim.HasValue)
            {
                dataFim = DateTime.Now.Date;
            }

            var entradas = await _context.Entradas
                .Include(e => e.Membro)
                .Include(e => e.CentroCusto)
                .Include(e => e.MeioDePagamento)
                .Include(e => e.PlanoDeContas)
                .Where(e => e.Data >= dataInicio && e.Data <= dataFim)
                .OrderBy(e => e.Data)
                .ToListAsync();

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
            if (!dataInicio.HasValue)
            {
                dataInicio = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            }
            if (!dataFim.HasValue)
            {
                dataFim = DateTime.Now.Date;
            }

            var saidas = await _context.Saidas
                .Include(s => s.Fornecedor)
                .Include(s => s.CentroCusto)
                .Include(s => s.MeioDePagamento)
                .Include(s => s.PlanoDeContas)
                .Where(s => s.Data >= dataInicio && s.Data <= dataFim)
                .OrderBy(s => s.Data)
                .ToListAsync();

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