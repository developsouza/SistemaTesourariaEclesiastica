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
    [Authorize(Roles = Roles.AdminOuTesoureiro)]
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
            var applicationDbContext = _context.TransferenciasInternas
                .Include(t => t.MeioDePagamentoOrigem)
                .Include(t => t.MeioDePagamentoDestino)
                .Include(t => t.CentroCustoOrigem)
                .Include(t => t.CentroCustoDestino)
                .Include(t => t.Usuario);
            return View(await applicationDbContext.ToListAsync());
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
                // CRÍTICO: Remover navigation properties do ModelState
                ModelState.Remove("MeioDePagamentoOrigem");
                ModelState.Remove("MeioDePagamentoDestino");
                ModelState.Remove("CentroCustoOrigem");
                ModelState.Remove("CentroCustoDestino");
                ModelState.Remove("Usuario");

                // Validação customizada: origem e destino devem ser diferentes
                if (transferenciaInterna.MeioDePagamentoOrigemId == transferenciaInterna.MeioDePagamentoDestinoId)
                {
                    ModelState.AddModelError("MeioDePagamentoDestinoId", "O meio de pagamento de destino deve ser diferente do origem.");
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

                    // Buscar nomes dos meios de pagamento para log
                    var meioDePagamentoOrigem = await _context.MeiosDePagamento.FindAsync(transferenciaInterna.MeioDePagamentoOrigemId);
                    var meioDePagamentoDestino = await _context.MeiosDePagamento.FindAsync(transferenciaInterna.MeioDePagamentoDestinoId);

                    if (meioDePagamentoOrigem == null || meioDePagamentoDestino == null)
                    {
                        ModelState.AddModelError(string.Empty, "Meio de pagamento de origem ou destino não encontrado.");
                        ViewData["MeioDePagamentoOrigemId"] = new SelectList(_context.MeiosDePagamento, "Id", "Nome", transferenciaInterna.MeioDePagamentoOrigemId);
                        ViewData["MeioDePagamentoDestinoId"] = new SelectList(_context.MeiosDePagamento, "Id", "Nome", transferenciaInterna.MeioDePagamentoDestinoId);
                        ViewData["CentroCustoOrigemId"] = new SelectList(_context.CentrosCusto, "Id", "Nome", transferenciaInterna.CentroCustoOrigemId);
                        ViewData["CentroCustoDestinoId"] = new SelectList(_context.CentrosCusto, "Id", "Nome", transferenciaInterna.CentroCustoDestinoId);
                        return View(transferenciaInterna);
                    }

                    _context.Add(transferenciaInterna);
                    await _context.SaveChangesAsync();

                    // Log de auditoria com método tipado
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
                // Log do erro
                // _logger.LogError(ex, "Erro ao criar transferência interna");
                ModelState.AddModelError(string.Empty, "Erro interno ao salvar transferência. Tente novamente.");
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
        public async Task<IActionResult> Edit(int id, [Bind("Id,Data,Valor,Descricao,MeioDePagamentoOrigemId,MeioDePagamentoDestinoId,CentroCustoOrigemId,CentroCustoDestinoId,Quitada")] TransferenciaInterna transferenciaInterna)
        {
            if (id != transferenciaInterna.Id)
            {
                return NotFound();
            }

            try
            {
                // CRÍTICO: Remover navigation properties do ModelState
                ModelState.Remove("MeioDePagamentoOrigem");
                ModelState.Remove("MeioDePagamentoDestino");
                ModelState.Remove("CentroCustoOrigem");
                ModelState.Remove("CentroCustoDestino");
                ModelState.Remove("Usuario");
                ModelState.Remove("UsuarioId");
                ModelState.Remove("DataCriacao");

                // Validação customizada: origem e destino devem ser diferentes
                if (transferenciaInterna.MeioDePagamentoOrigemId == transferenciaInterna.MeioDePagamentoDestinoId)
                {
                    ModelState.AddModelError("MeioDePagamentoDestinoId", "O meio de pagamento de destino deve ser diferente do origem.");
                }

                if (transferenciaInterna.CentroCustoOrigemId == transferenciaInterna.CentroCustoDestinoId)
                {
                    ModelState.AddModelError("CentroCustoDestinoId", "O centro de custo de destino deve ser diferente do origem.");
                }

                // Buscar original para manter dados de auditoria
                var originalTransferencia = await _context.TransferenciasInternas.AsNoTracking()
                    .FirstOrDefaultAsync(t => t.Id == id);
                if (originalTransferencia == null) return NotFound();

                if (ModelState.IsValid)
                {
                    // Manter dados originais de auditoria
                    transferenciaInterna.UsuarioId = originalTransferencia.UsuarioId;
                    transferenciaInterna.DataCriacao = originalTransferencia.DataCriacao;

                    _context.Update(transferenciaInterna);
                    await _context.SaveChangesAsync();

                    var user = await _userManager.GetUserAsync(User);
                    if (user != null)
                    {
                        await _auditService.LogUpdateAsync(user.Id, originalTransferencia, transferenciaInterna, transferenciaInterna.Id.ToString());
                    }

                    TempData["SuccessMessage"] = "Transferência interna atualizada com sucesso!";
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
                // Log do erro
                // _logger.LogError(ex, "Erro ao atualizar transferência {Id}", id);
                ModelState.AddModelError(string.Empty, "Erro interno ao atualizar transferência. Tente novamente.");
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
                TempData["SuccessMessage"] = "Transferência interna excluída com sucesso!";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool TransferenciaInternaExists(int id)
        {
            return _context.TransferenciasInternas.Any(e => e.Id == id);
        }
    }
}

