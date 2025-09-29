using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaTesourariaEclesiastica.Data;
using SistemaTesourariaEclesiastica.Enums;
using SistemaTesourariaEclesiastica.Models;
using SistemaTesourariaEclesiastica.Services;

namespace SistemaTesourariaEclesiastica.Controllers
{
    [Authorize]
    public class PlanoDeContasController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly AuditService _auditService;
        private readonly UserManager<ApplicationUser> _userManager;

        public PlanoDeContasController(ApplicationDbContext context, AuditService auditService, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _auditService = auditService;
            _userManager = userManager;
        }

        // GET: PlanoDeContas
        public async Task<IActionResult> Index(string searchString, TipoPlanoContas? tipo, string sortOrder, int? pageNumber)
        {
            ViewData["CurrentFilter"] = searchString;
            ViewData["TipoFilter"] = tipo;
            ViewData["CurrentSort"] = sortOrder;
            ViewData["NameSortParm"] = string.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            ViewData["TipoSortParm"] = sortOrder == "tipo" ? "tipo_desc" : "tipo";
            ViewData["DateSortParm"] = sortOrder == "date" ? "date_desc" : "date";

            var planosDeContas = from p in _context.PlanosDeContas select p;

            if (!string.IsNullOrEmpty(searchString))
            {
                planosDeContas = planosDeContas.Where(p => p.Nome.Contains(searchString) ||
                                                          p.Descricao.Contains(searchString));
            }

            if (tipo.HasValue)
            {
                planosDeContas = planosDeContas.Where(p => p.Tipo == tipo);
            }

            switch (sortOrder)
            {
                case "name_desc":
                    planosDeContas = planosDeContas.OrderByDescending(p => p.Nome);
                    break;
                case "tipo":
                    planosDeContas = planosDeContas.OrderBy(p => p.Tipo);
                    break;
                case "tipo_desc":
                    planosDeContas = planosDeContas.OrderByDescending(p => p.Tipo);
                    break;
                case "date":
                    planosDeContas = planosDeContas.OrderBy(p => p.DataCriacao);
                    break;
                case "date_desc":
                    planosDeContas = planosDeContas.OrderByDescending(p => p.DataCriacao);
                    break;
                default:
                    planosDeContas = planosDeContas.OrderBy(p => p.Nome);
                    break;
            }

            var totalItems = await planosDeContas.CountAsync();
            ViewBag.TotalItems = totalItems;

            int pageSize = 10;
            return View(await PaginatedList<PlanoDeContas>.CreateAsync(planosDeContas.AsNoTracking(), pageNumber ?? 1, pageSize));
        }

        // GET: PlanoDeContas/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var planoDeContas = await _context.PlanosDeContas
                .Include(p => p.Entradas)
                .Include(p => p.Saidas)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (planoDeContas == null)
            {
                return NotFound();
            }

            ViewBag.TotalEntradas = planoDeContas.Entradas?.Sum(e => e.Valor) ?? 0;
            ViewBag.TotalSaidas = planoDeContas.Saidas?.Sum(s => s.Valor) ?? 0;
            ViewBag.QuantidadeEntradas = planoDeContas.Entradas?.Count ?? 0;
            ViewBag.QuantidadeSaidas = planoDeContas.Saidas?.Count ?? 0;

            return View(planoDeContas);
        }

        // GET: PlanoDeContas/Create
        public IActionResult Create()
        {
            return View(new PlanoDeContas());
        }

        // POST: PlanoDeContas/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Nome,Descricao,Tipo")] PlanoDeContas planoDeContas)
        {
            if (ModelState.IsValid)
            {
                // Verifica se a descrição já existe
                var descricaoExistente = await _context.PlanosDeContas.AnyAsync(p => p.Descricao == planoDeContas.Descricao);
                if (descricaoExistente)
                {
                    ModelState.AddModelError("Descricao", "Esta descrição já está cadastrada.");
                    return View(planoDeContas);
                }

                _context.Add(planoDeContas);
                await _context.SaveChangesAsync();

                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    await _auditService.LogAuditAsync(user.Id, "Criar", "PlanoDeContas", planoDeContas.Id.ToString(),
                        $"Plano de Contas {planoDeContas.Nome} - {planoDeContas.Descricao} criado.");
                }

                TempData["SuccessMessage"] = "Plano de contas cadastrado com sucesso!";
                return RedirectToAction(nameof(Index));
            }

            return View(planoDeContas);
        }

        // GET: PlanoDeContas/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var planoDeContas = await _context.PlanosDeContas.FindAsync(id);
            if (planoDeContas == null)
            {
                return NotFound();
            }

            return View(planoDeContas);
        }

        // POST: PlanoDeContas/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Nome,Descricao,Tipo,Ativo")] PlanoDeContas planoDeContas)
        {
            if (id != planoDeContas.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Verifica se a descrição já existe para outro plano
                    var descricaoExistente = await _context.PlanosDeContas.AnyAsync(p => p.Descricao == planoDeContas.Descricao && p.Id != planoDeContas.Id);
                    if (descricaoExistente)
                    {
                        ModelState.AddModelError("Descricao", "Esta descrição já está cadastrada para outro plano de contas.");
                        return View(planoDeContas);
                    }

                    _context.Update(planoDeContas);
                    await _context.SaveChangesAsync();

                    var user = await _userManager.GetUserAsync(User);
                    if (user != null)
                    {
                        await _auditService.LogAuditAsync(user.Id, "Editar", "PlanoDeContas", planoDeContas.Id.ToString(),
                            $"Plano de Contas {planoDeContas.Nome} - {planoDeContas.Descricao} atualizado.");
                    }

                    TempData["SuccessMessage"] = "Plano de contas atualizado com sucesso!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PlanoDeContasExists(planoDeContas.Id))
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

            return View(planoDeContas);
        }

        // GET: PlanoDeContas/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var planoDeContas = await _context.PlanosDeContas
                .Include(p => p.Entradas)
                .Include(p => p.Saidas)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (planoDeContas == null)
            {
                return NotFound();
            }

            // Verifica se há movimentações vinculadas
            if (planoDeContas.Entradas.Any() || planoDeContas.Saidas.Any())
            {
                TempData["ErrorMessage"] = "Não é possível excluir este plano de contas pois existem movimentações vinculadas a ele.";
                return RedirectToAction(nameof(Index));
            }

            return View(planoDeContas);
        }

        // POST: PlanoDeContas/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var planoDeContas = await _context.PlanosDeContas
                .Include(p => p.Entradas)
                .Include(p => p.Saidas)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (planoDeContas != null)
            {
                // Verifica novamente se há movimentações vinculadas
                if (planoDeContas.Entradas.Any() || planoDeContas.Saidas.Any())
                {
                    TempData["ErrorMessage"] = "Não é possível excluir este plano de contas pois existem movimentações vinculadas a ele.";
                    return RedirectToAction(nameof(Index));
                }

                _context.PlanosDeContas.Remove(planoDeContas);
                await _context.SaveChangesAsync();

                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    await _auditService.LogAuditAsync(user.Id, "Excluir", "PlanoDeContas", planoDeContas.Id.ToString(),
                        $"Plano de Contas {planoDeContas.Nome} - {planoDeContas.Descricao} excluído.");
                }

                TempData["SuccessMessage"] = "Plano de contas excluído com sucesso!";
            }

            return RedirectToAction(nameof(Index));
        }

        // AJAX: Buscar planos por tipo
        [HttpGet]
        public async Task<IActionResult> BuscarPlanosPorTipo(TipoPlanoContas tipo)
        {
            var planos = await _context.PlanosDeContas
                .Where(p => p.Tipo == tipo && p.Ativo)
                .Select(p => new
                {
                    id = p.Id,
                    nome = p.Nome,
                    descricao = p.Descricao,
                    text = $"{p.Nome} - {p.Descricao}"
                })
                .OrderBy(p => p.nome)
                .ToListAsync();

            return Json(planos);
        }

        // AJAX: Verificar Descrição Única
        [HttpGet]
        public async Task<IActionResult> VerificarDescricao(string descricao, int? id)
        {
            var query = _context.PlanosDeContas.Where(p => p.Descricao == descricao);

            if (id.HasValue)
            {
                query = query.Where(p => p.Id != id);
            }

            var existe = await query.AnyAsync();
            return Json(!existe);
        }

        private bool PlanoDeContasExists(int id)
        {
            return _context.PlanosDeContas.Any(e => e.Id == id);
        }
    }
}