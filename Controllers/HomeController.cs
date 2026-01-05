using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaTesourariaEclesiastica.Attributes;
using SistemaTesourariaEclesiastica.Data;
using SistemaTesourariaEclesiastica.Enums;
using SistemaTesourariaEclesiastica.Models;
using SistemaTesourariaEclesiastica.Services;
using System.Diagnostics;

namespace SistemaTesourariaEclesiastica.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AuditService _auditService;


        public HomeController(
            ILogger<HomeController> logger,
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            AuditService auditService)
        {
            _logger = logger;
            _context = context;
            _userManager = userManager;
            _auditService = auditService;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                // =====================================================
                // 1. VERIFICAR USUÁRIO E AUTENTICAÇÃO
                // =====================================================
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    _logger.LogWarning("Usuário não encontrado, redirecionando para login");
                    return RedirectToAction("Login", "Account");
                }

                var roles = await _userManager.GetRolesAsync(user);
                var primaryRole = roles.FirstOrDefault() ?? "Sem Perfil";

                _logger.LogInformation($"Dashboard acessado por: {user.NomeCompleto} ({primaryRole})");

                // =====================================================
                // 2. LOG DE AUDITORIA (com proteção contra erro)
                // =====================================================
                try
                {
                    await _auditService.LogAsync("DASHBOARD_ACCESS", "Home",
                        $"Acesso ao dashboard por {user.NomeCompleto} ({primaryRole})");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Erro ao registrar auditoria (não crítico)");
                }

                // =====================================================
                // 3. VERIFICAR SE AS TABELAS EXISTEM
                // =====================================================
                var tabelasExistem = await VerificarTabelas();
                if (!tabelasExistem)
                {
                    _logger.LogWarning("Tabelas do banco não existem, redirecionando para configuração");
                    TempData["Aviso"] = "O banco de dados não foi inicializado. Execute: dotnet ef database update";
                    return View(await ObterDadosVazios(user, primaryRole));
                }

                // =====================================================
                // 4. CONFIGURAR PERÍODO E DATAS
                // =====================================================
                var hoje = DateTime.Now.Date;
                var inicioMes = new DateTime(hoje.Year, hoje.Month, 1);
                var fimMes = inicioMes.AddMonths(1).AddDays(-1);

                // =====================================================
                // 5. DETERMINAR CENTRO DE CUSTO BASEADO NO PERFIL
                // =====================================================
                int? centroCustoFiltro = null;

                // ✅ CORRIGIDO: Administrador, TesoureiroGeral e Pastor veem TODOS os dados
                if (!User.IsInRole(Roles.Administrador) && 
                    !User.IsInRole(Roles.TesoureiroGeral) && 
                    !User.IsInRole(Roles.Pastor))
                {
                    // Apenas Tesoureiro Local precisa de filtro por centro de custo
                    if (user.CentroCustoId.HasValue)
                    {
                        centroCustoFiltro = user.CentroCustoId.Value;
                    }
                    else
                    {
                        _logger.LogWarning($"Usuário {user.NomeCompleto} sem Centro de Custo definido");
                        // Usuário sem centro de custo verá dados vazios
                        ViewBag.EntradasMes = "R$ 0,00";
                        ViewBag.SaidasMes = "R$ 0,00";
                        ViewBag.SaldoTotal = "R$ 0,00";
                        ViewBag.DizimosMes = "R$ 0,00";
                        ViewBag.FluxoCaixaData = new List<object>();
                        ViewBag.DespesasData = new List<object>();
                        ViewBag.UserRole = primaryRole;
                        ViewBag.UserName = user.NomeCompleto;
                        ViewBag.CentroCusto = "Não definido";
                        ViewBag.ShowFullData = false;
                        ViewBag.CanManageOperations = false;
                        ViewBag.CanApproveClosures = false;
                        ViewBag.CanViewReports = true;
                        ViewBag.Alertas = new List<object>();
                        ViewBag.AtividadesRecentes = new List<object>();
                        ViewBag.UltimasTransacoes = new List<object>();

                        TempData["Aviso"] = "Seu usuário não possui um Centro de Custo definido. Entre em contato com o administrador.";
                        return View();
                    }
                }
                // Administrador, TesoureiroGeral e Pastor: centroCustoFiltro = null (vê tudo)

                // =====================================================
                // 6. BUSCAR IDs DE LANÇAMENTOS APROVADOS (MÊS ATUAL)
                // =====================================================
                List<int> idsEntradasAprovadasMes = new List<int>();
                List<int> idsSaidasAprovadasMes = new List<int>();

                try
                {
                    // ✅ CORREÇÃO: Buscar entradas que estão em fechamentos aprovados OU processados do mês
                    var queryEntradasMes = _context.Entradas
                        .Where(e => e.Data >= inicioMes && e.Data <= fimMes);

                    if (centroCustoFiltro.HasValue)
                    {
                        queryEntradasMes = queryEntradasMes.Where(e => e.CentroCustoId == centroCustoFiltro.Value);
                    }

                    idsEntradasAprovadasMes = await queryEntradasMes
                        .Where(e => _context.FechamentosPeriodo.Any(f =>
                            f.CentroCustoId == e.CentroCustoId &&
                            (f.Status == StatusFechamentoPeriodo.Aprovado || f.Status == StatusFechamentoPeriodo.Processado) &&
                            e.Data >= f.DataInicio &&
                            e.Data <= f.DataFim))
                        .Select(e => e.Id)
                        .ToListAsync();

                    // ✅ CORREÇÃO: Buscar saídas que estão em fechamentos aprovados OU processados do mês
                    var querySaidasMes = _context.Saidas
                        .Where(s => s.Data >= inicioMes && s.Data <= fimMes);

                    if (centroCustoFiltro.HasValue)
                    {
                        querySaidasMes = querySaidasMes.Where(s => s.CentroCustoId == centroCustoFiltro.Value);
                    }

                    idsSaidasAprovadasMes = await querySaidasMes
                        .Where(s => _context.FechamentosPeriodo.Any(f =>
                            f.CentroCustoId == s.CentroCustoId &&
                            (f.Status == StatusFechamentoPeriodo.Aprovado || f.Status == StatusFechamentoPeriodo.Processado) &&
                            s.Data >= f.DataInicio &&
                            s.Data <= f.DataFim))
                        .Select(s => s.Id)
                        .ToListAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao buscar IDs de lançamentos aprovados");
                }

                // =====================================================
                // 7. CALCULAR ESTATÍSTICAS DO MÊS (APENAS LANÇAMENTOS INCLUÍDOS EM FECHAMENTOS APROVADOS/PROCESSADOS)
                // =====================================================
                decimal entradasMes = 0, saidasMes = 0, dizimosMes = 0;

                try
                {
                    // ✅ CORRIGIDO: Buscar entradas que foram incluídas em fechamentos aprovados ou processados
                    var queryEntradasMes = _context.Entradas
                        .Include(e => e.FechamentoQueIncluiu)
                        .Where(e => e.Data >= inicioMes &&
                            e.Data <= fimMes &&
                     e.IncluidaEmFechamento == true &&
                           e.FechamentoQueIncluiu != null &&
                           (e.FechamentoQueIncluiu.Status == StatusFechamentoPeriodo.Aprovado ||
                           e.FechamentoQueIncluiu.Status == StatusFechamentoPeriodo.Processado));

                    if (centroCustoFiltro.HasValue)
                    {
                        queryEntradasMes = queryEntradasMes.Where(e => e.CentroCustoId == centroCustoFiltro.Value);
                    }

                    entradasMes = await queryEntradasMes.SumAsync(e => (decimal?)e.Valor) ?? 0;

                    // Dízimos do mês (também apenas os aprovados)
                    dizimosMes = await _context.Entradas
                 .Include(e => e.PlanoDeContas)
                                  .Include(e => e.FechamentoQueIncluiu)
                   .Where(e => e.Data >= inicioMes &&
                    e.Data <= fimMes &&
                    e.IncluidaEmFechamento == true &&
                     e.FechamentoQueIncluiu != null &&
                  (e.FechamentoQueIncluiu.Status == StatusFechamentoPeriodo.Aprovado ||
                    e.FechamentoQueIncluiu.Status == StatusFechamentoPeriodo.Processado) &&
                    e.PlanoDeContas != null &&
                  e.PlanoDeContas.Nome.ToLower().Contains("dízimo") &&
                   (centroCustoFiltro == null || e.CentroCustoId == centroCustoFiltro.Value))
                        .SumAsync(e => (decimal?)e.Valor) ?? 0;

                    // ✅ CORRIGIDO: Buscar saídas que foram incluídas em fechamentos aprovados ou processados
                    var querySaidasMes = _context.Saidas
                   .Include(s => s.FechamentoQueIncluiu)
                    .Where(s => s.Data >= inicioMes &&
                    s.Data <= fimMes &&
                         s.IncluidaEmFechamento == true &&
                        s.FechamentoQueIncluiu != null &&
                    (s.FechamentoQueIncluiu.Status == StatusFechamentoPeriodo.Aprovado ||
                    s.FechamentoQueIncluiu.Status == StatusFechamentoPeriodo.Processado));

                    if (centroCustoFiltro.HasValue)
                    {
                        querySaidasMes = querySaidasMes.Where(s => s.CentroCustoId == centroCustoFiltro.Value);
                    }

                    saidasMes = await querySaidasMes.SumAsync(s => (decimal?)s.Valor) ?? 0;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao calcular estatísticas do mês");
                }

                // =====================================================
                // 8. CALCULAR SALDO TOTAL (APENAS LANÇAMENTOS INCLUÍDOS EM FECHAMENTOS APROVADOS/PROCESSADOS DE TODOS OS TEMPOS)
                // =====================================================
                decimal totalEntradas = 0, totalSaidas = 0;

                try
                {
                    // ✅ CORRIGIDO: Buscar TODAS as entradas incluídas em fechamentos aprovados ou processados
                    var queryTodasEntradas = _context.Entradas
                        .Include(e => e.FechamentoQueIncluiu)
                       .Where(e => e.IncluidaEmFechamento == true &&
                      e.FechamentoQueIncluiu != null &&
                 (e.FechamentoQueIncluiu.Status == StatusFechamentoPeriodo.Aprovado ||
                  e.FechamentoQueIncluiu.Status == StatusFechamentoPeriodo.Processado));

                    if (centroCustoFiltro.HasValue)
                    {
                        queryTodasEntradas = queryTodasEntradas.Where(e => e.CentroCustoId == centroCustoFiltro.Value);
                    }

                    totalEntradas = await queryTodasEntradas.SumAsync(e => (decimal?)e.Valor) ?? 0;

                    // ✅ CORRIGIDO: Buscar TODAS as saídas incluídas em fechamentos aprovados ou processados
                    var queryTodasSaidas = _context.Saidas
                    .Include(s => s.FechamentoQueIncluiu)
                      .Where(s => s.IncluidaEmFechamento == true &&
                  s.FechamentoQueIncluiu != null &&
                  (s.FechamentoQueIncluiu.Status == StatusFechamentoPeriodo.Aprovado ||
                 s.FechamentoQueIncluiu.Status == StatusFechamentoPeriodo.Processado));

                    if (centroCustoFiltro.HasValue)
                    {
                        queryTodasSaidas = queryTodasSaidas.Where(s => s.CentroCustoId == centroCustoFiltro.Value);
                    }

                    totalSaidas = await queryTodasSaidas.SumAsync(s => (decimal?)s.Valor) ?? 0;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao calcular totais históricos");
                }

                var saldoTotal = totalEntradas - totalSaidas;

                // =====================================================
                // 9. DADOS PARA GRÁFICO DE FLUXO DE CAIXA (ÚLTIMOS 6 MESES - APENAS APROVADOS)
                // =====================================================
                var fluxoCaixaData = new List<object>();

                try
                {
                    for (int i = 5; i >= 0; i--)
                    {
                        var mesInicio = hoje.AddMonths(-i).AddDays(-(hoje.Day - 1));
                        var mesFim = mesInicio.AddMonths(1).AddDays(-1);

                        // ✅ CORRIGIDO: Entradas do mês incluídas em fechamentos aprovados/processados
                        var queryEntradasGrafico = _context.Entradas
                      .Include(e => e.FechamentoQueIncluiu)
                       .Where(e => e.Data >= mesInicio &&
                         e.Data <= mesFim &&
                    e.IncluidaEmFechamento == true &&
                      e.FechamentoQueIncluiu != null &&
                       (e.FechamentoQueIncluiu.Status == StatusFechamentoPeriodo.Aprovado ||
                         e.FechamentoQueIncluiu.Status == StatusFechamentoPeriodo.Processado));

                        if (centroCustoFiltro.HasValue)
                        {
                            queryEntradasGrafico = queryEntradasGrafico.Where(e => e.CentroCustoId == centroCustoFiltro.Value);
                        }

                        var entradasMesGrafico = await queryEntradasGrafico.SumAsync(e => (decimal?)e.Valor) ?? 0;

                        // ✅ CORRIGIDO: Saídas do mês incluídas em fechamentos aprovados/processados
                        var querySaidasGrafico = _context.Saidas
                      .Include(s => s.FechamentoQueIncluiu)
                        .Where(s => s.Data >= mesInicio &&
                   s.Data <= mesFim &&
                        s.IncluidaEmFechamento == true &&
                       s.FechamentoQueIncluiu != null &&
                     (s.FechamentoQueIncluiu.Status == StatusFechamentoPeriodo.Aprovado ||
                   s.FechamentoQueIncluiu.Status == StatusFechamentoPeriodo.Processado));

                        if (centroCustoFiltro.HasValue)
                        {
                            querySaidasGrafico = querySaidasGrafico.Where(s => s.CentroCustoId == centroCustoFiltro.Value);
                        }

                        var saidasMesGrafico = await querySaidasGrafico.SumAsync(s => (decimal?)s.Valor) ?? 0;

                        fluxoCaixaData.Add(new
                        {
                            mes = mesInicio.ToString("MMM", new System.Globalization.CultureInfo("pt-BR")),
                            entradas = entradasMesGrafico,
                            saidas = saidasMesGrafico,
                            saldo = entradasMesGrafico - saidasMesGrafico
                        });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao gerar dados de fluxo de caixa");
                }

                // =====================================================
                // 10. DESPESAS POR CATEGORIA (ÚLTIMOS 3 MESES - APENAS APROVADAS)
                // =====================================================
                var despesasPorCategoria = new List<object>();

                try
                {
                    var tresMesesAtras = hoje.AddMonths(-3);

                    var querySaidasCategoria = _context.Saidas
                   .Include(s => s.PlanoDeContas)
                       .Include(s => s.FechamentoQueIncluiu)
                .Where(s => s.Data >= tresMesesAtras &&
                      s.IncluidaEmFechamento == true &&
                    s.FechamentoQueIncluiu != null &&
                     (s.FechamentoQueIncluiu.Status == StatusFechamentoPeriodo.Aprovado ||
                       s.FechamentoQueIncluiu.Status == StatusFechamentoPeriodo.Processado) &&
                  s.PlanoDeContas != null);

                    if (centroCustoFiltro.HasValue)
                    {
                        querySaidasCategoria = querySaidasCategoria.Where(s => s.CentroCustoId == centroCustoFiltro.Value);
                    }

                    despesasPorCategoria = await querySaidasCategoria
                   .GroupBy(s => s.PlanoDeContas.Nome)
                    .Select(g => new
                    {
                        categoria = g.Key ?? "Sem Categoria",
                        valor = g.Sum(s => s.Valor)
                    })
                   .OrderByDescending(x => x.valor)
                       .Take(5)
                    .ToListAsync<object>();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao gerar dados de despesas por categoria");
                }

                // =====================================================
                // 11. BUSCAR ÚLTIMAS TRANSAÇÕES (10 MAIS RECENTES APROVADAS)
                // =====================================================
                var ultimasTransacoes = new List<object>();

                try
                {
                    // ✅ CORRIGIDO: Últimas entradas incluídas em fechamentos aprovados/processados
                    var queryUltimasEntradas = _context.Entradas
                 .Include(e => e.PlanoDeContas)
                  .Include(e => e.CentroCusto)
                    .Include(e => e.FechamentoQueIncluiu)
                      .Where(e => e.IncluidaEmFechamento == true &&
                 e.FechamentoQueIncluiu != null &&
                     (e.FechamentoQueIncluiu.Status == StatusFechamentoPeriodo.Aprovado ||
                e.FechamentoQueIncluiu.Status == StatusFechamentoPeriodo.Processado));

                    if (centroCustoFiltro.HasValue)
                    {
                        queryUltimasEntradas = queryUltimasEntradas.Where(e => e.CentroCustoId == centroCustoFiltro.Value);
                    }

                    var ultimasEntradas = await queryUltimasEntradas
                  .OrderByDescending(e => e.Data)
                 .Take(5)
                 .Select(e => new
                 {
                     Tipo = "Entrada",
                     Descricao = e.Descricao ?? e.PlanoDeContas.Nome,
                     CentroCusto = e.CentroCusto.Nome,
                     Data = e.Data,
                     Valor = e.Valor
                 })
                   .ToListAsync<object>();

                    // ✅ CORRIGIDO: Últimas saídas incluídas em fechamentos aprovados/processados
                    var queryUltimasSaidas = _context.Saidas
                     .Include(s => s.PlanoDeContas)
                   .Include(s => s.CentroCusto)
                   .Include(s => s.FechamentoQueIncluiu)
                .Where(s => s.IncluidaEmFechamento == true &&
                      s.FechamentoQueIncluiu != null &&
                (s.FechamentoQueIncluiu.Status == StatusFechamentoPeriodo.Aprovado ||
                     s.FechamentoQueIncluiu.Status == StatusFechamentoPeriodo.Processado));

                    if (centroCustoFiltro.HasValue)
                    {
                        queryUltimasSaidas = queryUltimasSaidas.Where(s => s.CentroCustoId == centroCustoFiltro.Value);
                    }

                    var ultimasSaidas = await queryUltimasSaidas
                    .OrderByDescending(s => s.Data)
                    .Take(5)
                    .Select(s => new
                    {
                        Tipo = "Saída",
                        Descricao = s.Descricao,
                        CentroCusto = s.CentroCusto.Nome,
                        Data = s.Data,
                        Valor = s.Valor
                    })
                    .ToListAsync<object>();

                    // Combinar e ordenar por data
                    ultimasTransacoes = ultimasEntradas
                       .Concat<object>(ultimasSaidas)
                    .OrderByDescending(t => ((dynamic)t).Data)
                        .Take(10)
                           .ToList();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao buscar últimas transações");
                }

                // =====================================================
                // 12. BUSCAR ATIVIDADES RECENTES (LOG DE AUDITORIA)
                // =====================================================
                var atividadesRecentes = new List<object>();

                try
                {
                    var seteDiasAtras = DateTime.Now.AddDays(-7);

                    var queryAtividades = _context.LogsAuditoria
                        .Include(l => l.Usuario)
                        .Where(l => l.DataHora >= seteDiasAtras)
                        .AsQueryable();

                    // Se não for admin ou tesoureiro geral, filtrar por centro de custo
                    if (centroCustoFiltro.HasValue)
                    {
                        queryAtividades = queryAtividades.Where(l =>
                            l.Usuario.CentroCustoId == centroCustoFiltro.Value);
                    }

                    atividadesRecentes = await queryAtividades
                        .OrderByDescending(l => l.DataHora)
                        .Take(10)
                        .Select(l => new
                        {
                            Acao = l.Acao,
                            Usuario = l.Usuario.NomeCompleto,
                            DataHora = l.DataHora,
                            Icone = ObterIconeAtividade(l.Acao)
                        })
                        .ToListAsync<object>();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao buscar atividades recentes");
                }

                // =====================================================
                // 13. PREENCHER VIEWBAG COM TODOS OS DADOS
                // =====================================================
                ViewBag.EntradasMes = entradasMes.ToString("C");
                ViewBag.SaidasMes = saidasMes.ToString("C");
                ViewBag.SaldoTotal = saldoTotal.ToString("C");
                ViewBag.DizimosMes = dizimosMes.ToString("C");
                ViewBag.FluxoCaixaData = fluxoCaixaData;
                ViewBag.DespesasData = despesasPorCategoria;
                ViewBag.UserRole = primaryRole;
                ViewBag.UserName = user.NomeCompleto;
                ViewBag.CentroCusto = user.CentroCusto?.Nome ?? "Não definido";

                // Permissões
                ViewBag.ShowFullData = User.IsInRole(Roles.Administrador) || 
                                      User.IsInRole(Roles.TesoureiroGeral) || 
                                      User.IsInRole(Roles.Pastor);
                ViewBag.CanManageOperations = User.IsInRole(Roles.Administrador) ||
                                             User.IsInRole(Roles.TesoureiroGeral) ||
                                             User.IsInRole(Roles.TesoureiroLocal);
                ViewBag.CanApproveClosures = User.IsInRole(Roles.Administrador) || User.IsInRole(Roles.TesoureiroGeral);
                ViewBag.CanViewReports = true;

                // Novos dados
                ViewBag.UltimasTransacoes = ultimasTransacoes;
                ViewBag.AtividadesRecentes = atividadesRecentes;
                ViewBag.Alertas = new List<object>(); // Manter para compatibilidade

                _logger.LogInformation($"Dashboard carregado com sucesso para {user.NomeCompleto}");

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERRO CRÍTICO ao carregar dashboard");
                _logger.LogError($"Mensagem: {ex.Message}");
                _logger.LogError($"StackTrace: {ex.StackTrace}");

                if (ex.InnerException != null)
                {
                    _logger.LogError($"InnerException: {ex.InnerException.Message}");
                }

                TempData["Erro"] = $"Erro ao carregar o dashboard: {ex.Message}. Verifique os logs para mais detalhes.";

                return View("Error", new ErrorViewModel
                {
                    RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
                });
            }
        }

        private async Task<bool> VerificarTabelas()
        {
            try
            {
                // Tentar fazer uma consulta simples para verificar se as tabelas existem
                await _context.Entradas.AnyAsync();
                await _context.Saidas.AnyAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Tabelas não existem ou banco não foi criado");
                return false;
            }
        }

        private async Task<object> ObterDadosVazios(ApplicationUser user, string primaryRole)
        {
            ViewBag.EntradasMes = "R$ 0,00";
            ViewBag.SaidasMes = "R$ 0,00";
            ViewBag.SaldoTotal = "R$ 0,00";
            ViewBag.DizimosMes = "R$ 0,00";
            ViewBag.FluxoCaixaData = new List<object>();
            ViewBag.DespesasData = new List<object>();
            ViewBag.UserRole = primaryRole;
            ViewBag.UserName = user.NomeCompleto;
            ViewBag.CentroCusto = user.CentroCusto?.Nome ?? "Não definido";
            ViewBag.ShowFullData = User.IsInRole(Roles.Administrador) || 
                                  User.IsInRole(Roles.TesoureiroGeral) || 
                                  User.IsInRole(Roles.Pastor);
            ViewBag.CanManageOperations = User.IsInRole(Roles.Administrador) ||
                                         User.IsInRole(Roles.TesoureiroGeral) ||
                                         User.IsInRole(Roles.TesoureiroLocal);
            ViewBag.CanApproveClosures = User.IsInRole(Roles.Administrador) || User.IsInRole(Roles.TesoureiroGeral);
            ViewBag.CanViewReports = true;
            ViewBag.Alertas = new List<object>();
            ViewBag.AtividadesRecentes = new List<object>();
            ViewBag.UltimasTransacoes = new List<object>();

            return new { };
        }

        [AuthorizeRoles(Roles.TodosComAcessoRelatorios)]
        public async Task<IActionResult> Reports()
        {
            var user = await _userManager.GetUserAsync(User);
            await _auditService.LogAsync("REPORTS_ACCESS", "Home", "Acesso à página de relatórios");

            var canViewAllData = User.IsInRole(Roles.Administrador) || 
                                User.IsInRole(Roles.TesoureiroGeral) || 
                                User.IsInRole(Roles.Pastor);

            ViewBag.CanViewAllData = canViewAllData;
            ViewBag.UserCentroCustoId = user?.CentroCustoId;

            // Carregar opções de centros de custo se o usuário pode ver todos os dados
            if (canViewAllData)
            {
                try
                {
                    var centrosCusto = await _context.CentrosCusto
                        .Where(c => c.Ativo)
                        .OrderBy(c => c.Nome)
                        .Select(c => new { c.Id, c.Nome })
                        .ToListAsync();

                    ViewBag.CentrosCusto = centrosCusto;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao carregar centros de custo");
                    ViewBag.CentrosCusto = new List<object>();
                }
            }

            return View();
        }

        [AuthorizeRoles(Roles.AdminOnly)]
        public async Task<IActionResult> SystemManagement()
        {
            var user = await _userManager.GetUserAsync(User);
            await _auditService.LogAsync("SYSTEM_MANAGEMENT_ACCESS", "Home",
                "Acesso ao painel de administração do sistema");

            try
            {
                var totalUsuarios = await _userManager.Users.CountAsync();
                var usuariosAtivos = await _userManager.Users.Where(u => u.Ativo).CountAsync();
                var totalCentrosCusto = await _context.CentrosCusto.CountAsync();

                // Estatísticas adicionais
                var totalEntradas = await _context.Entradas.CountAsync();
                var totalSaidas = await _context.Saidas.CountAsync();
                var totalFechamentos = await _context.FechamentosPeriodo.CountAsync();
                var ultimoBackup = "Nunca realizado"; // Implementar quando tiver backup
                var versaoSistema = "1.0.0";

                ViewBag.TotalUsuarios = totalUsuarios;
                ViewBag.UsuariosAtivos = usuariosAtivos;
                ViewBag.TotalCentrosCusto = totalCentrosCusto;
                ViewBag.TotalEntradas = totalEntradas;
                ViewBag.TotalSaidas = totalSaidas;
                ViewBag.TotalFechamentos = totalFechamentos;
                ViewBag.UltimoBackup = ultimoBackup;
                ViewBag.VersaoSistema = versaoSistema;
                ViewBag.ServidorNome = Environment.MachineName;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao carregar dados de administração");
                TempData["ErrorMessage"] = "Erro ao carregar estatísticas do sistema";

                // Valores padrão em caso de erro
                ViewBag.TotalUsuarios = 0;
                ViewBag.UsuariosAtivos = 0;
                ViewBag.TotalCentrosCusto = 0;
            }

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpGet]
        public async Task<IActionResult> GetDashboardStats()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                var hoje = DateTime.Now.Date;
                var inicioMes = new DateTime(hoje.Year, hoje.Month, 1);

                IQueryable<Entrada> entradasQuery = _context.Entradas;
                IQueryable<Saida> saidasQuery = _context.Saidas;

                // ✅ CORRIGIDO: Administrador, TesoureiroGeral e Pastor veem todos os dados
                if (!User.IsInRole(Roles.Administrador) && 
                    !User.IsInRole(Roles.TesoureiroGeral) && 
                    !User.IsInRole(Roles.Pastor))
                {
                    // Apenas Tesoureiro Local tem filtro
                    if (user.CentroCustoId.HasValue)
                    {
                        entradasQuery = entradasQuery.Where(e => e.CentroCustoId == user.CentroCustoId.Value);
                        saidasQuery = saidasQuery.Where(s => s.CentroCustoId == user.CentroCustoId.Value);
                    }
                }

                var stats = new
                {
                    entradasHoje = await entradasQuery.Where(e => e.Data.Date == hoje).SumAsync(e => (decimal?)e.Valor) ?? 0,
                    saidasHoje = await saidasQuery.Where(s => s.Data.Date == hoje).SumAsync(s => (decimal?)s.Valor) ?? 0,
                    entradasMes = await entradasQuery.Where(e => e.Data >= inicioMes).SumAsync(e => (decimal?)e.Valor) ?? 0,
                    saidasMes = await saidasQuery.Where(s => s.Data >= inicioMes).SumAsync(s => (decimal?)s.Valor) ?? 0
                };

                return Json(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter estatísticas do dashboard");
                return Json(new { error = "Erro ao carregar dados" });
            }
        }

        private static string ObterIconeAtividade(string acao)
        {
            if (acao.Contains("CREATE") || acao.Contains("INSERT"))
                return "plus-circle-fill";
            if (acao.Contains("UPDATE") || acao.Contains("EDIT"))
                return "pencil-square";
            if (acao.Contains("DELETE"))
                return "trash-fill";
            if (acao.Contains("LOGIN") || acao.Contains("ACCESS"))
                return "box-arrow-in-right";
            if (acao.Contains("APPROVAL") || acao.Contains("APPROVE"))
                return "check-circle-fill";
            if (acao.Contains("REJECT"))
                return "x-circle-fill";

            return "activity";
        }
    }
}