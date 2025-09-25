using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SistemaTesourariaEclesiastica.Data;
using SistemaTesourariaEclesiastica.Models;
using SistemaTesourariaEclesiastica.Middleware;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
    throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

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

    // Nome do cookie
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
    options.ModelBindingMessageProvider.SetValueMustBeANumberAccessor(x => $"O campo {x} deve ser um número.");
    options.ModelBindingMessageProvider.SetMissingBindRequiredValueAccessor(x => $"O campo {x} é obrigatório.");
});

// Configurações de autorização personalizadas
builder.Services.AddAuthorization(options =>
{
    // Política para Administradores
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("Administrador"));

    // Política para Tesoureiros (Geral e Local)
    options.AddPolicy("Tesoureiros", policy =>
        policy.RequireRole("Administrador", "TesoureiroGeral", "TesoureiroLocal"));

    // Política para acesso a relatórios
    options.AddPolicy("Relatorios", policy =>
        policy.RequireRole("Administrador", "TesoureiroGeral", "TesoureiroLocal", "Pastor"));

    // Política para operações financeiras
    options.AddPolicy("OperacoesFinanceiras", policy =>
        policy.RequireRole("Administrador", "TesoureiroGeral", "TesoureiroLocal"));

    // Política para aprovação de prestações de contas
    options.AddPolicy("AprovacaoPrestacoes", policy =>
        policy.RequireRole("Administrador", "TesoureiroGeral"));

    // Política para gerenciamento de usuários
    options.AddPolicy("GerenciarUsuarios", policy =>
        policy.RequireRole("Administrador"));

    // Política para auditoria
    options.AddPolicy("Auditoria", policy =>
        policy.RequireRole("Administrador"));
});

// Configurações adicionais
builder.Services.Configure<RouteOptions>(options =>
{
    options.LowercaseUrls = true;
    options.LowercaseQueryStrings = false;
});

// Configuração de logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

if (builder.Environment.IsProduction())
{
    builder.Logging.SetMinimumLevel(LogLevel.Warning);
}
else
{
    builder.Logging.SetMinimumLevel(LogLevel.Information);
}

var app = builder.Build();

// Inicialização do banco de dados e roles
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        await context.Database.EnsureCreatedAsync();
        logger.LogInformation("Banco de dados inicializado com sucesso.");

        // Inicializar roles e usuário administrador
        await SistemaTesourariaEclesiastica.Services.RoleInitializerService.InitializeAsync(app.Services);
        logger.LogInformation("Roles e usuário administrador inicializados com sucesso.");

        // Remover roles antigas se necessário
        if (app.Environment.IsDevelopment())
        {
            await SistemaTesourariaEclesiastica.Services.RoleInitializerService.RemoverRolesAntigasAsync(app.Services);
            logger.LogInformation("Roles antigas removidas com sucesso.");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Erro durante a inicialização do banco de dados ou roles.");
        throw; // Re-throw para interromper a aplicação em caso de erro crítico
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Middleware de segurança
app.UseHttpsRedirection();
app.UseStaticFiles();

// Middleware de roteamento
app.UseRouting();

// Middleware de autenticação e autorização
app.UseAuthentication();
app.UseAuthorization();

// Middleware personalizado para controle de acesso
app.UseAccessControl();

// Middleware personalizado para controle de centro de custo
app.UseCentroCustoAccess();

// Middleware personalizado para redirecionamento de usuários não autenticados
app.Use(async (context, next) =>
{
    // Se não está autenticado e não está tentando acessar uma página de autenticação
    if (!context.User.Identity.IsAuthenticated &&
        !context.Request.Path.StartsWithSegments("/Account") &&
        !context.Request.Path.StartsWithSegments("/css") &&
        !context.Request.Path.StartsWithSegments("/js") &&
        !context.Request.Path.StartsWithSegments("/lib") &&
        !context.Request.Path.StartsWithSegments("/favicon") &&
        !context.Request.Path.StartsWithSegments("/images"))
    {
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Redirecionando usuário não autenticado para login: {Path}", context.Request.Path);

        context.Response.Redirect("/Account/Login");
        return;
    }

    await next();
});

// Configuração das rotas
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Health check endpoint
app.MapGet("/health", () => new { status = "healthy", timestamp = DateTime.UtcNow });

// Configuração de graceful shutdown
var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
lifetime.ApplicationStopping.Register(() =>
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Aplicação sendo finalizada...");
});

// Log de inicialização
var startupLogger = app.Services.GetRequiredService<ILogger<Program>>();
startupLogger.LogInformation("Sistema de Tesouraria Eclesiástica iniciado em {Environment} mode",
    app.Environment.EnvironmentName);

if (app.Environment.IsDevelopment())
{
    startupLogger.LogInformation("Usuários padrão criados:");
    startupLogger.LogInformation("- Administrador: admin@tesouraria.com / Admin@123");
    startupLogger.LogInformation("- Tesoureiro Geral: tesoureiro@tesouraria.com / Tesoureiro@123");
    startupLogger.LogInformation("- Tesoureiro Local: local@tesouraria.com / Local@123");
    startupLogger.LogInformation("- Pastor: pastor@tesouraria.com / Pastor@123");
}

app.Run();