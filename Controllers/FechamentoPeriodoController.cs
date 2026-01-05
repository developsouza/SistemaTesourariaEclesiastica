using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SistemaTesourariaEclesiastica.Attributes;
using SistemaTesourariaEclesiastica.Data;
using SistemaTesourariaEclesiastica.Enums;
using SistemaTesourariaEclesiastica.Helpers;
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

            // ✅ OTIMIZADO: Usar AsNoTracking para consultas somente leitura
            var query = _context.FechamentosPeriodo
                .AsNoTracking()
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

            // ✅ OTIMIZADO: Usar AsNoTracking para consultas somente leitura
            var fechamento = await _context.FechamentosPeriodo
                .AsNoTracking()
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

            // ✅ CARREGAR LANÇAMENTOS DO PRÓPRIO CENTRO DE CUSTO (Congregação ou SEDE)
            // Carregar entradas não incluídas em fechamentos aprovados
            viewModel.LancamentosSedeEntradas = await _context.Entradas
                .Include(e => e.PlanoDeContas)
                .Include(e => e.MeioDePagamento)
                .Include(e => e.Membro)
                .Where(e => e.CentroCustoId == centroCusto.Id &&
                            e.Data >= viewModel.DataInicio &&
                            e.Data <= viewModel.DataFim &&
                            !e.IncluidaEmFechamento)
                .OrderByDescending(e => e.Data)
                .Select(e => new DetalhePrestacaoContas
                {
                    Data = e.Data,
                    Descricao = e.Descricao ?? "",
                    Valor = e.Valor,
                    PlanoContas = e.PlanoDeContas != null ? e.PlanoDeContas.Nome : null,
                    MeioPagamento = e.MeioDePagamento != null ? e.MeioDePagamento.Nome : null,
                    MembroOuFornecedor = e.Membro != null ? e.Membro.NomeCompleto : null,
                    Observacoes = e.Observacoes
                })
                .ToListAsync();

            // Carregar saídas não incluídas em fechamentos aprovados
            viewModel.LancamentosSedeSaidas = await _context.Saidas
                .Include(s => s.PlanoDeContas)
                .Include(s => s.MeioDePagamento)
                .Include(s => s.Fornecedor)
                .Where(s => s.CentroCustoId == centroCusto.Id &&
                            s.Data >= viewModel.DataInicio &&
                            s.Data <= viewModel.DataFim &&
                            !s.IncluidaEmFechamento)
                .OrderByDescending(s => s.Data)
                .Select(s => new DetalhePrestacaoContas
                {
                    Data = s.Data,
                    Descricao = s.Descricao ?? "",
                    Valor = s.Valor,
                    PlanoContas = s.PlanoDeContas != null ? s.PlanoDeContas.Nome : null,
                    MeioPagamento = s.MeioDePagamento != null ? s.MeioDePagamento.Nome : null,
                    MembroOuFornecedor = s.Fornecedor != null ? s.Fornecedor.Nome : null,
                    Observacoes = s.Observacoes
                })
                .ToListAsync();

            // Se for SEDE, buscar fechamentos de congregações disponíveis
            if (viewModel.EhSede)
            {
                // ✅ OTIMIZADO: Projeção direta no banco sem carregar coleções aninhadas
                viewModel.FechamentosDisponiveis = await _context.FechamentosPeriodo
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
                        DataAprovacao = f.DataAprovacao.HasValue ? f.DataAprovacao.Value : DateTime.Now,
                        Selecionado = true
                        // ✅ REMOVIDO: Detalhes serão carregados sob demanda se necessário
                        // Isso evita operações simultâneas no DbContext
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
            _logger.LogInformation($"=== INICIANDO CREATE FECHAMENTO ===");
            _logger.LogInformation($"CentroCustoId: {viewModel.CentroCustoId}");
            _logger.LogInformation($"TipoFechamento: {viewModel.TipoFechamento}");
            _logger.LogInformation($"DataInicio: {viewModel.DataInicio:dd/MM/yyyy}");
            _logger.LogInformation($"DataFim: {viewModel.DataFim:dd/MM/yyyy}");
            _logger.LogInformation($"Ano: {viewModel.Ano}, Mes: {viewModel.Mes}");
            _logger.LogInformation($"EhSede: {viewModel.EhSede}");

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("ModelState inválido. Erros de validação:");
                foreach (var modelState in ModelState.Values)
                {
                    foreach (var error in modelState.Errors)
                    {
                        _logger.LogWarning($"  - {error.ErrorMessage}");
                    }
                }

                // Criar mensagem de erro detalhada
                var erros = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .Where(m => !string.IsNullOrEmpty(m))
                    .ToList();

                if (erros.Any())
                {
                    TempData["ErrorMessage"] = "Erro de validação: " + string.Join("; ", erros);
                }
                else
                {
                    TempData["ErrorMessage"] = "Erro de validação. Verifique os campos preenchidos e tente novamente.";
                }

                // Recarregar dados se for SEDE
                if (viewModel.EhSede)
                {
                    viewModel.FechamentosDisponiveis = await CarregarFechamentosDisponiveis();
                }
                return View(viewModel);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            _logger.LogInformation($"Iniciando criação de fechamento {viewModel.TipoFechamento} para {viewModel.NomeCentroCusto} - Período: {viewModel.DataInicio:dd/MM/yyyy} a {viewModel.DataFim:dd/MM/yyyy}");

            // ✅ VALIDAÇÕES ANTES DA TRANSAÇÃO
            var fechamentoExistente = await FechamentoQueryHelper.BuscarFechamentoAprovadoNoPeriodo(
                _context, viewModel.CentroCustoId, viewModel.DataInicio, viewModel.DataFim);

            if (fechamentoExistente != null)
            {
                // Para fechamentos NÃO DIÁRIOS, bloquear totalmente
                if (viewModel.TipoFechamento != TipoFechamento.Diario)
                {
                    _logger.LogWarning($"Tentativa de criar fechamento duplicado: já existe fechamento aprovado ID {fechamentoExistente.Id} para o período {viewModel.DataInicio:dd/MM/yyyy} - {viewModel.DataFim:dd/MM/yyyy}");
                    TempData["ErrorMessage"] = $"Já existe um fechamento APROVADO para este período ({viewModel.DataInicio:dd/MM/yyyy} - {viewModel.DataFim:dd/MM/yyyy}). Não é possível criar outro fechamento no mesmo período.";

                    if (viewModel.EhSede)
                    {
                        viewModel.FechamentosDisponiveis = await CarregarFechamentosDisponiveis();
                    }
                    return View(viewModel);
                }

                // Para fechamentos DIÁRIOS, verificar se há lançamentos novos
                var temLancamentosNovosAntesDeCalcular = await FechamentoQueryHelper.TemLancamentosNovos(
                    _context, viewModel.CentroCustoId, viewModel.DataInicio, viewModel.DataFim);

                if (!temLancamentosNovosAntesDeCalcular)
                {
                    _logger.LogWarning($"Fechamento diário sem lançamentos novos - Período: {viewModel.DataInicio:dd/MM/yyyy}");
                    TempData["ErrorMessage"] = $"Já existe um fechamento APROVADO para o dia {viewModel.DataInicio:dd/MM/yyyy} e não há novos lançamentos desde então.";

                    if (viewModel.EhSede)
                    {
                        viewModel.FechamentosDisponiveis = await CarregarFechamentosDisponiveis();
                    }
                    return View(viewModel);
                }
            }

            // ✅ CORREÇÃO: Usar ExecutionStrategy para suportar SqlServerRetryingExecutionStrategy
            try
            {
                var strategy = _context.Database.CreateExecutionStrategy();
                return await strategy.ExecuteAsync(async () =>
                {
                    using var transaction = await _context.Database.BeginTransactionAsync();
                    try
                    {

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

                            _logger.LogInformation($"Selecionados {fechamentosCongregacoes.Count} fechamentos de congregações para consolidação");
                        }

                        // ✅ CALCULAR TOTAIS USANDO HELPER (APENAS LANÇAMENTOS NÃO INCLUÍDOS)
                        _logger.LogInformation($"Calculando totais para o período {viewModel.DataInicio:dd/MM/yyyy} a {viewModel.DataFim:dd/MM/yyyy}");
                        var totais = await FechamentoQueryHelper.CalcularTotais(
                            _context, viewModel.CentroCustoId, viewModel.DataInicio, viewModel.DataFim);

                        _logger.LogInformation($"Totais calculados - Entradas: {totais.TotalEntradas:C}, Saídas: {totais.TotalSaidas:C}, Balanço: {totais.BalancoFisico + totais.BalancoDigital:C}");

                        // ✅ VERIFICAR SE HÁ LANÇAMENTOS NOVOS
                        var temLancamentosNovos = totais.TotalEntradas > 0 || totais.TotalSaidas > 0;

                        if (!temLancamentosNovos && (!viewModel.EhSede || !fechamentosCongregacoes.Any()))
                        {
                            _logger.LogWarning("Nenhum lançamento novo encontrado para incluir no fechamento");
                            TempData["ErrorMessage"] = "Não há lançamentos novos para incluir neste fechamento. Todos os lançamentos do período já foram incluídos em fechamentos aprovados anteriormente.";

                            if (viewModel.EhSede)
                            {
                                viewModel.FechamentosDisponiveis = await CarregarFechamentosDisponiveis();
                            }
                            throw new InvalidOperationException("Sem lançamentos para processar");
                        }

                        // Somar valores das congregações (se houver)
                        decimal totalEntradasCongregacoes = fechamentosCongregacoes.Sum(f => f.TotalEntradas);
                        decimal totalSaidasCongregacoes = fechamentosCongregacoes.Sum(f => f.TotalSaidas);
                        decimal totalEntradasFisicasCongregacoes = fechamentosCongregacoes.Sum(f => f.TotalEntradasFisicas);
                        decimal totalSaidasFisicasCongregacoes = fechamentosCongregacoes.Sum(f => f.TotalSaidasFisicas);
                        decimal totalEntradasDigitaisCongregacoes = fechamentosCongregacoes.Sum(f => f.TotalEntradasDigitais);
                        decimal totalSaidasDigitaisCongregacoes = fechamentosCongregacoes.Sum(f => f.TotalSaidasDigitais);

                        if (fechamentosCongregacoes.Any())
                        {
                            _logger.LogInformation($"Totais das congregações - Entradas: {totalEntradasCongregacoes:C}, Saídas: {totalSaidasCongregacoes:C}");
                        }

                        // Preparar observação
                        var observacaoFinal = viewModel.Observacoes ?? "";

                        if (viewModel.EhSede && fechamentosCongregacoes.Any())
                        {
                            observacaoFinal += $"\n\n✓ Fechamento consolidado: incluídos {fechamentosCongregacoes.Count} fechamento(s) de congregações aprovados.";
                        }

                        if (temLancamentosNovos)
                        {
                            observacaoFinal += $"\n\n✓ Lançamentos novos incluídos: {totais.TotalEntradas:C} em entradas e {totais.TotalSaidas:C} em saídas.";
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
                            TotalEntradas = totais.TotalEntradas + totalEntradasCongregacoes,
                            TotalSaidas = totais.TotalSaidas + totalSaidasCongregacoes,

                            TotalEntradasFisicas = totais.EntradasFisicas + totalEntradasFisicasCongregacoes,
                            TotalSaidasFisicas = totais.SaidasFisicas + totalSaidasFisicasCongregacoes,

                            TotalEntradasDigitais = totais.EntradasDigitais + totalEntradasDigitaisCongregacoes,
                            TotalSaidasDigitais = totais.SaidasDigitais + totalSaidasDigitaisCongregacoes,

                            BalancoFisico = (totais.EntradasFisicas + totalEntradasFisicasCongregacoes) -
                                (totais.SaidasFisicas + totalSaidasFisicasCongregacoes),

                            BalancoDigital = (totais.EntradasDigitais + totalEntradasDigitaisCongregacoes) -
                                (totais.SaidasDigitais + totalSaidasDigitaisCongregacoes),

                            Observacoes = observacaoFinal,

                            // Marcar como fechamento da SEDE se aplicável
                            EhFechamentoSede = viewModel.EhSede,
                            QuantidadeCongregacoesIncluidas = fechamentosCongregacoes.Count,

                            Status = StatusFechamentoPeriodo.Pendente,
                            UsuarioSubmissaoId = user.Id,
                            DataSubmissao = DateTime.Now
                        };

                        _logger.LogInformation($"Fechamento criado - Total Entradas: {fechamento.TotalEntradas:C}, Total Saídas: {fechamento.TotalSaidas:C}");

                        // Aplicar rateios (apenas para SEDE)
                        if (viewModel.EhSede)
                        {
                            _logger.LogInformation("Aplicando rateios ao fechamento da SEDE");
                            await AplicarRateiosComLog(fechamento);
                        }
                        else
                        {
                            fechamento.TotalRateios = 0;
                            fechamento.SaldoFinal = fechamento.BalancoFisico + fechamento.BalancoDigital;
                        }

                        // Gerar detalhes do fechamento (APENAS COM LANÇAMENTOS NÃO INCLUÍDOS)
                        _logger.LogInformation("Gerando detalhes do fechamento (entradas e saídas)");
                        await GerarDetalhesFechamento(fechamento);

                        // Salvar fechamento
                        _context.Add(fechamento);
                        await _context.SaveChangesAsync();

                        _logger.LogInformation($"Fechamento ID {fechamento.Id} salvo com sucesso - Total de {fechamento.DetalhesFechamento.Count} detalhes incluídos");

                        // ✅ MARCAR LANÇAMENTOS COMO INCLUÍDOS EM FECHAMENTO (APENAS OS NOVOS)
                        _logger.LogInformation("Marcando lançamentos como incluídos no fechamento");
                        await MarcarLancamentosComoIncluidos(fechamento);

                        // Marcar fechamentos das congregações como PROCESSADOS (se houver)
                        if (fechamentosCongregacoes.Any())
                        {
                            _logger.LogInformation($"Marcando {fechamentosCongregacoes.Count} fechamentos de congregações como PROCESSADOS");
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
                            mensagemSucesso += $" Total de {totais.TotalEntradas:C} em entradas e {totais.TotalSaidas:C} em saídas foram incluídos.";
                        }

                        await _auditService.LogAsync("Criação", "FechamentoPeriodo", mensagemSucesso);

                        _logger.LogInformation($"Fechamento ID {fechamento.Id} criado com sucesso - {mensagemSucesso}");

                        TempData["SuccessMessage"] = mensagemSucesso;

                        return RedirectToAction(nameof(Details), new { id = fechamento.Id });
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        _logger.LogError(ex, "Erro ao criar fechamento interno");
                        throw; // Re-lançar para que o ExecuteAsync possa tratar
                    }
                });
            }
            catch (Exception ex)
            {
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
            // ✅ OTIMIZADO: Incluir apenas o necessário e usar AsNoTracking onde possível
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

            // ✅ OTIMIZAÇÃO: Contar rateios sem carregar dados desnecessários
            var quantidadeRateios = fechamento.ItensRateio.Count;

            _logger.LogInformation($"Aprovando fechamento ID {id}. Itens de rateio: {quantidadeRateios}");

            fechamento.Status = StatusFechamentoPeriodo.Aprovado;
            fechamento.DataAprovacao = DateTime.Now;
            fechamento.UsuarioAprovacaoId = user.Id;

            _context.Update(fechamento);

            try
            {
                await _context.SaveChangesAsync();

                await _auditService.LogAsync("Aprovação", "FechamentoPeriodo",
                    $"Fechamento {fechamento.Mes:00}/{fechamento.Ano} aprovado com {quantidadeRateios} rateios");

                TempData["SuccessMessage"] = $"Fechamento aprovado com sucesso! {quantidadeRateios} rateio(s) confirmado(s).";

                _logger.LogInformation($"Fechamento ID {id} aprovado com sucesso por {user.UserName}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao aprovar fechamento ID {id}");
                TempData["ErrorMessage"] = $"Erro ao aprovar fechamento: {ex.Message}";
                return RedirectToAction(nameof(Details), new { id });
            }

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
                await DesmarcarLancamentosComoIncluidos(id);

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

            // ✅ OTIMIZADO: Usar AsNoTracking para consultas somente leitura
            var fechamento = await _context.FechamentosPeriodo
                .AsNoTracking()
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

            // ✅ NOVO: Verificar se pode ser excluído
            // APENAS ADMINISTRADORES podem excluir fechamentos aprovados
            if (fechamento.Status == StatusFechamentoPeriodo.Aprovado)
            {
                if (!User.IsInRole(Roles.Administrador))
                {
                    TempData["ErrorMessage"] = "Apenas administradores podem excluir fechamentos aprovados. Entre em contato com um administrador do sistema.";
                    return RedirectToAction(nameof(Details), new { id = fechamento.Id });
                }
                
                // Admin pode ver a página, mas com aviso especial
                ViewBag.FechamentoAprovado = true;
                ViewBag.AvisoAdmin = "ATENÇÃO ADMINISTRADOR: Este fechamento está APROVADO. A exclusão afetará relatórios e pode impactar a integridade financeira. Prossiga apenas se tiver certeza absoluta!";
            }
            else
            {
                ViewBag.FechamentoAprovado = false;
            }

            return View(fechamento);
        }

        // POST: FechamentoPeriodo/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var fechamento = await _context.FechamentosPeriodo
                .Include(f => f.CentroCusto)
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

            // ✅ NOVO: Verificar permissão especial para excluir fechamentos aprovados
            if (fechamento.Status == StatusFechamentoPeriodo.Aprovado)
            {
                if (!User.IsInRole(Roles.Administrador))
                {
                    TempData["ErrorMessage"] = "Apenas administradores podem excluir fechamentos aprovados.";
                    return RedirectToAction(nameof(Details), new { id = fechamento.Id });
                }
                
                _logger.LogWarning($"ATENÇÃO: Administrador {User.Identity.Name} está excluindo fechamento APROVADO ID {id}");
            }

            try
            {
                // ✅ DESMARCAR LANÇAMENTOS ANTES DE EXCLUIR
                await DesmarcarLancamentosComoIncluidos(fechamento.Id);

                // ✅ NOVO: Liberar fechamentos de congregações se for fechamento da SEDE
                if (fechamento.EhFechamentoSede)
                {
                    await LiberarFechamentosCongregacoesProcessados(fechamento.Id);
                }

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

                // Log de auditoria detalhado
                var logMensagem = fechamento.Status == StatusFechamentoPeriodo.Aprovado
                    ? $"⚠️ EXCLUSÃO DE FECHAMENTO APROVADO por ADMINISTRADOR - Fechamento {ObterDescricaoFechamento(fechamento)} do centro {fechamento.CentroCusto.Nome} excluído. Total Entradas: {fechamento.TotalEntradas:C}, Total Saídas: {fechamento.TotalSaidas:C}. Lançamentos liberados."
                    : $"Fechamento {ObterDescricaoFechamento(fechamento)} excluído. Lançamentos liberados para novo fechamento.";

                await _auditService.LogAsync("Exclusão", "FechamentoPeriodo", logMensagem);

                var mensagemSucesso = fechamento.Status == StatusFechamentoPeriodo.Aprovado
                    ? "Fechamento APROVADO excluído com sucesso! ⚠️ Os lançamentos e fechamentos de congregações foram liberados. Considere refazer o fechamento com os dados corretos."
                    : "Fechamento excluído com sucesso! Os lançamentos foram liberados para novo fechamento.";

                TempData["SuccessMessage"] = mensagemSucesso;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao excluir fechamento ID {id}");
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

            // ✅ OTIMIZADO: Usar AsNoTracking para consultas somente leitura
            // Buscar apenas o Centro de Custo SEDE
            var sede = await _context.CentrosCusto
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Tipo == TipoCentroCusto.Sede && c.Ativo);

            if (sede == null)
            {
                TempData["ErrorMessage"] = "Nenhuma SEDE configurada no sistema.";
                return RedirectToAction(nameof(Index));
            }

            // Buscar fechamentos de congregações APROVADOS e NÃO PROCESSADOS
            var fechamentosDisponiveis = await _context.FechamentosPeriodo
                .AsNoTracking()
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

            // ✅ VALIDAÇÕES ANTES DA TRANSAÇÃO
            var fechamentoExistente = await FechamentoQueryHelper.BuscarFechamentoAprovadoNoPeriodo(
                _context, sede.Id, viewModel.DataInicio, viewModel.DataFim);

            if (fechamentoExistente != null)
            {
                // Para fechamentos NÃO DIÁRIOS, bloquear totalmente
                if (viewModel.TipoFechamento != TipoFechamento.Diario)
                {
                    TempData["ErrorMessage"] = $"Já existe um fechamento APROVADO da SEDE para este período ({viewModel.DataInicio:dd/MM/yyyy} - {viewModel.DataFim:dd/MM/yyyy}).";
                    viewModel.FechamentosDisponiveis = await CarregarFechamentosDisponiveis();
                    return View(viewModel);
                }

                // Para fechamentos DIÁRIOS, verificar se há lançamentos novos
                var temLancamentosNovosAntesDeCalcular = await FechamentoQueryHelper.TemLancamentosNovos(
                    _context, sede.Id, viewModel.DataInicio, viewModel.DataFim);

                if (!temLancamentosNovosAntesDeCalcular)
                {
                    TempData["ErrorMessage"] = $"Já existe um fechamento APROVADO para o dia {viewModel.DataInicio:dd/MM/yyyy} e não há novos lançamentos desde então.";
                    viewModel.FechamentosDisponiveis = await CarregarFechamentosDisponiveis();
                    return View(viewModel);
                }
            }

            // ✅ CORREÇÃO: Usar ExecutionStrategy para suportar SqlServerRetryingExecutionStrategy
            try
            {
                var strategy = _context.Database.CreateExecutionStrategy();
                return await strategy.ExecuteAsync(async () =>
                {
                    using var transaction = await _context.Database.BeginTransactionAsync();
                    try
                    {

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

                            _logger.LogInformation($"Selecionados {fechamentosCongregacoes.Count} fechamentos de congregações para consolidação");
                        }

                        // 2. Calcular totais da SEDE no período usando HELPER
                        var totais = await FechamentoQueryHelper.CalcularTotais(
                            _context, sede.Id, viewModel.DataInicio, viewModel.DataFim);

                        // ✅ VERIFICAR SE HÁ LANÇAMENTOS NOVOS
                        var temLancamentosNovos = totais.TotalEntradas > 0 || totais.TotalSaidas > 0;

                        if (!temLancamentosNovos && !fechamentosCongregacoes.Any())
                        {
                            TempData["ErrorMessage"] = "Não há lançamentos novos da SEDE nem prestações de congregações para incluir neste fechamento.";
                            viewModel.FechamentosDisponiveis = await CarregarFechamentosDisponiveis();
                            throw new InvalidOperationException("Sem lançamentos para processar");
                        }

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
                            observacaoFinal += $"\n\n✓ Fechamento consolidado: incluídos {fechamentosCongregacoes.Count} fechamento(s) de congregações aprovados.";
                        }

                        if (temLancamentosNovos)
                        {
                            observacaoFinal += $"\n\n✓ Lançamentos novos da SEDE incluídos: {totais.TotalEntradas:C} em entradas e {totais.TotalSaidas:C} em saídas.";
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
                            TotalEntradas = totais.TotalEntradas + totalEntradasCongregacoes,
                            TotalSaidas = totais.TotalSaidas + totalSaidasCongregacoes,

                            TotalEntradasFisicas = totais.EntradasFisicas + totalEntradasFisicasCongregacoes,
                            TotalSaidasFisicas = totais.SaidasFisicas + totalSaidasFisicasCongregacoes,

                            TotalEntradasDigitais = totais.EntradasDigitais + totalEntradasDigitaisCongregacoes,
                            TotalSaidasDigitais = totais.SaidasDigitais + totalSaidasDigitaisCongregacoes,

                            BalancoFisico = (totais.EntradasFisicas + totalEntradasFisicasCongregacoes) -
                                (totais.SaidasFisicas + totalSaidasFisicasCongregacoes),

                            BalancoDigital = (totais.EntradasDigitais + totalEntradasDigitaisCongregacoes) -
                                (totais.SaidasDigitais + totalSaidasDigitaisCongregacoes),

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

                        // 6. Gerar detalhes do fechamento (APENAS COM LANÇAMENTOS NOVOS)
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
                            mensagemSucesso += $" Total de {totais.TotalEntradas:C} em entradas e {totais.TotalSaidas:C} em saídas da SEDE foram incluídos.";
                        }

                        await _auditService.LogAsync("Criação", "FechamentoPeriodo", mensagemSucesso);

                        TempData["SuccessMessage"] = mensagemSucesso;

                        return RedirectToAction(nameof(Details), new { id = fechamentoSede.Id });
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        _logger.LogError(ex, "Erro ao criar fechamento da SEDE interno");
                        throw; // Re-lançar para que o ExecuteAsync possa tratar
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar fechamento da SEDE");
                TempData["ErrorMessage"] = $"Erro ao criar fechamento: {ex.Message}";

                viewModel.FechamentosDisponiveis = await CarregarFechamentosDisponiveis();
                return View(viewModel);
            }
        }

        // Método auxiliar para recarregar lista
        // ✅ OTIMIZADO: Usa AsNoTracking e projeta apenas campos necessários
        private async Task<List<FechamentoCongregacaoDisponivel>> CarregarFechamentosDisponiveis()
        {
            return await _context.FechamentosPeriodo
                .AsNoTracking()
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
            // Administradores e Tesoureiro Gerais podem acessar qualquer centro de custo
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
            // ✅ USAR HELPER PARA CALCULAR TOTAIS (evita duplicação de queries complexas)
            var totais = await FechamentoQueryHelper.CalcularTotais(
                _context,
                fechamento.CentroCustoId,
                fechamento.DataInicio,
                fechamento.DataFim,
                fechamento.Id);

            // Atribuir valores calculados ao fechamento
            fechamento.TotalEntradasFisicas = totais.EntradasFisicas;
            fechamento.TotalEntradasDigitais = totais.EntradasDigitais;
            fechamento.TotalSaidasFisicas = totais.SaidasFisicas;
            fechamento.TotalSaidasDigitais = totais.SaidasDigitais;
            fechamento.TotalEntradas = totais.TotalEntradas;
            fechamento.TotalSaidas = totais.TotalSaidas;
            fechamento.BalancoFisico = totais.BalancoFisico;
            fechamento.BalancoDigital = totais.BalancoDigital;
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
        // ✅ MELHORADO: Inclui informações detalhadas para fechamento mensal
        private async Task GerarDetalhesFechamento(FechamentoPeriodo fechamento)
        {
            _logger.LogInformation($"Gerando detalhes do fechamento {fechamento.Id} - Tipo: {fechamento.TipoFechamento}");

            // ✅ EXECUTAR SEQUENCIALMENTE - DbContext não suporta operações paralelas
            var detalheEntradas = await _context.Entradas
                .AsNoTracking()
                .Include(e => e.PlanoDeContas)
                .Include(e => e.MeioDePagamento)
                .Include(e => e.Membro)
                .Where(FechamentoQueryHelper.EntradasNaoIncluidasEmFechamentosAprovados(
                    fechamento.CentroCustoId,
                    fechamento.DataInicio,
                    fechamento.DataFim))
                .Select(e => new DetalheFechamento
                {
                    FechamentoPeriodoId = fechamento.Id,
                    TipoMovimento = "Entrada",
                    Descricao = e.Descricao ?? "Sem descrição",
                    Valor = e.Valor,
                    Data = e.Data,
                    PlanoContas = e.PlanoDeContas != null ? e.PlanoDeContas.Nome : null,
                    MeioPagamento = e.MeioDePagamento != null ? e.MeioDePagamento.Nome : null,
                    Membro = e.Membro != null ? e.Membro.NomeCompleto : null,
                    Observacoes = e.Observacoes
                })
                .ToListAsync();

            _logger.LogInformation($"Encontradas {detalheEntradas.Count} entradas para incluir no fechamento");

            var detalheSaidas = await _context.Saidas
                .AsNoTracking()
                .Include(s => s.PlanoDeContas)
                .Include(s => s.MeioDePagamento)
                .Include(s => s.Fornecedor)
                .Where(FechamentoQueryHelper.SaidasNaoIncluidasEmFechamentosAprovados(
                    fechamento.CentroCustoId,
                    fechamento.DataInicio,
                    fechamento.DataFim))
                .Select(s => new DetalheFechamento
                {
                    FechamentoPeriodoId = fechamento.Id,
                    TipoMovimento = "Saida",
                    Descricao = s.Descricao ?? "Sem descrição",
                    Valor = s.Valor,
                    Data = s.Data,
                    PlanoContas = s.PlanoDeContas != null ? s.PlanoDeContas.Nome : null,
                    MeioPagamento = s.MeioDePagamento != null ? s.MeioDePagamento.Nome : null,
                    Fornecedor = s.Fornecedor != null ? s.Fornecedor.Nome : null,
                    Observacoes = s.Observacoes
                })
                .ToListAsync();

            _logger.LogInformation($"Encontradas {detalheSaidas.Count} saídas para incluir no fechamento");

            // ✅ CORREÇÃO CRÍTICA: Adicionar detalhes sem cast inválido
            // O EF Core rastreia automaticamente os objetos adicionados à coleção
            foreach (var detalhe in detalheEntradas)
            {
                fechamento.DetalhesFechamento.Add(detalhe);
            }

            foreach (var detalhe in detalheSaidas)
            {
                fechamento.DetalhesFechamento.Add(detalhe);
            }

            _logger.LogInformation($"Total de {detalheEntradas.Count + detalheSaidas.Count} detalhes adicionados ao fechamento {fechamento.Id}");
        }

        // ✅ NOVO MÉTODO: Marcar Lançamentos como Incluídos em Fechamento
        private async Task MarcarLancamentosComoIncluidos(FechamentoPeriodo fechamento)
        {
            // Atualizar entradas em massa usando ExecuteUpdate (EF Core 7+)
            var entradasAtualizadas = await _context.Entradas
                .Where(FechamentoQueryHelper.EntradasNaoIncluidasEmFechamentosAprovados(
                    fechamento.CentroCustoId,
                    fechamento.DataInicio,
                    fechamento.DataFim,
                    fechamento.Id))
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(e => e.IncluidaEmFechamento, true)
                    .SetProperty(e => e.FechamentoQueIncluiuId, fechamento.Id)
                    .SetProperty(e => e.DataInclusaoFechamento, DateTime.Now));

            // Atualizar saídas em massa usando ExecuteUpdate
            var saidasAtualizadas = await _context.Saidas
                .Where(FechamentoQueryHelper.SaidasNaoIncluidasEmFechamentosAprovados(
                    fechamento.CentroCustoId,
                    fechamento.DataInicio,
                    fechamento.DataFim,
                    fechamento.Id))
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(s => s.IncluidaEmFechamento, true)
                    .SetProperty(s => s.FechamentoQueIncluiuId, fechamento.Id)
                    .SetProperty(s => s.DataInclusaoFechamento, DateTime.Now));

            _logger.LogInformation($"Marcados {entradasAtualizadas} entradas e {saidasAtualizadas} saídas como incluídas no fechamento ID {fechamento.Id}");
        }

        // ✅ NOVO MÉTODO: Desmarcar Lançamentos (para quando fechamento for rejeitado ou excluído)
        private async Task DesmarcarLancamentosComoIncluidos(int fechamentoId)
        {
            // Desmarcar entradas em massa
            var entradasDesmarcadas = await _context.Entradas
                .Where(e => e.FechamentoQueIncluiuId == fechamentoId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(e => e.IncluidaEmFechamento, false)
                    .SetProperty(e => e.FechamentoQueIncluiuId, (int?)null)
                    .SetProperty(e => e.DataInclusaoFechamento, (DateTime?)null));

            // Desmarcar saídas em massa
            var saídasDesmarcadas = await _context.Saidas
                .Where(s => s.FechamentoQueIncluiuId == fechamentoId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(s => s.IncluidaEmFechamento, false)
                    .SetProperty(s => s.FechamentoQueIncluiuId, (int?)null)
                    .SetProperty(s => s.DataInclusaoFechamento, (DateTime?)null));

            _logger.LogInformation($"Desmarcados {entradasDesmarcadas} entradas e {saídasDesmarcadas} saídas do fechamento ID {fechamentoId}");
        }

        /// <summary>
        /// ✅ NOVO MÉTODO: Libera fechamentos de congregações que foram processados por um fechamento da SEDE excluído.
        /// Reverte o status de Processado para Aprovado, permitindo que sejam incluídos em novo fechamento da SEDE.
        /// </summary>
        private async Task LiberarFechamentosCongregacoesProcessados(int fechamentoSedeId)
        {
            var fechamentosCongregacoesProcessados = await _context.FechamentosPeriodo
                .Where(f => f.FechamentoSedeProcessadorId == fechamentoSedeId)
                .ToListAsync();

            if (fechamentosCongregacoesProcessados.Any())
            {
                _logger.LogInformation($"Liberando {fechamentosCongregacoesProcessados.Count} fechamentos de congregações que estavam processados pelo fechamento da SEDE ID {fechamentoSedeId}");

                foreach (var fechamentoCongregacao in fechamentosCongregacoesProcessados)
                {
                    fechamentoCongregacao.FoiProcessadoPelaSede = false;
                    fechamentoCongregacao.FechamentoSedeProcessadorId = null;
                    fechamentoCongregacao.DataProcessamentoPelaSede = null;
                    fechamentoCongregacao.Status = StatusFechamentoPeriodo.Aprovado; // Voltar para Aprovado
                    _context.Update(fechamentoCongregacao);

                    _logger.LogInformation($"Fechamento congregação ID {fechamentoCongregacao.Id} liberado - Status retornado para Aprovado");
                }

                await _context.SaveChangesAsync();

                await _auditService.LogAsync("Liberação", "FechamentoPeriodo",
                    $"Liberados {fechamentosCongregacoesProcessados.Count} fechamento(s) de congregações devido à exclusão do fechamento da SEDE ID {fechamentoSedeId}. Status retornado para Aprovado.");
            }
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