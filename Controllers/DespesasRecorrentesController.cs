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

            var dataInicio = despesa.DataInicio ?? DateTime.Today;
            var pagamentosGerados = 0;

            for (int i = 0; i < meses; i++)
            {
                DateTime dataVencimento = CalcularProximoVencimento(despesa, dataInicio, i);

                // Verificar se já existe pagamento para esta data
                var jaExiste = despesa.Pagamentos.Any(p =>
                  p.DataVencimento.Year == dataVencimento.Year &&
                p.DataVencimento.Month == dataVencimento.Month &&
                    p.DataVencimento.Day == dataVencimento.Day);

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

                    _context.PagamentosDespesasRecorrentes.Add(pagamento);
                    pagamentosGerados++;
                }
            }

            await _context.SaveChangesAsync();

            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                await _auditService.LogAuditAsync(user.Id, "Gerar Pagamentos", "DespesaRecorrente",
                   despesa.Id.ToString(), $"{pagamentosGerados} pagamentos gerados para '{despesa.Nome}'.");
            }

            TempData["SuccessMessage"] = $"{pagamentosGerados} pagamento(s) gerado(s) com sucesso!";
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
        public async Task<IActionResult> MarcarPago(int pagamentoId, decimal? valorPago)
        {
            var pagamento = await _context.PagamentosDespesasRecorrentes
            .Include(p => p.DespesaRecorrente)
                .FirstOrDefaultAsync(p => p.Id == pagamentoId);

            if (pagamento == null)
            {
                return NotFound();
            }

            if (!await CanAccessDespesa(pagamento.DespesaRecorrente))
            {
                return Forbid();
            }

            pagamento.Pago = true;
            pagamento.DataPagamento = DateTime.Now;
            pagamento.ValorPago = valorPago ?? pagamento.ValorPrevisto;

            await _context.SaveChangesAsync();

            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                await _auditService.LogAuditAsync(user.Id, "Marcar Pago", "PagamentoDespesaRecorrente",
                   pagamento.Id.ToString(), $"Pagamento de {pagamento.ValorPago:C2} marcado como pago.");
            }

            TempData["SuccessMessage"] = "Pagamento marcado como pago com sucesso!";
            return RedirectToAction(nameof(Pagamentos), new { id = pagamento.DespesaRecorrenteId });
        }

        // POST: DespesasRecorrentes/DesmarcarPago
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DesmarcarPago(int pagamentoId)
        {
            var pagamento = await _context.PagamentosDespesasRecorrentes
               .Include(p => p.DespesaRecorrente)
           .FirstOrDefaultAsync(p => p.Id == pagamentoId);

            if (pagamento == null)
            {
                return NotFound();
            }

            if (!await CanAccessDespesa(pagamento.DespesaRecorrente))
            {
                return Forbid();
            }

            pagamento.Pago = false;
            pagamento.DataPagamento = null;
            pagamento.ValorPago = null;

            await _context.SaveChangesAsync();

            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                await _auditService.LogAuditAsync(user.Id, "Desmarcar Pago", "PagamentoDespesaRecorrente",
              pagamento.Id.ToString(), "Pagamento desmarcado.");
            }

            TempData["SuccessMessage"] = "Pagamento desmarcado com sucesso!";
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

            var saida = new Saida
            {
                Data = pagamento.DataPagamento ?? DateTime.Now,
                Valor = pagamento.ValorPago ?? pagamento.ValorPrevisto,
                Descricao = $"{despesa.Nome} - {pagamento.DataVencimento:MM/yyyy}",
                MeioDePagamentoId = despesa.MeioDePagamentoId ?? 0,
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

            pagamento.SaidaGerada = true;
            pagamento.SaidaId = saida.Id;
            pagamento.Pago = true;
            pagamento.DataPagamento = saida.Data;
            pagamento.ValorPago = saida.Valor;

            await _context.SaveChangesAsync();

            await _auditService.LogAuditAsync(user.Id, "Gerar Saída", "PagamentoDespesaRecorrente",
            pagamento.Id.ToString(), $"Saída #{saida.Id} gerada automaticamente.");

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
            var dataVencimento = dataBase;

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
            }

            // Ajustar dia de vencimento se especificado
            if (despesa.DiaVencimento.HasValue)
            {
                var diasNoMes = DateTime.DaysInMonth(dataVencimento.Year, dataVencimento.Month);
                var dia = Math.Min(despesa.DiaVencimento.Value, diasNoMes);
                dataVencimento = new DateTime(dataVencimento.Year, dataVencimento.Month, dia);
            }

            return dataVencimento;
        }

        #endregion
    }
}
