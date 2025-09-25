using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SistemaTesourariaEclesiastica.Data;
using SistemaTesourariaEclesiastica.Models;
using SistemaTesourariaEclesiastica.Services;
using SistemaTesourariaEclesiastica.Enums;

namespace SistemaTesourariaEclesiastica.Controllers
{
    [Authorize]
    public class FechamentoPeriodoController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly AuditService _auditService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly BusinessRulesService _businessRules;
        private readonly PdfService _pdfService;

        public FechamentoPeriodoController(
            ApplicationDbContext context,
            AuditService auditService,
            UserManager<ApplicationUser> userManager,
            BusinessRulesService businessRules,
            PdfService pdfService)
        {
            _context = context;
            _auditService = auditService;
            _userManager = userManager;
            _businessRules = businessRules;
            _pdfService = pdfService;
        }

        // GET: FechamentoPeriodo
        public async Task<IActionResult> Index()
        {
            var fechamentos = await _context.FechamentosPeriodo
                .Include(f => f.CentroCusto)
                .Include(f => f.UsuarioSubmissao)
                .Include(f => f.UsuarioAprovacao)
                .OrderByDescending(f => f.Ano)
                .ThenByDescending(f => f.Mes)
                .ToListAsync();

            await _auditService.LogAsync("Visualização", "FechamentoPeriodo", "Listagem de fechamentos visualizada");
            return View(fechamentos);
        }

        // GET: FechamentoPeriodo/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var fechamento = await _context.FechamentosPeriodo
                .Include(f => f.CentroCusto)
                .Include(f => f.UsuarioSubmissao)
                .Include(f => f.UsuarioAprovacao)
                .Include(f => f.DetalhesFechamento)
                .Include(f => f.ItensRateio)
                    .ThenInclude(i => i.RegraRateio)
                        .ThenInclude(r => r.CentroCustoDestino)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (fechamento == null)
            {
                return NotFound();
            }

            await _auditService.LogAsync("Visualização", "FechamentoPeriodo", $"Detalhes do fechamento {fechamento.Mes:00}/{fechamento.Ano} visualizados");
            return View(fechamento);
        }

        // GET: FechamentoPeriodo/Create
        public async Task<IActionResult> Create()
        {
            await PopulateDropdowns();
            
            var model = new FechamentoPeriodo
            {
                DataInicio = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                DataFim = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month)),
                Ano = DateTime.Now.Year,
                Mes = DateTime.Now.Month
            };

            return View(model);
        }

        // POST: FechamentoPeriodo/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("CentroCustoId,Ano,Mes,DataInicio,DataFim,BalancoFisico,Observacoes")] FechamentoPeriodo fechamento)
        {
            ModelState.Remove("CentroCusto");
            ModelState.Remove("UsuarioSubmissao");
            ModelState.Remove("UsuarioSubmissaoId");
            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Unauthorized();
                }

                // Verificar se já existe fechamento para este período e centro de custo
                var fechamentoExistente = await _context.FechamentosPeriodo
                    .AnyAsync(f => f.CentroCustoId == fechamento.CentroCustoId &&
                                   f.Ano == fechamento.Ano &&
                                   f.Mes == fechamento.Mes);

                if (fechamentoExistente)
                {
                    ModelState.AddModelError("", "Já existe um fechamento para este período e centro de custo.");
                    await PopulateDropdowns(fechamento);
                    return View(fechamento);
                }

                fechamento.UsuarioSubmissaoId = user.Id;
                fechamento.DataSubmissao = DateTime.Now;

                // Calcular totais do período
                await CalcularTotaisFechamento(fechamento);

                // Aplicar rateios se for tesoureiro geral (SEDE)
                var centroCusto = await _context.CentrosCusto.FindAsync(fechamento.CentroCustoId);
                if (centroCusto?.Nome.ToUpper().Contains("SEDE") == true || centroCusto?.Nome.ToUpper().Contains("GERAL") == true)
                {
                    await AplicarRateios(fechamento);
                }

                // Gerar detalhes do fechamento
                await GerarDetalhesFechamento(fechamento);

                _context.Add(fechamento);
                await _context.SaveChangesAsync();

                await _auditService.LogAsync("Criação", "FechamentoPeriodo", $"Fechamento {fechamento.Mes:00}/{fechamento.Ano} criado");
                TempData["SuccessMessage"] = "Fechamento criado com sucesso!";
                return RedirectToAction(nameof(Details), new { id = fechamento.Id });
            }

            await PopulateDropdowns(fechamento);
            return View(fechamento);
        }

        // GET: FechamentoPeriodo/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var fechamento = await _context.FechamentosPeriodo.FindAsync(id);
            if (fechamento == null)
            {
                return NotFound();
            }

            if (fechamento.Status != StatusFechamentoPeriodo.Pendente)
            {
                TempData["ErrorMessage"] = "Apenas fechamentos pendentes podem ser editados.";
                return RedirectToAction(nameof(Index));
            }

            await PopulateDropdowns(fechamento);
            return View(fechamento);
        }

        // POST: FechamentoPeriodo/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,CentroCustoId,Ano,Mes,DataInicio,DataFim,BalancoFisico,Observacoes,UsuarioSubmissaoId,DataSubmissao")] FechamentoPeriodo fechamento)
        {
            if (id != fechamento.Id)
            {
                return NotFound();
            }

            ModelState.Remove("CentroCusto");
            ModelState.Remove("UsuarioSubmissao");
            ModelState.Remove("UsuarioSubmissaoId");
            if (ModelState.IsValid)
            {
                try
                {
                    // Recalcular totais
                    await CalcularTotaisFechamento(fechamento);

                    // Limpar rateios e detalhes existentes
                    var itensRateio = await _context.ItensRateioFechamento.Where(i => i.FechamentoPeriodoId == id).ToListAsync();
                    var detalhes = await _context.DetalhesFechamento.Where(d => d.FechamentoPeriodoId == id).ToListAsync();
                    
                    _context.ItensRateioFechamento.RemoveRange(itensRateio);
                    _context.DetalhesFechamento.RemoveRange(detalhes);

                    // Reaplicar rateios se necessário
                    var centroCusto = await _context.CentrosCusto.FindAsync(fechamento.CentroCustoId);
                    if (centroCusto?.Nome.ToUpper().Contains("SEDE") == true || centroCusto?.Nome.ToUpper().Contains("GERAL") == true)
                    {
                        await AplicarRateios(fechamento);
                    }

                    // Regerar detalhes
                    await GerarDetalhesFechamento(fechamento);

                    _context.Update(fechamento);
                    await _context.SaveChangesAsync();

                    await _auditService.LogAsync("Edição", "FechamentoPeriodo", $"Fechamento {fechamento.Mes:00}/{fechamento.Ano} editado");
                    TempData["SuccessMessage"] = "Fechamento atualizado com sucesso!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!FechamentoPeriodoExists(fechamento.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Details), new { id = fechamento.Id });
            }

            await PopulateDropdowns(fechamento);
            return View(fechamento);
        }

        // POST: FechamentoPeriodo/Aprovar/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Aprovar(int id)
        {
            var fechamento = await _context.FechamentosPeriodo.FindAsync(id);
            if (fechamento == null)
            {
                return NotFound();
            }

            if (fechamento.Status != StatusFechamentoPeriodo.Pendente)
            {
                TempData["ErrorMessage"] = "Apenas fechamentos pendentes podem ser aprovados.";
                return RedirectToAction(nameof(Index));
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            fechamento.Status = StatusFechamentoPeriodo.Aprovado;
            fechamento.DataAprovacao = DateTime.Now;
            fechamento.UsuarioAprovacaoId = user.Id;

            _context.Update(fechamento);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync("Aprovação", "FechamentoPeriodo", $"Fechamento {fechamento.Mes:00}/{fechamento.Ano} aprovado");
            TempData["SuccessMessage"] = "Fechamento aprovado com sucesso!";

            return RedirectToAction(nameof(Index));
        }

        // GET: FechamentoPeriodo/GerarPdf/5
        public async Task<IActionResult> GerarPdf(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var fechamento = await _context.FechamentosPeriodo
                .Include(f => f.CentroCusto)
                .Include(f => f.UsuarioSubmissao)
                .Include(f => f.UsuarioAprovacao)
                .Include(f => f.DetalhesFechamento)
                .Include(f => f.ItensRateio)
                    .ThenInclude(i => i.RegraRateio)
                        .ThenInclude(r => r.CentroCustoDestino)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (fechamento == null)
            {
                return NotFound();
            }

            var pdfBytes = _pdfService.GerarReciboFechamento(fechamento);
            var fileName = $"Fechamento_{fechamento.CentroCusto.Nome}_{fechamento.Mes:00}_{fechamento.Ano}.pdf";

            await _auditService.LogAsync("Geração PDF", "FechamentoPeriodo", $"PDF do fechamento {fechamento.Mes:00}/{fechamento.Ano} gerado");

            return File(pdfBytes, "application/pdf", fileName);
        }

        // GET: FechamentoPeriodo/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var fechamento = await _context.FechamentosPeriodo
                .Include(f => f.CentroCusto)
                .Include(f => f.UsuarioSubmissao)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (fechamento == null)
            {
                return NotFound();
            }

            if (fechamento.Status != StatusFechamentoPeriodo.Pendente)
            {
                TempData["ErrorMessage"] = "Apenas fechamentos pendentes podem ser excluídos.";
                return RedirectToAction(nameof(Index));
            }

            return View(fechamento);
        }

        // POST: FechamentoPeriodo/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var fechamento = await _context.FechamentosPeriodo.FindAsync(id);
            if (fechamento != null)
            {
                if (fechamento.Status != StatusFechamentoPeriodo.Pendente)
                {
                    TempData["ErrorMessage"] = "Apenas fechamentos pendentes podem ser excluídos.";
                    return RedirectToAction(nameof(Index));
                }

                // Remover detalhes e rateios relacionados
                var itensRateio = await _context.ItensRateioFechamento.Where(i => i.FechamentoPeriodoId == id).ToListAsync();
                var detalhes = await _context.DetalhesFechamento.Where(d => d.FechamentoPeriodoId == id).ToListAsync();
                
                _context.ItensRateioFechamento.RemoveRange(itensRateio);
                _context.DetalhesFechamento.RemoveRange(detalhes);
                _context.FechamentosPeriodo.Remove(fechamento);
                
                await _context.SaveChangesAsync();

                await _auditService.LogAsync("Exclusão", "FechamentoPeriodo", $"Fechamento {fechamento.Mes:00}/{fechamento.Ano} excluído");
                TempData["SuccessMessage"] = "Fechamento excluído com sucesso!";
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task CalcularTotaisFechamento(FechamentoPeriodo fechamento)
        {
            // Calcular total de entradas
            fechamento.TotalEntradas = await _context.Entradas
                .Where(e => e.CentroCustoId == fechamento.CentroCustoId &&
                           e.Data >= fechamento.DataInicio &&
                           e.Data <= fechamento.DataFim)
                .SumAsync(e => e.Valor);

            // Calcular total de saídas
            fechamento.TotalSaidas = await _context.Saidas
                .Where(s => s.CentroCustoId == fechamento.CentroCustoId &&
                           s.Data >= fechamento.DataInicio &&
                           s.Data <= fechamento.DataFim)
                .SumAsync(s => s.Valor);

            // Calcular balanço digital
            fechamento.BalancoDigital = fechamento.TotalEntradas - fechamento.TotalSaidas;
        }

        private async Task AplicarRateios(FechamentoPeriodo fechamento)
        {
            var regrasRateio = await _context.RegrasRateio
                .Include(r => r.CentroCustoDestino)
                .Where(r => r.CentroCustoOrigemId == fechamento.CentroCustoId && r.Ativo)
                .ToListAsync();

            decimal totalRateios = 0;

            foreach (var regra in regrasRateio)
            {
                var valorRateio = fechamento.BalancoDigital * (regra.Percentual / 100);
                
                var itemRateio = new ItemRateioFechamento
                {
                    FechamentoPeriodoId = fechamento.Id,
                    RegraRateioId = regra.Id,
                    ValorBase = fechamento.BalancoDigital,
                    Percentual = regra.Percentual,
                    ValorRateio = valorRateio,
                    Observacoes = $"Rateio automático aplicado conforme regra {regra.Nome}"
                };

                fechamento.ItensRateio.Add(itemRateio);
                totalRateios += valorRateio;
            }

            fechamento.TotalRateios = totalRateios;
            fechamento.SaldoFinal = fechamento.BalancoDigital - totalRateios;
        }

        private async Task GerarDetalhesFechamento(FechamentoPeriodo fechamento)
        {
            // Gerar detalhes das entradas
            var entradas = await _context.Entradas
                .Include(e => e.PlanoDeContas)
                .Include(e => e.Membro)
                .Where(e => e.CentroCustoId == fechamento.CentroCustoId &&
                           e.Data >= fechamento.DataInicio &&
                           e.Data <= fechamento.DataFim)
                .ToListAsync();

            foreach (var entrada in entradas)
            {
                var detalhe = new DetalheFechamento
                {
                    FechamentoPeriodoId = fechamento.Id,
                    TipoMovimento = "Entrada",
                    Descricao = entrada.Descricao,
                    Valor = entrada.Valor,
                    Data = entrada.Data,
                    PlanoContas = entrada.PlanoDeContas?.Nome,
                    Membro = entrada.Membro?.Nome,
                    Observacoes = entrada.Observacoes
                };

                fechamento.DetalhesFechamento.Add(detalhe);
            }

            // Gerar detalhes das saídas
            var saidas = await _context.Saidas
                .Include(s => s.PlanoDeContas)
                .Include(s => s.Fornecedor)
                .Where(s => s.CentroCustoId == fechamento.CentroCustoId &&
                           s.Data >= fechamento.DataInicio &&
                           s.Data <= fechamento.DataFim)
                .ToListAsync();

            foreach (var saida in saidas)
            {
                var detalhe = new DetalheFechamento
                {
                    FechamentoPeriodoId = fechamento.Id,
                    TipoMovimento = "Saida",
                    Descricao = saida.Descricao,
                    Valor = saida.Valor,
                    Data = saida.Data,
                    PlanoContas = saida.PlanoDeContas?.Nome,
                    Fornecedor = saida.Fornecedor?.Nome,
                    Observacoes = saida.Observacoes
                };

                fechamento.DetalhesFechamento.Add(detalhe);
            }
        }

        private async Task PopulateDropdowns(FechamentoPeriodo? fechamento = null)
        {
            ViewData["CentroCustoId"] = new SelectList(
                await _context.CentrosCusto.Where(c => c.Ativo).OrderBy(c => c.Nome).ToListAsync(),
                "Id", "Nome", fechamento?.CentroCustoId);
        }

        private bool FechamentoPeriodoExists(int id)
        {
            return _context.FechamentosPeriodo.Any(e => e.Id == id);
        }
    }
}
