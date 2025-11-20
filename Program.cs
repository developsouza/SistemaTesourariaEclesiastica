using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using SistemaTesourariaEclesiastica.Data;
using SistemaTesourariaEclesiastica.Middleware;
using SistemaTesourariaEclesiastica.Models;
using SistemaTesourariaEclesiastica.Services;
using Microsoft.AspNetCore.ResponseCompression;
using System.IO.Compression;

var builder = WebApplication.CreateBuilder(args);

// ✅ OTIMIZAÇÃO: Response Compression
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<GzipCompressionProvider>();
    options.Providers.Add<BrotliCompressionProvider>();
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[]
    {
        "application/json",
   "application/javascript",
        "text/css",
 "text/html",
        "text/plain"
    });
});

builder.Services.Configure<GzipCompressionProviderOptions>(options =>
{
    options.Level = CompressionLevel.Fastest;
});

builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
{
    options.Level = CompressionLevel.Fastest;
});

// ✅ OTIMIZAÇÃO: Output Caching (novo no .NET 9)
builder.Services.AddOutputCache(options =>
{
    options.AddBasePolicy(policy => policy.Expire(TimeSpan.FromMinutes(5)));
    options.AddPolicy("StaticContent", policy => policy.Expire(TimeSpan.FromHours(1)));
});

// ✅ OTIMIZAÇÃO: Memory Cache
builder.Services.AddMemoryCache();

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
  throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// ✅ OTIMIZAÇÃO: Configuração do Entity Framework com pooling e timeout otimizado
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
  options.UseSqlServer(connectionString, sqlOptions =>
 {
        sqlOptions.CommandTimeout(30);
        sqlOptions.EnableRetryOnFailure(
     maxRetryCount: 3,
 maxRetryDelay: TimeSpan.FromSeconds(5),
    errorNumbersToAdd: null);
    });
 
 // ✅ Melhor performance em produção
    if (!builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging(false);
        options.EnableDetailedErrors(false);
    }
});

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
builder.Services.AddScoped<AuditService>();
builder.Services.AddScoped<BusinessRulesService>();
builder.Services.AddScoped<PdfService>();
builder.Services.AddScoped<BalanceteService>();

// ✅ OTIMIZAÇÃO: Remover LancamentoAprovadoService (substituído pelo FechamentoQueryHelper que é mais eficiente)
// builder.Services.AddScoped<LancamentoAprovadoService>();


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

// ✅ OTIMIZAÇÃO: Usar Response Compression no início do pipeline
app.UseResponseCompression();

// ✅ OTIMIZAÇÃO: Adicionar Output Cache
app.UseOutputCache();

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

// ✅ OTIMIZAÇÃO: Cache estático para arquivos wwwroot
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        const int durationInSeconds = 60 * 60 * 24 * 30; // 30 dias
        ctx.Context.Response.Headers["Cache-Control"] = $"public,max-age={durationInSeconds}";
    }
});

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
app.UseAccessControl();

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