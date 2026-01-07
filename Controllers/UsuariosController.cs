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
                await _context.CentrosCusto.Where(c => c.Ativo).OrderBy(c => c.Nome).ToListAsync(),
                "Id", "Nome");

            var roles = await _roleManager.Roles.OrderBy(r => r.Name).ToListAsync();
            ViewData["Roles"] = new SelectList(
                roles.Select(r => new { Value = r.Name, Text = FormatRoleName(r.Name) }),
                "Value", "Text");

            return View();
        }

        // POST: Usuarios/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CriarUsuarioViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Verifica se o email já existe
                var usuarioExistente = await _userManager.FindByEmailAsync(model.Email);
                if (usuarioExistente != null)
                {
                    ModelState.AddModelError("Email", "Este e-mail já está cadastrado no sistema.");
                    ViewData["CentroCustoId"] = new SelectList(
                        await _context.CentrosCusto.Where(c => c.Ativo).OrderBy(c => c.Nome).ToListAsync(),
                        "Id", "Nome", model.CentroCustoId);

                    var rolesError = await _roleManager.Roles.OrderBy(r => r.Name).ToListAsync();
                    ViewData["Roles"] = new SelectList(
                        rolesError.Select(r => new { Value = r.Name, Text = FormatRoleName(r.Name) }),
                        "Value", "Text", model.Role);
                    return View(model);
                }

                var usuario = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    NomeCompleto = model.NomeCompleto,
                    CentroCustoId = model.CentroCustoId,
                    Ativo = true,
                    DataCriacao = DateTime.Now,
                    EmailConfirmed = true // Confirmação automática
                };

                var result = await _userManager.CreateAsync(usuario, model.Senha);

                if (result.Succeeded)
                {
                    if (!string.IsNullOrEmpty(model.Role))
                    {
                        await _userManager.AddToRoleAsync(usuario, model.Role);
                    }

                    await _auditService.LogAsync("Criação", "Usuario", $"Usuário {usuario.Email} criado com sucesso");
                    TempData["SuccessMessage"] = "Usuário criado com sucesso!";
                    return RedirectToAction(nameof(Index));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            ViewData["CentroCustoId"] = new SelectList(
                await _context.CentrosCusto.Where(c => c.Ativo).OrderBy(c => c.Nome).ToListAsync(),
                "Id", "Nome", model.CentroCustoId);

            var rolesReturn = await _roleManager.Roles.OrderBy(r => r.Name).ToListAsync();
            ViewData["Roles"] = new SelectList(
                rolesReturn.Select(r => new { Value = r.Name, Text = FormatRoleName(r.Name) }),
                "Value", "Text", model.Role);

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
                await _context.CentrosCusto.Where(c => c.Ativo).OrderBy(c => c.Nome).ToListAsync(),
                "Id", "Nome", model.CentroCustoId);

            var allRoles = await _roleManager.Roles.OrderBy(r => r.Name).ToListAsync();
            ViewData["Roles"] = new SelectList(
                allRoles.Select(r => new { Value = r.Name, Text = FormatRoleName(r.Name) }),
                "Value", "Text", model.Role);

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

                // Atualiza informações básicas
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
                        var passwordResult = await _userManager.ResetPasswordAsync(usuario, token, model.NovaSenha);

                        if (!passwordResult.Succeeded)
                        {
                            foreach (var error in passwordResult.Errors)
                            {
                                ModelState.AddModelError(string.Empty, error.Description);
                            }

                            ViewData["CentroCustoId"] = new SelectList(
                                await _context.CentrosCusto.Where(c => c.Ativo).OrderBy(c => c.Nome).ToListAsync(),
                                "Id", "Nome", model.CentroCustoId);

                            var rolesError = await _roleManager.Roles.OrderBy(r => r.Name).ToListAsync();
                            ViewData["Roles"] = new SelectList(
                                rolesError.Select(r => new { Value = r.Name, Text = FormatRoleName(r.Name) }),
                                "Value", "Text", model.Role);
                            return View(model);
                        }
                    }

                    await _auditService.LogAsync("Edição", "Usuario", $"Usuário {usuario.Email} editado com sucesso");
                    TempData["SuccessMessage"] = "Usuário atualizado com sucesso!";
                    return RedirectToAction(nameof(Index));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            ViewData["CentroCustoId"] = new SelectList(
                await _context.CentrosCusto.Where(c => c.Ativo).OrderBy(c => c.Nome).ToListAsync(),
                "Id", "Nome", model.CentroCustoId);

            var rolesReturn = await _roleManager.Roles.OrderBy(r => r.Name).ToListAsync();
            ViewData["Roles"] = new SelectList(
                rolesReturn.Select(r => new { Value = r.Name, Text = FormatRoleName(r.Name) }),
                "Value", "Text", model.Role);

            return View(model);
        }

        // Adicione este método ao seu UsuariosController.cs

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

            // Verificar se há logs de auditoria associados
            var possuiLogsAuditoria = await _context.LogsAuditoria
                .AnyAsync(la => la.UsuarioId == id);

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

            // Passar informação para a view se há logs de auditoria
            ViewBag.PossuiLogsAuditoria = possuiLogsAuditoria;

            // Contar registros relacionados
            ViewBag.TotalEntradas = await _context.Entradas.CountAsync(e => e.UsuarioId == id);
            ViewBag.TotalSaidas = await _context.Saidas.CountAsync(s => s.UsuarioId == id);
            ViewBag.TotalTransferencias = await _context.TransferenciasInternas.CountAsync(t => t.UsuarioId == id);
            ViewBag.TotalFechamentos = await _context.FechamentosPeriodo.CountAsync(f => f.UsuarioSubmissaoId == id);
            ViewBag.TotalLogsAuditoria = await _context.LogsAuditoria.CountAsync(la => la.UsuarioId == id);

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
                // Verifica se não é o próprio usuário logado
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser?.Id == id)
                {
                    TempData["ErrorMessage"] = "Você não pode excluir sua própria conta enquanto está logado.";
                    return RedirectToAction(nameof(Index));
                }

                // Verificar se há logs de auditoria (impede exclusão para preservar histórico)
                var possuiLogsAuditoria = await _context.LogsAuditoria.AnyAsync(la => la.UsuarioId == id);
                if (possuiLogsAuditoria)
                {
                    TempData["ErrorMessage"] = "Não é possível excluir este usuário pois existem logs de auditoria associados. Para preservar o histórico, desative o usuário ao invés de excluí-lo.";
                    return RedirectToAction(nameof(Index));
                }

                var result = await _userManager.DeleteAsync(usuario);
                if (result.Succeeded)
                {
                    await _auditService.LogAsync("Exclusão", "Usuario", $"Usuário {usuario.Email} excluído com sucesso");
                    TempData["SuccessMessage"] = "Usuário excluído com sucesso!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Erro ao excluir usuário: " + string.Join(", ", result.Errors.Select(e => e.Description));
                }
            }
            else
            {
                TempData["ErrorMessage"] = "Usuário não encontrado.";
            }

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Formata o nome da role para exibição com espaçamento adequado
        /// </summary>
        private string FormatRoleName(string roleName)
        {
            return roleName switch
            {
                "Administrador" => "Administrador",
                "TesoureiroGeral" => "Tesoureiro Geral",
                "TesoureiroLocal" => "Tesoureiro Local",
                "Pastor" => "Pastor",
                _ => roleName
            };
        }
    }
}