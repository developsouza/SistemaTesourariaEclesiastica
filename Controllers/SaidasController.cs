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

        public SaidasController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, AuditService auditService)
        {
            _context = context;
            _userManager = userManager;
            _auditService = auditService;
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

            var saidas = _context.Saidas
                .Include(s => s.MeioDePagamento)
                .Include(s => s.CentroCusto)
                .Include(s => s.PlanoDeContas)
                .Include(s => s.Fornecedor)
                .Include(s => s.Usuario)
                .AsQueryable();

            // Filtros
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

            // Dropdowns para filtros
            ViewBag.CentrosCusto = new SelectList(await _context.CentrosCusto.ToListAsync(), "Id", "Nome", centroCustoId);
            ViewBag.PlanosContas = new SelectList(
                await _context.PlanosDeContas.Where(p => p.Tipo == TipoPlanoContas.Despesa).ToListAsync(),
                "Id", "Descricao", planoContasId);
            ViewBag.Fornecedores = new SelectList(await _context.Fornecedores.ToListAsync(), "Id", "Nome", fornecedorId);

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

            return View(saida);
        }

        // GET: Saidas/Create
        public async Task<IActionResult> Create()
        {
            await PopulateDropdowns();

            var saida = new Saida
            {
                Data = DateTime.Today
            };

            return View(saida);
        }

        // POST: Saidas/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Data,Valor,Descricao,MeioDePagamentoId,CentroCustoId,PlanoDeContasId,FornecedorId,TipoDespesa,NumeroDocumento,DataVencimento,ComprovanteUrl,Observacoes")] Saida saida)
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

                if (ModelState.IsValid)
                {
                    var user = await _userManager.GetUserAsync(User);
                    saida.UsuarioId = user!.Id;
                    saida.DataCriacao = DateTime.Now;

                    _context.Add(saida);
                    await _context.SaveChangesAsync();

                    // Log de auditoria (já corrigido no AuditService)
                    if (user != null)
                    {
                        await _auditService.LogCreateAsync(user.Id, saida, saida.Id.ToString());
                    }

                    TempData["SuccessMessage"] = "Saída registrada com sucesso!";
                    return RedirectToAction(nameof(Index));
                }

                await PopulateDropdowns(saida);
                return View(saida);
            }
            catch (Exception ex)
            {
                // Log do erro
                // _logger.LogError(ex, "Erro ao criar saída");
                ModelState.AddModelError(string.Empty, "Erro interno ao salvar saída. Tente novamente.");
                await PopulateDropdowns(saida);
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
                // Log do erro
                // _logger.LogError(ex, "Erro ao atualizar saída {SaidaId}", id);
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

            return View(saida);
        }

        // POST: Saidas/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var saida = await _context.Saidas.FindAsync(id);
            if (saida != null)
            {
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

        private async Task PopulateDropdowns(Saida? saida = null)
        {
            ViewData["MeioDePagamentoId"] = new SelectList(
                await _context.MeiosDePagamento.ToListAsync(),
                "Id", "Nome", saida?.MeioDePagamentoId);

            ViewData["CentroCustoId"] = new SelectList(
                await _context.CentrosCusto.ToListAsync(),
                "Id", "Nome", saida?.CentroCustoId);

            ViewData["PlanoDeContasId"] = new SelectList(
                await _context.PlanosDeContas.Where(p => p.Tipo == TipoPlanoContas.Despesa).ToListAsync(),
                "Id", "Descricao", saida?.PlanoDeContasId);

            ViewData["FornecedorId"] = new SelectList(
                await _context.Fornecedores.ToListAsync(),
                "Id", "Nome", saida?.FornecedorId);
        }

        private bool SaidaExists(int id)
        {
            return _context.Saidas.Any(e => e.Id == id);
        }
    }
}

