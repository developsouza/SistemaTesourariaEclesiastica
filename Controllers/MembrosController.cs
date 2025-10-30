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
    [AuthorizeRoles(Roles.AdminOuTesoureiro)]
    public class MembrosController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly AuditService _auditService;
        private readonly UserManager<ApplicationUser> _userManager;

        public MembrosController(ApplicationDbContext context, AuditService auditService, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _auditService = auditService;
            _userManager = userManager;
        }

        // GET: Membros
        public async Task<IActionResult> Index(string searchString, int? centroCustoId, int page = 1, int pageSize = 10)
        {
            ViewData["CurrentFilter"] = searchString;
            ViewData["CurrentCentroCusto"] = centroCustoId;

            var user = await _userManager.GetUserAsync(User);
            var membros = _context.Membros.Include(m => m.CentroCusto).AsQueryable();

            // ==================== FILTRO POR PERFIL DE USUÁRIO ====================
            if (!User.IsInRole(Roles.Administrador) && !User.IsInRole(Roles.TesoureiroGeral))
            {
                // Tesoureiros Locais só veem membros do seu centro de custo
                if (user?.CentroCustoId.HasValue == true)
                {
                    membros = membros.Where(m => m.CentroCustoId == user.CentroCustoId.Value);
                }
                else
                {
                    // Se não tem centro de custo, não vê nada
                    membros = membros.Where(m => false);
                }
            }
            // Tesoureiro Geral vê todos os membros
            // Administrador vê todos os membros
            // ======================================================================

            if (!string.IsNullOrEmpty(searchString))
            {
                membros = membros.Where(m => m.NomeCompleto.Contains(searchString) ||
                                           m.CPF.Contains(searchString) ||
                                           (m.Apelido != null && m.Apelido.Contains(searchString)));
            }

            if (centroCustoId.HasValue)
            {
                membros = membros.Where(m => m.CentroCustoId == centroCustoId);
            }

            var totalItems = await membros.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalItems = totalItems;

            // Filtrar centros de custo para dropdown baseado no perfil
            var centrosCustoQuery = _context.CentrosCusto.AsQueryable();

            if (!User.IsInRole(Roles.Administrador) && !User.IsInRole(Roles.TesoureiroGeral))
            {
                // Tesoureiros Locais só veem seu centro de custo
                if (user?.CentroCustoId.HasValue == true)
                {
                    centrosCustoQuery = centrosCustoQuery.Where(c => c.Id == user.CentroCustoId.Value);
                }
            }

            ViewBag.CentrosCusto = new SelectList(await centrosCustoQuery.ToListAsync(), "Id", "Nome", centroCustoId);

            var paginatedMembros = await membros
                .OrderBy(m => m.NomeCompleto)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return View(paginatedMembros);
        }

        // GET: Membros/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var membro = await _context.Membros
                .Include(m => m.CentroCusto)
                .Include(m => m.Entradas)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (membro == null)
            {
                return NotFound();
            }

            // Verificar se o usuário tem permissão para ver este membro
            var user = await _userManager.GetUserAsync(User);
            if (!await UsuarioPodeAcessarMembro(user, membro))
            {
                return Forbid();
            }

            return View(membro);
        }

        // GET: Membros/Create
        public async Task<IActionResult> Create()
        {
            var user = await _userManager.GetUserAsync(User);
            var centrosCustoQuery = _context.CentrosCusto.AsQueryable();

            // Filtrar centros de custo baseado no perfil
            if (!User.IsInRole(Roles.Administrador) && !User.IsInRole(Roles.TesoureiroGeral))
            {
                // Tesoureiros Locais só podem cadastrar no seu centro de custo
                if (user?.CentroCustoId.HasValue == true)
                {
                    centrosCustoQuery = centrosCustoQuery.Where(c => c.Id == user.CentroCustoId.Value);
                }
            }

            ViewData["CentroCustoId"] = new SelectList(await centrosCustoQuery.ToListAsync(), "Id", "Nome");

            // Se tesoureiro local, pré-selecionar seu centro de custo
            if (!User.IsInRole(Roles.Administrador) && !User.IsInRole(Roles.TesoureiroGeral) && user?.CentroCustoId.HasValue == true)
            {
                ViewData["CentroCustoIdPreSelecionado"] = user.CentroCustoId.Value;
            }

            return View();
        }

        // POST: Membros/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("NomeCompleto,Apelido,CPF,CentroCustoId")] Membro membro)
        {
            if (ModelState.IsValid)
            {
                // Verificar se o usuário pode cadastrar neste centro de custo
                var user = await _userManager.GetUserAsync(User);
                if (!await UsuarioPodeCadastrarNoCentroCusto(user, membro.CentroCustoId))
                {
                    ModelState.AddModelError("CentroCustoId", "Você não tem permissão para cadastrar membros neste centro de custo.");
                    await PreencherViewDataCentroCusto(user);
                    return View(membro);
                }

                // Verifica se o CPF já existe
                var cpfExistente = await _context.Membros.AnyAsync(m => m.CPF == membro.CPF);
                if (cpfExistente)
                {
                    ModelState.AddModelError("CPF", "Este CPF já está cadastrado.");
                    await PreencherViewDataCentroCusto(user);
                    return View(membro);
                }

                // Define valores padrão
                membro.DataCadastro = DateTime.Now;
                membro.Ativo = true;

                _context.Add(membro);
                await _context.SaveChangesAsync();

                if (user != null)
                {
                    await _auditService.LogAuditAsync(user.Id, "Criar", "Membro", membro.Id.ToString(), $"Membro {membro.NomeCompleto} criado.");
                }

                TempData["SuccessMessage"] = "Membro cadastrado com sucesso!";
                return RedirectToAction(nameof(Index));
            }

            await PreencherViewDataCentroCusto(await _userManager.GetUserAsync(User));
            return View(membro);
        }

        // GET: Membros/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var membro = await _context.Membros.FindAsync(id);
            if (membro == null)
            {
                return NotFound();
            }

            // Verificar se o usuário tem permissão para editar este membro
            var user = await _userManager.GetUserAsync(User);
            if (!await UsuarioPodeAcessarMembro(user, membro))
            {
                return Forbid();
            }

            await PreencherViewDataCentroCusto(user, membro.CentroCustoId);
            return View(membro);
        }

        // POST: Membros/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,NomeCompleto,Apelido,CPF,CentroCustoId,Ativo")] Membro membro)
        {
            if (id != membro.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var user = await _userManager.GetUserAsync(User);

                    // Verificar permissões
                    var membroOriginal = await _context.Membros.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id);
                    if (membroOriginal == null)
                    {
                        return NotFound();
                    }

                    if (!await UsuarioPodeAcessarMembro(user, membroOriginal))
                    {
                        return Forbid();
                    }

                    if (!await UsuarioPodeCadastrarNoCentroCusto(user, membro.CentroCustoId))
                    {
                        ModelState.AddModelError("CentroCustoId", "Você não tem permissão para mover membros para este centro de custo.");
                        await PreencherViewDataCentroCusto(user, membro.CentroCustoId);
                        return View(membro);
                    }

                    // Verifica se o CPF já existe para outro membro
                    var cpfExistente = await _context.Membros.AnyAsync(m => m.CPF == membro.CPF && m.Id != membro.Id);
                    if (cpfExistente)
                    {
                        ModelState.AddModelError("CPF", "Este CPF já está cadastrado para outro membro.");
                        await PreencherViewDataCentroCusto(user, membro.CentroCustoId);
                        return View(membro);
                    }

                    // Preserva a data de cadastro original
                    membro.DataCadastro = membroOriginal.DataCadastro;

                    _context.Update(membro);
                    await _context.SaveChangesAsync();

                    if (user != null)
                    {
                        await _auditService.LogAuditAsync(user.Id, "Editar", "Membro", membro.Id.ToString(), $"Membro {membro.NomeCompleto} atualizado.");
                    }

                    TempData["SuccessMessage"] = "Membro atualizado com sucesso!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MembroExists(membro.Id))
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

            await PreencherViewDataCentroCusto(await _userManager.GetUserAsync(User), membro.CentroCustoId);
            return View(membro);
        }

        // GET: Membros/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var membro = await _context.Membros
                .Include(m => m.CentroCusto)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (membro == null)
            {
                return NotFound();
            }

            // Verificar se o usuário tem permissão para excluir este membro
            var user = await _userManager.GetUserAsync(User);
            if (!await UsuarioPodeAcessarMembro(user, membro))
            {
                return Forbid();
            }

            return View(membro);
        }

        // POST: Membros/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var membro = await _context.Membros.FindAsync(id);
            if (membro != null)
            {
                // Verificar permissões
                var user = await _userManager.GetUserAsync(User);
                if (!await UsuarioPodeAcessarMembro(user, membro))
                {
                    return Forbid();
                }

                // Verifica se o membro possui entradas associadas
                var possuiEntradas = await _context.Entradas.AnyAsync(e => e.MembroId == id);
                if (possuiEntradas)
                {
                    TempData["ErrorMessage"] = "Não é possível excluir este membro pois ele possui entradas associadas.";
                    return RedirectToAction(nameof(Index));
                }

                _context.Membros.Remove(membro);
                await _context.SaveChangesAsync();

                if (user != null)
                {
                    await _auditService.LogAuditAsync(user.Id, "Excluir", "Membro", membro.Id.ToString(), $"Membro {membro.NomeCompleto} excluído.");
                }
                TempData["SuccessMessage"] = "Membro excluído com sucesso!";
            }

            return RedirectToAction(nameof(Index));
        }

        // AJAX: Verificar CPF
        [HttpGet]
        public async Task<IActionResult> VerificarCPF(string cpf, int? id)
        {
            var query = _context.Membros.Where(m => m.CPF == cpf);

            if (id.HasValue)
            {
                query = query.Where(m => m.Id != id);
            }

            var existe = await query.AnyAsync();
            return Json(!existe);
        }

        // AJAX: Buscar membros por nome
        [HttpGet]
        public async Task<IActionResult> BuscarMembros(string term)
        {
            if (string.IsNullOrEmpty(term))
            {
                return Json(new List<object>());
            }

            var user = await _userManager.GetUserAsync(User);
            var membrosQuery = _context.Membros.AsQueryable();

            // Filtrar por centro de custo baseado no perfil
            if (!User.IsInRole(Roles.Administrador) && !User.IsInRole(Roles.TesoureiroGeral))
            {
                if (user?.CentroCustoId.HasValue == true)
                {
                    membrosQuery = membrosQuery.Where(m => m.CentroCustoId == user.CentroCustoId.Value);
                }
                else
                {
                    return Json(new List<object>());
                }
            }

            var membros = await membrosQuery
                .Where(m => m.NomeCompleto.Contains(term) || (m.Apelido != null && m.Apelido.Contains(term)))
                .Select(m => new
                {
                    id = m.Id,
                    text = m.NomeCompleto,
                    apelido = m.Apelido,
                    centroCustoId = m.CentroCustoId,
                    centroCustoNome = m.CentroCusto.Nome
                })
                .Take(10)
                .ToListAsync();

            return Json(membros);
        }

        private bool MembroExists(int id)
        {
            return _context.Membros.Any(e => e.Id == id);
        }

        // Métodos auxiliares para controle de acesso
        private async Task<bool> UsuarioPodeAcessarMembro(ApplicationUser? user, Membro membro)
        {
            if (user == null) return false;

            // Administrador e Tesoureiro Geral têm acesso a tudo
            if (User.IsInRole(Roles.Administrador) || User.IsInRole(Roles.TesoureiroGeral))
            {
                return true;
            }

            // Tesoureiro Local só acessa membros do seu centro de custo
            if (user.CentroCustoId.HasValue)
            {
                return membro.CentroCustoId == user.CentroCustoId.Value;
            }

            return false;
        }

        private async Task<bool> UsuarioPodeCadastrarNoCentroCusto(ApplicationUser? user, int centroCustoId)
        {
            if (user == null) return false;

            // Administrador e Tesoureiro Geral podem cadastrar em qualquer centro de custo
            if (User.IsInRole(Roles.Administrador) || User.IsInRole(Roles.TesoureiroGeral))
            {
                return true;
            }

            // Tesoureiro Local só pode cadastrar no seu centro de custo
            if (user.CentroCustoId.HasValue)
            {
                return centroCustoId == user.CentroCustoId.Value;
            }

            return false;
        }

        private async Task PreencherViewDataCentroCusto(ApplicationUser? user, int? centroCustoIdSelecionado = null)
        {
            var centrosCustoQuery = _context.CentrosCusto.AsQueryable();

            // Filtrar centros de custo baseado no perfil
            if (!User.IsInRole(Roles.Administrador) && !User.IsInRole(Roles.TesoureiroGeral))
            {
                // Tesoureiros Locais só veem seu centro de custo
                if (user?.CentroCustoId.HasValue == true)
                {
                    centrosCustoQuery = centrosCustoQuery.Where(c => c.Id == user.CentroCustoId.Value);
                }
            }

            ViewData["CentroCustoId"] = new SelectList(await centrosCustoQuery.ToListAsync(), "Id", "Nome", centroCustoIdSelecionado);
        }
    }
}

