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
    public class DespesasRecorrentesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AuditService _auditService;

        public DespesasRecorrentesController(
    ApplicationDbContext context,
UserManager<ApplicationUser> userManager,
     AuditService auditService)
        {
            _context = context;
            _userManager = userManager;
            _auditService = auditService;
        }

        // GET: DespesasRecorrentes
        public async Task<IActionResult> Index(int? centroCustoId, bool? ativa, Periodicidade? periodicidade)
        {
            ViewData["CentroCustoId"] = centroCustoId;
            ViewData["Ativa"] = ativa;
            ViewData["Periodicidade"] = periodicidade;

            var user = await _userManager.GetUserAsync(User);

            var despesas = _context.DespesasRecorrentes
            .Include(d => d.CentroCusto)
            .Include(d => d.PlanoDeContas)
            .Include(d => d.Fornecedor)
            .Include(d => d.MeioDePagamento)
            .Include(d => d.Pagamentos)
            .AsQueryable();

            // Filtro por permissão
            if (!User.IsInRole(Roles.Administrador) && !User.IsInRole(Roles.TesoureiroGeral))
            {
                if (user.CentroCustoId.HasValue)
                {
                    despesas = despesas.Where(d => d.CentroCustoId == user.CentroCustoId.Value);
                }
                else
                {
                    despesas = despesas.Where(d => false);
                }
            }

            // Filtros de pesquisa
            if (centroCustoId.HasValue)
            {
                despesas = despesas.Where(d => d.CentroCustoId == centroCustoId);
            }

            if (ativa.HasValue)
            {
                despesas = despesas.Where(d => d.Ativa == ativa);
            }

            if (periodicidade.HasValue)
            {
                despesas = despesas.Where(d => d.Periodicidade == periodicidade);
            }

            // Dropdown para filtros
            var centrosCustoQuery = _context.CentrosCusto.Where(c => c.Ativo);
            if (!User.IsInRole(Roles.Administrador) && !User.IsInRole(Roles.TesoureiroGeral))
            {
                if (user.CentroCustoId.HasValue)
                {
                    centrosCustoQuery = centrosCustoQuery.Where(c => c.Id == user.CentroCustoId.Value);
                }
            }

            ViewBag.CentrosCusto = new SelectList(await centrosCustoQuery.ToListAsync(), "Id", "Nome", centroCustoId);

            return View(await despesas.OrderBy(d => d.Nome).ToListAsync());
        }

        // GET: DespesasRecorrentes/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var despesa = await _context.DespesasRecorrentes
            .Include(d => d.CentroCusto)
            .Include(d => d.PlanoDeContas)
            .Include(d => d.Fornecedor)
            .Include(d => d.MeioDePagamento)
            .Include(d => d.Pagamentos.OrderByDescending(p => p.DataVencimento))
            .FirstOrDefaultAsync(m => m.Id == id);

            if (despesa == null)
            {
                return NotFound();
            }

            if (!await CanAccessDespesa(despesa))
            {
                return Forbid();
            }

            return View(despesa);
        }

        // GET: DespesasRecorrentes/Create
        public async Task<IActionResult> Create()
        {
            await PopulateDropdowns();
            var user = await _userManager.GetUserAsync(User);

            var despesa = new DespesaRecorrente
            {
                CentroCustoId = user.CentroCustoId ?? 0,
                DataInicio = DateTime.Today,
                Ativa = true
            };

            return View(despesa);
        }

        // POST: DespesasRecorrentes/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Nome,Descricao,ValorPadrao,Periodicidade,CentroCustoId,PlanoDeContasId,FornecedorId,MeioDePagamentoId,DiaVencimento,DataInicio,DataTermino,Observacoes,Ativa")] DespesaRecorrente despesa)
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
                    ModelState.AddModelError("CentroCustoId", "Você não tem permissão para criar despesas neste centro de custo.");
                }

                if (ModelState.IsValid)
                {
                    despesa.DataCadastro = DateTime.Now;
                    _context.Add(despesa);
                    await _context.SaveChangesAsync();

                    var user = await _userManager.GetUserAsync(User);
                    if (user != null)
                    {
                        await _auditService.LogCreateAsync(user.Id, despesa, despesa.Id.ToString());
                    }

                    TempData["SuccessMessage"] = "Despesa recorrente cadastrada com sucesso!";
                    return RedirectToAction(nameof(Index));
                }

                await PopulateDropdowns(despesa);
                return View(despesa);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Erro interno ao salvar despesa. Tente novamente.");
                await PopulateDropdowns(despesa);
                return View(despesa);
            }
        }

        // GET: DespesasRecorrentes/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var despesa = await _context.DespesasRecorrentes.FindAsync(id);
            if (despesa == null)
            {
                return NotFound();
            }

            if (!await CanAccessDespesa(despesa))
            {
                return Forbid();
            }

            await PopulateDropdowns(despesa);
            return View(despesa);
        }

        // POST: DespesasRecorrentes/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Nome,Descricao,ValorPadrao,Periodicidade,CentroCustoId,PlanoDeContasId,FornecedorId,MeioDePagamentoId,DiaVencimento,DataInicio,DataTermino,Observacoes,Ativa")] DespesaRecorrente despesa)
        {
            if (id != despesa.Id)
            {
                return NotFound();
            }

            try
            {
                ModelState.Remove("CentroCusto");
                ModelState.Remove("PlanoDeContas");
                ModelState.Remove("Fornecedor");
                ModelState.Remove("MeioDePagamento");
                ModelState.Remove("Pagamentos");
                ModelState.Remove("DataCadastro");

                var original = await _context.DespesasRecorrentes.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id);
                if (original == null) return NotFound();

                if (!await CanAccessDespesa(original))
                {
                    return Forbid();
                }

                if (!await CanAccessCentroCusto(despesa.CentroCustoId))
                {
                    ModelState.AddModelError("CentroCustoId", "Você não tem permissão para mover despesas para este centro de custo.");
                }

                if (ModelState.IsValid)
                {
                    despesa.DataCadastro = original.DataCadastro;
                    _context.Update(despesa);
                    await _context.SaveChangesAsync();

                    var user = await _userManager.GetUserAsync(User);
                    if (user != null)
                    {
                        await _auditService.LogUpdateAsync(user.Id, original, despesa, despesa.Id.ToString());
                    }

                    TempData["SuccessMessage"] = "Despesa recorrente atualizada com sucesso!";
                    return RedirectToAction(nameof(Index));
                }

                await PopulateDropdowns(despesa);
                return View(despesa);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!DespesaExists(despesa.Id))
                {
                    return NotFound();
                }
                throw;
            }
        }

        // GET: DespesasRecorrentes/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var despesa = await _context.DespesasRecorrentes
            .Include(d => d.CentroCusto)
            .Include(d => d.PlanoDeContas)
            .Include(d => d.Fornecedor)
            .Include(d => d.MeioDePagamento)
            .FirstOrDefaultAsync(m => m.Id == id);

            if (despesa == null)
            {
                return NotFound();
            }

            if (!await CanAccessDespesa(despesa))
            {
                return Forbid();
            }

            return View(despesa);
        }

        // POST: DespesasRecorrentes/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var despesa = await _context.DespesasRecorrentes.FindAsync(id);
            if (despesa != null)
            {
                if (!await CanAccessDespesa(despesa))
                {
                    return Forbid();
                }

                _context.DespesasRecorrentes.Remove(despesa);
                await _context.SaveChangesAsync();

                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    await _auditService.LogAuditAsync(user.Id, "Excluir", "DespesaRecorrente", despesa.Id.ToString(), $"Despesa '{despesa.Nome}' excluída.");
                }

                TempData["SuccessMessage"] = "Despesa recorrente excluída com sucesso!";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: DespesasRecorrentes/GerarPagamentos/5
        public async Task<IActionResult> GerarPagamentos(int? id, int meses = 3)
        {
            if (id == null)
            {
                return NotFound();
            }

            var despesa = await _context.DespesasRecorrentes
            .Include(d => d.Pagamentos)
            .FirstOrDefaultAsync(d => d.Id == id);

            if (despesa == null)
            {
                return NotFound();
            }

            if (!await CanAccessDespesa(despesa))
            {
                return Forbid();
            }

            ViewBag.Despesa = despesa;
            ViewBag.MesesGerar = meses;

            return View();
        }

        // POST: DespesasRecorrentes/GerarPagamentos/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GerarPagamentosConfirmed(int id, int meses)
        {
            if (meses <= 0 || meses > 24)
            {
                TempData["ErrorMessage"] = "O número de meses deve estar entre 1 e 24.";
                return RedirectToAction(nameof(GerarPagamentos), new { id });
            }

            var despesa = await _context.DespesasRecorrentes
           .Include(d => d.Pagamentos)
             .FirstOrDefaultAsync(d => d.Id == id);

            if (despesa == null)
            {
                return NotFound();
            }

            if (!await CanAccessDespesa(despesa))
            {
                return Forbid();
            }

            if (!despesa.Ativa)
            {
                TempData["ErrorMessage"] = "Não é possível gerar pagamentos para uma despesa inativa.";
                return RedirectToAction(nameof(Details), new { id });
            }

            // Determinar a data base para gerar os novos pagamentos
            DateTime dataBase;
            int indiceInicial = 0;

            if (despesa.Pagamentos.Any())
            {
                // Se já existem pagamentos, buscar o último vencimento e calcular o próximo
                var ultimoPagamento = despesa.Pagamentos.OrderByDescending(p => p.DataVencimento).First();
                dataBase = ultimoPagamento.DataVencimento;

                // Começar do próximo período após o último pagamento
                indiceInicial = 1;
            }
            else
            {
                // Se não existem pagamentos, começar da data de início
                dataBase = despesa.DataInicio ?? DateTime.Today;
            }

            var pagamentosGerados = 0;
            var pagamentosJaExistentes = 0;
            var pagamentosNovos = new List<PagamentoDespesaRecorrente>();

            for (int i = indiceInicial; i < meses + indiceInicial; i++)
            {
                DateTime dataVencimento = CalcularProximoVencimento(despesa, dataBase, i);

                // Verificar se está dentro do período de término (se especificado)
                if (despesa.DataTermino.HasValue && dataVencimento > despesa.DataTermino.Value)
                {
                    break; // Não gerar pagamentos além da data de término
                }

                // Verificar se já existe pagamento para esta data exata
                var jaExiste = despesa.Pagamentos.Any(p =>
                    p.DataVencimento.Date == dataVencimento.Date);

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
                else
                {
                    pagamentosJaExistentes++;
                }
            }

            // Adicionar todos os pagamentos de uma vez
            if (pagamentosNovos.Any())
            {
                await _context.PagamentosDespesasRecorrentes.AddRangeAsync(pagamentosNovos);
                await _context.SaveChangesAsync();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                await _auditService.LogAuditAsync(user.Id, "Gerar Pagamentos", "DespesaRecorrente",
              despesa.Id.ToString(),
                  $"{pagamentosGerados} pagamentos gerados para '{despesa.Nome}'. {pagamentosJaExistentes} já existentes.");
            }

            if (pagamentosGerados > 0)
            {
                TempData["SuccessMessage"] = $"{pagamentosGerados} pagamento(s) gerado(s) com sucesso!";
                if (pagamentosJaExistentes > 0)
                {
                    TempData["InfoMessage"] = $"{pagamentosJaExistentes} pagamento(s) já existia(m) e foram ignorados.";
                }
            }
            else
            {
                TempData["InfoMessage"] = "Nenhum pagamento novo foi gerado. Todos os pagamentos já existem.";
            }

            return RedirectToAction(nameof(Pagamentos), new { id = despesa.Id });
        }

        // GET: DespesasRecorrentes/Pagamentos/5
        public async Task<IActionResult> Pagamentos(int? id, DateTime? mes)
        {
            if (id == null)
            {
                return NotFound();
            }

            var despesa = await _context.DespesasRecorrentes
            .Include(d => d.CentroCusto)
            .Include(d => d.PlanoDeContas)
            .Include(d => d.Pagamentos)
            .FirstOrDefaultAsync(d => d.Id == id);

            if (despesa == null)
            {
                return NotFound();
            }

            if (!await CanAccessDespesa(despesa))
            {
                return Forbid();
            }

            var pagamentos = despesa.Pagamentos
            .OrderBy(p => p.DataVencimento)
            .AsQueryable();

            if (mes.HasValue)
            {
                pagamentos = pagamentos.Where(p =>
                p.DataVencimento.Year == mes.Value.Year &&
                p.DataVencimento.Month == mes.Value.Month);
            }

            ViewBag.Despesa = despesa;
            ViewBag.MesFiltro = mes;

            return View(pagamentos.ToList());
        }

        // POST: DespesasRecorrentes/MarcarPago
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarcarPago(int pagamentoId, [FromForm] string? valorPago)
        {
            var pagamento = await _context.PagamentosDespesasRecorrentes
            .Include(p => p.DespesaRecorrente)
                .ThenInclude(d => d.PlanoDeContas)
                    .Include(p => p.DespesaRecorrente.MeioDePagamento)
                   .Include(p => p.DespesaRecorrente.Fornecedor)
                         .FirstOrDefaultAsync(p => p.Id == pagamentoId);

            if (pagamento == null)
            {
                return NotFound();
            }

            if (!await CanAccessDespesa(pagamento.DespesaRecorrente))
            {
                return Forbid();
            }

            if (pagamento.Pago)
            {
                TempData["InfoMessage"] = "Este pagamento já está marcado como pago.";
                return RedirectToAction(nameof(Pagamentos), new { id = pagamento.DespesaRecorrenteId });
            }

            if (pagamento.SaidaGerada)
            {
                TempData["ErrorMessage"] = "Não é possível marcar como pago um pagamento que já gerou uma saída. Use a função 'Gerar Saída' ou desmarque a saída primeiro.";
                return RedirectToAction(nameof(Pagamentos), new { id = pagamento.DespesaRecorrenteId });
            }

            // Converter o valor pago usando cultura invariante (ponto como separador decimal)
            decimal valorFinal;
            if (!string.IsNullOrWhiteSpace(valorPago))
            {
                // Tentar converter usando cultura invariante (formato americano com ponto)
                if (!decimal.TryParse(valorPago, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out valorFinal))
                {
                    TempData["ErrorMessage"] = $"Valor inválido: '{valorPago}'. Use o formato 100.00";
                    return RedirectToAction(nameof(Pagamentos), new { id = pagamento.DespesaRecorrenteId });
                }
            }
            else
            {
                valorFinal = pagamento.ValorPrevisto;
            }

            if (valorFinal <= 0)
            {
                TempData["ErrorMessage"] = "O valor pago deve ser maior que zero.";
                return RedirectToAction(nameof(Pagamentos), new { id = pagamento.DespesaRecorrenteId });
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Forbid();
            }

            var despesa = pagamento.DespesaRecorrente;

            // Validar se há meio de pagamento configurado
            if (!despesa.MeioDePagamentoId.HasValue)
            {
                TempData["ErrorMessage"] = "A despesa recorrente não possui meio de pagamento configurado. Configure antes de marcar como pago.";
                return RedirectToAction(nameof(Edit), new { id = despesa.Id });
            }

            // Verificar se o meio de pagamento está ativo
            var meioPagamento = await _context.MeiosDePagamento.FindAsync(despesa.MeioDePagamentoId.Value);
            if (meioPagamento == null || !meioPagamento.Ativo)
            {
                TempData["ErrorMessage"] = "O meio de pagamento configurado está inativo. Atualize a despesa recorrente.";
                return RedirectToAction(nameof(Edit), new { id = despesa.Id });
            }

            // Usar a data atual como data de pagamento
            var dataPagamento = DateTime.Now;

            // Criar a saída automaticamente
            var saida = new Saida
            {
                Data = dataPagamento,
                Valor = valorFinal,
                Descricao = $"{despesa.Nome} - {pagamento.DataVencimento:MM/yyyy}",
                MeioDePagamentoId = despesa.MeioDePagamentoId.Value,
                CentroCustoId = despesa.CentroCustoId,
                PlanoDeContasId = despesa.PlanoDeContasId,
                FornecedorId = despesa.FornecedorId,
                TipoDespesa = TipoDespesa.Fixa,
                DataVencimento = pagamento.DataVencimento,
                Observacoes = $"Gerada automaticamente a partir da despesa recorrente: {despesa.Nome}",
                UsuarioId = user.Id,
                DataCriacao = DateTime.Now
            };

            _context.Saidas.Add(saida);
            await _context.SaveChangesAsync();

            // Atualizar o pagamento
            pagamento.Pago = true;
            pagamento.DataPagamento = dataPagamento;
            pagamento.ValorPago = valorFinal;
            pagamento.SaidaGerada = true;
            pagamento.SaidaId = saida.Id;

            await _context.SaveChangesAsync();

            await _auditService.LogAuditAsync(user.Id, "Marcar Pago", "PagamentoDespesaRecorrente",
              pagamento.Id.ToString(), $"Pagamento de {pagamento.ValorPago:C2} marcado como pago em {pagamento.DataPagamento:dd/MM/yyyy} e saída #{saida.Id} gerada automaticamente.");

            TempData["SuccessMessage"] = $"Pagamento marcado como pago e saída de {saida.Valor:C2} gerada automaticamente!";
            return RedirectToAction(nameof(Pagamentos), new { id = pagamento.DespesaRecorrenteId });
        }

        // POST: DespesasRecorrentes/DesmarcarPago
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DesmarcarPago(int pagamentoId)
        {
            var pagamento = await _context.PagamentosDespesasRecorrentes
          .Include(p => p.DespesaRecorrente)
        .Include(p => p.Saida)
      .FirstOrDefaultAsync(p => p.Id == pagamentoId);

            if (pagamento == null)
            {
                return NotFound();
            }

            if (!await CanAccessDespesa(pagamento.DespesaRecorrente))
            {
                return Forbid();
            }

            if (!pagamento.Pago)
            {
                TempData["InfoMessage"] = "Este pagamento já está desmarcado.";
                return RedirectToAction(nameof(Pagamentos), new { id = pagamento.DespesaRecorrenteId });
            }

            // Se há saída gerada, excluí-la automaticamente
            if (pagamento.SaidaGerada && pagamento.SaidaId.HasValue)
            {
                var saida = await _context.Saidas.FindAsync(pagamento.SaidaId.Value);
                if (saida != null)
                {
                    // Excluir a saída
                    _context.Saidas.Remove(saida);

                    // Registrar auditoria da exclusão da saída
                    var user = await _userManager.GetUserAsync(User);
                    if (user != null)
                    {
                        await _auditService.LogAuditAsync(user.Id, "Excluir Saída (Desmarcar Pago)", "Saida",
                             saida.Id.ToString(), $"Saída de {saida.Valor:C2} excluída automaticamente ao desmarcar pagamento #{pagamentoId}.");
                    }
                }
            }

            // Desmarcar o pagamento
            pagamento.Pago = false;
            pagamento.DataPagamento = null;
            pagamento.ValorPago = null;
            pagamento.SaidaGerada = false;
            pagamento.SaidaId = null;

            await _context.SaveChangesAsync();

            var userAudit = await _userManager.GetUserAsync(User);
            if (userAudit != null)
            {
                await _auditService.LogAuditAsync(userAudit.Id, "Desmarcar Pago", "PagamentoDespesaRecorrente",
                        pagamento.Id.ToString(), "Pagamento desmarcado como não pago e saída automaticamente excluída.");
            }

            TempData["SuccessMessage"] = "Pagamento desmarcado com sucesso! A saída vinculada foi excluída automaticamente.";
            return RedirectToAction(nameof(Pagamentos), new { id = pagamento.DespesaRecorrenteId });
        }

        // POST: DespesasRecorrentes/GerarSaida
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GerarSaida(int pagamentoId)
        {
            var pagamento = await _context.PagamentosDespesasRecorrentes
        .Include(p => p.DespesaRecorrente)
          .ThenInclude(d => d.PlanoDeContas)
      .Include(p => p.DespesaRecorrente.MeioDePagamento)
            .Include(p => p.DespesaRecorrente.Fornecedor)
      .FirstOrDefaultAsync(p => p.Id == pagamentoId);

            if (pagamento == null)
            {
                return NotFound();
            }

            if (!await CanAccessDespesa(pagamento.DespesaRecorrente))
            {
                return Forbid();
            }

            if (pagamento.SaidaGerada)
            {
                TempData["ErrorMessage"] = "Já existe uma saída gerada para este pagamento!";
                return RedirectToAction(nameof(Pagamentos), new { id = pagamento.DespesaRecorrenteId });
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Forbid();
            }

            var despesa = pagamento.DespesaRecorrente;

            // Validar se há meio de pagamento configurado
            if (!despesa.MeioDePagamentoId.HasValue)
            {
                TempData["ErrorMessage"] = "A despesa recorrente não possui meio de pagamento configurado. Configure antes de gerar a saída.";
                return RedirectToAction(nameof(Edit), new { id = despesa.Id });
            }

            // Verificar se o meio de pagamento está ativo
            var meioPagamento = await _context.MeiosDePagamento.FindAsync(despesa.MeioDePagamentoId.Value);
            if (meioPagamento == null || !meioPagamento.Ativo)
            {
                TempData["ErrorMessage"] = "O meio de pagamento configurado está inativo. Atualize a despesa recorrente.";
                return RedirectToAction(nameof(Edit), new { id = despesa.Id });
            }

            // Usar a data de pagamento se já foi pago, senão usar hoje
            var dataSaida = pagamento.DataPagamento ?? DateTime.Now;
            var valorSaida = pagamento.ValorPago ?? pagamento.ValorPrevisto;

            var saida = new Saida
            {
                Data = dataSaida,
                Valor = valorSaida,
                Descricao = $"{despesa.Nome} - {pagamento.DataVencimento:MM/yyyy}",
                MeioDePagamentoId = despesa.MeioDePagamentoId.Value,
                CentroCustoId = despesa.CentroCustoId,
                PlanoDeContasId = despesa.PlanoDeContasId,
                FornecedorId = despesa.FornecedorId,
                TipoDespesa = TipoDespesa.Fixa,
                DataVencimento = pagamento.DataVencimento,
                Observacoes = $"Gerada automaticamente a partir da despesa recorrente: {despesa.Nome}",
                UsuarioId = user.Id,
                DataCriacao = DateTime.Now
            };

            _context.Saidas.Add(saida);
            await _context.SaveChangesAsync();

            // Atualizar o pagamento
            pagamento.SaidaGerada = true;
            pagamento.SaidaId = saida.Id;
            pagamento.Pago = true;
            pagamento.DataPagamento = dataSaida;
            pagamento.ValorPago = valorSaida;

            await _context.SaveChangesAsync();

            await _auditService.LogAuditAsync(user.Id, "Gerar Saída", "PagamentoDespesaRecorrente",
       pagamento.Id.ToString(), $"Saída #{saida.Id} gerada automaticamente no valor de {saida.Valor:C2}.");

            TempData["SuccessMessage"] = $"Saída de {saida.Valor:C2} gerada com sucesso!";
            return RedirectToAction(nameof(Pagamentos), new { id = pagamento.DespesaRecorrenteId });
        }

        #region Métodos Auxiliares

        private async Task PopulateDropdowns(DespesaRecorrente? despesa = null)
        {
            var user = await _userManager.GetUserAsync(User);

            var centrosCustoQuery = _context.CentrosCusto.Where(c => c.Ativo);
            if (!User.IsInRole(Roles.Administrador) && !User.IsInRole(Roles.TesoureiroGeral))
            {
                if (user.CentroCustoId.HasValue)
                {
                    centrosCustoQuery = centrosCustoQuery.Where(c => c.Id == user.CentroCustoId.Value);
                }
            }

            ViewData["CentroCustoId"] = new SelectList(await centrosCustoQuery.ToListAsync(), "Id", "Nome", despesa?.CentroCustoId);

            ViewData["PlanoDeContasId"] = new SelectList(
             await _context.PlanosDeContas.Where(p => p.Tipo == TipoPlanoContas.Despesa && p.Ativo).ToListAsync(),
                "Id", "Nome", despesa?.PlanoDeContasId);

            ViewData["MeioDePagamentoId"] = new SelectList(
           await _context.MeiosDePagamento.Where(m => m.Ativo).ToListAsync(),
                "Id", "Nome", despesa?.MeioDePagamentoId);

            ViewData["FornecedorId"] = new SelectList(
            await _context.Fornecedores.Where(f => f.Ativo).ToListAsync(),
              "Id", "Nome", despesa?.FornecedorId);
        }

        private async Task<bool> CanAccessDespesa(DespesaRecorrente despesa)
        {
            if (User.IsInRole(Roles.Administrador) || User.IsInRole(Roles.TesoureiroGeral))
                return true;

            var user = await _userManager.GetUserAsync(User);
            return user.CentroCustoId.HasValue && despesa.CentroCustoId == user.CentroCustoId.Value;
        }

        private async Task<bool> CanAccessCentroCusto(int centroCustoId)
        {
            if (User.IsInRole(Roles.Administrador) || User.IsInRole(Roles.TesoureiroGeral))
                return true;

            var user = await _userManager.GetUserAsync(User);
            return user.CentroCustoId.HasValue && centroCustoId == user.CentroCustoId.Value;
        }

        private bool DespesaExists(int id)
        {
            return _context.DespesasRecorrentes.Any(e => e.Id == id);
        }

        private DateTime CalcularProximoVencimento(DespesaRecorrente despesa, DateTime dataBase, int indice)
        {
            DateTime dataVencimento;

            // Se há dia de vencimento especificado, usar ele como referência
            if (despesa.DiaVencimento.HasValue)
            {
                // Começar do mês da data base
                int mes = dataBase.Month;
                int ano = dataBase.Year;
                int dia = despesa.DiaVencimento.Value;

                switch (despesa.Periodicidade)
                {
                    case Periodicidade.Semanal:
                        // Para semanal, ignorar DiaVencimento e usar apenas incremento de dias
                        dataVencimento = dataBase.AddDays(7 * indice);
                        break;

                    case Periodicidade.Quinzenal:
                        // Para quinzenal, ignorar DiaVencimento e usar apenas incremento de dias
                        dataVencimento = dataBase.AddDays(15 * indice);
                        break;

                    case Periodicidade.Mensal:
                        // Adicionar meses e ajustar o dia
                        mes = dataBase.Month + indice;
                        ano = dataBase.Year;

                        // Ajustar ano se necessário
                        while (mes > 12)
                        {
                            mes -= 12;
                            ano++;
                        }

                        // Ajustar dia para o máximo de dias do mês
                        int diasNoMes = DateTime.DaysInMonth(ano, mes);
                        dia = Math.Min(despesa.DiaVencimento.Value, diasNoMes);

                        dataVencimento = new DateTime(ano, mes, dia);
                        break;

                    case Periodicidade.Bimestral:
                        // Adicionar 2 meses por iteração
                        mes = dataBase.Month + (indice * 2);
                        ano = dataBase.Year;

                        while (mes > 12)
                        {
                            mes -= 12;
                            ano++;
                        }

                        diasNoMes = DateTime.DaysInMonth(ano, mes);
                        dia = Math.Min(despesa.DiaVencimento.Value, diasNoMes);

                        dataVencimento = new DateTime(ano, mes, dia);
                        break;

                    case Periodicidade.Trimestral:
                        // Adicionar 3 meses por iteração
                        mes = dataBase.Month + (indice * 3);
                        ano = dataBase.Year;

                        while (mes > 12)
                        {
                            mes -= 12;
                            ano++;
                        }

                        diasNoMes = DateTime.DaysInMonth(ano, mes);
                        dia = Math.Min(despesa.DiaVencimento.Value, diasNoMes);

                        dataVencimento = new DateTime(ano, mes, dia);
                        break;

                    case Periodicidade.Semestral:
                        // Adicionar 6 meses por iteração
                        mes = dataBase.Month + (indice * 6);
                        ano = dataBase.Year;

                        while (mes > 12)
                        {
                            mes -= 12;
                            ano++;
                        }

                        diasNoMes = DateTime.DaysInMonth(ano, mes);
                        dia = Math.Min(despesa.DiaVencimento.Value, diasNoMes);

                        dataVencimento = new DateTime(ano, mes, dia);
                        break;

                    case Periodicidade.Anual:
                        ano = dataBase.Year + indice;
                        mes = dataBase.Month;

                        // Para anual, verificar se é 29 de fevereiro em ano não bissexto
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
                // Se não há dia de vencimento especificado, usar incremento simples
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
        #endregion
    }
}
