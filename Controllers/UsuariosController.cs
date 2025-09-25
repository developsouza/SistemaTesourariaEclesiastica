using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SistemaTesourariaEclesiastica.Data;
using SistemaTesourariaEclesiastica.Models;
using SistemaTesourariaEclesiastica.Services;
using SistemaTesourariaEclesiastica.ViewModels;

namespace SistemaTesourariaEclesiastica.Controllers
{
    [Authorize]
    public class UsuariosController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly AuditService _auditService;

        public UsuariosController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            AuditService auditService)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _auditService = auditService;
        }

        // GET: Usuarios
        public async Task<IActionResult> Index()
        {
            var usuarios = await _userManager.Users
                .Include(u => u.CentroCusto)
                .ToListAsync();

            var usuariosViewModel = new List<UsuarioViewModel>();

            foreach (var usuario in usuarios)
            {
                var roles = await _userManager.GetRolesAsync(usuario);
                usuariosViewModel.Add(new UsuarioViewModel
                {
                    Id = usuario.Id,
                    Email = usuario.Email!,
                    NomeCompleto = usuario.NomeCompleto,
                    CentroCustoNome = usuario.CentroCusto?.Nome ?? "Não definido",
                    Ativo = usuario.Ativo,
                    DataCriacao = usuario.DataCriacao,
                    Roles = roles.ToList()
                });
            }

            await _auditService.LogAsync("Visualização", "Usuario", "Listagem de usuários visualizada");
            return View(usuariosViewModel);
        }

        // GET: Usuarios/Details/5
        public async Task<IActionResult> Details(string? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var usuario = await _userManager.Users
                .Include(u => u.CentroCusto)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (usuario == null)
            {
                return NotFound();
            }

            var roles = await _userManager.GetRolesAsync(usuario);
            var viewModel = new UsuarioViewModel
            {
                Id = usuario.Id,
                Email = usuario.Email!,
                NomeCompleto = usuario.NomeCompleto,
                CentroCustoNome = usuario.CentroCusto?.Nome ?? "Não definido",
                Ativo = usuario.Ativo,
                DataCriacao = usuario.DataCriacao,
                Roles = roles.ToList()
            };

            await _auditService.LogAsync("Visualização", "Usuario", $"Detalhes do usuário {usuario.Email} visualizados");
            return View(viewModel);
        }

        // GET: Usuarios/Create
        public async Task<IActionResult> Create()
        {
            ViewData["CentroCustoId"] = new SelectList(
                await _context.CentrosCusto.Where(c => c.Ativo).ToListAsync(),
                "Id", "Nome");

            ViewData["Roles"] = new SelectList(
                await _roleManager.Roles.ToListAsync(),
                "Name", "Name");

            return View();
        }

        // POST: Usuarios/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CriarUsuarioViewModel model)
        {
            if (ModelState.IsValid)
            {
                var usuario = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    NomeCompleto = model.NomeCompleto,
                    CentroCustoId = model.CentroCustoId,
                    Ativo = true,
                    DataCriacao = DateTime.Now
                };

                var result = await _userManager.CreateAsync(usuario, model.Senha);

                if (result.Succeeded)
                {
                    if (!string.IsNullOrEmpty(model.Role))
                    {
                        await _userManager.AddToRoleAsync(usuario, model.Role);
                    }

                    await _auditService.LogAsync("Criação", "Usuario", $"Usuário {usuario.Email} criado");
                    TempData["SuccessMessage"] = "Usuário criado com sucesso!";
                    return RedirectToAction(nameof(Index));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            ViewData["CentroCustoId"] = new SelectList(
                await _context.CentrosCusto.Where(c => c.Ativo).ToListAsync(),
                "Id", "Nome", model.CentroCustoId);

            ViewData["Roles"] = new SelectList(
                await _roleManager.Roles.ToListAsync(),
                "Name", "Name", model.Role);

            return View(model);
        }

        // GET: Usuarios/Edit/5
        public async Task<IActionResult> Edit(string? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var usuario = await _userManager.FindByIdAsync(id);
            if (usuario == null)
            {
                return NotFound();
            }

            var roles = await _userManager.GetRolesAsync(usuario);
            var model = new EditarUsuarioViewModel
            {
                Id = usuario.Id,
                Email = usuario.Email!,
                NomeCompleto = usuario.NomeCompleto,
                CentroCustoId = usuario.CentroCustoId,
                Ativo = usuario.Ativo,
                Role = roles.FirstOrDefault()
            };

            ViewData["CentroCustoId"] = new SelectList(
                await _context.CentrosCusto.Where(c => c.Ativo).ToListAsync(),
                "Id", "Nome", model.CentroCustoId);

            ViewData["Roles"] = new SelectList(
                await _roleManager.Roles.ToListAsync(),
                "Name", "Name", model.Role);

            return View(model);
        }

        // POST: Usuarios/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, EditarUsuarioViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var usuario = await _userManager.FindByIdAsync(id);
                if (usuario == null)
                {
                    return NotFound();
                }

                usuario.NomeCompleto = model.NomeCompleto;
                usuario.CentroCustoId = model.CentroCustoId;
                usuario.Ativo = model.Ativo;

                var result = await _userManager.UpdateAsync(usuario);

                if (result.Succeeded)
                {
                    // Atualizar roles
                    var currentRoles = await _userManager.GetRolesAsync(usuario);
                    await _userManager.RemoveFromRolesAsync(usuario, currentRoles);

                    if (!string.IsNullOrEmpty(model.Role))
                    {
                        await _userManager.AddToRoleAsync(usuario, model.Role);
                    }

                    // Atualizar senha se fornecida
                    if (!string.IsNullOrEmpty(model.NovaSenha))
                    {
                        var token = await _userManager.GeneratePasswordResetTokenAsync(usuario);
                        await _userManager.ResetPasswordAsync(usuario, token, model.NovaSenha);
                    }

                    await _auditService.LogAsync("Edição", "Usuario", $"Usuário {usuario.Email} editado");
                    TempData["SuccessMessage"] = "Usuário atualizado com sucesso!";
                    return RedirectToAction(nameof(Index));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            ViewData["CentroCustoId"] = new SelectList(
                await _context.CentrosCusto.Where(c => c.Ativo).ToListAsync(),
                "Id", "Nome", model.CentroCustoId);

            ViewData["Roles"] = new SelectList(
                await _roleManager.Roles.ToListAsync(),
                "Name", "Name", model.Role);

            return View(model);
        }

        // GET: Usuarios/Delete/5
        public async Task<IActionResult> Delete(string? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var usuario = await _userManager.Users
                .Include(u => u.CentroCusto)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (usuario == null)
            {
                return NotFound();
            }

            var roles = await _userManager.GetRolesAsync(usuario);
            var viewModel = new UsuarioViewModel
            {
                Id = usuario.Id,
                Email = usuario.Email!,
                NomeCompleto = usuario.NomeCompleto,
                CentroCustoNome = usuario.CentroCusto?.Nome ?? "Não definido",
                Ativo = usuario.Ativo,
                DataCriacao = usuario.DataCriacao,
                Roles = roles.ToList()
            };

            return View(viewModel);
        }

        // POST: Usuarios/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var usuario = await _userManager.FindByIdAsync(id);
            if (usuario != null)
            {
                var result = await _userManager.DeleteAsync(usuario);
                if (result.Succeeded)
                {
                    await _auditService.LogAsync("Exclusão", "Usuario", $"Usuário {usuario.Email} excluído");
                    TempData["SuccessMessage"] = "Usuário excluído com sucesso!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Erro ao excluir usuário.";
                }
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
