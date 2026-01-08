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
    [Authorize(Roles = Roles.AdminOuTesoureiro)]
    public class SaidasController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AuditService _auditService;
        private readonly ILogger<SaidasController> _logger;

        public SaidasController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            AuditService auditService,
            ILogger<SaidasController> logger)
        {
            _context = context;
            _userManager = userManager;
            _auditService = auditService;
            _logger = logger;
        }

        // GET: Saidas
        public async Task<IActionResult> Index(DateTime? dataInicio, DateTime? dataFim, int? centroCustoId,
            int? planoContasId, int? fornecedorId, TipoDespesa? tipoDespesa, int page = 1, int pageSize = 10)
        {
            ViewData["DataInicio"] = dataInicio?.ToString("yyyy-MM-dd");
            ViewData["DataFim"] = dataFim?.ToString("yyyy-MM-dd");
            ViewData["CentroCustoId"] = centroCustoId;
            ViewData["PlanoContasId"] = planoContasId;
            ViewData["FornecedorId"] = fornecedorId;
            ViewData["TipoDespesa"] = tipoDespesa;

            var user = await _userManager.GetUserAsync(User);

            var saidas = _context.Saidas
                .Include(s => s.MeioDePagamento)
                .Include(s => s.CentroCusto)
                .Include(s => s.PlanoDeContas)
                .Include(s => s.Fornecedor)
                .Include(s => s.Usuario)
                .AsQueryable();

            // ==================== FILTRO POR PERFIL DE USUÁRIO ====================
            if (!User.IsInRole(Roles.Administrador) && !User.IsInRole(Roles.TesoureiroGeral))
            {
                // Tesoureiros Locais e Pastores só veem dados do seu centro de custo
                if (user.CentroCustoId.HasValue)
                {
                    saidas = saidas.Where(s => s.CentroCustoId == user.CentroCustoId.Value);
                }
                else
                {
                    // Se não tem centro de custo, não vê nada
                    saidas = saidas.Where(s => false);
                }
            }
            // ======================================================================

            // Filtros de pesquisa
            if (dataInicio.HasValue)
            {
                saidas = saidas.Where(s => s.Data >= dataInicio.Value);
            }

            if (dataFim.HasValue)
            {
                saidas = saidas.Where(s => s.Data <= dataFim.Value);
            }

            if (centroCustoId.HasValue)
            {
                saidas = saidas.Where(s => s.CentroCustoId == centroCustoId);
            }

            if (planoContasId.HasValue)
            {
                saidas = saidas.Where(s => s.PlanoDeContasId == planoContasId);
            }

            if (fornecedorId.HasValue)
            {
                saidas = saidas.Where(s => s.FornecedorId == fornecedorId);
            }

            if (tipoDespesa.HasValue)
            {
                saidas = saidas.Where(s => s.TipoDespesa == tipoDespesa);
            }

            var totalItems = await saidas.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            var totalValor = await saidas.SumAsync(s => s.Valor);

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalItems = totalItems;
            ViewBag.TotalValor = totalValor;

            // Dropdowns para filtros (respeitando permissões)
            var centrosCustoQuery = _context.CentrosCusto.Where(c => c.Ativo);
            if (!User.IsInRole(Roles.Administrador) && !User.IsInRole(Roles.TesoureiroGeral))
            {
                if (user.CentroCustoId.HasValue)
                {
                    centrosCustoQuery = centrosCustoQuery.Where(c => c.Id == user.CentroCustoId.Value);
                }
            }

            ViewBag.CentrosCusto = new SelectList(await centrosCustoQuery.ToListAsync(), "Id", "Nome", centroCustoId);
            ViewBag.PlanosContas = new SelectList(
                await _context.PlanosDeContas.Where(p => p.Tipo == TipoPlanoContas.Despesa && p.Ativo).ToListAsync(),
                "Id", "Nome", planoContasId);
            ViewBag.Fornecedores = new SelectList(await _context.Fornecedores.Where(f => f.Ativo).ToListAsync(), "Id", "Nome", fornecedorId);

            var paginatedSaidas = await saidas
                .OrderByDescending(s => s.Data)
                .ThenByDescending(s => s.DataCriacao)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return View(paginatedSaidas);
        }

        // GET: Saidas/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var saida = await _context.Saidas
                .Include(s => s.MeioDePagamento)
                .Include(s => s.CentroCusto)
                .Include(s => s.PlanoDeContas)
                .Include(s => s.Fornecedor)
                .Include(s => s.Usuario)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (saida == null)
            {
                return NotFound();
            }

            // Verificar permissão de acesso
            if (!await CanAccessSaida(saida))
            {
                return Forbid();
            }

            return View(saida);
        }

        // GET: Saidas/Create
        public async Task<IActionResult> Create()
        {
            await PopulateDropdowns();
            await PopulateDespesasRecorrentes(); // ✅ NOVO

            var user = await _userManager.GetUserAsync(User);
            var saida = new Saida
            {
                Data = DateTime.Today,
                CentroCustoId = user.CentroCustoId ?? 0
            };

            return View(saida);
        }

        // POST: Saidas/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Data,Valor,Descricao,MeioDePagamentoId,CentroCustoId,PlanoDeContasId,FornecedorId,TipoDespesa,NumeroDocumento,DataVencimento,ComprovanteUrl,Observacoes")] Saida saida, 
            int? despesaRecorrenteId, string? pagamentoDespesaRecorrenteIds) // ✅ ALTERADO: string para múltiplos IDs
        {
            try
            {
                // CRÍTICO: Remover navigation properties do ModelState
                ModelState.Remove("MeioDePagamento");
                ModelState.Remove("CentroCusto");
                ModelState.Remove("PlanoDeContas");
                ModelState.Remove("Fornecedor");
                ModelState.Remove("Usuario");
                ModelState.Remove("UsuarioId");

                // Verificar permissão de acesso ao centro de custo
                if (!await CanAccessCentroCusto(saida.CentroCustoId))
                {
                    ModelState.AddModelError("CentroCustoId", "Você não tem permissão para criar saídas neste centro de custo.");
                }

                if (ModelState.IsValid)
                {
                    var user = await _userManager.GetUserAsync(User);
                    saida.UsuarioId = user!.Id;
                    saida.DataCriacao = DateTime.Now;

                    _context.Add(saida);
                    await _context.SaveChangesAsync();

                    // ✅ NOVO: Se vinculado a despesa recorrente, marcar múltiplos pagamentos como pagos
                    if (!string.IsNullOrEmpty(pagamentoDespesaRecorrenteIds))
                    {
                        var ids = pagamentoDespesaRecorrenteIds.Split(',')
                            .Select(id => int.TryParse(id, out int result) ? result : 0)
                            .Where(id => id > 0)
                            .ToList();
                        
                        if (ids.Any())
                        {
                            var pagamentos = await _context.PagamentosDespesasRecorrentes
                                .Where(p => ids.Contains(p.Id) && !p.Pago)
                                .ToListAsync();
                            
                            // ✅ CORRIGIDO: Dividir valor igualmente entre os pagamentos
                            decimal valorPorPagamento = pagamentos.Count > 0 ? saida.Valor / pagamentos.Count : 0;
                            int pagamentosAtualizados = 0;
                            
                            foreach (var pagamento in pagamentos)
                            {
                                pagamento.Pago = true;
                                pagamento.DataPagamento = saida.Data;
                                pagamento.ValorPago = valorPorPagamento;
                                pagamento.SaidaGerada = true;
                                pagamento.SaidaId = saida.Id;
                                pagamentosAtualizados++;
                            }
                            
                            if (pagamentosAtualizados > 0)
                            {
                                await _context.SaveChangesAsync();
                                
                                _logger.LogInformation($"{pagamentosAtualizados} pagamento(s) ID(s): [{string.Join(", ", ids)}] vinculado(s) à saída ID {saida.Id}");
                                
                                if (pagamentosAtualizados > 1)
                                {
                                    TempData["InfoMessage"] = $"{pagamentosAtualizados} pagamentos foram marcados como pagos com esta saída.";
                                }
                            }
                        }
                    }

                    // Log de auditoria
                    if (user != null)
                    {
                        await _auditService.LogCreateAsync(user.Id, saida, saida.Id.ToString());
                    }

                    TempData["SuccessMessage"] = "Saída registrada com sucesso!";
                    return RedirectToAction(nameof(Index));
                }

                await PopulateDropdowns(saida);
                await PopulateDespesasRecorrentes(saida.CentroCustoId); // ✅ NOVO
                return View(saida);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar saída");
                ModelState.AddModelError(string.Empty, "Erro interno ao salvar saída. Tente novamente.");
                await PopulateDropdowns(saida);
                await PopulateDespesasRecorrentes(saida.CentroCustoId); // ✅ NOVO
                return View(saida);
            }
        }

        // GET: Saidas/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var saida = await _context.Saidas.FindAsync(id);
            if (saida == null)
            {
                return NotFound();
            }

            // Verificar permissão de acesso
            if (!await CanAccessSaida(saida))
            {
                return Forbid();
            }

            await PopulateDropdowns(saida);
            return View(saida);
        }

        // POST: Saidas/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Data,Valor,Descricao,MeioDePagamentoId,CentroCustoId,PlanoDeContasId,FornecedorId,TipoDespesa,NumeroDocumento,DataVencimento,ComprovanteUrl,Observacoes")] Saida saida)
        {
            if (id != saida.Id)
            {
                return NotFound();
            }

            try
            {
                // CRÍTICO: Remover navigation properties do ModelState
                ModelState.Remove("MeioDePagamento");
                ModelState.Remove("CentroCusto");
                ModelState.Remove("PlanoDeContas");
                ModelState.Remove("Fornecedor");
                ModelState.Remove("Usuario");
                ModelState.Remove("UsuarioId");
                ModelState.Remove("DataCriacao");

                // Buscar saída original para manter dados de auditoria
                var originalSaida = await _context.Saidas.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id);
                if (originalSaida == null) return NotFound();

                // Verificar permissão de acesso à saída original
                if (!await CanAccessSaida(originalSaida))
                {
                    return Forbid();
                }

                // Verificar permissão de acesso ao novo centro de custo
                if (!await CanAccessCentroCusto(saida.CentroCustoId))
                {
                    ModelState.AddModelError("CentroCustoId", "Você não tem permissão para mover saídas para este centro de custo.");
                }

                if (ModelState.IsValid)
                {
                    // Manter dados originais de auditoria
                    saida.UsuarioId = originalSaida.UsuarioId;
                    saida.DataCriacao = originalSaida.DataCriacao;

                    _context.Update(saida);
                    await _context.SaveChangesAsync();

                    var user = await _userManager.GetUserAsync(User);
                    if (user != null)
                    {
                        await _auditService.LogUpdateAsync(user.Id, originalSaida, saida, saida.Id.ToString());
                    }

                    TempData["SuccessMessage"] = "Saída atualizada com sucesso!";
                    return RedirectToAction(nameof(Index));
                }

                await PopulateDropdowns(saida);
                return View(saida);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!SaidaExists(saida.Id))
                {
                    return NotFound();
                }
                throw;
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Erro interno ao atualizar saída. Tente novamente.");
                await PopulateDropdowns(saida);
                return View(saida);
            }
        }

        // GET: Saidas/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var saida = await _context.Saidas
                .Include(s => s.MeioDePagamento)
                .Include(s => s.CentroCusto)
                .Include(s => s.PlanoDeContas)
                .Include(s => s.Fornecedor)
                .Include(s => s.Usuario)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (saida == null)
            {
                return NotFound();
            }

            // Verificar permissão de acesso
            if (!await CanAccessSaida(saida))
            {
                return Forbid();
            }

            return View(saida);
        }

        // POST: Saidas/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var saida = await _context.Saidas
                .Include(s => s.MeioDePagamento)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (saida != null)
            {
                // Verificar permissão de acesso
                if (!await CanAccessSaida(saida))
                {
                    return Forbid();
                }

                // ✅ NOVO: Limpar fechamentos pendentes antes de excluir
                if (saida.IncluidaEmFechamento && saida.FechamentoQueIncluiuId.HasValue)
                {
                    await LimparFechamentosPendentesAposExclusaoSaida(saida);
                }

                _context.Saidas.Remove(saida);
                await _context.SaveChangesAsync();

                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    await _auditService.LogAuditAsync(user.Id, "Excluir", "Saida", saida.Id.ToString(), $"Saída de {saida.Valor:C2} excluída.");
                }
                TempData["SuccessMessage"] = "Saída excluída com sucesso!";
            }

            return RedirectToAction(nameof(Index));
        }

        // AJAX: Obter dados do fornecedor
        [HttpGet]
        public async Task<IActionResult> ObterDadosFornecedor(int fornecedorId)
        {
            var fornecedor = await _context.Fornecedores.FirstOrDefaultAsync(f => f.Id == fornecedorId);

            if (fornecedor == null)
            {
                return Json(new { success = false });
            }

            return Json(new
            {
                success = true,
                nome = fornecedor.Nome,
                documento = fornecedor.CNPJ ?? fornecedor.CPF,
                telefone = fornecedor.Telefone,
                email = fornecedor.Email
            });
        }

        // ✅ NOVO: Endpoint para verificar pagamentos pendentes
        [HttpGet]
        public async Task<IActionResult> VerificarPagamentosPendentes(int despesaRecorrenteId, int mes, int ano)
        {
            var dataInicio = new DateTime(ano, mes, 1);
            var dataFim = dataInicio.AddMonths(1).AddDays(-1);
            
            var pagamentos = await _context.PagamentosDespesasRecorrentes
                .Where(p => p.DespesaRecorrenteId == despesaRecorrenteId)
                .Where(p => p.DataVencimento >= dataInicio && p.DataVencimento <= dataFim)
                .Select(p => new {
                    p.Id,
                    p.DataVencimento,
                    p.ValorPrevisto,
                    p.Pago,
                    p.ValorPago,
                    DiasAtraso = p.Pago ? (int?)null : (DateTime.Today > p.DataVencimento ? (DateTime.Today - p.DataVencimento).Days : 0)
                })
                .OrderBy(p => p.DataVencimento)
                .ToListAsync();
            
            return Json(pagamentos);
        }

        // ✅ NOVO: Cadastro rápido de despesa recorrente via modal
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CadastrarDespesaRecorrenteRapido([Bind("Nome,ValorPadrao,Periodicidade,CentroCustoId,PlanoDeContasId,FornecedorId,MeioDePagamentoId,DiaVencimento")] DespesaRecorrente despesa)
        {
            try
            {
                ModelState.Remove("CentroCusto");
                ModelState.Remove("PlanoDeContas");
                ModelState.Remove("Fornecedor");
                ModelState.Remove("MeioDePagamento");
                ModelState.Remove("Pagamentos");
                
                if (!await CanAccessCentroCusto(despesa.CentroCustoId))
                {
                    return Json(new { success = false, message = "Você não tem permissão para criar despesas neste centro de custo." });
                }
                
                if (ModelState.IsValid)
                {
                    despesa.DataCadastro = DateTime.Now;
                    despesa.DataInicio = DateTime.Today;
                    despesa.Ativa = true;
                    
                    _context.Add(despesa);
                    await _context.SaveChangesAsync();
                    
                    var user = await _userManager.GetUserAsync(User);
                    if (user != null)
                    {
                        await _auditService.LogCreateAsync(user.Id, despesa, despesa.Id.ToString());
                    }
                    
                    return Json(new { 
                        success = true, 
                        message = "Despesa recorrente cadastrada com sucesso!",
                        despesaId = despesa.Id,
                        despesaNome = $"{despesa.Nome} ({despesa.ValorPadrao:C2})"
                    });
                }
                
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                return Json(new { success = false, message = string.Join("; ", errors) });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao cadastrar despesa recorrente rápida");
                return Json(new { success = false, message = "Erro ao cadastrar despesa recorrente." });
            }
        }

        // ✅ NOVO: Gerar pagamentos automaticamente para o mês
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GerarPagamentosAutomatico(int despesaRecorrenteId, int mes, int ano)
        {
            try
            {
                var despesa = await _context.DespesasRecorrentes
                    .Include(d => d.Pagamentos)
                    .FirstOrDefaultAsync(d => d.Id == despesaRecorrenteId);
                
                if (despesa == null)
                {
                    return Json(new { success = false, message = "Despesa recorrente não encontrada." });
                }
                
                if (!await CanAccessCentroCusto(despesa.CentroCustoId))
                {
                    return Json(new { success = false, message = "Você não tem permissão para acessar esta despesa." });
                }
                
                if (!despesa.Ativa)
                {
                    return Json(new { success = false, message = "Não é possível gerar pagamentos para uma despesa inativa." });
                }
                
                // Determinar data base e índice inicial
                DateTime dataBase;
                int indiceInicial = 0;
                
                if (despesa.Pagamentos.Any())
                {
                    var ultimoPagamento = despesa.Pagamentos.OrderByDescending(p => p.DataVencimento).First();
                    dataBase = ultimoPagamento.DataVencimento;
                    indiceInicial = 1;
                }
                else
                {
                    dataBase = despesa.DataInicio ?? DateTime.Today;
                }
                
                // Calcular quantos períodos necessários para cobrir o mês solicitado
                var dataAlvo = new DateTime(ano, mes, 1);
                var pagamentosGerados = 0;
                var pagamentosNovos = new List<PagamentoDespesaRecorrente>();
                
                // Gerar até 12 períodos ou até cobrir o mês solicitado
                for (int i = indiceInicial; i < indiceInicial + 12; i++)
                {
                    DateTime dataVencimento = CalcularProximoVencimento(despesa, dataBase, i);
                    
                    // Parar se passou muito do mês alvo
                    if (dataVencimento.Year > ano || (dataVencimento.Year == ano && dataVencimento.Month > mes + 1))
                    {
                        break;
                    }
                    
                    // Verificar se está dentro do período de término
                    if (despesa.DataTermino.HasValue && dataVencimento > despesa.DataTermino.Value)
                    {
                        break;
                    }
                    
                    // Verificar duplicação
                    var jaExiste = despesa.Pagamentos.Any(p => p.DataVencimento.Date == dataVencimento.Date);
                    
                    if (!jaExiste)
                    {
                        var pagamento = new PagamentoDespesaRecorrente
                        {
                            DespesaRecorrenteId = despesa.Id,
                            DataVencimento = dataVencimento,
                            ValorPrevisto = despesa.ValorPadrao,
                            Pago = false,
                            DataRegistro = DateTime.Now
                        };
                        
                        pagamentosNovos.Add(pagamento);
                        pagamentosGerados++;
                    }
                }
                
                if (pagamentosNovos.Any())
                {
                    await _context.PagamentosDespesasRecorrentes.AddRangeAsync(pagamentosNovos);
                    await _context.SaveChangesAsync();
                    
                    var user = await _userManager.GetUserAsync(User);
                    if (user != null)
                    {
                        await _auditService.LogAuditAsync(user.Id, "Gerar Pagamentos Automático", "DespesaRecorrente",
                            despesa.Id.ToString(), $"{pagamentosGerados} pagamentos gerados automaticamente para '{despesa.Nome}'.");
                    }
                    
                    return Json(new { 
                        success = true, 
                        message = $"{pagamentosGerados} pagamento(s) gerado(s) com sucesso!" 
                    });
                }
                
                return Json(new { success = false, message = "Nenhum pagamento novo precisou ser gerado." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao gerar pagamentos automáticos");
                return Json(new { success = false, message = "Erro ao gerar pagamentos automaticamente." });
            }
        }

        // ✅ NOVO: Gerar pagamentos para despesa recém-cadastrada (quantidade de meses)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GerarPagamentosParaDespesaNova(int despesaRecorrenteId, int meses)
        {
            try
            {
                if (meses <= 0 || meses > 24)
                {
                    return Json(new { success = false, message = "O número de meses deve estar entre 1 e 24." });
                }

                var despesa = await _context.DespesasRecorrentes
                    .Include(d => d.Pagamentos)
                    .FirstOrDefaultAsync(d => d.Id == despesaRecorrenteId);
                
                if (despesa == null)
                {
                    return Json(new { success = false, message = "Despesa recorrente não encontrada." });
                }
                
                if (!await CanAccessCentroCusto(despesa.CentroCustoId))
                {
                    return Json(new { success = false, message = "Você não tem permissão para acessar esta despesa." });
                }
                
                // Usar lógica do DespesasRecorrentesController
                DateTime dataBase = despesa.DataInicio ?? DateTime.Today;
                int indiceInicial = 0;
                
                if (despesa.Pagamentos.Any())
                {
                    var ultimoPagamento = despesa.Pagamentos.OrderByDescending(p => p.DataVencimento).First();
                    dataBase = ultimoPagamento.DataVencimento;
                    indiceInicial = 1;
                }
                
                // ✅ NOVO: Calcular quantidade de períodos baseado na periodicidade
                int quantidadePeriodos = CalcularQuantidadePeriodos(despesa.Periodicidade, meses);
                
                var pagamentosGerados = 0;
                var pagamentosNovos = new List<PagamentoDespesaRecorrente>();
                
                for (int i = indiceInicial; i < quantidadePeriodos + indiceInicial; i++)
                {
                    DateTime dataVencimento = CalcularProximoVencimento(despesa, dataBase, i);
                    
                    // Verificar se está dentro do período de término
                    if (despesa.DataTermino.HasValue && dataVencimento > despesa.DataTermino.Value)
                    {
                        break;
                    }
                    
                    // Verificar duplicação
                    var jaExiste = despesa.Pagamentos.Any(p => p.DataVencimento.Date == dataVencimento.Date);
                    
                    if (!jaExiste)
                    {
                        var pagamento = new PagamentoDespesaRecorrente
                        {
                            DespesaRecorrenteId = despesa.Id,
                            DataVencimento = dataVencimento,
                            ValorPrevisto = despesa.ValorPadrao,
                            Pago = false,
                            DataRegistro = DateTime.Now
                        };
                        
                        pagamentosNovos.Add(pagamento);
                        pagamentosGerados++;
                    }
                }
                
                if (pagamentosNovos.Any())
                {
                    await _context.PagamentosDespesasRecorrentes.AddRangeAsync(pagamentosNovos);
                    await _context.SaveChangesAsync();
                    
                    var user = await _userManager.GetUserAsync(User);
                    if (user != null)
                    {
                        await _auditService.LogAuditAsync(user.Id, "Gerar Pagamentos", "DespesaRecorrente",
                            despesa.Id.ToString(), $"{pagamentosGerados} pagamentos gerados para '{despesa.Nome}'.");
                    }
                    
                    return Json(new { 
                        success = true, 
                        message = $"{pagamentosGerados} pagamento(s) gerado(s)!" 
                    });
                }
                
                return Json(new { success = false, message = "Nenhum pagamento novo foi gerado." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao gerar pagamentos para despesa nova");
                return Json(new { success = false, message = "Erro ao gerar pagamentos." });
            }
        }

        // ✅ NOVO: Método auxiliar para calcular quantidade de períodos (copiado de DespesasRecorrentesController)
        private int CalcularQuantidadePeriodos(Periodicidade periodicidade, int meses)
        {
            return periodicidade switch
            {
                Periodicidade.Semanal => (int)Math.Ceiling(meses * 4.33), // ~4.33 semanas por mês
                Periodicidade.Quinzenal => meses * 2, // 2 quinzenas por mês
                Periodicidade.Mensal => meses, // 1:1
                Periodicidade.Bimestral => (int)Math.Ceiling(meses / 2.0), // A cada 2 meses
                Periodicidade.Trimestral => (int)Math.Ceiling(meses / 3.0), // A cada 3 meses
                Periodicidade.Semestral => (int)Math.Ceiling(meses / 6.0), // A cada 6 meses
                Periodicidade.Anual => (int)Math.Ceiling(meses / 12.0), // A cada 12 meses
                _ => meses // Fallback: usar como mensal
            };
        }

        // ✅ NOVO: Método auxiliar para calcular próximo vencimento (copiado de DespesasRecorrentesController)
        private DateTime CalcularProximoVencimento(DespesaRecorrente despesa, DateTime dataBase, int indice)
        {
            DateTime dataVencimento;

            if (despesa.DiaVencimento.HasValue)
            {
                int mes = dataBase.Month;
                int ano = dataBase.Year;
                int dia = despesa.DiaVencimento.Value;

                switch (despesa.Periodicidade)
                {
                    case Periodicidade.Semanal:
                        dataVencimento = dataBase.AddDays(7 * indice);
                        break;

                    case Periodicidade.Quinzenal:
                        dataVencimento = dataBase.AddDays(15 * indice);
                        break;

                    case Periodicidade.Mensal:
                        mes = dataBase.Month + indice;
                        ano = dataBase.Year;
                        while (mes > 12) { mes -= 12; ano++; }
                        int diasNoMes = DateTime.DaysInMonth(ano, mes);
                        dia = Math.Min(despesa.DiaVencimento.Value, diasNoMes);
                        dataVencimento = new DateTime(ano, mes, dia);
                        break;

                    case Periodicidade.Bimestral:
                        mes = dataBase.Month + (indice * 2);
                        ano = dataBase.Year;
                        while (mes > 12) { mes -= 12; ano++; }
                        diasNoMes = DateTime.DaysInMonth(ano, mes);
                        dia = Math.Min(despesa.DiaVencimento.Value, diasNoMes);
                        dataVencimento = new DateTime(ano, mes, dia);
                        break;

                    case Periodicidade.Trimestral:
                        mes = dataBase.Month + (indice * 3);
                        ano = dataBase.Year;
                        while (mes > 12) { mes -= 12; ano++; }
                        diasNoMes = DateTime.DaysInMonth(ano, mes);
                        dia = Math.Min(despesa.DiaVencimento.Value, diasNoMes);
                        dataVencimento = new DateTime(ano, mes, dia);
                        break;

                    case Periodicidade.Semestral:
                        mes = dataBase.Month + (indice * 6);
                        ano = dataBase.Year;
                        while (mes > 12) { mes -= 12; ano++; }
                        diasNoMes = DateTime.DaysInMonth(ano, mes);
                        dia = Math.Min(despesa.DiaVencimento.Value, diasNoMes);
                        dataVencimento = new DateTime(ano, mes, dia);
                        break;

                    case Periodicidade.Anual:
                        ano = dataBase.Year + indice;
                        mes = dataBase.Month;
                        if (mes == 2 && despesa.DiaVencimento.Value == 29 && !DateTime.IsLeapYear(ano))
                        {
                            dia = 28;
                        }
                        else
                        {
                            diasNoMes = DateTime.DaysInMonth(ano, mes);
                            dia = Math.Min(despesa.DiaVencimento.Value, diasNoMes);
                        }
                        dataVencimento = new DateTime(ano, mes, dia);
                        break;

                    default:
                        dataVencimento = dataBase.AddMonths(indice);
                        break;
                }
            }
            else
            {
                switch (despesa.Periodicidade)
                {
                    case Periodicidade.Semanal:
                        dataVencimento = dataBase.AddDays(7 * indice);
                        break;
                    case Periodicidade.Quinzenal:
                        dataVencimento = dataBase.AddDays(15 * indice);
                        break;
                    case Periodicidade.Mensal:
                        dataVencimento = dataBase.AddMonths(indice);
                        break;
                    case Periodicidade.Bimestral:
                        dataVencimento = dataBase.AddMonths(2 * indice);
                        break;
                    case Periodicidade.Trimestral:
                        dataVencimento = dataBase.AddMonths(3 * indice);
                        break;
                    case Periodicidade.Semestral:
                        dataVencimento = dataBase.AddMonths(6 * indice);
                        break;
                    case Periodicidade.Anual:
                        dataVencimento = dataBase.AddYears(indice);
                        break;
                    default:
                        dataVencimento = dataBase.AddMonths(indice);
                        break;
                }
            }

            return dataVencimento;
        }

        #region Métodos Auxiliares

        /// <summary>
        /// ✅ NOVO: Limpa fechamentos PENDENTES que incluíram a saída excluída.
        /// Remove DetalheFechamento correspondente e recalcula totais do fechamento.
        /// </summary>
        private async Task LimparFechamentosPendentesAposExclusaoSaida(Saida saida)
        {
            // Buscar fechamentos PENDENTES que incluíram esta saída
            var fechamentosPendentes = await _context.FechamentosPeriodo
                .Include(f => f.DetalhesFechamento)
                .Include(f => f.ItensRateio)
                .Include(f => f.CentroCusto)
                .Where(f => f.Id == saida.FechamentoQueIncluiuId.Value &&
                           f.Status == StatusFechamentoPeriodo.Pendente)
                .ToListAsync();

            foreach (var fechamento in fechamentosPendentes)
            {
                _logger.LogInformation($"Limpando saída ID {saida.Id} do fechamento pendente ID {fechamento.Id}");

                // Remover DetalheFechamento correspondente (buscar por Data, Valor e TipoMovimento)
                var detalhesParaRemover = fechamento.DetalhesFechamento
                    .Where(d => d.TipoMovimento == "Saida" &&
                               d.Data.Date == saida.Data.Date &&
                               d.Valor == saida.Valor &&
                               (d.Descricao == saida.Descricao ||
                                (string.IsNullOrEmpty(d.Descricao) && string.IsNullOrEmpty(saida.Descricao))))
                    .ToList();

                if (detalhesParaRemover.Any())
                {
                    _context.DetalhesFechamento.RemoveRange(detalhesParaRemover);
                    _logger.LogInformation($"Removidos {detalhesParaRemover.Count} detalhes do fechamento ID {fechamento.Id}");

                    // Recalcular totais do fechamento
                    await RecalcularTotaisFechamentoPendente(fechamento, saida.MeioDePagamento.TipoCaixa, saida.Valor, isEntrada: false);

                    // Registrar auditoria
                    await _auditService.LogAsync("Exclusão", "DetalheFechamento",
                        $"Saída ID {saida.Id} (R$ {saida.Valor:N2}) removida do fechamento pendente ID {fechamento.Id} devido à exclusão do lançamento.");
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

        private async Task PopulateDropdowns(Saida? saida = null)
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

            ViewData["CentroCustoId"] = new SelectList(await centrosCustoQuery.ToListAsync(), "Id", "Nome", saida?.CentroCustoId);

            ViewData["PlanoDeContasId"] = new SelectList(
                await _context.PlanosDeContas.Where(p => p.Tipo == TipoPlanoContas.Despesa && p.Ativo).ToListAsync(),
                "Id", "Nome", saida?.PlanoDeContasId);

            ViewData["MeioDePagamentoId"] = new SelectList(
                await _context.MeiosDePagamento.Where(m => m.Ativo).ToListAsync(),
                "Id", "Nome", saida?.MeioDePagamentoId);

            ViewData["FornecedorId"] = new SelectList(
                await _context.Fornecedores.Where(f => f.Ativo).ToListAsync(),
                "Id", "Nome", saida?.FornecedorId);
        }

        // ✅ NOVO: Popular dropdown de despesas recorrentes
        private async Task PopulateDespesasRecorrentes(int? centroCustoId = null)
        {
            var user = await _userManager.GetUserAsync(User);
            
            var query = _context.DespesasRecorrentes
                .Include(d => d.CentroCusto)
                .Include(d => d.PlanoDeContas)
                .Where(d => d.Ativa);
            
            // Filtrar por permissão
            if (!User.IsInRole(Roles.Administrador) && !User.IsInRole(Roles.TesoureiroGeral))
            {
                if (user.CentroCustoId.HasValue)
                {
                    query = query.Where(d => d.CentroCustoId == user.CentroCustoId.Value);
                }
            }
            
            // Filtrar por centro de custo selecionado
            if (centroCustoId.HasValue)
            {
                query = query.Where(d => d.CentroCustoId == centroCustoId.Value);
            }
            
            var despesas = await query
                .OrderBy(d => d.Nome)
                .Select(d => new {
                    d.Id,
                    Nome = $"{d.Nome} - {d.CentroCusto.Nome} ({d.ValorPadrao:C2})"
                })
                .ToListAsync();
            
            ViewBag.DespesasRecorrentes = new SelectList(despesas, "Id", "Nome");
        }

        private async Task<bool> CanAccessSaida(Saida saida)
        {
            // Administradores e Tesoureiros Gerais podem acessar tudo
            if (User.IsInRole(Roles.Administrador) || User.IsInRole(Roles.TesoureiroGeral))
                return true;

            var user = await _userManager.GetUserAsync(User);

            // Outros usuários só podem acessar saídas do seu centro de custo
            return user.CentroCustoId.HasValue && saida.CentroCustoId == user.CentroCustoId.Value;
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

        private bool SaidaExists(int id)
        {
            return _context.Saidas.Any(e => e.Id == id);
        }

        #endregion
    }
}