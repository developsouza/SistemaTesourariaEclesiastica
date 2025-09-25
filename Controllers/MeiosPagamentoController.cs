using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaTesourariaEclesiastica.Data;
using SistemaTesourariaEclesiastica.Models;
using Microsoft.AspNetCore.Identity;
using SistemaTesourariaEclesiastica.Services;

namespace SistemaTesourariaEclesiastica.Controllers
{
    public class MeiosPagamentoController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly AuditService _auditService;
        private readonly UserManager<ApplicationUser> _userManager;

        public MeiosPagamentoController(ApplicationDbContext context, AuditService auditService, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _auditService = auditService;
            _userManager = userManager;
        }

        // GET: MeiosPagamento
        public async Task<IActionResult> Index(string searchString, int page = 1, int pageSize = 10)
        {
            ViewData["CurrentFilter"] = searchString;

            var meiosPagamento = _context.MeiosDePagamento.AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                meiosPagamento = meiosPagamento.Where(m => m.Nome.Contains(searchString));
            }

            var totalItems = await meiosPagamento.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalItems = totalItems;

            var paginatedMeios = await meiosPagamento
                .OrderBy(m => m.Nome)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return View(paginatedMeios);
        }

        // GET: MeiosPagamento/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var meioDePagamento = await _context.MeiosDePagamento
                .Include(m => m.Entradas)
                .Include(m => m.Saidas)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (meioDePagamento == null)
            {
                return NotFound();
            }

            // Calcular estatísticas
            ViewBag.TotalEntradas = meioDePagamento.Entradas?.Sum(e => e.Valor) ?? 0;
            ViewBag.TotalSaidas = meioDePagamento.Saidas?.Sum(s => s.Valor) ?? 0;
            ViewBag.QuantidadeEntradas = meioDePagamento.Entradas?.Count ?? 0;
            ViewBag.QuantidadeSaidas = meioDePagamento.Saidas?.Count ?? 0;
            ViewBag.SaldoLiquido = ViewBag.TotalEntradas - ViewBag.TotalSaidas;

            return View(meioDePagamento);
        }

        // GET: MeiosPagamento/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: MeiosPagamento/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Nome,Descricao,Ativo")] MeioDePagamento meioDePagamento)
        {
            if (ModelState.IsValid)
            {
                // Verifica se o nome já existe
                var nomeExistente = await _context.MeiosDePagamento.AnyAsync(m => m.Nome == meioDePagamento.Nome);
                if (nomeExistente)
                {
                    ModelState.AddModelError("Nome", "Este nome já está cadastrado.");
                    return View(meioDePagamento);
                }

                _context.Add(meioDePagamento);
                await _context.SaveChangesAsync();
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    await _auditService.LogAuditAsync(user.Id, "Criar", "MeioDePagamento", meioDePagamento.Id.ToString(), $"Meio de Pagamento {meioDePagamento.Nome} criado.");
                }
                TempData["SuccessMessage"] = "Meio de pagamento cadastrado com sucesso!";
                return RedirectToAction(nameof(Index));
            }

            return View(meioDePagamento);
        }

        // GET: MeiosPagamento/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var meioDePagamento = await _context.MeiosDePagamento.FindAsync(id);
            if (meioDePagamento == null)
            {
                return NotFound();
            }

            return View(meioDePagamento);
        }

        // POST: MeiosPagamento/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Nome,Descricao,Ativo")] MeioDePagamento meioDePagamento)
        {
            if (id != meioDePagamento.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Verifica se o nome já existe para outro meio de pagamento
                    var nomeExistente = await _context.MeiosDePagamento.AnyAsync(m => m.Nome == meioDePagamento.Nome && m.Id != meioDePagamento.Id);
                    if (nomeExistente)
                    {
                        ModelState.AddModelError("Nome", "Este nome já está cadastrado para outro meio de pagamento.");
                        return View(meioDePagamento);
                    }

                    _context.Update(meioDePagamento);
                    await _context.SaveChangesAsync();
                    var user = await _userManager.GetUserAsync(User);
                    if (user != null)
                    {
                        await _auditService.LogAuditAsync(user.Id, "Editar", "MeioDePagamento", meioDePagamento.Id.ToString(), $"Meio de Pagamento {meioDePagamento.Nome} atualizado.");
                    }
                    TempData["SuccessMessage"] = "Meio de pagamento atualizado com sucesso!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MeioDePagamentoExists(meioDePagamento.Id))
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

            return View(meioDePagamento);
        }

        // GET: MeiosPagamento/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var meioDePagamento = await _context.MeiosDePagamento
                .FirstOrDefaultAsync(m => m.Id == id);

            if (meioDePagamento == null)
            {
                return NotFound();
            }

            return View(meioDePagamento);
        }

        // POST: MeiosPagamento/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var meioDePagamento = await _context.MeiosDePagamento.FindAsync(id);
            if (meioDePagamento != null)
            {
                // Verifica se o meio de pagamento possui movimentações associadas
                var possuiEntradas = await _context.Entradas.AnyAsync(e => e.MeioDePagamentoId == id);
                var possuiSaidas = await _context.Saidas.AnyAsync(s => s.MeioDePagamentoId == id);

                if (possuiEntradas || possuiSaidas)
                {
                    TempData["ErrorMessage"] = "Não é possível excluir este meio de pagamento pois ele possui movimentações associadas.";
                    return RedirectToAction(nameof(Index));
                }

                _context.MeiosDePagamento.Remove(meioDePagamento);
                await _context.SaveChangesAsync();
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    await _auditService.LogAuditAsync(user.Id, "Excluir", "MeioDePagamento", meioDePagamento.Id.ToString(), $"Meio de Pagamento {meioDePagamento.Nome} excluído.");
                }
                TempData["SuccessMessage"] = "Meio de pagamento excluído com sucesso!";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: MeiosPagamento/ToggleStatus/5
        [HttpPost]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var meioDePagamento = await _context.MeiosDePagamento.FindAsync(id);
            if (meioDePagamento != null)
            {
                meioDePagamento.Ativo = !meioDePagamento.Ativo;
                await _context.SaveChangesAsync();
                
                var status = meioDePagamento.Ativo ? "ativado" : "desativado";
                TempData["SuccessMessage"] = $"Meio de pagamento {status} com sucesso!";
            }

            return RedirectToAction(nameof(Index));
        }

        // AJAX: Verificar Nome
        [HttpGet]
        public async Task<IActionResult> VerificarNome(string nome, int? id)
        {
            var query = _context.MeiosDePagamento.Where(m => m.Nome == nome);
            
            if (id.HasValue)
            {
                query = query.Where(m => m.Id != id);
            }

            var existe = await query.AnyAsync();
            return Json(!existe);
        }

        // AJAX: Buscar meios de pagamento ativos
        [HttpGet]
        public async Task<IActionResult> BuscarMeiosAtivos()
        {
            var meios = await _context.MeiosDePagamento
                .Where(m => m.Ativo)
                .Select(m => new
                {
                    id = m.Id,
                    text = m.Nome,
                    descricao = m.Descricao
                })
                .OrderBy(m => m.text)
                .ToListAsync();

            return Json(meios);
        }

        private bool MeioDePagamentoExists(int id)
        {
            return _context.MeiosDePagamento.Any(e => e.Id == id);
        }
    }
}

