using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SistemaTesourariaEclesiastica.Attributes;
using SistemaTesourariaEclesiastica.Data;
using SistemaTesourariaEclesiastica.Enums;
using SistemaTesourariaEclesiastica.Models;
using SistemaTesourariaEclesiastica.Services;
using SistemaTesourariaEclesiastica.ViewModels;
using System.Text;

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
        private readonly ILogger<FechamentoPeriodoController> _logger;

        public FechamentoPeriodoController(
            ApplicationDbContext context,
            AuditService auditService,
            UserManager<ApplicationUser> userManager,
            BusinessRulesService businessRules,
            PdfService pdfService,
            ILogger<FechamentoPeriodoController> logger)
        {
            _context = context;
            _auditService = auditService;
            _userManager = userManager;
            _businessRules = businessRules;
            _pdfService = pdfService;
            _logger = logger;
        }

        // GET: FechamentoPeriodo
        // ==================== CORRIGIDO - FILTRO POR CENTRO DE CUSTO ====================
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            var query = _context.FechamentosPeriodo
                .Include(f => f.CentroCusto)
                .Include(f => f.UsuarioSubmissao)
                .Include(f => f.UsuarioAprovacao)
                .AsQueryable();

            // APLICAR FILTRO BASEADO NO PERFIL DO USUÁRIO
            if (!User.IsInRole(Roles.Administrador) && !User.IsInRole(Roles.TesoureiroGeral))
            {
                // Tesoureiros Locais e Pastores só veem fechamentos do seu centro de custo
                if (user.CentroCustoId.HasValue)
                {
                    query = query.Where(f => f.CentroCustoId == user.CentroCustoId.Value);
                }
                else
                {
                    // Se não tem centro de custo, não vê nada
                    query = query.Where(f => false);
                }
            }

            var fechamentos = await query
                .OrderByDescending(f => f.Ano)
                .ThenByDescending(f => f.Mes)
                .ToListAsync();

            await PopulateDropdowns();

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

            // VALIDAR PERMISSÃO DE ACESSO
            if (!await CanAccessFechamento(fechamento))
            {
                return Forbid();
            }

            await _auditService.LogAsync("Visualização", "FechamentoPeriodo", $"Detalhes do fechamento {fechamento.Mes:00}/{fechamento.Ano} visualizados");
            return View(fechamento);
        }

        // GET: FechamentoPeriodo/Create
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            // Buscar o centro de custo do usuário
            var centroCusto = await _context.CentrosCusto
                .FirstOrDefaultAsync(c => c.Id == user.CentroCustoId);

            if (centroCusto == null)
            {
                TempData["ErrorMessage"] = "Usuário sem centro de custo definido. Entre em contato com o administrador.";
                return RedirectToAction(nameof(Index));
            }

            var viewModel = new FechamentoUnificadoViewModel
            {
                CentroCustoId = centroCusto.Id,
                NomeCentroCusto = centroCusto.Nome,
                EhSede = centroCusto.Tipo == TipoCentroCusto.Sede,
                DataInicio = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                DataFim = DateTime.Now.Date,
                Ano = DateTime.Now.Year,
                Mes = DateTime.Now.Month,
                TipoFechamento = TipoFechamento.Mensal
            };

            // Se for SEDE, buscar fechamentos de congregações disponíveis
            if (viewModel.EhSede)
            {
                viewModel.FechamentosDisponiveis = await _context.FechamentosPeriodo
                    .Include(f => f.CentroCusto)
                    .Where(f => f.CentroCusto.Tipo == TipoCentroCusto.Congregacao &&
                                f.Status == StatusFechamentoPeriodo.Aprovado &&
                                f.FoiProcessadoPelaSede == false)
                    .OrderByDescending(f => f.DataAprovacao)
                    .Select(f => new FechamentoCongregacaoDisponivel
                    {
                        Id = f.Id,
                        NomeCongregacao = f.CentroCusto.Nome,
                        DataInicio = f.DataInicio,
                        DataFim = f.DataFim,
                        TotalEntradas = f.TotalEntradas,
                        TotalSaidas = f.TotalSaidas,
                        BalancoFisico = f.BalancoFisico,
                        BalancoDigital = f.BalancoDigital,
                        DataAprovacao = f.DataAprovacao.Value,
                        Selecionado = true // Por padrão, todos selecionados
                    })
                    .ToListAsync();
            }

            return View(viewModel);
        }

        // POST: FechamentoPeriodo/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(FechamentoUnificadoViewModel viewModel)
        {
            if (!ModelState.IsValid)
            {
                // Recarregar dados se for SEDE
                if (viewModel.EhSede)
                {
                    viewModel.FechamentosDisponiveis = await CarregarFechamentosDisponiveis();
                }
                return View(viewModel);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // ✅ VERIFICAR SE JÁ EXISTE FECHAMENTO APROVADO NO PERÍODO
                var fechamentoExistente = await _context.FechamentosPeriodo
                    .Where(f => f.CentroCustoId == viewModel.CentroCustoId &&
                                f.Status == StatusFechamentoPeriodo.Aprovado &&
                                f.DataInicio == viewModel.DataInicio &&
                                f.DataFim == viewModel.DataFim)
                    .FirstOrDefaultAsync();

                if (fechamentoExistente != null)
                {
                    TempData["ErrorMessage"] = $"Já existe um fechamento APROVADO para este período ({viewModel.DataInicio:dd/MM/yyyy} - {viewModel.DataFim:dd/MM/yyyy}). Não é possível criar outro fechamento no mesmo período.";
                    
                    if (viewModel.EhSede)
                    {
                        viewModel.FechamentosDisponiveis = await CarregarFechamentosDisponiveis();
                    }
                    return View(viewModel);
                }

                // Buscar fechamentos das congregações selecionados (apenas se for SEDE)
                List<FechamentoPeriodo> fechamentosCongregacoes = new List<FechamentoPeriodo>();

                if (viewModel.EhSede && viewModel.FechamentosIncluidos != null && viewModel.FechamentosIncluidos.Any())
                {
                    fechamentosCongregacoes = await _context.FechamentosPeriodo
                        .Include(f => f.CentroCusto)
                        .Where(f => viewModel.FechamentosIncluidos.Contains(f.Id) &&
                                    f.Status == StatusFechamentoPeriodo.Aprovado &&
                                    f.FoiProcessadoPelaSede == false)
                        .ToListAsync();
                }

                // ✅ CALCULAR TOTAIS APENAS COM LANÇAMENTOS NÃO INCLUÍDOS EM FECHAMENTOS APROVADOS
                var totalEntradas = await _context.Entradas
                    .Where(e => e.CentroCustoId == viewModel.CentroCustoId &&
                                e.Data >= viewModel.DataInicio &&
                                e.Data <= viewModel.DataFim &&
                                (!e.IncluidaEmFechamento || 
                                 !_context.FechamentosPeriodo.Any(f => f.Id == e.FechamentoQueIncluiuId && f.Status == StatusFechamentoPeriodo.Aprovado)))
                    .SumAsync(e => e.Valor);

                var totalSaidas = await _context.Saidas
                    .Where(s => s.CentroCustoId == viewModel.CentroCustoId &&
                                s.Data >= viewModel.DataInicio &&
                                s.Data <= viewModel.DataFim &&
                                (!s.IncluidaEmFechamento || 
                                 !_context.FechamentosPeriodo.Any(f => f.Id == s.FechamentoQueIncluiuId && f.Status == StatusFechamentoPeriodo.Aprovado)))
                    .SumAsync(s => s.Valor);

                // ✅ VERIFICAR SE HÁ LANÇAMENTOS NOVOS
                var temLancamentosNovos = totalEntradas > 0 || totalSaidas > 0;
                
                if (!temLancamentosNovos && (!viewModel.EhSede || !fechamentosCongregacoes.Any()))
                {
                    TempData["ErrorMessage"] = "Não há lançamentos novos para incluir neste fechamento. Todos os lançamentos do período já foram incluídos em fechamentos aprovados anteriormente.";
                    
                    if (viewModel.EhSede)
                    {
                        viewModel.FechamentosDisponiveis = await CarregarFechamentosDisponiveis();
                    }
                    return View(viewModel);
                }

                // Separar por tipo de caixa (APENAS LANÇAMENTOS NÃO INCLUÍDOS)
                var entradasFisicas = await _context.Entradas
                    .Include(e => e.MeioDePagamento)
                    .Where(e => e.CentroCustoId == viewModel.CentroCustoId &&
                                e.Data >= viewModel.DataInicio &&
                                e.Data <= viewModel.DataFim &&
                                e.MeioDePagamento.TipoCaixa == TipoCaixa.Fisico &&
                                (!e.IncluidaEmFechamento || 
                                 !_context.FechamentosPeriodo.Any(f => f.Id == e.FechamentoQueIncluiuId && f.Status == StatusFechamentoPeriodo.Aprovado)))
                    .SumAsync(e => e.Valor);

                var entradasDigitais = await _context.Entradas
                    .Include(e => e.MeioDePagamento)
                    .Where(e => e.CentroCustoId == viewModel.CentroCustoId &&
                                e.Data >= viewModel.DataInicio &&
                                e.Data <= viewModel.DataFim &&
                                e.MeioDePagamento.TipoCaixa == TipoCaixa.Digital &&
                                (!e.IncluidaEmFechamento || 
                                 !_context.FechamentosPeriodo.Any(f => f.Id == e.FechamentoQueIncluiuId && f.Status == StatusFechamentoPeriodo.Aprovado)))
                    .SumAsync(e => e.Valor);

                var saidasFisicas = await _context.Saidas
                    .Include(s => s.MeioDePagamento)
                    .Where(s => s.CentroCustoId == viewModel.CentroCustoId &&
                                s.Data >= viewModel.DataInicio &&
                                s.Data <= viewModel.DataFim &&
                                s.MeioDePagamento.TipoCaixa == TipoCaixa.Fisico &&
                                (!s.IncluidaEmFechamento || 
                                 !_context.FechamentosPeriodo.Any(f => f.Id == s.FechamentoQueIncluiuId && f.Status == StatusFechamentoPeriodo.Aprovado)))
                    .SumAsync(s => s.Valor);

                var saidasDigitais = await _context.Saidas
                    .Include(s => s.MeioDePagamento)
                    .Where(s => s.CentroCustoId == viewModel.CentroCustoId &&
                                s.Data >= viewModel.DataInicio &&
                                s.Data <= viewModel.DataFim &&
                                s.MeioDePagamento.TipoCaixa == TipoCaixa.Digital &&
                                (!s.IncluidaEmFechamento || 
                                 !_context.FechamentosPeriodo.Any(f => f.Id == s.FechamentoQueIncluiuId && f.Status == StatusFechamentoPeriodo.Aprovado)))
                    .SumAsync(s => s.Valor);

                // Somar valores das congregações (se houver)
                decimal totalEntradasCongregacoes = fechamentosCongregacoes.Sum(f => f.TotalEntradas);
                decimal totalSaidasCongregacoes = fechamentosCongregacoes.Sum(f => f.TotalSaidas);
                decimal totalEntradasFisicasCongregacoes = fechamentosCongregacoes.Sum(f => f.TotalEntradasFisicas);
                decimal totalSaidasFisicasCongregacoes = fechamentosCongregacoes.Sum(f => f.TotalSaidasFisicas);
                decimal totalEntradasDigitaisCongregacoes = fechamentosCongregacoes.Sum(f => f.TotalEntradasDigitais);
                decimal totalSaidasDigitaisCongregacoes = fechamentosCongregacoes.Sum(f => f.TotalSaidasDigitais);

                // Preparar observação
                var observacaoFinal = viewModel.Observacoes ?? "";

                if (viewModel.EhSede && fechamentosCongregacoes.Any())
                {
                    observacaoFinal += $"\n\n✓ Fechamento consolidado: incluídos {fechamentosCongregacoes.Count} fechamento(s) de congregações aprovados.";
                }

                if (temLancamentosNovos)
                {
                    observacaoFinal += $"\n\n✓ Lançamentos novos incluídos: {totalEntradas:C} em entradas e {totalSaidas:C} em saídas.";
                }

                // Criar fechamento
                var fechamento = new FechamentoPeriodo
                {
                    CentroCustoId = viewModel.CentroCustoId,
                    Ano = viewModel.Ano,
                    Mes = viewModel.Mes,
                    DataInicio = viewModel.DataInicio,
                    DataFim = viewModel.DataFim,
                    TipoFechamento = viewModel.TipoFechamento,

                    // TOTAIS CONSOLIDADOS
                    TotalEntradas = totalEntradas + totalEntradasCongregacoes,
                    TotalSaidas = totalSaidas + totalSaidasCongregacoes,

                    TotalEntradasFisicas = entradasFisicas + totalEntradasFisicasCongregacoes,
                    TotalSaidasFisicas = saidasFisicas + totalSaidasFisicasCongregacoes,

                    TotalEntradasDigitais = entradasDigitais + totalEntradasDigitaisCongregacoes,
                    TotalSaidasDigitais = saidasDigitais + totalSaidasDigitaisCongregacoes,

                    BalancoFisico = (entradasFisicas + totalEntradasFisicasCongregacoes) -
                                   (saidasFisicas + totalSaidasFisicasCongregacoes),

                    BalancoDigital = (entradasDigitais + totalEntradasDigitaisCongregacoes) -
                                    (saidasDigitais + totalSaidasDigitaisCongregacoes),

                    Observacoes = observacaoFinal,

                    // Marcar como fechamento da SEDE se aplicável
                    EhFechamentoSede = viewModel.EhSede,
                    QuantidadeCongregacoesIncluidas = fechamentosCongregacoes.Count,

                    Status = StatusFechamentoPeriodo.Pendente,
                    UsuarioSubmissaoId = user.Id,
                    DataSubmissao = DateTime.Now
                };

                // Aplicar rateios (apenas para SEDE)
                if (viewModel.EhSede)
                {
                    await AplicarRateiosComLog(fechamento);
                }
                else
                {
                    fechamento.TotalRateios = 0;
                    fechamento.SaldoFinal = fechamento.BalancoFisico + fechamento.BalancoDigital;
                }

                // Gerar detalhes do fechamento (APENAS COM LANÇAMENTOS NÃO INCLUÍDOS)
                await GerarDetalhesFechamento(fechamento);

                // Salvar fechamento
                _context.Add(fechamento);
                await _context.SaveChangesAsync();

                // ✅ MARCAR LANÇAMENTOS COMO INCLUÍDOS EM FECHAMENTO (APENAS OS NOVOS)
                await MarcarLancamentosComoIncluidos(fechamento);

                // Marcar fechamentos das congregações como PROCESSADOS (se houver)
                if (fechamentosCongregacoes.Any())
                {
                    foreach (var fechamentoCongregacao in fechamentosCongregacoes)
                    {
                        fechamentoCongregacao.FoiProcessadoPelaSede = true;
                        fechamentoCongregacao.FechamentoSedeProcessadorId = fechamento.Id;
                        fechamentoCongregacao.DataProcessamentoPelaSede = DateTime.Now;
                        fechamentoCongregacao.Status = StatusFechamentoPeriodo.Processado;
                        _context.Update(fechamentoCongregacao);
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                var mensagemSucesso = viewModel.EhSede && fechamentosCongregacoes.Any()
                    ? $"Fechamento da SEDE criado com sucesso! {fechamentosCongregacoes.Count} prestação(ões) de congregação(ões) incluída(s)."
                    : "Fechamento criado com sucesso!";

                if (temLancamentosNovos)
                {
                    mensagemSucesso += $" Total de {totalEntradas:C} em entradas e {totalSaidas:C} em saídas foram incluídos.";
                }

                await _auditService.LogAsync("Criação", "FechamentoPeriodo", mensagemSucesso);

                TempData["SuccessMessage"] = mensagemSucesso;

                return RedirectToAction(nameof(Details), new { id = fechamento.Id });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Erro ao criar fechamento");
                TempData["ErrorMessage"] = $"Erro ao criar fechamento: {ex.Message}";

                // Recarregar dados se for SEDE
                if (viewModel.EhSede)
                {
                    viewModel.FechamentosDisponiveis = await CarregarFechamentosDisponiveis();
                }
                return View(viewModel);
            }
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

            // VALIDAR PERMISSÃO DE ACESSO
            if (!await CanAccessFechamento(fechamento))
            {
                return Forbid();
            }

            // Verificar se o fechamento pode ser editado
            if (fechamento.Status != StatusFechamentoPeriodo.Pendente)
            {
                TempData["ErrorMessage"] = "Apenas fechamentos pendentes podem ser editados.";
                return RedirectToAction(nameof(Details), new { id = fechamento.Id });
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

            // VALIDAR PERMISSÃO DE ACESSO
            if (!await CanAccessFechamento(fechamento))
            {
                return Forbid();
            }

            // Verificar se ainda está pendente
            if (fechamento.Status != StatusFechamentoPeriodo.Pendente)
            {
                TempData["ErrorMessage"] = "Apenas fechamentos pendentes podem ser editados.";
                return RedirectToAction(nameof(Details), new { id = fechamento.Id });
            }

            try
            {
                // ATUALIZAR APENAS AS OBSERVAÇÕES (único campo editável)
                fechamento.Observacoes = fechamentoForm.Observacoes;

                // REMOVER ITENS ANTIGOS
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

                // RECALCULAR TODOS OS VALORES
                await CalcularTotaisFechamento(fechamento);

                // REAPLICAR RATEIOS SE FOR SEDE
                var centroCusto = await _context.CentrosCusto.FindAsync(fechamento.CentroCustoId);
                if (centroCusto?.Nome.ToUpper().Contains("SEDE") == true ||
                    centroCusto?.Nome.ToUpper().Contains("GERAL") == true)
                {
                    await AplicarRateiosComLog(fechamento);
                }
                else
                {
                    fechamento.TotalRateios = 0;
                    fechamento.SaldoFinal = fechamento.BalancoDigital;
                }

                // REGERAR DETALHES
                await GerarDetalhesFechamento(fechamento);

                // SALVAR (Entity já está sendo rastreado)
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
        [Authorize(Roles = Roles.AdminOuTesoureiroGeral)] // Apenas Admin e Tesoureiro Geral podem aprovar
        public async Task<IActionResult> Aprovar(int id)
        {
            var fechamento = await _context.FechamentosPeriodo
                .Include(f => f.ItensRateio)
                    .ThenInclude(i => i.RegraRateio)
                        .ThenInclude(r => r.CentroCustoDestino)
                .FirstOrDefaultAsync(f => f.Id == id);

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

            // LOGGING ANTES DA APROVAÇÃO
            _logger.LogInformation($"Aprovando fechamento ID {id}. Itens de rateio: {fechamento.ItensRateio.Count}");

            foreach (var item in fechamento.ItensRateio)
            {
                _logger.LogInformation($"  Rateio: {item.ValorRateio:C} para {item.RegraRateio?.CentroCustoDestino?.Nome ?? "N/A"}");
            }

            fechamento.Status = StatusFechamentoPeriodo.Aprovado;
            fechamento.DataAprovacao = DateTime.Now;
            fechamento.UsuarioAprovacaoId = user.Id;

            _context.Update(fechamento);
            await _context.SaveChangesAsync();

            // VERIFICAR SE OS RATEIOS FORAM PERSISTIDOS
            var rateiosAprovados = await _context.ItensRateioFechamento
                .Include(i => i.FechamentoPeriodo)
                .Include(i => i.RegraRateio)
                    .ThenInclude(r => r.CentroCustoDestino)
                .Where(i => i.FechamentoPeriodoId == id)
                .ToListAsync();

            _logger.LogInformation($"Após aprovação: {rateiosAprovados.Count} rateios persistidos no banco");

            await _auditService.LogAsync("Aprovação", "FechamentoPeriodo",
                $"Fechamento {fechamento.Mes:00}/{fechamento.Ano} aprovado com {rateiosAprovados.Count} rateios");

            TempData["SuccessMessage"] = $"Fechamento aprovado com sucesso! {rateiosAprovados.Count} rateio(s) confirmado(s).";

            return RedirectToAction(nameof(Index));
        }

        // GET: FechamentoPeriodo/Rejeitar/5
        [HttpGet]
        [Authorize(Roles = Roles.AdminOuTesoureiroGeral)] // Apenas Admin e Tesoureiro Geral podem rejeitar
        public async Task<IActionResult> Rejeitar(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var fechamento = await _context.FechamentosPeriodo
                .Include(f => f.CentroCusto)
                .Include(f => f.UsuarioSubmissao)
                .Include(f => f.ItensRateio)
                    .ThenInclude(i => i.RegraRateio)
                        .ThenInclude(r => r.CentroCustoDestino)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (fechamento == null)
            {
                return NotFound();
            }

            // Verificar se pode ser rejeitado
            if (fechamento.Status != StatusFechamentoPeriodo.Pendente)
            {
                TempData["ErrorMessage"] = "Apenas fechamentos pendentes podem ser rejeitados.";
                return RedirectToAction(nameof(Details), new { id = fechamento.Id });
            }

            return View(fechamento);
        }

        // POST: FechamentoPeriodo/Rejeitar/5
        [HttpPost, ActionName("Rejeitar")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Roles.AdminOuTesoureiroGeral)]
        public async Task<IActionResult> RejeitarConfirmed(int id, string? motivoRejeicao)
        {
            var fechamento = await _context.FechamentosPeriodo
                .Include(f => f.CentroCusto)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (fechamento == null)
            {
                return NotFound();
            }

            // Verificar se pode ser rejeitado
            if (fechamento.Status != StatusFechamentoPeriodo.Pendente)
            {
                TempData["ErrorMessage"] = "Apenas fechamentos pendentes podem ser rejeitados.";
                return RedirectToAction(nameof(Details), new { id = fechamento.Id });
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            try
            {
                // ✅ DESMARCAR LANÇAMENTOS QUANDO REJEITAR
                await DesmarcarLancamentosComoIncluidos(fechamento.Id);

                // Atualizar status para Rejeitado
                fechamento.Status = StatusFechamentoPeriodo.Rejeitado;
                fechamento.DataAprovacao = DateTime.Now;
                fechamento.UsuarioAprovacaoId = user.Id;

                // Adicionar motivo da rejeição nas observações
                if (!string.IsNullOrWhiteSpace(motivoRejeicao))
                {
                    fechamento.Observacoes = string.IsNullOrWhiteSpace(fechamento.Observacoes)
                        ? $"REJEITADO: {motivoRejeicao}"
                        : $"{fechamento.Observacoes}\n\nREJEITADO: {motivoRejeicao}";
                }

                _context.Update(fechamento);
                await _context.SaveChangesAsync();

                // Log de auditoria
                await _auditService.LogAsync("Rejeição", "FechamentoPeriodo",
                    $"Fechamento {ObterDescricaoFechamento(fechamento)} rejeitado. Motivo: {motivoRejeicao ?? "Não informado"}. Lançamentos liberados para novo fechamento.");

                TempData["SuccessMessage"] = "Fechamento rejeitado com sucesso! Os lançamentos foram liberados para novo fechamento.";
                _logger.LogInformation($"Fechamento ID {id} rejeitado por {user.UserName}. Motivo: {motivoRejeicao}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao rejeitar fechamento");
                TempData["ErrorMessage"] = $"Erro ao rejeitar fechamento: {ex.Message}";
                return RedirectToAction(nameof(Details), new { id = fechamento.Id });
            }

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
                // NOVOS INCLUDES PARA DETALHES COMPLETOS
                .Include(f => f.FechamentosCongregacoesIncluidos)
                    .ThenInclude(fc => fc.CentroCusto)
                .Include(f => f.FechamentosCongregacoesIncluidos)
                    .ThenInclude(fc => fc.DetalhesFechamento)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (fechamento == null)
            {
                return NotFound();
            }

            // VALIDAR PERMISSÃO DE ACESSO
            if (!await CanAccessFechamento(fechamento))
            {
                return Forbid();
            }

            var pdfBytes = _pdfService.GerarReciboFechamento(fechamento);
            var fileName = $"Fechamento_{fechamento.CentroCusto.Nome.Replace(" ", "_")}_{fechamento.Mes:00}_{fechamento.Ano}.pdf";

            await _auditService.LogAsync("Geração PDF", "FechamentoPeriodo",
                $"PDF do fechamento {fechamento.Mes:00}/{fechamento.Ano} gerado");

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

            // VALIDAR PERMISSÃO DE ACESSO
            if (!await CanAccessFechamento(fechamento))
            {
                return Forbid();
            }

            // Verificar se pode ser excluído
            if (fechamento.Status == StatusFechamentoPeriodo.Aprovado)
            {
                TempData["ErrorMessage"] = "Fechamentos aprovados não podem ser excluídos.";
                return RedirectToAction(nameof(Details), new { id = fechamento.Id });
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

            // VALIDAR PERMISSÃO DE ACESSO
            if (!await CanAccessFechamento(fechamento))
            {
                return Forbid();
            }

            // Verificar se pode ser excluído
            if (fechamento.Status == StatusFechamentoPeriodo.Aprovado)
            {
                TempData["ErrorMessage"] = "Fechamentos aprovados não podem ser excluídos.";
                return RedirectToAction(nameof(Details), new { id = fechamento.Id });
            }

            try
            {
                // ✅ DESMARCAR LANÇAMENTOS ANTES DE EXCLUIR
                await DesmarcarLancamentosComoIncluidos(fechamento.Id);

                // Remover itens relacionados primeiro
                if (fechamento.ItensRateio.Any())
                {
                    _context.ItensRateioFechamento.RemoveRange(fechamento.ItensRateio);
                }

                if (fechamento.DetalhesFechamento.Any())
                {
                    _context.DetalhesFechamento.RemoveRange(fechamento.DetalhesFechamento);
                }

                // Remover o fechamento
                _context.FechamentosPeriodo.Remove(fechamento);
                await _context.SaveChangesAsync();

                await _auditService.LogAsync("Exclusão", "FechamentoPeriodo",
                    $"Fechamento {ObterDescricaoFechamento(fechamento)} excluído");

                TempData["SuccessMessage"] = "Fechamento excluído com sucesso! Os lançamentos foram liberados para novo fechamento.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Erro ao excluir fechamento: {ex.Message}";
                return RedirectToAction(nameof(Details), new { id = fechamento.Id });
            }

            return RedirectToAction(nameof(Index));
        }

        // DIAGNÓSTICO: DiagnósticoRateios
        [HttpGet]
        [Authorize(Roles = Roles.AdminOuTesoureiroGeral)]
        public async Task<IActionResult> DiagnosticoRateios()
        {
            var diagnostico = new StringBuilder();
            diagnostico.AppendLine("=== DIAGNÓSTICO COMPLETO DO SISTEMA DE RATEIOS ===");
            diagnostico.AppendLine($"Data/Hora: {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
            diagnostico.AppendLine("");

            try
            {
                // 1. Verificar Centro de Custo FUNDO
                diagnostico.AppendLine("1. CENTRO DE CUSTO FUNDO");
                var centroCustoFundo = await _context.CentrosCusto
                    .FirstOrDefaultAsync(c => c.Nome.ToUpper().Contains("FUNDO") ||
                                             c.Nome.ToUpper().Contains("REPASSE") ||
                                             c.Nome.ToUpper().Contains("DÍZIMO") ||
                                             c.Nome.ToUpper().Contains("DIZIMO"));

                if (centroCustoFundo != null)
                {
                    diagnostico.AppendLine($"   ✓ Encontrado: {centroCustoFundo.Nome} (ID: {centroCustoFundo.Id})");
                    diagnostico.AppendLine($"   ✓ Ativo: {centroCustoFundo.Ativo}");
                }
                else
                {
                    diagnostico.AppendLine("   ❌ NÃO ENCONTRADO!");
                    diagnostico.AppendLine("   AÇÃO NECESSÁRIA: Criar um Centro de Custo com nome 'FUNDO' ou 'REPASSE'");
                }
                diagnostico.AppendLine("");

                // 2. Verificar Centro de Custo SEDE
                diagnostico.AppendLine("2. CENTRO DE CUSTO SEDE");
                var centroCustoSede = await _context.CentrosCusto
                    .FirstOrDefaultAsync(c => c.Nome.ToUpper().Contains("SEDE") ||
                                             c.Nome.ToUpper().Contains("GERAL") ||
                                             c.Nome.ToUpper().Contains("PRINCIPAL") ||
                                             c.Nome.ToUpper().Contains("CENTRAL"));

                if (centroCustoSede != null)
                {
                    diagnostico.AppendLine($"   ✓ Encontrado: {centroCustoSede.Nome} (ID: {centroCustoSede.Id})");
                    diagnostico.AppendLine($"   ✓ Ativo: {centroCustoSede.Ativo}");
                }
                else
                {
                    diagnostico.AppendLine("   ❌ NÃO ENCONTRADO!");
                    diagnostico.AppendLine("   AÇÃO NECESSÁRIA: Criar um Centro de Custo com nome 'SEDE' ou 'GERAL'");
                }
                diagnostico.AppendLine("");

                // 3. Verificar Regras de Rateio
                diagnostico.AppendLine("3. REGRAS DE RATEIO ATIVAS");
                var regrasAtivas = await _context.RegrasRateio
                    .Include(r => r.CentroCustoOrigem)
                    .Include(r => r.CentroCustoDestino)
                    .Where(r => r.Ativo)
                    .ToListAsync();

                if (regrasAtivas.Any())
                {
                    diagnostico.AppendLine($"   ✓ Total de regras ativas: {regrasAtivas.Count}");
                    foreach (var regra in regrasAtivas)
                    {
                        diagnostico.AppendLine($"   - {regra.Nome}: {regra.CentroCustoOrigem.Nome} → {regra.CentroCustoDestino.Nome} ({regra.Percentual:F2}%)");
                    }
                }
                else
                {
                    diagnostico.AppendLine("   ❌ NENHUMA REGRA ATIVA ENCONTRADA!");
                    diagnostico.AppendLine("   AÇÃO NECESSÁRIA: Criar regras de rateio em Cadastros > Regras de Rateio");
                }

                ViewBag.DiagnosticoRateios = diagnostico.ToString();
            }
            catch (Exception ex)
            {
                diagnostico.AppendLine($"❌ ERRO no diagnóstico: {ex.Message}");
                _logger.LogError(ex, "Erro no diagnóstico de rateios");
            }

            return View("DiagnosticoRateios");
        }

        // GET: FechamentoPeriodo/CreateSede
        [HttpGet]
        [Authorize(Roles = Roles.AdminOuTesoureiroGeral)]
        public async Task<IActionResult> CreateSede()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            // Buscar apenas o Centro de Custo SEDE
            var sede = await _context.CentrosCusto
                .FirstOrDefaultAsync(c => c.Tipo == TipoCentroCusto.Sede && c.Ativo);

            if (sede == null)
            {
                TempData["ErrorMessage"] = "Nenhuma SEDE configurada no sistema.";
                return RedirectToAction(nameof(Index));
            }

            // Buscar fechamentos de congregações APROVADOS e NÃO PROCESSADOS
            var fechamentosDisponiveis = await _context.FechamentosPeriodo
                .Include(f => f.CentroCusto)
                .Where(f => f.CentroCusto.Tipo == TipoCentroCusto.Congregacao &&
                            f.Status == StatusFechamentoPeriodo.Aprovado &&
                            f.FoiProcessadoPelaSede == false)
                .OrderByDescending(f => f.DataAprovacao)
                .Select(f => new FechamentoCongregacaoDisponivel
                {
                    Id = f.Id,
                    NomeCongregacao = f.CentroCusto.Nome,
                    DataInicio = f.DataInicio,
                    DataFim = f.DataFim,
                    TotalEntradas = f.TotalEntradas,
                    TotalSaidas = f.TotalSaidas,
                    BalancoFisico = f.BalancoFisico,
                    BalancoDigital = f.BalancoDigital,
                    DataAprovacao = f.DataAprovacao.Value,
                    Selecionado = true // Por padrão, todos selecionados
                })
                .ToListAsync();

            var viewModel = new FechamentoSedeViewModel
            {
                DataInicio = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                DataFim = DateTime.Now.Date,
                Ano = DateTime.Now.Year,
                Mes = DateTime.Now.Month,
                TipoFechamento = TipoFechamento.Mensal,
                FechamentosDisponiveis = fechamentosDisponiveis
            };

            ViewBag.SedeId = sede.Id;
            ViewBag.SedeNome = sede.Nome;

            return View(viewModel);
        }

        // POST: FechamentoPeriodo/CreateSede
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Roles.AdminOuTesoureiroGeral)]
        public async Task<IActionResult> CreateSede(FechamentoSedeViewModel viewModel)
        {
            if (!ModelState.IsValid)
            {
                viewModel.FechamentosDisponiveis = await CarregarFechamentosDisponiveis();
                return View(viewModel);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var sede = await _context.CentrosCusto
                .FirstOrDefaultAsync(c => c.Tipo == TipoCentroCusto.Sede && c.Ativo);

            if (sede == null)
            {
                TempData["ErrorMessage"] = "SEDE não encontrada.";
                return RedirectToAction(nameof(Index));
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // ✅ VERIFICAR SE JÁ EXISTE FECHAMENTO APROVADO NO PERÍODO
                var fechamentoExistente = await _context.FechamentosPeriodo
                    .Where(f => f.CentroCustoId == sede.Id &&
                                f.Status == StatusFechamentoPeriodo.Aprovado &&
                                f.DataInicio == viewModel.DataInicio &&
                                f.DataFim == viewModel.DataFim)
                    .FirstOrDefaultAsync();

                if (fechamentoExistente != null)
                {
                    TempData["ErrorMessage"] = $"Já existe um fechamento APROVADO da SEDE para este período ({viewModel.DataInicio:dd/MM/yyyy} - {viewModel.DataFim:dd/MM/yyyy}).";
                    viewModel.FechamentosDisponiveis = await CarregarFechamentosDisponiveis();
                    return View(viewModel);
                }

                // 1. Buscar fechamentos das congregações selecionados (SE HOUVER)
                List<FechamentoPeriodo> fechamentosCongregacoes = new List<FechamentoPeriodo>();

                if (viewModel.FechamentosIncluidos != null && viewModel.FechamentosIncluidos.Any())
                {
                    fechamentosCongregacoes = await _context.FechamentosPeriodo
                        .Include(f => f.CentroCusto)
                        .Where(f => viewModel.FechamentosIncluidos.Contains(f.Id) &&
                                    f.Status == StatusFechamentoPeriodo.Aprovado &&
                                    f.FoiProcessadoPelaSede == false)
                        .ToListAsync();
                }

                // 2. Calcular totais da SEDE no período (APENAS LANÇAMENTOS NÃO INCLUÍDOS EM FECHAMENTOS APROVADOS)
                var totalEntradasSede = await _context.Entradas
                    .Where(e => e.CentroCustoId == sede.Id &&
                                e.Data >= viewModel.DataInicio &&
                                e.Data <= viewModel.DataFim &&
                                (!e.IncluidaEmFechamento || 
                                 e.FechamentoQueIncluiuId == sede.Id ||
                                 !_context.FechamentosPeriodo.Any(f => f.Id == e.FechamentoQueIncluiuId && f.Status == StatusFechamentoPeriodo.Aprovado)))
                    .SumAsync(e => e.Valor);

                var totalSaidasSede = await _context.Saidas
                    .Where(s => s.CentroCustoId == sede.Id &&
                                s.Data >= viewModel.DataInicio &&
                                s.Data <= viewModel.DataFim &&
                                (!s.IncluidaEmFechamento || 
                                 s.FechamentoQueIncluiuId == sede.Id ||
                                 !_context.FechamentosPeriodo.Any(f => f.Id == s.FechamentoQueIncluiuId && f.Status == StatusFechamentoPeriodo.Aprovado)))
                    .SumAsync(s => s.Valor);

                // ✅ VERIFICAR SE HÁ LANÇAMENTOS NOVOS
                var temLancamentosNovos = totalEntradasSede > 0 || totalSaidasSede > 0;

                if (!temLancamentosNovos && !fechamentosCongregacoes.Any())
                {
                    TempData["ErrorMessage"] = "Não há lançamentos novos da SEDE nem prestações de congregações para incluir neste fechamento.";
                    viewModel.FechamentosDisponiveis = await CarregarFechamentosDisponiveis();
                    return View(viewModel);
                }

                // Separar por tipo de caixa (Físico/Digital) para a SEDE (APENAS LANÇAMENTOS NÃO INCLUÍDOS)
                var entradasFisicasSede = await _context.Entradas
                    .Include(e => e.MeioDePagamento)
                    .Where(e => e.CentroCustoId == sede.Id &&
                                e.Data >= viewModel.DataInicio &&
                                e.Data <= viewModel.DataFim &&
                                e.MeioDePagamento.TipoCaixa == TipoCaixa.Fisico &&
                                (!e.IncluidaEmFechamento || 
                                 e.FechamentoQueIncluiuId == sede.Id ||
                                 !_context.FechamentosPeriodo.Any(f => f.Id == e.FechamentoQueIncluiuId && f.Status == StatusFechamentoPeriodo.Aprovado)))
                    .SumAsync(e => e.Valor);

                var entradasDigitaisSede = await _context.Entradas
                    .Include(e => e.MeioDePagamento)
                    .Where(e => e.CentroCustoId == sede.Id &&
                                e.Data >= viewModel.DataInicio &&
                                e.Data <= viewModel.DataFim &&
                                e.MeioDePagamento.TipoCaixa == TipoCaixa.Digital &&
                                (!e.IncluidaEmFechamento || 
                                 e.FechamentoQueIncluiuId == sede.Id ||
                                 !_context.FechamentosPeriodo.Any(f => f.Id == e.FechamentoQueIncluiuId && f.Status == StatusFechamentoPeriodo.Aprovado)))
                    .SumAsync(e => e.Valor);

                var saidasFisicasSede = await _context.Saidas
                    .Include(s => s.MeioDePagamento)
                    .Where(s => s.CentroCustoId == sede.Id &&
                                s.Data >= viewModel.DataInicio &&
                                s.Data <= viewModel.DataFim &&
                                s.MeioDePagamento.TipoCaixa == TipoCaixa.Fisico &&
                                (!s.IncluidaEmFechamento || 
                                 s.FechamentoQueIncluiuId == sede.Id ||
                                 !_context.FechamentosPeriodo.Any(f => f.Id == s.FechamentoQueIncluiuId && f.Status == StatusFechamentoPeriodo.Aprovado)))
                    .SumAsync(s => s.Valor);

                var saidasDigitaisSede = await _context.Saidas
                    .Include(s => s.MeioDePagamento)
                    .Where(s => s.CentroCustoId == sede.Id &&
                                s.Data >= viewModel.DataInicio &&
                                s.Data <= viewModel.DataFim &&
                                s.MeioDePagamento.TipoCaixa == TipoCaixa.Digital &&
                                (!s.IncluidaEmFechamento || 
                                 s.FechamentoQueIncluiuId == sede.Id ||
                                 !_context.FechamentosPeriodo.Any(f => f.Id == s.FechamentoQueIncluiuId && f.Status == StatusFechamentoPeriodo.Aprovado)))
                    .SumAsync(s => s.Valor);

                // 3. Somar valores das congregações APROVADAS (SE HOUVER)
                decimal totalEntradasCongregacoes = fechamentosCongregacoes.Sum(f => f.TotalEntradas);
                decimal totalSaidasCongregacoes = fechamentosCongregacoes.Sum(f => f.TotalSaidas);
                decimal totalEntradasFisicasCongregacoes = fechamentosCongregacoes.Sum(f => f.TotalEntradasFisicas);
                decimal totalSaidasFisicasCongregacoes = fechamentosCongregacoes.Sum(f => f.TotalSaidasFisicas);
                decimal totalEntradasDigitaisCongregacoes = fechamentosCongregacoes.Sum(f => f.TotalEntradasDigitais);
                decimal totalSaidasDigitaisCongregacoes = fechamentosCongregacoes.Sum(f => f.TotalSaidasDigitais);

                // 4. Criar fechamento consolidado da SEDE
                var observacaoFinal = viewModel.Observacoes ?? "";

                if (fechamentosCongregacoes.Any())
                {
                    observacaoFinal += $"\n\n✓ Incluídos {fechamentosCongregacoes.Count} fechamento(s) de congregações aprovados.";
                }
                else
                {
                    observacaoFinal += "\n\n✓ Fechamento da SEDE SEM congregações incluídas (fechamento independente).";
                }

                if (temLancamentosNovos)
                {
                    observacaoFinal += $"\n\n✓ Lançamentos novos da SEDE incluídos: {totalEntradasSede:C} em entradas e {totalSaidasSede:C} em saídas.";
                }

                var fechamentoSede = new FechamentoPeriodo
                {
                    CentroCustoId = sede.Id,
                    Ano = viewModel.Ano,
                    Mes = viewModel.Mes,
                    DataInicio = viewModel.DataInicio,
                    DataFim = viewModel.DataFim,
                    TipoFechamento = viewModel.TipoFechamento,

                    // TOTAIS CONSOLIDADOS (SEDE + CONGREGAÇÕES, se houver)
                    TotalEntradas = totalEntradasSede + totalEntradasCongregacoes,
                    TotalSaidas = totalSaidasSede + totalSaidasCongregacoes,

                    TotalEntradasFisicas = entradasFisicasSede + totalEntradasFisicasCongregacoes,
                    TotalSaidasFisicas = saidasFisicasSede + totalSaidasFisicasCongregacoes,

                    TotalEntradasDigitais = entradasDigitaisSede + totalEntradasDigitaisCongregacoes,
                    TotalSaidasDigitais = saidasDigitaisSede + totalSaidasDigitaisCongregacoes,

                    BalancoFisico = (entradasFisicasSede + totalEntradasFisicasCongregacoes) -
                                   (saidasFisicasSede + totalSaidasFisicasCongregacoes),

                    BalancoDigital = (entradasDigitaisSede + totalEntradasDigitaisCongregacoes) -
                                    (saidasDigitaisSede + totalSaidasDigitaisCongregacoes),

                    Observacoes = observacaoFinal,

                    // NOVAS PROPRIEDADES
                    EhFechamentoSede = true,
                    QuantidadeCongregacoesIncluidas = fechamentosCongregacoes.Count,

                    Status = StatusFechamentoPeriodo.Pendente,
                    UsuarioSubmissaoId = user.Id,
                    DataSubmissao = DateTime.Now
                };

                // 5. Aplicar RATEIOS sobre o TOTAL CONSOLIDADO
                await AplicarRateiosComLog(fechamentoSede);

                // 6. Gerar detalhes do fechamento (APENAS COM LANÇAMENTOS NÃO INCLUÍDOS)
                await GerarDetalhesFechamento(fechamentoSede);

                // 7. Salvar fechamento da SEDE
                _context.Add(fechamentoSede);
                await _context.SaveChangesAsync();

                // ✅ 8. MARCAR LANÇAMENTOS DA SEDE COMO INCLUÍDOS
                if (temLancamentosNovos)
                {
                    await MarcarLancamentosComoIncluidos(fechamentoSede);
                }

                // 9. Marcar fechamentos das congregações como PROCESSADOS (SE HOUVER)
                if (fechamentosCongregacoes.Any())
                {
                    foreach (var fechamentoCongregacao in fechamentosCongregacoes)
                    {
                        fechamentoCongregacao.FoiProcessadoPelaSede = true;
                        fechamentoCongregacao.FechamentoSedeProcessadorId = fechamentoSede.Id;
                        fechamentoCongregacao.DataProcessamentoPelaSede = DateTime.Now;
                        fechamentoCongregacao.Status = StatusFechamentoPeriodo.Processado;
                        _context.Update(fechamentoCongregacao);
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                var mensagemSucesso = fechamentosCongregacoes.Any()
                    ? $"Fechamento da SEDE criado com sucesso! {fechamentosCongregacoes.Count} prestação(ões) de congregação(ões) incluída(s)."
                    : "Fechamento da SEDE criado com sucesso! (Sem congregações incluídas)";

                if (temLancamentosNovos)
                {
                    mensagemSucesso += $" Total de {totalEntradasSede:C} em entradas e {totalSaidasSede:C} em saídas da SEDE foram incluídos.";
                }

                await _auditService.LogAsync("Criação", "FechamentoPeriodo", mensagemSucesso);

                TempData["SuccessMessage"] = mensagemSucesso;

                return RedirectToAction(nameof(Details), new { id = fechamentoSede.Id });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Erro ao criar fechamento da SEDE");
                TempData["ErrorMessage"] = $"Erro ao criar fechamento: {ex.Message}";

                viewModel.FechamentosDisponiveis = await CarregarFechamentosDisponiveis();
                return View(viewModel);
            }
        }

        // Método auxiliar para recarregar lista
        private async Task<List<FechamentoCongregacaoDisponivel>> CarregarFechamentosDisponiveis()
        {
            return await _context.FechamentosPeriodo
                .Include(f => f.CentroCusto)
                .Where(f => f.CentroCusto.Tipo == TipoCentroCusto.Congregacao &&
                            f.Status == StatusFechamentoPeriodo.Aprovado &&
                            f.FoiProcessadoPelaSede == false)
                .OrderByDescending(f => f.DataAprovacao)
                .Select(f => new FechamentoCongregacaoDisponivel
                {
                    Id = f.Id,
                    NomeCongregacao = f.CentroCusto.Nome,
                    DataInicio = f.DataInicio,
                    DataFim = f.DataFim,
                    TotalEntradas = f.TotalEntradas,
                    TotalSaidas = f.TotalSaidas,
                    BalancoFisico = f.BalancoFisico,
                    BalancoDigital = f.BalancoDigital,
                    DataAprovacao = f.DataAprovacao.Value,
                    Selecionado = true
                })
                .ToListAsync();
        }

        #region Métodos Auxiliares

        // MÉTODO AUXILIAR: Verificar permissão de acesso ao fechamento
        private async Task<bool> CanAccessFechamento(FechamentoPeriodo fechamento)
        {
            // Administradores e Tesoureiros Gerais podem acessar tudo
            if (User.IsInRole(Roles.Administrador) || User.IsInRole(Roles.TesoureiroGeral))
                return true;

            var user = await _userManager.GetUserAsync(User);

            // Outros usuários só podem acessar fechamentos do seu centro de custo
            return user.CentroCustoId.HasValue && fechamento.CentroCustoId == user.CentroCustoId.Value;
        }

        // MÉTODO AUXILIAR: Verificar permissão de acesso ao centro de custo
        private async Task<bool> CanAccessCentroCusto(int centroCustoId)
        {
            // Administradores e Tesoureiros Gerais podem acessar qualquer centro de custo
            if (User.IsInRole(Roles.Administrador) || User.IsInRole(Roles.TesoureiroGeral))
                return true;

            var user = await _userManager.GetUserAsync(User);

            // Outros usuários só podem acessar seu próprio centro de custo
            return user.CentroCustoId.HasValue && centroCustoId == user.CentroCustoId.Value;
        }

        // MÉTODO MELHORADO: Aplicar Rateios com Log Detalhado
        private async Task<string> AplicarRateiosComLog(FechamentoPeriodo fechamento)
        {
            var log = new StringBuilder();
            log.AppendLine("=== DIAGNÓSTICO DE RATEIO ===");

            try
            {
                // Buscar o centro de custo
                var centroCusto = await _context.CentrosCusto
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == fechamento.CentroCustoId);

                if (centroCusto == null)
                {
                    log.AppendLine("❌ ERRO: Centro de custo não encontrado!");
                    _logger.LogWarning($"Centro de custo ID {fechamento.CentroCustoId} não encontrado");
                    return log.ToString();
                }

                log.AppendLine($"✓ Centro de Custo: {centroCusto.Nome}");

                // Verificar se é SEDE
                var nomeCentro = centroCusto.Nome.ToUpper().Trim();
                var ehSede = nomeCentro.Contains("SEDE") ||
                             nomeCentro.Contains("GERAL") ||
                             nomeCentro.Contains("PRINCIPAL") ||
                             nomeCentro.Contains("CENTRAL");

                log.AppendLine($"✓ É SEDE/GERAL? {(ehSede ? "SIM" : "NÃO")}");

                if (!ehSede)
                {
                    fechamento.TotalRateios = 0;
                    fechamento.SaldoFinal = fechamento.BalancoDigital + fechamento.BalancoFisico;
                    log.AppendLine("⚠ Rateios não aplicados: Centro não é SEDE/GERAL");
                    log.AppendLine($"✓ Saldo Final = Balanço Total: {fechamento.SaldoFinal:C}");
                    return log.ToString();
                }

                // Buscar regras de rateio ativas
                var regrasRateio = await _context.RegrasRateio
                    .Include(r => r.CentroCustoOrigem)
                    .Include(r => r.CentroCustoDestino)
                    .Where(r => r.CentroCustoOrigemId == fechamento.CentroCustoId && r.Ativo)
                    .ToListAsync();

                log.AppendLine($"✓ Regras de Rateio Ativas Encontradas: {regrasRateio.Count}");

                if (!regrasRateio.Any())
                {
                    fechamento.TotalRateios = 0;
                    fechamento.SaldoFinal = fechamento.BalancoDigital + fechamento.BalancoFisico;
                    log.AppendLine("⚠ AVISO: Nenhuma regra de rateio ativa encontrada!");
                    log.AppendLine($"  Para aplicar rateios, crie uma regra em: Cadastros > Regras de Rateio");
                    log.AppendLine($"  Exemplo: {centroCusto.Nome} → FUNDO (10%)");
                    return log.ToString();
                }

                // CORREÇÃO: Calcular sobre TOTAL DE RECEITAS (não sobre o balanço)
                var totalReceitas = fechamento.TotalEntradas;

                log.AppendLine("");
                log.AppendLine("=== BASE DE CÁLCULO: TOTAL DE RECEITAS ===");
                log.AppendLine($"✓ Entradas Físicas: {fechamento.TotalEntradasFisicas:C}");
                log.AppendLine($"✓ Entradas Digitais: {fechamento.TotalEntradasDigitais:C}");
                log.AppendLine($"✓ TOTAL DE RECEITAS: {totalReceitas:C}");
                log.AppendLine("");

                // Aplicar cada regra
                decimal totalRateios = 0;
                var valorBase = totalReceitas;
                int contador = 0;

                log.AppendLine($"✓ Valor Base para Rateio: {valorBase:C}");
                log.AppendLine("");
                log.AppendLine("--- APLICANDO RATEIOS ---");

                foreach (var regra in regrasRateio)
                {
                    contador++;
                    var valorRateio = Math.Round(valorBase * (regra.Percentual / 100), 2);

                    log.AppendLine($"Rateio #{contador}:");
                    log.AppendLine($"  Regra: {regra.Nome}");
                    log.AppendLine($"  Origem: {regra.CentroCustoOrigem.Nome}");
                    log.AppendLine($"  Destino: {regra.CentroCustoDestino.Nome}");
                    log.AppendLine($"  Percentual: {regra.Percentual:F2}%");
                    log.AppendLine($"  Cálculo: {valorBase:C} × {regra.Percentual:F2}% = {valorRateio:C}");

                    var itemRateio = new ItemRateioFechamento
                    {
                        FechamentoPeriodoId = fechamento.Id,
                        FechamentoPeriodo = fechamento,
                        RegraRateioId = regra.Id,
                        RegraRateio = regra,
                        ValorBase = valorBase,
                        Percentual = regra.Percentual,
                        ValorRateio = valorRateio,
                        Observacoes = $"Rateio automático de {regra.Percentual:F2}% sobre RECEITAS TOTAIS de {valorBase:C} = {valorRateio:C} para {regra.CentroCustoDestino.Nome}"
                    };

                    fechamento.ItensRateio.Add(itemRateio);
                    totalRateios += valorRateio;

                    log.AppendLine($"  Status: ✓ Adicionado à coleção");
                    log.AppendLine("");
                }

                fechamento.TotalRateios = totalRateios;

                // SALDO FINAL: Balanço Total (lucro) - Rateios
                var balancoTotal = fechamento.BalancoFisico + fechamento.BalancoDigital;
                fechamento.SaldoFinal = balancoTotal - totalRateios;

                log.AppendLine("--- RESUMO ---");
                log.AppendLine($"✓ Total de Receitas (Base): {totalReceitas:C}");
                log.AppendLine($"✓ Total de Rateios: {totalRateios:C}");
                log.AppendLine($"✓ Balanço Total (Lucro): {balancoTotal:C}");
                log.AppendLine($"✓ Saldo Final: {balancoTotal:C} - {totalRateios:C} = {fechamento.SaldoFinal:C}");
                log.AppendLine($"✓ Itens na coleção: {fechamento.ItensRateio.Count}");

                _logger.LogInformation($"Rateios aplicados com sucesso: {contador} regras, total {totalRateios:C} sobre receitas de {totalReceitas:C}");
            }
            catch (Exception ex)
            {
                log.AppendLine($"❌ ERRO ao aplicar rateios: {ex.Message}");
                _logger.LogError(ex, "Erro ao aplicar rateios");
            }

            return log.ToString();
        }

        // MÉTODO AUXILIAR: Calcular Totais do Fechamento
        private async Task CalcularTotaisFechamento(FechamentoPeriodo fechamento)
        {
            // ✅ Total de entradas FÍSICAS (Dinheiro) - APENAS LANÇAMENTOS NÃO INCLUÍDOS EM FECHAMENTOS APROVADOS
            fechamento.TotalEntradasFisicas = await _context.Entradas
                .Include(e => e.MeioDePagamento)
                .Where(e => e.CentroCustoId == fechamento.CentroCustoId &&
                           e.Data >= fechamento.DataInicio &&
                           e.Data <= fechamento.DataFim &&
                           e.MeioDePagamento.TipoCaixa == TipoCaixa.Fisico &&
                           (!e.IncluidaEmFechamento || 
                            e.FechamentoQueIncluiuId == fechamento.Id ||
                            !_context.FechamentosPeriodo.Any(f => f.Id == e.FechamentoQueIncluiuId && f.Status == StatusFechamentoPeriodo.Aprovado)))
                .SumAsync(e => (decimal?)e.Valor) ?? 0;

            // ✅ Total de entradas DIGITAIS - APENAS LANÇAMENTOS NÃO INCLUÍDOS EM FECHAMENTOS APROVADOS
            fechamento.TotalEntradasDigitais = await _context.Entradas
                .Include(e => e.MeioDePagamento)
                .Where(e => e.CentroCustoId == fechamento.CentroCustoId &&
                           e.Data >= fechamento.DataInicio &&
                           e.Data <= fechamento.DataFim &&
                           e.MeioDePagamento.TipoCaixa == TipoCaixa.Digital &&
                           (!e.IncluidaEmFechamento || 
                            e.FechamentoQueIncluiuId == fechamento.Id ||
                            !_context.FechamentosPeriodo.Any(f => f.Id == e.FechamentoQueIncluiuId && f.Status == StatusFechamentoPeriodo.Aprovado)))
                .SumAsync(e => (decimal?)e.Valor) ?? 0;

            // ✅ Total de saídas FÍSICAS - APENAS LANÇAMENTOS NÃO INCLUÍDOS EM FECHAMENTOS APROVADOS
            fechamento.TotalSaidasFisicas = await _context.Saidas
                .Include(s => s.MeioDePagamento)
                .Where(s => s.CentroCustoId == fechamento.CentroCustoId &&
                           s.Data >= fechamento.DataInicio &&
                           s.Data <= fechamento.DataFim &&
                           s.MeioDePagamento.TipoCaixa == TipoCaixa.Fisico &&
                           (!s.IncluidaEmFechamento || 
                            s.FechamentoQueIncluiuId == fechamento.Id ||
                            !_context.FechamentosPeriodo.Any(f => f.Id == s.FechamentoQueIncluiuId && f.Status == StatusFechamentoPeriodo.Aprovado)))
                .SumAsync(s => (decimal?)s.Valor) ?? 0;

            // ✅ Total de saídas DIGITAIS - APENAS LANÇAMENTOS NÃO INCLUÍDOS EM FECHAMENTOS APROVADOS
            fechamento.TotalSaidasDigitais = await _context.Saidas
                .Include(s => s.MeioDePagamento)
                .Where(s => s.CentroCustoId == fechamento.CentroCustoId &&
                           s.Data >= fechamento.DataInicio &&
                           s.Data <= fechamento.DataFim &&
                           s.MeioDePagamento.TipoCaixa == TipoCaixa.Digital &&
                           (!s.IncluidaEmFechamento || 
                            s.FechamentoQueIncluiuId == fechamento.Id ||
                            !_context.FechamentosPeriodo.Any(f => f.Id == s.FechamentoQueIncluiuId && f.Status == StatusFechamentoPeriodo.Aprovado)))
                .SumAsync(s => (decimal?)s.Valor) ?? 0;

            // TOTAIS GERAIS
            fechamento.TotalEntradas = fechamento.TotalEntradasFisicas + fechamento.TotalEntradasDigitais;
            fechamento.TotalSaidas = fechamento.TotalSaidasFisicas + fechamento.TotalSaidasDigitais;

            // BALANÇOS SEPARADOS
            fechamento.BalancoFisico = fechamento.TotalEntradasFisicas - fechamento.TotalSaidasFisicas;
            fechamento.BalancoDigital = fechamento.TotalEntradasDigitais - fechamento.TotalSaidasDigitais;
        }

        // MÉTODO AUXILIAR: Validar Tipo de Fechamento
        private void ValidarTipoFechamento(FechamentoPeriodo fechamento)
        {
            if (fechamento.TipoFechamento == TipoFechamento.Diario)
            {
                fechamento.DataFim = fechamento.DataInicio;
                fechamento.Mes = fechamento.DataInicio.Month;
                fechamento.Ano = fechamento.DataInicio.Year;
            }
            else if (fechamento.TipoFechamento == TipoFechamento.Semanal)
            {
                if (fechamento.DataInicio == default || fechamento.DataFim == default)
                {
                    throw new InvalidOperationException("Para fechamento semanal, é necessário informar a data de início e fim da semana.");
                }

                if (fechamento.DataFim < fechamento.DataInicio)
                {
                    throw new InvalidOperationException("A data de fim deve ser posterior ou igual à data de início.");
                }

                fechamento.Mes = fechamento.DataInicio.Month;
                fechamento.Ano = fechamento.DataInicio.Year;
            }
            else if (fechamento.TipoFechamento == TipoFechamento.Mensal)
            {
                if (!fechamento.Mes.HasValue || !fechamento.Ano.HasValue)
                {
                    throw new InvalidOperationException("Para fechamento mensal, é necessário informar Mês e Ano.");
                }

                fechamento.DataInicio = new DateTime(fechamento.Ano.Value, fechamento.Mes.Value, 1);
                fechamento.DataFim = fechamento.DataInicio.AddMonths(1).AddDays(-1);
            }
        }

        // MÉTODO AUXILIAR: Gerar Detalhes do Fechamento
        private async Task GerarDetalhesFechamento(FechamentoPeriodo fechamento)
        {
            // Gerar detalhes das entradas
            var entradas = await _context.Entradas
                .Include(e => e.PlanoDeContas)
                .Include(e => e.MeioDePagamento)
                .Include(e => e.Membro)
                .Where(e => e.CentroCustoId == fechamento.CentroCustoId &&
                           e.Data >= fechamento.DataInicio &&
                           e.Data <= fechamento.DataFim &&
                           (!e.IncluidaEmFechamento || 
                            !_context.FechamentosPeriodo.Any(f => f.Id == e.FechamentoQueIncluiuId && f.Status == StatusFechamentoPeriodo.Aprovado)))
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
                           s.Data <= fechamento.DataFim &&
                           (!s.IncluidaEmFechamento || 
                            !_context.FechamentosPeriodo.Any(f => f.Id == s.FechamentoQueIncluiuId && f.Status == StatusFechamentoPeriodo.Aprovado)))
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

        // ✅ NOVO MÉTODO: Marcar Lançamentos como Incluídos em Fechamento
        private async Task MarcarLancamentosComoIncluidos(FechamentoPeriodo fechamento)
        {
            // Buscar entradas do período que ainda não foram incluídas em fechamentos aprovados
            var entradas = await _context.Entradas
                .Where(e => e.CentroCustoId == fechamento.CentroCustoId &&
                           e.Data >= fechamento.DataInicio &&
                           e.Data <= fechamento.DataFim &&
                           (!e.IncluidaEmFechamento || 
                            e.FechamentoQueIncluiuId == fechamento.Id ||
                            !_context.FechamentosPeriodo.Any(f => f.Id == e.FechamentoQueIncluiuId && f.Status == StatusFechamentoPeriodo.Aprovado)))
                .ToListAsync();

            foreach (var entrada in entradas)
            {
                entrada.IncluidaEmFechamento = true;
                entrada.FechamentoQueIncluiuId = fechamento.Id;
                entrada.DataInclusaoFechamento = DateTime.Now;
                _context.Update(entrada);
            }

            // Buscar saídas do período que ainda não foram incluídas em fechamentos aprovados
            var saidas = await _context.Saidas
                .Where(s => s.CentroCustoId == fechamento.CentroCustoId &&
                           s.Data >= fechamento.DataInicio &&
                           s.Data <= fechamento.DataFim &&
                           (!s.IncluidaEmFechamento || 
                            s.FechamentoQueIncluiuId == fechamento.Id ||
                            !_context.FechamentosPeriodo.Any(f => f.Id == s.FechamentoQueIncluiuId && f.Status == StatusFechamentoPeriodo.Aprovado)))
                .ToListAsync();

            foreach (var saida in saidas)
            {
                saida.IncluidaEmFechamento = true;
                saida.FechamentoQueIncluiuId = fechamento.Id;
                saida.DataInclusaoFechamento = DateTime.Now;
                _context.Update(saida);
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Marcados {entradas.Count} entradas e {saidas.Count} saídas como incluídas no fechamento ID {fechamento.Id}");
        }

        // ✅ NOVO MÉTODO: Desmarcar Lançamentos (para quando fechamento for rejeitado ou excluído)
        private async Task DesmarcarLancamentosComoIncluidos(int fechamentoId)
        {
            // Desmarcar entradas
            var entradas = await _context.Entradas
                .Where(e => e.FechamentoQueIncluiuId == fechamentoId)
                .ToListAsync();

            foreach (var entrada in entradas)
            {
                entrada.IncluidaEmFechamento = false;
                entrada.FechamentoQueIncluiuId = null;
                entrada.DataInclusaoFechamento = null;
                _context.Update(entrada);
            }

            // Desmarcar saídas
            var saidas = await _context.Saidas
                .Where(s => s.FechamentoQueIncluiuId == fechamentoId)
                .ToListAsync();

            foreach (var saida in saidas)
            {
                saida.IncluidaEmFechamento = false;
                saida.FechamentoQueIncluiuId = null;
                saida.DataInclusaoFechamento = null;
                _context.Update(saida);
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Desmarcados {entradas.Count} entradas e {saidas.Count} saídas do fechamento ID {fechamentoId}");
        }

        // MÉTODO AUXILIAR: Popular Dropdowns
        private async Task PopulateDropdowns(FechamentoPeriodo? fechamento = null)
        {
            var user = await _userManager.GetUserAsync(User);

            if (User.IsInRole(Roles.Administrador) || User.IsInRole(Roles.TesoureiroGeral))
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

        // MÉTODO AUXILIAR: Obter Descrição do Fechamento
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

        // MÉTODO AUXILIAR: Verificar se Fechamento Existe
        private bool FechamentoPeriodoExists(int id)
        {
            return _context.FechamentosPeriodo.Any(f => f.Id == id);
        }

        #endregion
    }
}