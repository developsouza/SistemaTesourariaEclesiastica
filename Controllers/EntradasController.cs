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

namespace SistemaTesourariaEclesiastica.Controllers
{
    [Authorize(Policy = "OperacoesFinanceiras")]
    public class EntradasController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AuditService _auditService;
        private readonly BusinessRulesService _businessRules;
        private readonly ILogger<EntradasController> _logger;

        public EntradasController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            AuditService auditService,
            BusinessRulesService businessRules,
            ILogger<EntradasController> logger)
        {
            _context = context;
            _userManager = userManager;
            _auditService = auditService;
            _businessRules = businessRules;
            _logger = logger;
        }

        // GET: Entradas
        public async Task<IActionResult> Index(DateTime? dataInicio, DateTime? dataFim, int? centroCustoId,
            int? planoContasId, int? meioPagamentoId, int? membroId, int page = 1, int pageSize = 50)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                await _auditService.LogAsync("Visualização", "Entradas", "Listagem de entradas acessada");

                // Aplicar filtros de data padrão se não informados
                if (!dataInicio.HasValue)
                    dataInicio = DateTime.Now.AddMonths(-3);
                if (!dataFim.HasValue)
                    dataFim = DateTime.Now;

                // Query base
                var query = _context.Entradas
                    .Include(e => e.Membro)
                    .Include(e => e.PlanoDeContas)
                    .Include(e => e.CentroCusto)
                    .Include(e => e.MeioDePagamento)
                    .AsQueryable();

                // Aplicar filtros baseados no perfil do usuário
                if (!User.IsInRole(Roles.Administrador) && !User.IsInRole(Roles.TesoureiroGeral))
                {
                    // Tesoureiros Locais e Pastores só veem dados do seu centro de custo
                    if (user.CentroCustoId.HasValue)
                    {
                        query = query.Where(e => e.CentroCustoId == user.CentroCustoId.Value);
                    }
                    else
                    {
                        // Se não tem centro de custo, não vê nada
                        query = query.Where(e => false);
                    }
                }

                // Aplicar filtros de pesquisa
                if (dataInicio.HasValue)
                    query = query.Where(e => e.Data.Date >= dataInicio.Value.Date);
                if (dataFim.HasValue)
                    query = query.Where(e => e.Data.Date <= dataFim.Value.Date);
                if (centroCustoId.HasValue)
                    query = query.Where(e => e.CentroCustoId == centroCustoId.Value);
                if (planoContasId.HasValue)
                    query = query.Where(e => e.PlanoDeContasId == planoContasId.Value);
                if (meioPagamentoId.HasValue)
                    query = query.Where(e => e.MeioDePagamentoId == meioPagamentoId.Value);
                if (membroId.HasValue)
                    query = query.Where(e => e.MembroId == membroId.Value);

                // Paginação
                var totalItems = await query.CountAsync();
                var entradas = await query
                    .OrderByDescending(e => e.Data)
                    .ThenByDescending(e => e.Id)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                // Preparar dados para os filtros (respeitando permissões)
                var centrosCustoQuery = _context.CentrosCusto.Where(c => c.Ativo);
                if (!User.IsInRole(Roles.Administrador) && !User.IsInRole(Roles.TesoureiroGeral))
                {
                    if (user.CentroCustoId.HasValue)
                    {
                        centrosCustoQuery = centrosCustoQuery.Where(c => c.Id == user.CentroCustoId.Value);
                    }
                }

                ViewBag.CentrosCusto = new SelectList(await centrosCustoQuery.ToListAsync(), "Id", "Nome", centroCustoId);
                ViewBag.PlanosContas = new SelectList(await _context.PlanosDeContas.Where(p => p.Tipo == TipoPlanoContas.Receita && p.Ativo).ToListAsync(), "Id", "Nome", planoContasId);
                ViewBag.MeiosPagamento = new SelectList(await _context.MeiosDePagamento.Where(m => m.Ativo).ToListAsync(), "Id", "Nome", meioPagamentoId);
                ViewBag.Membros = new SelectList(await _context.Membros.Where(m => m.Ativo).ToListAsync(), "Id", "NomeCompleto", membroId);

                // Dados de paginação
                ViewBag.CurrentPage = page;
                ViewBag.PageSize = pageSize;
                ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);
                ViewBag.TotalItems = totalItems;

                // Dados dos filtros atuais
                ViewBag.DataInicio = dataInicio?.ToString("yyyy-MM-dd");
                ViewBag.DataFim = dataFim?.ToString("yyyy-MM-dd");
                ViewBag.CentroCustoId = centroCustoId;
                ViewBag.PlanoContasId = planoContasId;
                ViewBag.MeioPagamentoId = meioPagamentoId;
                ViewBag.MembroId = membroId;

                // Estatísticas resumidas
                var totalValor = entradas.Sum(e => e.Valor);
                ViewBag.TotalValor = totalValor;
                ViewBag.TotalRegistros = totalItems;

                return View(entradas);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao listar entradas");
                TempData["Erro"] = "Erro ao carregar a listagem de entradas.";
                return View(new List<Entrada>());
            }
        }

        // GET: Entradas/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            try
            {
                var entrada = await _context.Entradas
                    .Include(e => e.Membro)
                    .Include(e => e.PlanoDeContas)
                    .Include(e => e.CentroCusto)
                    .Include(e => e.MeioDePagamento)
                    .FirstOrDefaultAsync(m => m.Id == id);

                if (entrada == null) return NotFound();

                // Verificar permissão de acesso
                if (!await CanAccessEntry(entrada))
                {
                    return Forbid();
                }

                var user = await _userManager.GetUserAsync(User);
                await _auditService.LogAsync("Visualização", "Entrada", $"Detalhes da entrada #{id} visualizados");

                return View(entrada);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao exibir detalhes da entrada {EntradaId}", id);
                TempData["Erro"] = "Erro ao carregar os detalhes da entrada.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Entradas/Create
        public async Task<IActionResult> Create()
        {
            try
            {
                await PrepareViewBags();
                var model = new Entrada
                {
                    Data = DateTime.Now,
                    CentroCustoId = await GetUserCentroCustoId() ?? 0
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao preparar tela de criação de entrada");
                TempData["Erro"] = "Erro ao carregar a tela de criação.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Entradas/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Data,Valor,Descricao,MembroId,PlanoDeContasId,CentroCustoId,MeioDePagamentoId,Observacoes")] Entrada entrada)
        {
            try
            {
                // CRÍTICO: Remover navigation properties do ModelState
                ModelState.Remove("MeioDePagamento");
                ModelState.Remove("CentroCusto");
                ModelState.Remove("PlanoDeContas");
                ModelState.Remove("Membro");
                ModelState.Remove("Usuario");
                ModelState.Remove("UsuarioId");
                ModelState.Remove("ModeloRateioEntrada");

                // Validações de negócio
                var validationResult = await _businessRules.ValidateEntradaAsync(entrada);
                if (!validationResult.IsValid)
                {
                    ModelState.AddModelError(string.Empty, validationResult.ErrorMessage);
                }

                // Verificar se pode criar entrada no centro de custo especificado
                if (!await CanAccessCentroCusto(entrada.CentroCustoId))
                {
                    ModelState.AddModelError("CentroCustoId", "Você não tem permissão para criar entradas neste centro de custo.");
                }

                if (ModelState.IsValid)
                {
                    var user = await _userManager.GetUserAsync(User);

                    // Definir dados de auditoria
                    entrada.DataCriacao = DateTime.Now;
                    entrada.UsuarioId = user.Id;

                    _context.Add(entrada);
                    await _context.SaveChangesAsync();

                    await _auditService.LogCreateAsync(user.Id, entrada, entrada.Id.ToString());

                    TempData["Sucesso"] = "Entrada cadastrada com sucesso!";
                    return RedirectToAction(nameof(Index));
                }

                await PrepareViewBags(entrada);
                return View(entrada);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar entrada");
                ModelState.AddModelError(string.Empty, "Erro interno ao salvar entrada. Tente novamente.");
                await PrepareViewBags(entrada);
                return View(entrada);
            }
        }

        // GET: Entradas/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            try
            {
                var entrada = await _context.Entradas.FindAsync(id);
                if (entrada == null) return NotFound();

                // Verificar permissão de acesso
                if (!await CanAccessEntry(entrada))
                {
                    return Forbid();
                }

                await PrepareViewBags(entrada);
                return View(entrada);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao carregar entrada para edição {EntradaId}", id);
                TempData["Erro"] = "Erro ao carregar entrada para edição.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Entradas/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Data,Valor,Descricao,MembroId,PlanoDeContasId,CentroCustoId,MeioDePagamentoId,Observacoes")] Entrada entrada)
        {
            if (id != entrada.Id) return NotFound();

            try
            {
                // CRÍTICO: Remover navigation properties do ModelState
                ModelState.Remove("MeioDePagamento");
                ModelState.Remove("CentroCusto");
                ModelState.Remove("PlanoDeContas");
                ModelState.Remove("Membro");
                ModelState.Remove("Usuario");
                ModelState.Remove("ModeloRateioEntrada");
                ModelState.Remove("UsuarioId");
                ModelState.Remove("DataCriacao");

                // Buscar entrada original
                var originalEntrada = await _context.Entradas.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id);
                if (originalEntrada == null) return NotFound();

                // Verificar permissão de acesso
                if (!await CanAccessEntry(originalEntrada))
                {
                    return Forbid();
                }

                // Validações de negócio
                var validationResult = await _businessRules.ValidateEntradaAsync(entrada, id);
                if (!validationResult.IsValid)
                {
                    ModelState.AddModelError(string.Empty, validationResult.ErrorMessage);
                }

                if (ModelState.IsValid)
                {
                    var user = await _userManager.GetUserAsync(User);

                    // Manter dados de criação originais
                    entrada.DataCriacao = originalEntrada.DataCriacao;
                    entrada.UsuarioId = originalEntrada.UsuarioId;

                    _context.Update(entrada);
                    await _context.SaveChangesAsync();

                    await _auditService.LogUpdateAsync(user.Id, originalEntrada, entrada, entrada.Id.ToString());

                    TempData["Sucesso"] = "Entrada atualizada com sucesso!";
                    return RedirectToAction(nameof(Index));
                }

                await PrepareViewBags(entrada);
                return View(entrada);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!EntradaExists(entrada.Id))
                    return NotFound();
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atualizar entrada {EntradaId}", id);
                ModelState.AddModelError(string.Empty, "Erro interno ao atualizar entrada. Tente novamente.");
                await PrepareViewBags(entrada);
                return View(entrada);
            }
        }

        // GET: Entradas/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            try
            {
                var entrada = await _context.Entradas
                    .Include(e => e.Membro)
                    .Include(e => e.PlanoDeContas)
                    .Include(e => e.CentroCusto)
                    .Include(e => e.MeioDePagamento)
                    .FirstOrDefaultAsync(m => m.Id == id);

                if (entrada == null) return NotFound();

                // Verificar permissão de acesso
                if (!await CanAccessEntry(entrada))
                {
                    return Forbid();
                }

                return View(entrada);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao carregar entrada para exclusão {EntradaId}", id);
                TempData["Erro"] = "Erro ao carregar entrada para exclusão.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Entradas/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var entrada = await _context.Entradas
                    .Include(e => e.MeioDePagamento)
                    .FirstOrDefaultAsync(e => e.Id == id);
                    
                if (entrada == null) return NotFound();

                // Verificar permissão de acesso
                if (!await CanAccessEntry(entrada))
                {
                    return Forbid();
                }

                var user = await _userManager.GetUserAsync(User);

                // ✅ NOVO: Limpar fechamentos pendentes antes de excluir
                if (entrada.IncluidaEmFechamento && entrada.FechamentoQueIncluiuId.HasValue)
                {
                    await LimparFechamentosPendentesAposExclusaoEntrada(entrada);
                }

                _context.Entradas.Remove(entrada);
                await _context.SaveChangesAsync();

                await _auditService.LogDeleteAsync(user.Id, entrada, entrada.Id.ToString());

                TempData["Sucesso"] = "Entrada excluída com sucesso!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao excluir entrada {EntradaId}", id);
                TempData["Erro"] = "Erro ao excluir entrada. Verifique se não há dependências.";
                return RedirectToAction(nameof(Index));
            }
        }

        #region Métodos Auxiliares

        private async Task PrepareViewBags(Entrada entrada = null)
        {
            var user = await _userManager.GetUserAsync(User);

            // Centros de custo (baseado em permissões)
            var centrosCustoQuery = _context.CentrosCusto.Where(c => c.Ativo);
            if (!User.IsInRole(Roles.Administrador) && !User.IsInRole(Roles.TesoureiroGeral))
            {
                if (user.CentroCustoId.HasValue)
                {
                    centrosCustoQuery = centrosCustoQuery.Where(c => c.Id == user.CentroCustoId.Value);
                }
            }

            // Membros (baseado em permissões)
            var membrosQuery = _context.Membros.Where(m => m.Ativo);
            if (!User.IsInRole(Roles.Administrador) && !User.IsInRole(Roles.TesoureiroGeral))
            {
                // Tesoureiros Locais só veem membros do seu centro de custo
                if (user?.CentroCustoId.HasValue == true)
                {
                    membrosQuery = membrosQuery.Where(m => m.CentroCustoId == user.CentroCustoId.Value);
                }
            }

            ViewData["CentroCustoId"] = new SelectList(await centrosCustoQuery.ToListAsync(), "Id", "Nome", entrada?.CentroCustoId);
            ViewData["MembroId"] = new SelectList(await membrosQuery.ToListAsync(), "Id", "NomeCompleto", entrada?.MembroId);
            ViewData["PlanoDeContasId"] = new SelectList(await _context.PlanosDeContas.Where(p => p.Tipo == TipoPlanoContas.Receita && p.Ativo).ToListAsync(), "Id", "Nome", entrada?.PlanoDeContasId);
            ViewData["MeioDePagamentoId"] = new SelectList(await _context.MeiosDePagamento.Where(m => m.Ativo).ToListAsync(), "Id", "Nome", entrada?.MeioDePagamentoId);
        }

        private async Task<bool> CanAccessEntry(Entrada entrada)
        {
            // Administradores e Tesoureiros Gerais podem acessar tudo
            if (User.IsInRole(Roles.Administrador) || User.IsInRole(Roles.TesoureiroGeral))
                return true;

            var user = await _userManager.GetUserAsync(User);

            // Outros usuários só podem acessar entradas do seu centro de custo
            return user.CentroCustoId.HasValue && entrada.CentroCustoId == user.CentroCustoId.Value;
        }

        private async Task<bool> CanAccessCentroCusto(int centroCustoId)
        {
            // Administradores e Tesoureiros Gerais podem acessar qualquer centro de custo
            if (User.IsInRole(Roles.Administrador) || User.IsInRole(Roles.TesoureiroGeral))
                return true;

            var user = await _userManager.GetUserAsync(User);

            // Outros usuários só podem acessar seu próprio centro de custo
            return user.CentroCustoId.HasValue && centroCustoId == user.CentroCustoId.Value;
        }

        private async Task<int?> GetUserCentroCustoId()
        {
            var user = await _userManager.GetUserAsync(User);
            return user.CentroCustoId;
        }

        private bool EntradaExists(int id)
        {
            return _context.Entradas.Any(e => e.Id == id);
        }

        /// <summary>
        /// ✅ NOVO: Limpa fechamentos PENDENTES que incluíram a entrada excluída.
        /// Remove DetalheFechamento correspondente e recalcula totais do fechamento.
        /// </summary>
        private async Task LimparFechamentosPendentesAposExclusaoEntrada(Entrada entrada)
        {
            // Buscar fechamentos PENDENTES que incluíram esta entrada
            var fechamentosPendentes = await _context.FechamentosPeriodo
                .Include(f => f.DetalhesFechamento)
                .Include(f => f.ItensRateio)
                .Include(f => f.CentroCusto)
                .Where(f => f.Id == entrada.FechamentoQueIncluiuId.Value && 
                           f.Status == StatusFechamentoPeriodo.Pendente)
                .ToListAsync();

            foreach (var fechamento in fechamentosPendentes)
            {
                _logger.LogInformation($"Limpando entrada ID {entrada.Id} do fechamento pendente ID {fechamento.Id}");

                // Remover DetalheFechamento correspondente (buscar por Data, Valor e TipoMovimento)
                var detalhesParaRemover = fechamento.DetalhesFechamento
                    .Where(d => d.TipoMovimento == "Entrada" &&
                               d.Data.Date == entrada.Data.Date &&
                               d.Valor == entrada.Valor &&
                               (d.Descricao == entrada.Descricao || 
                                (string.IsNullOrEmpty(d.Descricao) && string.IsNullOrEmpty(entrada.Descricao))))
                    .ToList();

                if (detalhesParaRemover.Any())
                {
                    _context.DetalhesFechamento.RemoveRange(detalhesParaRemover);
                    _logger.LogInformation($"Removidos {detalhesParaRemover.Count} detalhes do fechamento ID {fechamento.Id}");

                    // Recalcular totais do fechamento
                    await RecalcularTotaisFechamentoPendente(fechamento, entrada.MeioDePagamento.TipoCaixa, entrada.Valor, isEntrada: true);

                    // Registrar auditoria
                    await _auditService.LogAsync("Exclusão", "DetalheFechamento",
                        $"Entrada ID {entrada.Id} (R$ {entrada.Valor:N2}) removida do fechamento pendente ID {fechamento.Id} devido à exclusão do lançamento.");
                }
            }
        }

        /// <summary>
        /// ✅ NOVO: Recalcula totais de um fechamento PENDENTE após remoção de lançamento.
        /// </summary>
        private async Task RecalcularTotaisFechamentoPendente(FechamentoPeriodo fechamento, TipoCaixa tipoCaixa, decimal valorRemovido, bool isEntrada)
        {
            // Subtrair o valor removido dos totais correspondentes
            if (isEntrada)
            {
                fechamento.TotalEntradas -= valorRemovido;
                
                if (tipoCaixa == TipoCaixa.Fisico)
                {
                    fechamento.TotalEntradasFisicas -= valorRemovido;
                    fechamento.BalancoFisico -= valorRemovido;
                }
                else
                {
                    fechamento.TotalEntradasDigitais -= valorRemovido;
                    fechamento.BalancoDigital -= valorRemovido;
                }
            }
            else // Saída
            {
                fechamento.TotalSaidas -= valorRemovido;
                
                if (tipoCaixa == TipoCaixa.Fisico)
                {
                    fechamento.TotalSaidasFisicas -= valorRemovido;
                    fechamento.BalancoFisico += valorRemovido; // Saída reduz, então soma de volta
                }
                else
                {
                    fechamento.TotalSaidasDigitais -= valorRemovido;
                    fechamento.BalancoDigital += valorRemovido;
                }
            }

            // Recalcular saldo final (se houver rateios aplicados)
            var balancoTotal = fechamento.BalancoFisico + fechamento.BalancoDigital;
            fechamento.SaldoFinal = balancoTotal - fechamento.TotalRateios;

            _context.Update(fechamento);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Totais recalculados para fechamento ID {fechamento.Id} - " +
                $"Total Entradas: {fechamento.TotalEntradas:C}, Total Saídas: {fechamento.TotalSaidas:C}, " +
                $"Saldo Final: {fechamento.SaldoFinal:C}");
        }
        #endregion
    }
}