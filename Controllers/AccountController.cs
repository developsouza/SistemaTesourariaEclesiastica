using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SistemaTesourariaEclesiastica.Data;
using SistemaTesourariaEclesiastica.Models;
using SistemaTesourariaEclesiastica.Services;
using SistemaTesourariaEclesiastica.ViewModels;
using SistemaTesourariaEclesiastica.Attributes;

namespace SistemaTesourariaEclesiastica.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;
        private readonly AuditService _auditService;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext context,
            AuditService auditService,
            ILogger<AccountController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _auditService = auditService;
            _logger = logger;
        }

        // GET: Account/Login
        [AllowAnonymous]
        public IActionResult Login(string returnUrl = null)
        {
            // Se já estiver logado, redirecionar para dashboard
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel());
        }

        // POST: Account/Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null)
                {
                    ModelState.AddModelError(string.Empty, "Email ou senha inválidos.");
                    await _auditService.LogAsync("LOGIN_FAILED", "Account", $"Tentativa de login com email inexistente: {model.Email}");
                    return View(model);
                }

                // Verificar se o usuário está ativo
                if (!user.Ativo)
                {
                    ModelState.AddModelError(string.Empty, "Sua conta está desativada. Entre em contato com o administrador.");
                    await _auditService.LogAuditAsync(user.Id, "LOGIN_FAILED", "Account", "0", "Tentativa de login com usuário desativado");
                    return View(model);
                }

                var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: true);

                if (result.Succeeded)
                {
                    _logger.LogInformation($"Usuário {user.Email} logou com sucesso.");
                    await _auditService.LogAsync("LOGIN_SUCCESS", "Account", $"Login bem-sucedido para {user.NomeCompleto}");

                    // Atualizar último acesso
                    user.UltimoAcesso = DateTime.Now;
                    await _userManager.UpdateAsync(user);

                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    {
                        return Redirect(returnUrl);
                    }
                    return RedirectToAction("Index", "Home");
                }

                if (result.IsLockedOut)
                {
                    _logger.LogWarning($"Conta do usuário {user.Email} foi bloqueada.");
                    ModelState.AddModelError(string.Empty, "Sua conta foi bloqueada temporariamente devido a múltiplas tentativas de login. Tente novamente mais tarde.");
                    await _auditService.LogAuditAsync(user.Id, "LOGIN_FAILED", "Account", "0", "Conta bloqueada por tentativas excessivas");
                }
                else if (result.IsNotAllowed)
                {
                    ModelState.AddModelError(string.Empty, "Login não permitido. Verifique se sua conta foi confirmada.");
                    await _auditService.LogAuditAsync(user.Id, "LOGIN_FAILED", "Account", "0", "Login não permitido");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Email ou senha inválidos.");
                    await _auditService.LogAuditAsync(user.Id, "LOGIN_FAILED", "Account", "0", "Credenciais inválidas");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro durante login para {model.Email}");
                ModelState.AddModelError(string.Empty, "Ocorreu um erro interno. Tente novamente.");
            }

            return View(model);
        }

        // POST: Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    await _auditService.LogAsync("LOGOUT", "Account", $"Logout realizado por {user.NomeCompleto}");
                    _logger.LogInformation($"Usuário {user.Email} fez logout.");
                }

                await _signInManager.SignOutAsync();
                return RedirectToAction("Login", "Account");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro durante logout");
                return RedirectToAction("Login", "Account");
            }
        }

        // GET: Account/AccessDenied
        [AllowAnonymous]
        public async Task<IActionResult> AccessDenied(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (User.Identity.IsAuthenticated)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    await _auditService.LogAsync("ACCESS_DENIED", "Account",
                        $"Acesso negado para {returnUrl ?? "página não especificada"}");
                }
            }

            return View();
        }

        // GET: Account/Profile
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.Users
                .Include(u => u.CentroCusto)
                .FirstOrDefaultAsync(u => u.Id == _userManager.GetUserId(User));

            if (user == null)
            {
                return NotFound();
            }

            var roles = await _userManager.GetRolesAsync(user);

            var model = new ProfileViewModel
            {
                Id = user.Id,
                Email = user.Email,
                NomeCompleto = user.NomeCompleto,
                CentroCustoNome = user.CentroCusto?.Nome,
                DataCriacao = user.DataCriacao,
                UltimoAcesso = user.UltimoAcesso,
                Roles = roles.ToList()
            };

            return View(model);
        }

        // GET: Account/ChangePassword
        [Authorize]
        public IActionResult ChangePassword()
        {
            return View(new ChangePasswordViewModel());
        }

        // POST: Account/ChangePassword
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var result = await _userManager.ChangePasswordAsync(user, model.SenhaAtual, model.NovaSenha);

            if (result.Succeeded)
            {
                await _signInManager.RefreshSignInAsync(user);
                await _auditService.LogAsync("PASSWORD_CHANGED", "Account", "Senha alterada pelo usuário");

                TempData["Sucesso"] = "Senha alterada com sucesso!";
                return RedirectToAction("Profile");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        // GET: Account/GetUserInfo - Endpoint AJAX para informações do usuário
        [Authorize]
        public async Task<IActionResult> GetUserInfo()
        {
            var user = await _userManager.Users
                .Include(u => u.CentroCusto)
                .FirstOrDefaultAsync(u => u.Id == _userManager.GetUserId(User));

            if (user == null)
            {
                return NotFound();
            }

            var roles = await _userManager.GetRolesAsync(user);

            return Json(new
            {
                id = user.Id,
                email = user.Email,
                nomeCompleto = user.NomeCompleto,
                centroCusto = user.CentroCusto?.Nome,
                roles = roles,
                ultimoAcesso = user.UltimoAcesso?.ToString("dd/MM/yyyy HH:mm")
            });
        }

        // Método privado para validar força da senha
        private bool IsPasswordStrong(string password)
        {
            if (string.IsNullOrEmpty(password) || password.Length < 8)
                return false;

            bool hasUpper = password.Any(c => char.IsUpper(c));
            bool hasLower = password.Any(c => char.IsLower(c));
            bool hasDigit = password.Any(c => char.IsDigit(c));
            bool hasSpecial = password.Any(c => !char.IsLetterOrDigit(c));

            return hasUpper && hasLower && hasDigit && (hasSpecial || password.Length >= 10);
        }
    }
}