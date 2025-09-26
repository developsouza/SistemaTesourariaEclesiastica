using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaTesourariaEclesiastica.Data;
using SistemaTesourariaEclesiastica.Enums;
using SistemaTesourariaEclesiastica.Models;
using SistemaTesourariaEclesiastica.Services;

namespace SistemaTesourariaEclesiastica.Controllers
{
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
        public async Task<IActionResult> Index(string searchString, TipoPlanoContas? tipo, int page = 1, int pageSize = 10)
        {
            ViewData["CurrentFilter"] = searchString;
            ViewData["TipoFilter"] = tipo;

            var planosContas = _context.PlanosDeContas.AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                planosContas = planosContas.Where(p => p.Descricao.Contains(searchString) ||
                                                      p.Codigo.Contains(searchString));
            }

            if (tipo.HasValue)
            {
                planosContas = planosContas.Where(p => p.Tipo == tipo);
            }

            var totalItems = await planosContas.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalItems = totalItems;

            var paginatedPlanos = await planosContas
                .OrderBy(p => p.Codigo)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return View(paginatedPlanos);
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

            // Calcular estatísticas
            ViewBag.TotalEntradas = planoDeContas.Entradas?.Sum(e => e.Valor) ?? 0;
            ViewBag.TotalSaidas = planoDeContas.Saidas?.Sum(s => s.Valor) ?? 0;
            ViewBag.QuantidadeEntradas = planoDeContas.Entradas?.Count ?? 0;
            ViewBag.QuantidadeSaidas = planoDeContas.Saidas?.Count ?? 0;

            return View(planoDeContas);
        }

        // GET: PlanoDeContas/Create
        public IActionResult Create()
        {
            var proximoCodigo = GerarProximoCodigo();
            var planoDeContas = new PlanoDeContas
            {
                Codigo = proximoCodigo
            };

            return View(planoDeContas);
        }

        // POST: PlanoDeContas/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Codigo,Nome,Descricao,Tipo")] PlanoDeContas planoDeContas)
        {
            if (ModelState.IsValid)
            {
                // Verifica se o código já existe
                var codigoExistente = await _context.PlanosDeContas.AnyAsync(p => p.Codigo == planoDeContas.Codigo);
                if (codigoExistente)
                {
                    ModelState.AddModelError("Codigo", "Este código já está cadastrado.");
                    return View(planoDeContas);
                }

                _context.Add(planoDeContas);
                await _context.SaveChangesAsync();
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    await _auditService.LogAuditAsync(user.Id, "Criar", "PlanoDeContas", planoDeContas.Id.ToString(), $"Plano de Contas {planoDeContas.Codigo} - {planoDeContas.Descricao} criado.");
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
        public async Task<IActionResult> Edit(int id, [Bind("Id,Codigo,Nome,Descricao,Tipo,Ativo")] PlanoDeContas planoDeContas)
        {
            if (id != planoDeContas.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Verifica se o código já existe para outro plano
                    var codigoExistente = await _context.PlanosDeContas.AnyAsync(p => p.Codigo == planoDeContas.Codigo && p.Id != planoDeContas.Id);
                    if (codigoExistente)
                    {
                        ModelState.AddModelError("Codigo", "Este código já está cadastrado para outro plano de contas.");
                        return View(planoDeContas);
                    }

                    _context.Update(planoDeContas);
                    await _context.SaveChangesAsync();
                    var user = await _userManager.GetUserAsync(User);
                    if (user != null)
                    {
                        await _auditService.LogAuditAsync(user.Id, "Editar", "PlanoDeContas", planoDeContas.Id.ToString(), $"Plano de Contas {planoDeContas.Codigo} - {planoDeContas.Descricao} atualizado.");
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
                .FirstOrDefaultAsync(m => m.Id == id);

            if (planoDeContas == null)
            {
                return NotFound();
            }

            return View(planoDeContas);
        }

        // POST: PlanoDeContas/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var planoDeContas = await _context.PlanosDeContas.FindAsync(id);
            if (planoDeContas != null)
            {
                // Verifica se o plano possui movimentações associadas
                var possuiEntradas = await _context.Entradas.AnyAsync(e => e.PlanoDeContasId == id);
                var possuiSaidas = await _context.Saidas.AnyAsync(s => s.PlanoDeContasId == id);

                if (possuiEntradas || possuiSaidas)
                {
                    TempData["ErrorMessage"] = "Não é possível excluir este plano de contas pois ele possui movimentações associadas.";
                    return RedirectToAction(nameof(Index));
                }

                _context.PlanosDeContas.Remove(planoDeContas);
                await _context.SaveChangesAsync();
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    await _auditService.LogAuditAsync(user.Id, "Excluir", "PlanoDeContas", planoDeContas.Id.ToString(), $"Plano de Contas {planoDeContas.Codigo} - {planoDeContas.Descricao} excluído.");
                }
                TempData["SuccessMessage"] = "Plano de contas excluído com sucesso!";
            }

            return RedirectToAction(nameof(Index));
        }

        // AJAX: Verificar Código
        [HttpGet]
        public async Task<IActionResult> VerificarCodigo(string codigo, int? id)
        {
            var query = _context.PlanosDeContas.Where(p => p.Codigo == codigo);

            if (id.HasValue)
            {
                query = query.Where(p => p.Id != id);
            }

            var existe = await query.AnyAsync();
            return Json(!existe);
        }

        // AJAX: Buscar planos por tipo
        [HttpGet]
        public async Task<IActionResult> BuscarPlanosPorTipo(TipoPlanoContas tipo)
        {
            var planos = await _context.PlanosDeContas
                .Where(p => p.Tipo == tipo)
                .Select(p => new
                {
                    id = p.Id,
                    codigo = p.Codigo,
                    descricao = p.Descricao,
                    text = $"{p.Codigo} - {p.Descricao}"
                })
                .OrderBy(p => p.codigo)
                .ToListAsync();

            return Json(planos);
        }

        private string GerarProximoCodigo()
        {
            var ultimoCodigo = _context.PlanosDeContas
                .OrderByDescending(p => p.Codigo)
                .Select(p => p.Codigo)
                .FirstOrDefault();

            if (string.IsNullOrEmpty(ultimoCodigo))
            {
                return "1.01.001";
            }

            // Lógica simples para incrementar o código
            var partes = ultimoCodigo.Split('.');
            if (partes.Length == 3 && int.TryParse(partes[2], out int ultimoNumero))
            {
                var novoNumero = ultimoNumero + 1;
                return $"{partes[0]}.{partes[1]}.{novoNumero:D3}";
            }

            return "1.01.001";
        }

        private bool PlanoDeContasExists(int id)
        {
            return _context.PlanosDeContas.Any(e => e.Id == id);
        }
    }
}

