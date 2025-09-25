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
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;
        private readonly AuditService _auditService;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext context,
            AuditService auditService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _auditService = auditService;
        }

        // GET: Account/Login
        [AllowAnonymous]
        public IActionResult Login(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // POST: Account/Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);
                
                if (result.Succeeded)
                {
                    var user = await _userManager.FindByEmailAsync(model.Email);
                    await _auditService.LogAsync("Login", "Account", $"Usuário {user.NomeCompleto} fez login");
                    
                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    {
                        return Redirect(returnUrl);
                    }
                    return RedirectToAction("Index", "Home");
                }
                
                if (result.IsLockedOut)
                {
                    ModelState.AddModelError(string.Empty, "Conta bloqueada temporariamente.");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Email ou senha inválidos.");
                }
            }

            return View(model);
        }

        // GET: Account/Register
        [AllowAnonymous]
        public async Task<IActionResult> Register()
        {
            await PopulateDropdowns();
            return View();
        }

        // POST: Account/Register
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    NomeCompleto = model.NomeCompleto,
                    CentroCustoId = model.CentroCustoId,
                    Ativo = true,
                    DataCriacao = DateTime.Now
                };

                var result = await _userManager.CreateAsync(user, model.Password);
                
                if (result.Succeeded)
                {
                    // Adicionar role padrão
                    await _userManager.AddToRoleAsync(user, "Usuario");
                    
                    await _auditService.LogAsync("Registro", "Account", $"Novo usuário registrado: {user.NomeCompleto}");
                    
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return RedirectToAction("Index", "Home");
                }
                
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            await PopulateDropdowns(model);
            return View(model);
        }

        // POST: Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                await _auditService.LogAsync("Logout", "Account", $"Usuário {user.NomeCompleto} fez logout");
            }
            
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        // GET: Account/AccessDenied
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }

        // GET: Account/ForgotPassword
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        // POST: Account/ForgotPassword
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null)
                {
                    // Não revelar que o usuário não existe
                    return RedirectToAction(nameof(ForgotPasswordConfirmation));
                }

                // Aqui você implementaria o envio de email
                // Por enquanto, apenas redireciona para confirmação
                return RedirectToAction(nameof(ForgotPasswordConfirmation));
            }

            return View(model);
        }

        // GET: Account/ForgotPasswordConfirmation
        [AllowAnonymous]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        private async Task PopulateDropdowns(RegisterViewModel model = null)
        {
            ViewData["CentroCustoId"] = new SelectList(
                await _context.CentrosCusto.Where(c => c.Ativo).OrderBy(c => c.Nome).ToListAsync(),
                "Id", "Nome", model?.CentroCustoId);
        }
    }
}
