using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using SistemaTesourariaEclesiastica.Data;
using SistemaTesourariaEclesiastica.Middleware;
using SistemaTesourariaEclesiastica.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
    throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// Configuração do Entity Framework para SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Configuração do Identity com roles
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Configurações de senha
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 6;
    options.Password.RequiredUniqueChars = 1;

    // Configurações de usuário
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedAccount = false;

    // Configurações de lockout
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Configuração dos cookies de autenticação
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.Name = "TesourariaAuth";

    // Configurações de evento
    options.Events.OnRedirectToLogin = context =>
    {
        // Para requisições AJAX, retornar 401 ao invés de redirect
        if (context.Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            context.Response.StatusCode = 401;
            return Task.CompletedTask;
        }
        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };
});

// ========================================
// CONFIGURAÇÕES DE AUTORIZAÇÃO - CRÍTICO!
// ========================================
builder.Services.AddAuthorization(options =>
{
    // Política apenas para administradores
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("Administrador"));

    // Política para tesoureiros (inclui admin)
    options.AddPolicy("Tesoureiros", policy =>
        policy.RequireRole("Administrador", "TesoureiroGeral", "TesoureiroLocal"));

    // Política para relatórios (todos menos usuários básicos)
    options.AddPolicy("Relatorios", policy =>
        policy.RequireRole("Administrador", "TesoureiroGeral", "TesoureiroLocal", "Pastor"));

    // Política para operações financeiras (entradas, saídas, etc.)
    options.AddPolicy("OperacoesFinanceiras", policy =>
        policy.RequireRole("Administrador", "TesoureiroGeral", "TesoureiroLocal"));

    // Política para aprovação de prestações de contas
    options.AddPolicy("AprovacaoPrestacoes", policy =>
        policy.RequireRole("Administrador", "TesoureiroGeral"));

    // Política para gerenciar usuários
    options.AddPolicy("GerenciarUsuarios", policy =>
        policy.RequireRole("Administrador"));

    // Política para auditoria
    options.AddPolicy("Auditoria", policy =>
        policy.RequireRole("Administrador"));
});

// Serviços da aplicação
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<SistemaTesourariaEclesiastica.Services.AuditService>();
builder.Services.AddScoped<SistemaTesourariaEclesiastica.Services.BusinessRulesService>();
builder.Services.AddScoped<SistemaTesourariaEclesiastica.Services.PdfService>();

// Configuração do MVC com filtro global de autorização
builder.Services.AddControllersWithViews(options =>
{
    // Aplicar filtro de autorização globalmente
    var policy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
    options.Filters.Add(new AuthorizeFilter(policy));

    // Configuração de model binding
    options.ModelBindingMessageProvider.SetValueIsInvalidAccessor(x => $"O valor '{x}' é inválido.");
    options.ModelBindingMessageProvider.SetValueMustBeANumberAccessor(x => $"O campo '{x}' deve ser um número.");
    options.ModelBindingMessageProvider.SetMissingBindRequiredValueAccessor(x => $"O campo '{x}' é obrigatório.");
    options.ModelBindingMessageProvider.SetAttemptedValueIsInvalidAccessor((x, y) => $"O valor '{x}' não é válido para {y}.");
    options.ModelBindingMessageProvider.SetMissingKeyOrValueAccessor(() => "Valor obrigatório.");
    options.ModelBindingMessageProvider.SetUnknownValueIsInvalidAccessor(x => $"O valor fornecido é inválido para {x}.");
    options.ModelBindingMessageProvider.SetValueIsInvalidAccessor(x => $"O valor '{x}' é inválido.");
    options.ModelBindingMessageProvider.SetMissingRequestBodyRequiredValueAccessor(() => "O corpo da requisição é obrigatório.");
    options.ModelBindingMessageProvider.SetNonPropertyAttemptedValueIsInvalidAccessor(x => $"O valor '{x}' não é válido.");
    options.ModelBindingMessageProvider.SetNonPropertyUnknownValueIsInvalidAccessor(() => "O valor fornecido é inválido.");
    options.ModelBindingMessageProvider.SetNonPropertyValueMustBeANumberAccessor(() => "O campo deve ser um número.");
});

// Configuração de localização para português brasileiro
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture("pt-BR");
    options.SupportedCultures = new List<System.Globalization.CultureInfo> { new("pt-BR") };
    options.SupportedUICultures = new List<System.Globalization.CultureInfo> { new("pt-BR") };
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Configuração de localização
app.UseRequestLocalization();

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Middleware personalizado de auditoria
app.UseAuditMiddleware();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Seed de dados inicial
using (var scope = app.Services.CreateScope())
{
    var serviceProvider = scope.ServiceProvider;
    try
    {
        var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        // Aplica migrations automaticamente
        await context.Database.MigrateAsync();

        // Seed de dados iniciais
        await SeedData.Initialize(serviceProvider, userManager, roleManager);
    }
    catch (Exception ex)
    {
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Erro ao inicializar dados do banco.");
    }
}

app.Run();