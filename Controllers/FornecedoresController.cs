using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaTesourariaEclesiastica.Data;
using SistemaTesourariaEclesiastica.Models;
using SistemaTesourariaEclesiastica.Services;

namespace SistemaTesourariaEclesiastica.Controllers
{
    public class FornecedoresController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly AuditService _auditService;
        private readonly UserManager<ApplicationUser> _userManager;

        public FornecedoresController(ApplicationDbContext context, AuditService auditService, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _auditService = auditService;
            _userManager = userManager;
        }

        // GET: Fornecedores
        public async Task<IActionResult> Index(string searchString, int page = 1, int pageSize = 10)
        {
            ViewData["CurrentFilter"] = searchString;

            var fornecedores = _context.Fornecedores.AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                fornecedores = fornecedores.Where(f => f.Nome.Contains(searchString) ||
                                                      (f.CNPJ != null && f.CNPJ.Contains(searchString)) ||
                                                      (f.CPF != null && f.CPF.Contains(searchString)) ||
                                                      (f.Email != null && f.Email.Contains(searchString)));
            }

            var totalItems = await fornecedores.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalItems = totalItems;

            var paginatedFornecedores = await fornecedores
                .OrderBy(f => f.Nome)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return View(paginatedFornecedores);
        }

        // GET: Fornecedores/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var fornecedor = await _context.Fornecedores
                .Include(f => f.Saidas)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (fornecedor == null)
            {
                return NotFound();
            }

            // Calcular estatísticas
            ViewBag.TotalCompras = fornecedor.Saidas?.Sum(s => s.Valor) ?? 0;
            ViewBag.QuantidadeCompras = fornecedor.Saidas?.Count ?? 0;
            ViewBag.UltimaCompra = fornecedor.Saidas?.OrderByDescending(s => s.Data).FirstOrDefault()?.Data;

            return View(fornecedor);
        }

        // GET: Fornecedores/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Fornecedores/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Nome,CNPJ,CPF,Telefone,Email,Endereco,Cidade,Estado,CEP,Observacoes")] Fornecedor fornecedor)
        {
            if (ModelState.IsValid)
            {
                // Validações de documento único
                if (!string.IsNullOrEmpty(fornecedor.CNPJ))
                {
                    var cnpjExistente = await _context.Fornecedores.AnyAsync(f => f.CNPJ == fornecedor.CNPJ);
                    if (cnpjExistente)
                    {
                        ModelState.AddModelError("CNPJ", "Este CNPJ já está cadastrado.");
                        return View(fornecedor);
                    }
                }

                if (!string.IsNullOrEmpty(fornecedor.CPF))
                {
                    var cpfExistente = await _context.Fornecedores.AnyAsync(f => f.CPF == fornecedor.CPF);
                    if (cpfExistente)
                    {
                        ModelState.AddModelError("CPF", "Este CPF já está cadastrado.");
                        return View(fornecedor);
                    }
                }

                _context.Add(fornecedor);
                await _context.SaveChangesAsync();
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    await _auditService.LogAuditAsync(user.Id, "Criar", "Fornecedor", fornecedor.Id.ToString(), $"Fornecedor {fornecedor.Nome} criado.");
                }
                TempData["SuccessMessage"] = "Fornecedor cadastrado com sucesso!";
                return RedirectToAction(nameof(Index));
            }

            return View(fornecedor);
        }

        // GET: Fornecedores/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var fornecedor = await _context.Fornecedores.FindAsync(id);
            if (fornecedor == null)
            {
                return NotFound();
            }

            return View(fornecedor);
        }

        // POST: Fornecedores/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Nome,CNPJ,CPF,Telefone,Email,Endereco,Cidade,Estado,CEP,Observacoes")] Fornecedor fornecedor)
        {
            if (id != fornecedor.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Validações de documento único
                    if (!string.IsNullOrEmpty(fornecedor.CNPJ))
                    {
                        var cnpjExistente = await _context.Fornecedores.AnyAsync(f => f.CNPJ == fornecedor.CNPJ && f.Id != fornecedor.Id);
                        if (cnpjExistente)
                        {
                            ModelState.AddModelError("CNPJ", "Este CNPJ já está cadastrado para outro fornecedor.");
                            return View(fornecedor);
                        }
                    }

                    if (!string.IsNullOrEmpty(fornecedor.CPF))
                    {
                        var cpfExistente = await _context.Fornecedores.AnyAsync(f => f.CPF == fornecedor.CPF && f.Id != fornecedor.Id);
                        if (cpfExistente)
                        {
                            ModelState.AddModelError("CPF", "Este CPF já está cadastrado para outro fornecedor.");
                            return View(fornecedor);
                        }
                    }

                    _context.Update(fornecedor);
                    await _context.SaveChangesAsync();
                    var user = await _userManager.GetUserAsync(User);
                    if (user != null)
                    {
                        await _auditService.LogAuditAsync(user.Id, "Editar", "Fornecedor", fornecedor.Id.ToString(), $"Fornecedor {fornecedor.Nome} atualizado.");
                    }
                    TempData["SuccessMessage"] = "Fornecedor atualizado com sucesso!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!FornecedorExists(fornecedor.Id))
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

            return View(fornecedor);
        }

        // GET: Fornecedores/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var fornecedor = await _context.Fornecedores
                .FirstOrDefaultAsync(m => m.Id == id);

            if (fornecedor == null)
            {
                return NotFound();
            }

            return View(fornecedor);
        }

        // POST: Fornecedores/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var fornecedor = await _context.Fornecedores.FindAsync(id);
            if (fornecedor != null)
            {
                // Verifica se o fornecedor possui saídas associadas
                var possuiSaidas = await _context.Saidas.AnyAsync(s => s.FornecedorId == id);
                if (possuiSaidas)
                {
                    TempData["ErrorMessage"] = "Não é possível excluir este fornecedor pois ele possui saídas associadas.";
                    return RedirectToAction(nameof(Index));
                }

                _context.Fornecedores.Remove(fornecedor);
                await _context.SaveChangesAsync();
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    await _auditService.LogAuditAsync(user.Id, "Excluir", "Fornecedor", fornecedor.Id.ToString(), $"Fornecedor {fornecedor.Nome} excluído.");
                }
                TempData["SuccessMessage"] = "Fornecedor excluído com sucesso!";
            }

            return RedirectToAction(nameof(Index));
        }

        // AJAX: Verificar CNPJ
        [HttpGet]
        public async Task<IActionResult> VerificarCNPJ(string cnpj, int? id)
        {
            if (string.IsNullOrEmpty(cnpj))
                return Json(true);

            var query = _context.Fornecedores.Where(f => f.CNPJ == cnpj);

            if (id.HasValue)
            {
                query = query.Where(f => f.Id != id);
            }

            var existe = await query.AnyAsync();
            return Json(!existe);
        }

        // AJAX: Verificar CPF
        [HttpGet]
        public async Task<IActionResult> VerificarCPF(string cpf, int? id)
        {
            if (string.IsNullOrEmpty(cpf))
                return Json(true);

            var query = _context.Fornecedores.Where(f => f.CPF == cpf);

            if (id.HasValue)
            {
                query = query.Where(f => f.Id != id);
            }

            var existe = await query.AnyAsync();
            return Json(!existe);
        }

        // AJAX: Buscar fornecedores por nome
        [HttpGet]
        public async Task<IActionResult> BuscarFornecedores(string term)
        {
            if (string.IsNullOrEmpty(term))
            {
                return Json(new List<object>());
            }

            var fornecedores = await _context.Fornecedores
                .Where(f => f.Nome.Contains(term))
                .Select(f => new
                {
                    id = f.Id,
                    text = f.Nome,
                    documento = f.CNPJ ?? f.CPF,
                    telefone = f.Telefone,
                    email = f.Email
                })
                .Take(10)
                .ToListAsync();

            return Json(fornecedores);
        }

        // AJAX: Buscar CEP
        [HttpGet]
        public async Task<IActionResult> BuscarCEP(string cep)
        {
            if (string.IsNullOrEmpty(cep) || cep.Length != 8)
            {
                return Json(new { success = false, message = "CEP inválido" });
            }

            try
            {
                using var httpClient = new HttpClient();
                var response = await httpClient.GetStringAsync($"https://viacep.com.br/ws/{cep}/json/");

                return Json(new { success = true, data = response });
            }
            catch
            {
                return Json(new { success = false, message = "Erro ao buscar CEP" });
            }
        }

        private bool FornecedorExists(int id)
        {
            return _context.Fornecedores.Any(e => e.Id == id);
        }
    }
}

