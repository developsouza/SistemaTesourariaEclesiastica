using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SistemaTesourariaEclesiastica.Attributes;
using SistemaTesourariaEclesiastica.Data;
using SistemaTesourariaEclesiastica.Models;
using SistemaTesourariaEclesiastica.Services;

namespace SistemaTesourariaEclesiastica.Controllers
{
    // ==================== ADICIONADA RESTRIÇÃO DE ACESSO ====================
    [Authorize(Roles = Roles.AdminOuTesoureiroGeral)]
    // ========================================================================
    public class TransferenciasInternasController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AuditService _auditService;

        public TransferenciasInternasController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, AuditService auditService)
        {
            _context = context;
            _userManager = userManager;
            _auditService = auditService;
        }

        // GET: TransferenciasInternas
        public async Task<IActionResult> Index()
        {
            var transferencias = await _context.TransferenciasInternas
                .Include(t => t.MeioDePagamentoOrigem)
                .Include(t => t.MeioDePagamentoDestino)
                .Include(t => t.CentroCustoOrigem)
                .Include(t => t.CentroCustoDestino)
                .Include(t => t.Usuario)
                .OrderByDescending(t => t.Data)
                .ToListAsync();

            return View(transferencias);
        }

        // GET: TransferenciasInternas/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var transferenciaInterna = await _context.TransferenciasInternas
                .Include(t => t.MeioDePagamentoOrigem)
                .Include(t => t.MeioDePagamentoDestino)
                .Include(t => t.CentroCustoOrigem)
                .Include(t => t.CentroCustoDestino)
                .Include(t => t.Usuario)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (transferenciaInterna == null)
            {
                return NotFound();
            }

            return View(transferenciaInterna);
        }

        // GET: TransferenciasInternas/Create
        public IActionResult Create()
        {
            ViewData["MeioDePagamentoOrigemId"] = new SelectList(_context.MeiosDePagamento, "Id", "Nome");
            ViewData["MeioDePagamentoDestinoId"] = new SelectList(_context.MeiosDePagamento, "Id", "Nome");
            ViewData["CentroCustoOrigemId"] = new SelectList(_context.CentrosCusto, "Id", "Nome");
            ViewData["CentroCustoDestinoId"] = new SelectList(_context.CentrosCusto, "Id", "Nome");

            return View();
        }

        // POST: TransferenciasInternas/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Data,Valor,Descricao,MeioDePagamentoOrigemId,MeioDePagamentoDestinoId,CentroCustoOrigemId,CentroCustoDestinoId")] TransferenciaInterna transferenciaInterna)
        {
            try
            {
                // Remover validação de navigation properties
                ModelState.Remove("MeioDePagamentoOrigem");
                ModelState.Remove("MeioDePagamentoDestino");
                ModelState.Remove("CentroCustoOrigem");
                ModelState.Remove("CentroCustoDestino");
                ModelState.Remove("Usuario");
                ModelState.Remove("UsuarioId");

                // Validações de negócio
                if (transferenciaInterna.Valor <= 0)
                {
                    ModelState.AddModelError("Valor", "O valor deve ser maior que zero.");
                }

                if (transferenciaInterna.CentroCustoOrigemId == transferenciaInterna.CentroCustoDestinoId)
                {
                    ModelState.AddModelError("CentroCustoDestinoId", "O centro de custo de destino deve ser diferente do origem.");
                }

                if (ModelState.IsValid)
                {
                    var user = await _userManager.GetUserAsync(User);
                    transferenciaInterna.UsuarioId = user!.Id;
                    transferenciaInterna.DataCriacao = DateTime.Now;

                    _context.Add(transferenciaInterna);
                    await _context.SaveChangesAsync();

                    // Log de auditoria
                    if (user != null)
                    {
                        await _auditService.LogCreateAsync(user.Id, transferenciaInterna, transferenciaInterna.Id.ToString());
                    }

                    TempData["SuccessMessage"] = "Transferência interna registrada com sucesso!";
                    return RedirectToAction(nameof(Index));
                }

                ViewData["MeioDePagamentoOrigemId"] = new SelectList(_context.MeiosDePagamento, "Id", "Nome", transferenciaInterna.MeioDePagamentoOrigemId);
                ViewData["MeioDePagamentoDestinoId"] = new SelectList(_context.MeiosDePagamento, "Id", "Nome", transferenciaInterna.MeioDePagamentoDestinoId);
                ViewData["CentroCustoOrigemId"] = new SelectList(_context.CentrosCusto, "Id", "Nome", transferenciaInterna.CentroCustoOrigemId);
                ViewData["CentroCustoDestinoId"] = new SelectList(_context.CentrosCusto, "Id", "Nome", transferenciaInterna.CentroCustoDestinoId);

                return View(transferenciaInterna);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Erro ao registrar transferência. Tente novamente.");
                ViewData["MeioDePagamentoOrigemId"] = new SelectList(_context.MeiosDePagamento, "Id", "Nome", transferenciaInterna.MeioDePagamentoOrigemId);
                ViewData["MeioDePagamentoDestinoId"] = new SelectList(_context.MeiosDePagamento, "Id", "Nome", transferenciaInterna.MeioDePagamentoDestinoId);
                ViewData["CentroCustoOrigemId"] = new SelectList(_context.CentrosCusto, "Id", "Nome", transferenciaInterna.CentroCustoOrigemId);
                ViewData["CentroCustoDestinoId"] = new SelectList(_context.CentrosCusto, "Id", "Nome", transferenciaInterna.CentroCustoDestinoId);
                return View(transferenciaInterna);
            }
        }

        // GET: TransferenciasInternas/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var transferenciaInterna = await _context.TransferenciasInternas.FindAsync(id);
            if (transferenciaInterna == null)
            {
                return NotFound();
            }

            ViewData["MeioDePagamentoOrigemId"] = new SelectList(_context.MeiosDePagamento, "Id", "Nome", transferenciaInterna.MeioDePagamentoOrigemId);
            ViewData["MeioDePagamentoDestinoId"] = new SelectList(_context.MeiosDePagamento, "Id", "Nome", transferenciaInterna.MeioDePagamentoDestinoId);
            ViewData["CentroCustoOrigemId"] = new SelectList(_context.CentrosCusto, "Id", "Nome", transferenciaInterna.CentroCustoOrigemId);
            ViewData["CentroCustoDestinoId"] = new SelectList(_context.CentrosCusto, "Id", "Nome", transferenciaInterna.CentroCustoDestinoId);

            return View(transferenciaInterna);
        }

        // POST: TransferenciasInternas/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Data,Valor,Descricao,MeioDePagamentoOrigemId,MeioDePagamentoDestinoId,CentroCustoOrigemId,CentroCustoDestinoId")] TransferenciaInterna transferenciaInterna)
        {
            if (id != transferenciaInterna.Id)
            {
                return NotFound();
            }

            try
            {
                // Remover validação de navigation properties
                ModelState.Remove("MeioDePagamentoOrigem");
                ModelState.Remove("MeioDePagamentoDestino");
                ModelState.Remove("CentroCustoOrigem");
                ModelState.Remove("CentroCustoDestino");
                ModelState.Remove("Usuario");
                ModelState.Remove("UsuarioId");
                ModelState.Remove("DataCriacao");

                // Buscar transferência original
                var originalTransferencia = await _context.TransferenciasInternas.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id);
                if (originalTransferencia == null) return NotFound();

                // Validações de negócio
                if (transferenciaInterna.Valor <= 0)
                {
                    ModelState.AddModelError("Valor", "O valor deve ser maior que zero.");
                }

                if (transferenciaInterna.CentroCustoOrigemId == transferenciaInterna.CentroCustoDestinoId)
                {
                    ModelState.AddModelError("CentroCustoDestinoId", "O centro de custo de destino deve ser diferente do origem.");
                }

                if (ModelState.IsValid)
                {
                    // Manter dados originais
                    transferenciaInterna.UsuarioId = originalTransferencia.UsuarioId;
                    transferenciaInterna.DataCriacao = originalTransferencia.DataCriacao;

                    _context.Update(transferenciaInterna);
                    await _context.SaveChangesAsync();

                    var user = await _userManager.GetUserAsync(User);
                    if (user != null)
                    {
                        await _auditService.LogUpdateAsync(user.Id, originalTransferencia, transferenciaInterna, transferenciaInterna.Id.ToString());
                    }

                    TempData["SuccessMessage"] = "Transferência atualizada com sucesso!";
                    return RedirectToAction(nameof(Index));
                }

                ViewData["MeioDePagamentoOrigemId"] = new SelectList(_context.MeiosDePagamento, "Id", "Nome", transferenciaInterna.MeioDePagamentoOrigemId);
                ViewData["MeioDePagamentoDestinoId"] = new SelectList(_context.MeiosDePagamento, "Id", "Nome", transferenciaInterna.MeioDePagamentoDestinoId);
                ViewData["CentroCustoOrigemId"] = new SelectList(_context.CentrosCusto, "Id", "Nome", transferenciaInterna.CentroCustoOrigemId);
                ViewData["CentroCustoDestinoId"] = new SelectList(_context.CentrosCusto, "Id", "Nome", transferenciaInterna.CentroCustoDestinoId);

                return View(transferenciaInterna);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TransferenciaInternaExists(transferenciaInterna.Id))
                {
                    return NotFound();
                }
                throw;
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Erro ao atualizar transferência. Tente novamente.");
                ViewData["MeioDePagamentoOrigemId"] = new SelectList(_context.MeiosDePagamento, "Id", "Nome", transferenciaInterna.MeioDePagamentoOrigemId);
                ViewData["MeioDePagamentoDestinoId"] = new SelectList(_context.MeiosDePagamento, "Id", "Nome", transferenciaInterna.MeioDePagamentoDestinoId);
                ViewData["CentroCustoOrigemId"] = new SelectList(_context.CentrosCusto, "Id", "Nome", transferenciaInterna.CentroCustoOrigemId);
                ViewData["CentroCustoDestinoId"] = new SelectList(_context.CentrosCusto, "Id", "Nome", transferenciaInterna.CentroCustoDestinoId);
                return View(transferenciaInterna);
            }
        }

        // GET: TransferenciasInternas/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var transferenciaInterna = await _context.TransferenciasInternas
                .Include(t => t.MeioDePagamentoOrigem)
                .Include(t => t.MeioDePagamentoDestino)
                .Include(t => t.CentroCustoOrigem)
                .Include(t => t.CentroCustoDestino)
                .Include(t => t.Usuario)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (transferenciaInterna == null)
            {
                return NotFound();
            }

            return View(transferenciaInterna);
        }

        // POST: TransferenciasInternas/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var transferenciaInterna = await _context.TransferenciasInternas.FindAsync(id);
            if (transferenciaInterna != null)
            {
                _context.TransferenciasInternas.Remove(transferenciaInterna);
                await _context.SaveChangesAsync();

                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    await _auditService.LogAuditAsync(user.Id, "Excluir", "TransferenciaInterna", transferenciaInterna.Id.ToString(), $"Transferência de {transferenciaInterna.Valor:C2} excluída.");
                }

                TempData["SuccessMessage"] = "Transferência excluída com sucesso!";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool TransferenciaInternaExists(int id)
        {
            return _context.TransferenciasInternas.Any(e => e.Id == id);
        }
    }
}