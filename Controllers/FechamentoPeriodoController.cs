using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SistemaTesourariaEclesiastica.Data;
using SistemaTesourariaEclesiastica.Enums;
using SistemaTesourariaEclesiastica.Models;
using SistemaTesourariaEclesiastica.Services;

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
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var fechamento = new FechamentoPeriodo
            {
                TipoFechamento = TipoFechamento.Mensal,
                DataInicio = DateTime.Now.Date,
                Mes = DateTime.Now.Month,
                Ano = DateTime.Now.Year
            };

            await PopulateDropdowns(fechamento);
            return View(fechamento);
        }

        // POST: FechamentoPeriodo/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(FechamentoPeriodo fechamento)
        {
            ModelState.Remove("DataFim");
            ModelState.Remove("CentroCusto");
            ModelState.Remove("UsuarioSubmissao");
            ModelState.Remove("UsuarioSubmissaoId");
            if (ModelState.IsValid)
            {
                try
                {
                    var user = await _userManager.GetUserAsync(User);
                    if (user == null)
                    {
                        TempData["ErrorMessage"] = "Usuário não autenticado.";
                        return RedirectToAction(nameof(Index));
                    }

                    // VALIDAR E AJUSTAR DATAS CONFORME TIPO DE FECHAMENTO
                    ValidarTipoFechamento(fechamento);

                    fechamento.UsuarioSubmissaoId = user.Id;
                    fechamento.DataSubmissao = DateTime.Now;
                    fechamento.Status = StatusFechamentoPeriodo.Pendente;

                    // Calcular totais do período
                    await CalcularTotaisFechamento(fechamento);

                    // =====================================================
                    // APLICAR RATEIOS (com verificação melhorada)
                    // =====================================================
                    await AplicarRateios(fechamento);

                    // Gerar detalhes do fechamento
                    await GerarDetalhesFechamento(fechamento);

                    _context.Add(fechamento);
                    await _context.SaveChangesAsync();

                    await _auditService.LogAsync("Criação", "FechamentoPeriodo",
                        $"Fechamento {ObterDescricaoFechamento(fechamento)} criado");

                    TempData["SuccessMessage"] = "Fechamento criado com sucesso!";
                    return RedirectToAction(nameof(Details), new { id = fechamento.Id });
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"Erro ao criar fechamento: {ex.Message}";
                }
            }

            await PopulateDropdowns(fechamento);
            return View(fechamento);
        }

        // GET: FechamentoPeriodo/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var fechamento = await _context.FechamentosPeriodo
                .Include(f => f.CentroCusto)
                .Include(f => f.UsuarioSubmissao)
                .Include(f => f.UsuarioAprovacao)
                .Include(f => f.ItensRateio)
                .Include(f => f.DetalhesFechamento)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (fechamento == null)
            {
                return NotFound();
            }

            // Verificar se o fechamento pode ser editado
            if (fechamento.Status != StatusFechamentoPeriodo.Pendente)
            {
                TempData["ErrorMessage"] = "Apenas fechamentos pendentes podem ser editados.";
                return RedirectToAction(nameof(Details), new { id = fechamento.Id });
            }

            // Verificar permissões
            var user = await _userManager.GetUserAsync(User);
            if (!User.IsInRole("Administrador") && !User.IsInRole("TesoureiroGeral"))
            {
                // Tesoureiro Local só pode editar fechamentos do seu próprio centro de custo
                if (fechamento.CentroCustoId != user.CentroCustoId)
                {
                    TempData["ErrorMessage"] = "Você não tem permissão para editar este fechamento.";
                    return RedirectToAction(nameof(Index));
                }
            }

            await PopulateDropdowns(fechamento);
            return View(fechamento);
        }

        // POST: FechamentoPeriodo/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Observacoes")] FechamentoPeriodo fechamentoForm)
        {
            if (id != fechamentoForm.Id)
            {
                return NotFound();
            }

            // Buscar o fechamento original do banco COM TRACKING
            var fechamento = await _context.FechamentosPeriodo
                .Include(f => f.CentroCusto)
                .Include(f => f.UsuarioSubmissao)
                .Include(f => f.ItensRateio)
                .Include(f => f.DetalhesFechamento)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (fechamento == null)
            {
                return NotFound();
            }

            // Verificar se ainda está pendente
            if (fechamento.Status != StatusFechamentoPeriodo.Pendente)
            {
                TempData["ErrorMessage"] = "Apenas fechamentos pendentes podem ser editados.";
                return RedirectToAction(nameof(Details), new { id = fechamento.Id });
            }

            // Verificar permissões
            var user = await _userManager.GetUserAsync(User);
            if (!User.IsInRole("Administrador") && !User.IsInRole("TesoureiroGeral"))
            {
                if (fechamento.CentroCustoId != user.CentroCustoId)
                {
                    TempData["ErrorMessage"] = "Você não tem permissão para editar este fechamento.";
                    return RedirectToAction(nameof(Index));
                }
            }

            try
            {
                // =====================================================
                // ATUALIZAR APENAS AS OBSERVAÇÕES (único campo editável)
                // =====================================================
                fechamento.Observacoes = fechamentoForm.Observacoes;

                // =====================================================
                // REMOVER ITENS ANTIGOS
                // =====================================================
                if (fechamento.ItensRateio.Any())
                {
                    _context.ItensRateioFechamento.RemoveRange(fechamento.ItensRateio);
                }

                if (fechamento.DetalhesFechamento.Any())
                {
                    _context.DetalhesFechamento.RemoveRange(fechamento.DetalhesFechamento);
                }

                // Limpar as coleções
                fechamento.ItensRateio.Clear();
                fechamento.DetalhesFechamento.Clear();

                // =====================================================
                // RECALCULAR TODOS OS VALORES
                // =====================================================
                await CalcularTotaisFechamento(fechamento);

                // =====================================================
                // REAPLICAR RATEIOS SE FOR SEDE
                // =====================================================
                var centroCusto = await _context.CentrosCusto.FindAsync(fechamento.CentroCustoId);
                if (centroCusto?.Nome.ToUpper().Contains("SEDE") == true ||
                    centroCusto?.Nome.ToUpper().Contains("GERAL") == true)
                {
                    await AplicarRateios(fechamento);
                }
                else
                {
                    fechamento.TotalRateios = 0;
                    fechamento.SaldoFinal = fechamento.BalancoDigital;
                }

                // =====================================================
                // REGERAR DETALHES
                // =====================================================
                await GerarDetalhesFechamento(fechamento);

                // =====================================================
                // SALVAR (Entity já está sendo rastreado)
                // =====================================================
                await _context.SaveChangesAsync();

                // Log de auditoria
                await _auditService.LogAsync("Atualização", "FechamentoPeriodo",
                    $"Fechamento {(fechamento.TipoFechamento == TipoFechamento.Diario ? fechamento.DataInicio.ToString("dd/MM/yyyy") : $"{fechamento.Mes:00}/{fechamento.Ano}")} atualizado");

                TempData["SuccessMessage"] = "Fechamento atualizado com sucesso! Valores recalculados automaticamente.";
                return RedirectToAction(nameof(Details), new { id = fechamento.Id });
            }
            catch (DbUpdateConcurrencyException ex)
            {
                if (!FechamentoPeriodoExists(fechamento.Id))
                {
                    return NotFound();
                }
                else
                {
                    TempData["ErrorMessage"] = "Este fechamento foi modificado por outro usuário. Por favor, tente novamente.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Erro ao atualizar fechamento: {ex.Message}";
            }

            // Se chegou aqui, houve erro - recarregar o formulário
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
        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var fechamento = await _context.FechamentosPeriodo
                .Include(f => f.CentroCusto)
                .Include(f => f.UsuarioSubmissao)
                .Include(f => f.UsuarioAprovacao)
                .Include(f => f.ItensRateio)
                    .ThenInclude(i => i.RegraRateio)
                        .ThenInclude(r => r.CentroCustoDestino)
                .Include(f => f.DetalhesFechamento)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (fechamento == null)
            {
                return NotFound();
            }

            // Verificar permissões
            var user = await _userManager.GetUserAsync(User);
            if (!User.IsInRole("Administrador") && !User.IsInRole("TesoureiroGeral"))
            {
                if (fechamento.CentroCustoId != user.CentroCustoId)
                {
                    TempData["ErrorMessage"] = "Você não tem permissão para excluir este fechamento.";
                    return RedirectToAction(nameof(Index));
                }
            }

            return View(fechamento);
        }

        // POST: FechamentoPeriodo/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var fechamento = await _context.FechamentosPeriodo
                .Include(f => f.ItensRateio)
                .Include(f => f.DetalhesFechamento)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (fechamento == null)
            {
                return NotFound();
            }

            // Apenas fechamentos pendentes podem ser excluídos
            if (fechamento.Status != StatusFechamentoPeriodo.Pendente)
            {
                TempData["ErrorMessage"] = $"Não é possível excluir fechamentos com status '{fechamento.Status}'. Apenas fechamentos pendentes podem ser excluídos.";
                return RedirectToAction(nameof(Index));
            }

            // Verificar permissões
            var user = await _userManager.GetUserAsync(User);
            if (!User.IsInRole("Administrador") && !User.IsInRole("TesoureiroGeral"))
            {
                if (fechamento.CentroCustoId != user.CentroCustoId)
                {
                    TempData["ErrorMessage"] = "Você não tem permissão para excluir este fechamento.";
                    return RedirectToAction(nameof(Index));
                }
            }

            try
            {
                // Remover itens relacionados (Cascade já está configurado, mas vamos garantir)
                _context.ItensRateioFechamento.RemoveRange(fechamento.ItensRateio);
                _context.DetalhesFechamento.RemoveRange(fechamento.DetalhesFechamento);
                _context.FechamentosPeriodo.Remove(fechamento);

                await _context.SaveChangesAsync();

                await _auditService.LogAsync("Exclusão", "FechamentoPeriodo",
                    $"Fechamento {(fechamento.TipoFechamento == TipoFechamento.Diario ? fechamento.DataInicio.ToString("dd/MM/yyyy") : $"{fechamento.Mes:00}/{fechamento.Ano}")} excluído");

                TempData["SuccessMessage"] = "Fechamento excluído com sucesso!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Erro ao excluir fechamento: {ex.Message}";
                return RedirectToAction(nameof(Delete), new { id });
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task CalcularTotaisFechamento(FechamentoPeriodo fechamento)
        {
            // Total de entradas FÍSICAS (Dinheiro)
            fechamento.TotalEntradasFisicas = await _context.Entradas
                .Include(e => e.MeioDePagamento)
                .Where(e => e.CentroCustoId == fechamento.CentroCustoId &&
                           e.Data >= fechamento.DataInicio &&
                           e.Data <= fechamento.DataFim &&
                           e.MeioDePagamento.TipoCaixa == TipoCaixa.Fisico)
                .SumAsync(e => (decimal?)e.Valor) ?? 0;

            // Total de entradas DIGITAIS
            fechamento.TotalEntradasDigitais = await _context.Entradas
                .Include(e => e.MeioDePagamento)
                .Where(e => e.CentroCustoId == fechamento.CentroCustoId &&
                           e.Data >= fechamento.DataInicio &&
                           e.Data <= fechamento.DataFim &&
                           e.MeioDePagamento.TipoCaixa == TipoCaixa.Digital)
                .SumAsync(e => (decimal?)e.Valor) ?? 0;

            // Total de saídas FÍSICAS
            fechamento.TotalSaidasFisicas = await _context.Saidas
                .Include(s => s.MeioDePagamento)
                .Where(s => s.CentroCustoId == fechamento.CentroCustoId &&
                           s.Data >= fechamento.DataInicio &&
                           s.Data <= fechamento.DataFim &&
                           s.MeioDePagamento.TipoCaixa == TipoCaixa.Fisico)
                .SumAsync(s => (decimal?)s.Valor) ?? 0;

            // Total de saídas DIGITAIS
            fechamento.TotalSaidasDigitais = await _context.Saidas
                .Include(s => s.MeioDePagamento)
                .Where(s => s.CentroCustoId == fechamento.CentroCustoId &&
                           s.Data >= fechamento.DataInicio &&
                           s.Data <= fechamento.DataFim &&
                           s.MeioDePagamento.TipoCaixa == TipoCaixa.Digital)
                .SumAsync(s => (decimal?)s.Valor) ?? 0;

            // TOTAIS GERAIS
            fechamento.TotalEntradas = fechamento.TotalEntradasFisicas + fechamento.TotalEntradasDigitais;
            fechamento.TotalSaidas = fechamento.TotalSaidasFisicas + fechamento.TotalSaidasDigitais;

            // BALANÇOS SEPARADOS
            fechamento.BalancoFisico = fechamento.TotalEntradasFisicas - fechamento.TotalSaidasFisicas;
            fechamento.BalancoDigital = fechamento.TotalEntradasDigitais - fechamento.TotalSaidasDigitais;
        }


        private async Task AplicarRateios(FechamentoPeriodo fechamento)
        {
            // Buscar o centro de custo com Include para garantir que está carregado
            var centroCusto = await _context.CentrosCusto
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == fechamento.CentroCustoId);

            if (centroCusto == null)
            {
                return;
            }

            // =====================================================
            // VERIFICAÇÃO MELHORADA: É SEDE/GERAL?
            // =====================================================
            var nomeCentro = centroCusto.Nome.ToUpper().Trim();
            var ehSede = nomeCentro.Contains("SEDE") ||
                         nomeCentro.Contains("GERAL") ||
                         nomeCentro.Contains("PRINCIPAL") ||
                         nomeCentro.Contains("CENTRAL");


            if (!ehSede)
            {
                fechamento.TotalRateios = 0;
                fechamento.SaldoFinal = fechamento.BalancoDigital;
                return;
            }

            // =====================================================
            // BUSCAR REGRAS DE RATEIO ATIVAS
            // =====================================================
            var regrasRateio = await _context.RegrasRateio
                .Include(r => r.CentroCustoOrigem)
                .Include(r => r.CentroCustoDestino)
                .Where(r => r.CentroCustoOrigemId == fechamento.CentroCustoId && r.Ativo)
                .ToListAsync();


            if (!regrasRateio.Any())
            {
                fechamento.TotalRateios = 0;
                fechamento.SaldoFinal = fechamento.BalancoDigital;
                return;
            }

            // =====================================================
            // APLICAR CADA REGRA DE RATEIO
            // =====================================================
            decimal totalRateios = 0;
            var valorBase = fechamento.BalancoDigital;

            foreach (var regra in regrasRateio)
            {
                var valorRateio = valorBase * (regra.Percentual / 100);

                var itemRateio = new ItemRateioFechamento
                {
                    FechamentoPeriodoId = fechamento.Id,
                    RegraRateioId = regra.Id,
                    ValorBase = valorBase,
                    Percentual = regra.Percentual,
                    ValorRateio = valorRateio,
                    Observacoes = $"Rateio automático de {regra.Percentual:F2}% sobre balanço digital de {valorBase:C} = {valorRateio:C} para {regra.CentroCustoDestino.Nome}"
                };

                fechamento.ItensRateio.Add(itemRateio);
                totalRateios += valorRateio;
            }

            fechamento.TotalRateios = totalRateios;
            fechamento.SaldoFinal = fechamento.BalancoDigital - totalRateios;

        }

        // ==================================================
        // MÉTODO AUXILIAR: Validar Tipo de Fechamento
        // ==================================================
        private void ValidarTipoFechamento(FechamentoPeriodo fechamento)
        {
            if (fechamento.TipoFechamento == TipoFechamento.Diario)
            {
                // Para fechamento diário, data início = data fim
                fechamento.DataFim = fechamento.DataInicio;

                // Extrair mês e ano da data
                fechamento.Mes = fechamento.DataInicio.Month;
                fechamento.Ano = fechamento.DataInicio.Year;
            }
            else if (fechamento.TipoFechamento == TipoFechamento.Semanal)
            {
                // Para fechamento semanal, validar que as datas foram informadas
                if (fechamento.DataInicio == default || fechamento.DataFim == default)
                {
                    throw new InvalidOperationException("Para fechamento semanal, é necessário informar a data de início e fim da semana.");
                }

                // Validar que a data fim é posterior à data início
                if (fechamento.DataFim < fechamento.DataInicio)
                {
                    throw new InvalidOperationException("A data de fim deve ser posterior ou igual à data de início.");
                }

                // Validar que não excede 7 dias (mas permitir exatamente 7)
                var diasDiferenca = (fechamento.DataFim - fechamento.DataInicio).Days;
                if (diasDiferenca > 6)
                {
                }

                // Extrair mês e ano da data de início
                fechamento.Mes = fechamento.DataInicio.Month;
                fechamento.Ano = fechamento.DataInicio.Year;
            }
            else if (fechamento.TipoFechamento == TipoFechamento.Mensal)
            {
                // Para fechamento mensal, validar que Mês e Ano foram informados
                if (!fechamento.Mes.HasValue || !fechamento.Ano.HasValue)
                {
                    throw new InvalidOperationException("Para fechamento mensal, é necessário informar Mês e Ano.");
                }

                // Definir data início e fim do mês
                fechamento.DataInicio = new DateTime(fechamento.Ano.Value, fechamento.Mes.Value, 1);
                fechamento.DataFim = fechamento.DataInicio.AddMonths(1).AddDays(-1);
            }
        }


        private async Task GerarDetalhesFechamento(FechamentoPeriodo fechamento)
        {
            // Gerar detalhes das entradas
            var entradas = await _context.Entradas
                .Include(e => e.PlanoDeContas)
                .Include(e => e.MeioDePagamento)
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
                    MeioPagamento = entrada.MeioDePagamento?.Nome,
                    Membro = entrada.Membro?.NomeCompleto,
                    Observacoes = entrada.Observacoes
                };

                fechamento.DetalhesFechamento.Add(detalhe);
            }

            // Gerar detalhes das saídas
            var saidas = await _context.Saidas
                .Include(s => s.PlanoDeContas)
                .Include(s => s.MeioDePagamento)
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
                    MeioPagamento = saida.MeioDePagamento?.Nome,
                    Fornecedor = saida.Fornecedor?.Nome,
                    Observacoes = saida.Observacoes
                };

                fechamento.DetalhesFechamento.Add(detalhe);
            }
        }

        private async Task PopulateDropdowns(FechamentoPeriodo? fechamento = null)
        {
            // Popular Centro de Custo
            var user = await _userManager.GetUserAsync(User);

            if (User.IsInRole("Administrador") || User.IsInRole("TesoureiroGeral"))
            {
                // Admin e Tesoureiro Geral veem todos os centros
                ViewBag.CentrosCusto = new SelectList(
                    await _context.CentrosCusto
                        .Where(c => c.Ativo)
                        .OrderBy(c => c.Nome)
                        .ToListAsync(),
                    "Id",
                    "Nome",
                    fechamento?.CentroCustoId
                );
            }
            else
            {
                // Tesoureiro Local e Pastor veem apenas seu centro
                ViewBag.CentrosCusto = new SelectList(
                    await _context.CentrosCusto
                        .Where(c => c.Id == user.CentroCustoId && c.Ativo)
                        .ToListAsync(),
                    "Id",
                    "Nome",
                    fechamento?.CentroCustoId
                );
            }
        }

        // Adicione este método auxiliar na classe FechamentoPeriodoController para corrigir o erro CS0103

        private string ObterDescricaoFechamento(FechamentoPeriodo fechamento)
        {
            if (fechamento.TipoFechamento == TipoFechamento.Diario)
                return fechamento.DataInicio.ToString("dd/MM/yyyy");
            if (fechamento.TipoFechamento == TipoFechamento.Semanal)
                return $"Semana de {fechamento.DataInicio:dd/MM/yyyy} a {fechamento.DataFim:dd/MM/yyyy}";
            if (fechamento.TipoFechamento == TipoFechamento.Mensal)
                return $"{fechamento.Mes:00}/{fechamento.Ano}";
            return fechamento.Id.ToString();
        }

        private bool FechamentoPeriodoExists(int id)
        {
            return _context.FechamentosPeriodo.Any(f => f.Id == id);
        }
    }
}
