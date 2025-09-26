using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaTesourariaEclesiastica.Data;
using SistemaTesourariaEclesiastica.Models;
using SistemaTesourariaEclesiastica.Services;

namespace SistemaTesourariaEclesiastica.Controllers
{
    public class CentrosCustoController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly AuditService _auditService;
        private readonly UserManager<ApplicationUser> _userManager;

        public CentrosCustoController(ApplicationDbContext context, AuditService auditService, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _auditService = auditService;
            _userManager = userManager;
        }

        // GET: CentrosCusto
        public async Task<IActionResult> Index(string searchString, int page = 1, int pageSize = 10)
        {
            ViewData["CurrentFilter"] = searchString;

            var centrosCusto = _context.CentrosCusto.AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                centrosCusto = centrosCusto.Where(c => c.Nome.Contains(searchString));
            }

            var totalItems = await centrosCusto.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalItems = totalItems;

            var paginatedCentrosCusto = await centrosCusto
                .OrderBy(c => c.Nome)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return View(paginatedCentrosCusto);
        }

        // GET: CentrosCusto/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var centroCusto = await _context.CentrosCusto
                .Include(c => c.Membros)
                .Include(c => c.Usuarios)
                .Include(c => c.ContasBancarias)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (centroCusto == null)
            {
                return NotFound();
            }

            return View(centroCusto);
        }

        // GET: CentrosCusto/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: CentrosCusto/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Nome,Tipo")] CentroCusto centroCusto)
        {
            if (ModelState.IsValid)
            {
                // Verifica se o nome já existe
                var nomeExistente = await _context.CentrosCusto.AnyAsync(c => c.Nome == centroCusto.Nome);
                if (nomeExistente)
                {
                    ModelState.AddModelError("Nome", "Este nome já está cadastrado.");
                    return View(centroCusto);
                }

                _context.Add(centroCusto);
                await _context.SaveChangesAsync();
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    await _auditService.LogAuditAsync(user.Id, "Criar", "CentroCusto", centroCusto.Id.ToString(), $"Centro de Custo {centroCusto.Nome} criado.");
                }
                TempData["SuccessMessage"] = "Centro de custo cadastrado com sucesso!";
                return RedirectToAction(nameof(Index));
            }

            return View(centroCusto);
        }

        // GET: CentrosCusto/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var centroCusto = await _context.CentrosCusto.FindAsync(id);
            if (centroCusto == null)
            {
                return NotFound();
            }

            return View(centroCusto);
        }

        // POST: CentrosCusto/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Nome,Tipo")] CentroCusto centroCusto)
        {
            if (id != centroCusto.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Verifica se o nome já existe para outro centro de custo
                    var nomeExistente = await _context.CentrosCusto.AnyAsync(c => c.Nome == centroCusto.Nome && c.Id != centroCusto.Id);
                    if (nomeExistente)
                    {
                        ModelState.AddModelError("Nome", "Este nome já está cadastrado para outro centro de custo.");
                        return View(centroCusto);
                    }

                    _context.Update(centroCusto);
                    await _context.SaveChangesAsync();
                    var user = await _userManager.GetUserAsync(User);
                    if (user != null)
                    {
                        await _auditService.LogAuditAsync(user.Id, "Editar", "CentroCusto", centroCusto.Id.ToString(), $"Centro de Custo {centroCusto.Nome} atualizado.");
                    }
                    TempData["SuccessMessage"] = "Centro de custo atualizado com sucesso!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CentroCustoExists(centroCusto.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }

            return View(centroCusto);
        }

        // GET: CentrosCusto/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var centroCusto = await _context.CentrosCusto
                .FirstOrDefaultAsync(m => m.Id == id);

            if (centroCusto == null)
            {
                return NotFound();
            }

            return View(centroCusto);
        }

        // POST: CentrosCusto/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var centroCusto = await _context.CentrosCusto.FindAsync(id);
            if (centroCusto != null)
            {
                // Verifica se o centro de custo possui dependências
                var possuiMembros = await _context.Membros.AnyAsync(m => m.CentroCustoId == id);
                var possuiUsuarios = await _context.Users.AnyAsync(u => u.CentroCustoId == id);
                var possuiEntradas = await _context.Entradas.AnyAsync(e => e.CentroCustoId == id);
                var possuiSaidas = await _context.Saidas.AnyAsync(s => s.CentroCustoId == id);

                if (possuiMembros || possuiUsuarios || possuiEntradas || possuiSaidas)
                {
                    TempData["ErrorMessage"] = "Não é possível excluir este centro de custo pois ele possui registros associados.";
                    return RedirectToAction(nameof(Index));
                }

                _context.CentrosCusto.Remove(centroCusto);
                await _context.SaveChangesAsync();
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    await _auditService.LogAuditAsync(user.Id, "Excluir", "CentroCusto", centroCusto.Id.ToString(), $"Centro de Custo {centroCusto.Nome} excluído.");
                }
                TempData["SuccessMessage"] = "Centro de custo excluído com sucesso!";
            }

            return RedirectToAction(nameof(Index));
        }

        // AJAX: Verificar Nome
        [HttpGet]
        public async Task<IActionResult> VerificarNome(string nome, int? id)
        {
            var query = _context.CentrosCusto.Where(c => c.Nome == nome);

            if (id.HasValue)
            {
                query = query.Where(c => c.Id != id);
            }

            var existe = await query.AnyAsync();
            return Json(!existe);
        }

        private bool CentroCustoExists(int id)
        {
            return _context.CentrosCusto.Any(e => e.Id == id);
        }
    }
}

