using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using SistemaTesourariaEclesiastica.Data;
using SistemaTesourariaEclesiastica.Middleware;
using SistemaTesourariaEclesiastica.Models;
using SistemaTesourariaEclesiastica.Services;
using System.Globalization;
using System.Text;

// ========================================
// CONFIGURAÇÃO CRÍTICA - LOCALIZAÇÃO BRASIL
// ========================================
// Definir TimeZone padrão para Brasília (UTC-3)
// Isso garante que as datas funcionem corretamente mesmo em servidores AWS (UTC)
try
{
    TimeZoneInfo brasilTimeZone = TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time");
    Environment.SetEnvironmentVariable("TZ", brasilTimeZone.Id);
    Console.WriteLine($"? TimeZone configurado: {brasilTimeZone.DisplayName}");
}
catch (Exception ex)
{
    // Fallback para sistemas Linux/Unix
    try
    {
        TimeZoneInfo brasilTimeZone = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
        Environment.SetEnvironmentVariable("TZ", brasilTimeZone.Id);
        Console.WriteLine($"? TimeZone configurado (Linux): {brasilTimeZone.DisplayName}");
    }
    catch
    {
        Console.WriteLine($"?? Aviso: Não foi possível configurar TimeZone automaticamente. Detalhes: {ex.Message}");
    }
}

// Definir cultura padrão brasileira para toda a aplicação
var culturaBrasil = new CultureInfo("pt-BR");
CultureInfo.DefaultThreadCurrentCulture = culturaBrasil;
CultureInfo.DefaultThreadCurrentUICulture = culturaBrasil;
Thread.CurrentThread.CurrentCulture = culturaBrasil;
Thread.CurrentThread.CurrentUICulture = culturaBrasil;

// CORRECAO: Definir encoding UTF-8 como padrao para toda a aplicacao
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

var builder = WebApplication.CreateBuilder(args);

// OTIMIZACAO: Memory Cache
builder.Services.AddMemoryCache();

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
  throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// OTIMIZACAO: Configuracao do Entity Framework com pooling e timeout otimizado
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

    // Melhor performance em producao
    if (!builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging(false);
        options.EnableDetailedErrors(false);
    }
});

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Configuracao do Identity com roles
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Configuracoes de senha
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 6;
    options.Password.RequiredUniqueChars = 1;

    // Configuracoes de usuario
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedAccount = false;

    // Configuracoes de lockout
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Configuracao dos cookies de autenticacao
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

    // Configuracoes de evento
    options.Events.OnRedirectToLogin = context =>
    {
        // Para requisicoes AJAX, retornar 401 ao inves de redirect
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
// CONFIGURACOES DE AUTORIZACAO - CRITICO!
// ========================================
builder.Services.AddAuthorization(options =>
{
    // Politica apenas para administradores
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("Administrador"));

    // Politica para tesoureiros (inclui admin)
    options.AddPolicy("Tesoureiros", policy =>
        policy.RequireRole("Administrador", "TesoureiroGeral", "TesoureiroLocal"));

    // Politica para relatorios (todos menos usuarios basicos) - PASTOR TEM ACESSO
    options.AddPolicy("Relatorios", policy =>
        policy.RequireRole("Administrador", "TesoureiroGeral", "TesoureiroLocal", "Pastor"));

    // Politica para operacoes financeiras (entradas, saidas, etc.) - PASTOR NÃO TEM ACESSO
    options.AddPolicy("OperacoesFinanceiras", policy =>
        policy.RequireRole("Administrador", "TesoureiroGeral", "TesoureiroLocal"));

    // Politica para aprovacao de prestacoes de contas
    options.AddPolicy("AprovacaoPrestacoes", policy =>
        policy.RequireRole("Administrador", "TesoureiroGeral"));

    // Politica para gerenciar usuarios
    options.AddPolicy("GerenciarUsuarios", policy =>
        policy.RequireRole("Administrador"));

    // Politica para auditoria
    options.AddPolicy("Auditoria", policy =>
        policy.RequireRole("Administrador"));
});

// Servicos da aplicacao
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<AuditService>();
builder.Services.AddScoped<BusinessRulesService>();
builder.Services.AddScoped<PdfService>();
builder.Services.AddScoped<BalanceteService>();
builder.Services.AddScoped<EscalaPorteiroService>();
builder.Services.AddScoped<RateLimitService>();
builder.Services.AddScoped<ConsistenciaService>();

// SERVICO DE AUDITORIA EM BACKGROUND
// Registrado como Singleton para que seja compartilhado e como HostedService para rodar em background
builder.Services.AddSingleton<AuditQueueService>();
builder.Services.AddHostedService(provider => provider.GetRequiredService<AuditQueueService>());

// SERVICO DE LIMPEZA DE RATE LIMITING EM BACKGROUND
builder.Services.AddHostedService<SistemaTesourariaEclesiastica.BackgroundServices.RateLimitCleanupService>();

// SERVICO DE VERIFICACAO DE CONSISTENCIA EM BACKGROUND
builder.Services.AddHostedService<SistemaTesourariaEclesiastica.BackgroundServices.ConsistenciaBackgroundService>();


builder.Services.AddControllersWithViews(options =>
{
    // Aplicar filtro de autorizacao globalmente
    var policy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
    options.Filters.Add(new AuthorizeFilter(policy));

    // ADICIONAR: Filtro de auditoria global (substitui o middleware problematico)
    options.Filters.Add<SistemaTesourariaEclesiastica.Filters.AuditActionFilter>();

    // Configuracao de model binding
    options.ModelBindingMessageProvider.SetValueIsInvalidAccessor(x => $"O valor '{x}' e invalido.");
    options.ModelBindingMessageProvider.SetValueMustBeANumberAccessor(x => $"O campo '{x}' deve ser um numero.");
    options.ModelBindingMessageProvider.SetMissingBindRequiredValueAccessor(x => $"O campo '{x}' e obrigatorio.");
    options.ModelBindingMessageProvider.SetAttemptedValueIsInvalidAccessor((x, y) => $"O valor '{x}' nao e valido para {y}.");
    options.ModelBindingMessageProvider.SetMissingKeyOrValueAccessor(() => "Valor obrigatorio.");
    options.ModelBindingMessageProvider.SetUnknownValueIsInvalidAccessor(x => $"O valor fornecido e invalido para {x}.");
    options.ModelBindingMessageProvider.SetValueIsInvalidAccessor(x => $"O valor '{x}' e invalido.");
    options.ModelBindingMessageProvider.SetMissingRequestBodyRequiredValueAccessor(() => "O corpo da requisicao e obrigatorio.");
    options.ModelBindingMessageProvider.SetNonPropertyAttemptedValueIsInvalidAccessor(x => $"O valor '{x}' nao e valido.");
    options.ModelBindingMessageProvider.SetNonPropertyUnknownValueIsInvalidAccessor(() => "O valor fornecido e invalido.");
    options.ModelBindingMessageProvider.SetNonPropertyValueMustBeANumberAccessor(() => "O campo deve ser um numero.");
});

// CORRECAO: Configuracao de localizacao para portugues brasileiro com encoding correto
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var ptBR = new System.Globalization.CultureInfo("pt-BR");

    options.DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture(ptBR);
    options.SupportedCultures = new List<System.Globalization.CultureInfo> { ptBR };
    options.SupportedUICultures = new List<System.Globalization.CultureInfo> { ptBR };
    options.RequestCultureProviders.Clear();
    options.RequestCultureProviders.Add(new Microsoft.AspNetCore.Localization.CustomRequestCultureProvider(
        context => Task.FromResult(new Microsoft.AspNetCore.Localization.ProviderCultureResult("pt-BR"))
    ));
});

// CORRECAO: Configurar encoding UTF-8 para WebEncoderOptions
builder.Services.AddWebEncoders(options =>
{
    options.TextEncoderSettings = new System.Text.Encodings.Web.TextEncoderSettings(
        System.Text.Unicode.UnicodeRanges.All
    );
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

// CORRECAO: Configuracao de localizacao DEVE vir antes de UseStaticFiles
app.UseRequestLocalization();

app.UseHttpsRedirection();

// OTIMIZACAO: Cache estatico para arquivos wwwroot
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        const int durationInSeconds = 60 * 60 * 24 * 30; // 30 dias
        ctx.Context.Response.Headers["Cache-Control"] = $"public,max-age={durationInSeconds}";

        // CORRECAO: Adicionar headers de charset UTF-8 para arquivos de texto
        var path = ctx.Context.Request.Path.Value?.ToLower();
        if (path != null && (path.EndsWith(".html") || path.EndsWith(".htm") ||
            path.EndsWith(".css") || path.EndsWith(".js") || path.EndsWith(".json")))
        {
            var contentType = ctx.Context.Response.ContentType;
            if (!string.IsNullOrEmpty(contentType) && !contentType.Contains("charset"))
            {
                ctx.Context.Response.ContentType = $"{contentType}; charset=utf-8";
            }
        }
    }
});

app.UseRouting();

app.UseAccessControl();
app.UseAuthentication();
app.UseAuthorization();

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