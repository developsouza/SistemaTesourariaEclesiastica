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

                // ✅ NOVO: Se for Pastor, redirecionar para Dashboard específico
                if (User.IsInRole(Roles.Pastor))
                {
                    return RedirectToAction("DashboardPastor");
                }

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
                        ViewBag.TotalRateiosEnviados = "R$ 0,00";
                        ViewBag.DizimosMes = "R$ 0,00";
                        ViewBag.FluxoCaixaData = new List<object>();
                        ViewBag.DespesasData = new List<object>();
                        ViewBag.RateiosPorDestino = new List<object>();
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
                // 6. BUSCAR DADOS DO MÊS ATUAL
                // =====================================================
                // (Os IDs não são mais necessários aqui, pois agora buscamos diretamente com filtro)

                // =====================================================
                // 7. CALCULAR ESTATÍSTICAS DO MÊS (APENAS LANÇAMENTOS INCLUÍDOS EM FECHAMENTOS APROVADOS/PROCESSADOS)
                // =====================================================
                decimal entradasMes = 0, saidasMes = 0, dizimosMes = 0;

                try
                {
                    // Buscar entradas aprovadas do mês
                    var queryEntradasMes = _context.Entradas
                        .Where(e => e.Data >= inicioMes && e.Data <= fimMes);

                    if (centroCustoFiltro.HasValue)
                    {
                        queryEntradasMes = queryEntradasMes.Where(e => e.CentroCustoId == centroCustoFiltro.Value);
                    }

                    var idsEntradasMes = await queryEntradasMes
                        .Where(e => _context.FechamentosPeriodo.Any(f =>
                            f.CentroCustoId == e.CentroCustoId &&
                            (f.Status == StatusFechamentoPeriodo.Aprovado || f.Status == StatusFechamentoPeriodo.Processado) &&
                            e.Data >= f.DataInicio &&
                            e.Data <= f.DataFim))
                        .Select(e => e.Id)
                        .ToListAsync();

                    entradasMes = idsEntradasMes.Any()
                        ? await _context.Entradas
                            .Where(e => idsEntradasMes.Contains(e.Id))
                            .SumAsync(e => (decimal?)e.Valor) ?? 0
                        : 0;

                    // Dízimos do mês (também apenas os aprovados)
                    dizimosMes = idsEntradasMes.Any()
                        ? await _context.Entradas
                            .Include(e => e.PlanoDeContas)
                            .Where(e => idsEntradasMes.Contains(e.Id) &&
                                       e.PlanoDeContas != null &&
                                       e.PlanoDeContas.Nome.ToLower().Contains("dízimo"))
                            .SumAsync(e => (decimal?)e.Valor) ?? 0
                        : 0;

                    // ✅ CORREÇÃO: Buscar saídas diretas aprovadas do mês
                    var querySaidasMes = _context.Saidas
                        .Where(s => s.Data >= inicioMes && s.Data <= fimMes);

                    if (centroCustoFiltro.HasValue)
                    {
                        querySaidasMes = querySaidasMes.Where(s => s.CentroCustoId == centroCustoFiltro.Value);
                    }

                    var idsSaidasMes = await querySaidasMes
                        .Where(s => _context.FechamentosPeriodo.Any(f =>
                            f.CentroCustoId == s.CentroCustoId &&
                            (f.Status == StatusFechamentoPeriodo.Aprovado || f.Status == StatusFechamentoPeriodo.Processado) &&
                            s.Data >= f.DataInicio &&
                            s.Data <= f.DataFim))
                        .Select(s => s.Id)
                        .ToListAsync();

                    var saidasDiretasMes = idsSaidasMes.Any()
                        ? await _context.Saidas
                            .Where(s => idsSaidasMes.Contains(s.Id))
                            .SumAsync(s => (decimal?)s.Valor) ?? 0
                        : 0;

                    // ✅ NOVO: Buscar rateios enviados no mês
                    var queryRateiosMes = _context.ItensRateioFechamento
                        .Include(i => i.FechamentoPeriodo)
                        .Where(i => (i.FechamentoPeriodo.Status == StatusFechamentoPeriodo.Aprovado ||
                                    i.FechamentoPeriodo.Status == StatusFechamentoPeriodo.Processado) &&
                                   i.FechamentoPeriodo.DataAprovacao >= inicioMes &&
                                   i.FechamentoPeriodo.DataAprovacao <= fimMes);

                    if (centroCustoFiltro.HasValue)
                    {
                        queryRateiosMes = queryRateiosMes.Where(i => i.FechamentoPeriodo.CentroCustoId == centroCustoFiltro.Value);
                    }

                    var rateiosMes = await queryRateiosMes.SumAsync(i => (decimal?)i.ValorRateio) ?? 0;

                    // ✅ CORREÇÃO: Total de saídas = saídas diretas + rateios
                    saidasMes = saidasDiretasMes + rateiosMes;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao calcular estatísticas do mês");
                }

                // =====================================================
                // 8. CALCULAR SALDO TOTAL (APENAS LANÇAMENTOS INCLUÍDOS EM FECHAMENTOS APROVADOS/PROCESSADOS DE TODOS OS TEMPOS)
                // =====================================================
                decimal totalEntradas = 0, totalSaidas = 0, totalRateiosEnviados = 0;

                try
                {
                    // Buscar TODAS as entradas incluídas em fechamentos aprovados ou processados
                    var queryTodasEntradas = _context.Entradas.AsQueryable();

                    if (centroCustoFiltro.HasValue)
                    {
                        queryTodasEntradas = queryTodasEntradas.Where(e => e.CentroCustoId == centroCustoFiltro.Value);
                    }

                    var idsTodasEntradas = await queryTodasEntradas
                        .Where(e => _context.FechamentosPeriodo.Any(f =>
                            f.CentroCustoId == e.CentroCustoId &&
                            (f.Status == StatusFechamentoPeriodo.Aprovado || f.Status == StatusFechamentoPeriodo.Processado) &&
                            e.Data >= f.DataInicio &&
                            e.Data <= f.DataFim))
                        .Select(e => e.Id)
                        .ToListAsync();

                    totalEntradas = idsTodasEntradas.Any()
                        ? await _context.Entradas
                            .Where(e => idsTodasEntradas.Contains(e.Id))
                            .SumAsync(e => (decimal?)e.Valor) ?? 0
                        : 0;

                    // Buscar TODAS as saídas incluídas em fechamentos aprovados ou processados
                    var queryTodasSaidas = _context.Saidas.AsQueryable();

                    if (centroCustoFiltro.HasValue)
                    {
                        queryTodasSaidas = queryTodasSaidas.Where(s => s.CentroCustoId == centroCustoFiltro.Value);
                    }

                    var idsTodasSaidas = await queryTodasSaidas
                        .Where(s => _context.FechamentosPeriodo.Any(f =>
                            f.CentroCustoId == s.CentroCustoId &&
                            (f.Status == StatusFechamentoPeriodo.Aprovado || f.Status == StatusFechamentoPeriodo.Processado) &&
                            s.Data >= f.DataInicio &&
                            s.Data <= f.DataFim))
                        .Select(s => s.Id)
                        .ToListAsync();

                    totalSaidas = idsTodasSaidas.Any()
                        ? await _context.Saidas
                            .Where(s => idsTodasSaidas.Contains(s.Id))
                            .SumAsync(s => (decimal?)s.Valor) ?? 0
                        : 0;

                    // Buscar TODOS os rateios enviados por este centro de custo
                    var queryRateiosEnviados = _context.ItensRateioFechamento
                        .Include(i => i.FechamentoPeriodo)
                        .Where(i => (i.FechamentoPeriodo.Status == StatusFechamentoPeriodo.Aprovado ||
                                    i.FechamentoPeriodo.Status == StatusFechamentoPeriodo.Processado));

                    if (centroCustoFiltro.HasValue)
                    {
                        queryRateiosEnviados = queryRateiosEnviados.Where(i => i.FechamentoPeriodo.CentroCustoId == centroCustoFiltro.Value);
                    }

                    totalRateiosEnviados = await queryRateiosEnviados.SumAsync(i => (decimal?)i.ValorRateio) ?? 0;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao calcular totais históricos");
                }

                // ✅ CORRIGIDO: Saldo Total = Entradas - Saídas - Rateios Enviados
                var saldoTotal = totalEntradas - totalSaidas - totalRateiosEnviados;

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

                        // Buscar entradas do mês aprovadas
                        var queryEntradasGrafico = _context.Entradas
                            .Where(e => e.Data >= mesInicio && e.Data <= mesFim);

                        if (centroCustoFiltro.HasValue)
                        {
                            queryEntradasGrafico = queryEntradasGrafico.Where(e => e.CentroCustoId == centroCustoFiltro.Value);
                        }

                        var idsEntradasGrafico = await queryEntradasGrafico
                            .Where(e => _context.FechamentosPeriodo.Any(f =>
                                f.CentroCustoId == e.CentroCustoId &&
                                (f.Status == StatusFechamentoPeriodo.Aprovado || f.Status == StatusFechamentoPeriodo.Processado) &&
                                e.Data >= f.DataInicio &&
                                e.Data <= f.DataFim))
                            .Select(e => e.Id)
                            .ToListAsync();

                        var entradasMesGrafico = idsEntradasGrafico.Any()
                            ? await _context.Entradas
                                .Where(e => idsEntradasGrafico.Contains(e.Id))
                                .SumAsync(e => (decimal?)e.Valor) ?? 0
                            : 0;

                        // Buscar saídas diretas do mês aprovadas
                        var querySaidasGrafico = _context.Saidas
                            .Where(s => s.Data >= mesInicio && s.Data <= mesFim);

                        if (centroCustoFiltro.HasValue)
                        {
                            querySaidasGrafico = querySaidasGrafico.Where(s => s.CentroCustoId == centroCustoFiltro.Value);
                        }

                        var idsSaidasGrafico = await querySaidasGrafico
                            .Where(s => _context.FechamentosPeriodo.Any(f =>
                                f.CentroCustoId == s.CentroCustoId &&
                                (f.Status == StatusFechamentoPeriodo.Aprovado || f.Status == StatusFechamentoPeriodo.Processado) &&
                                s.Data >= f.DataInicio &&
                                s.Data <= f.DataFim))
                            .Select(s => s.Id)
                            .ToListAsync();

                        var saidasDiretasGrafico = idsSaidasGrafico.Any()
                            ? await _context.Saidas
                                .Where(s => idsSaidasGrafico.Contains(s.Id))
                                .SumAsync(s => (decimal?)s.Valor) ?? 0
                            : 0;

                        // ✅ NOVO: Buscar rateios do mês
                        var queryRateiosGrafico = _context.ItensRateioFechamento
                            .Include(i => i.FechamentoPeriodo)
                            .Where(i => (i.FechamentoPeriodo.Status == StatusFechamentoPeriodo.Aprovado ||
                                        i.FechamentoPeriodo.Status == StatusFechamentoPeriodo.Processado) &&
                                       i.FechamentoPeriodo.DataAprovacao >= mesInicio &&
                                       i.FechamentoPeriodo.DataAprovacao <= mesFim);

                        if (centroCustoFiltro.HasValue)
                        {
                            queryRateiosGrafico = queryRateiosGrafico.Where(i => i.FechamentoPeriodo.CentroCustoId == centroCustoFiltro.Value);
                        }

                        var rateiosGrafico = await queryRateiosGrafico.SumAsync(i => (decimal?)i.ValorRateio) ?? 0;

                        // ✅ CORREÇÃO: Total saídas = saídas diretas + rateios
                        var saidasMesGrafico = saidasDiretasGrafico + rateiosGrafico;

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
                    // Buscar últimas entradas aprovadas
                    var queryUltimasEntradas = _context.Entradas.AsQueryable();

                    if (centroCustoFiltro.HasValue)
                    {
                        queryUltimasEntradas = queryUltimasEntradas.Where(e => e.CentroCustoId == centroCustoFiltro.Value);
                    }

                    var idsUltimasEntradas = await queryUltimasEntradas
                        .Where(e => _context.FechamentosPeriodo.Any(f =>
                            f.CentroCustoId == e.CentroCustoId &&
                            (f.Status == StatusFechamentoPeriodo.Aprovado || f.Status == StatusFechamentoPeriodo.Processado) &&
                            e.Data >= f.DataInicio &&
                            e.Data <= f.DataFim))
                        .OrderByDescending(e => e.Data)
                        .Take(5)
                        .Select(e => e.Id)
                        .ToListAsync();

                    var ultimasEntradas = idsUltimasEntradas.Any()
                        ? await _context.Entradas
                            .Include(e => e.PlanoDeContas)
                            .Include(e => e.CentroCusto)
                            .Where(e => idsUltimasEntradas.Contains(e.Id))
                            .OrderByDescending(e => e.Data)
                            .Select(e => new
                            {
                                Tipo = "Entrada",
                                Descricao = e.Descricao ?? e.PlanoDeContas.Nome,
                                CentroCusto = e.CentroCusto.Nome,
                                Data = e.Data,
                                Valor = e.Valor
                            })
                            .ToListAsync<object>()
                        : new List<object>();

                    // Buscar últimas saídas aprovadas
                    var queryUltimasSaidas = _context.Saidas.AsQueryable();

                    if (centroCustoFiltro.HasValue)
                    {
                        queryUltimasSaidas = queryUltimasSaidas.Where(s => s.CentroCustoId == centroCustoFiltro.Value);
                    }

                    var idsUltimasSaidas = await queryUltimasSaidas
                        .Where(s => _context.FechamentosPeriodo.Any(f =>
                            f.CentroCustoId == s.CentroCustoId &&
                            (f.Status == StatusFechamentoPeriodo.Aprovado || f.Status == StatusFechamentoPeriodo.Processado) &&
                            s.Data >= f.DataInicio &&
                            s.Data <= f.DataFim))
                        .OrderByDescending(s => s.Data)
                        .Take(5)
                        .Select(s => s.Id)
                        .ToListAsync();

                    var ultimasSaidas = idsUltimasSaidas.Any()
                        ? await _context.Saidas
                            .Include(s => s.PlanoDeContas)
                            .Include(s => s.CentroCusto)
                            .Where(s => idsUltimasSaidas.Contains(s.Id))
                            .OrderByDescending(s => s.Data)
                            .Select(s => new
                            {
                                Tipo = "Saída",
                                Descricao = s.Descricao,
                                CentroCusto = s.CentroCusto.Nome,
                                Data = s.Data,
                                Valor = s.Valor
                            })
                            .ToListAsync<object>()
                        : new List<object>();

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
                ViewBag.TotalRateiosEnviados = totalRateiosEnviados.ToString("C");
                ViewBag.DizimosMes = dizimosMes.ToString("C");
                ViewBag.FluxoCaixaData = fluxoCaixaData;
                ViewBag.DespesasData = despesasPorCategoria;
                ViewBag.UserRole = primaryRole;
                ViewBag.UserName = user.NomeCompleto;
                ViewBag.CentroCusto = user.CentroCusto?.Nome ?? "Não definido";

                // ✅ NOVO: Buscar detalhamento dos rateios por destino
                var rateiosPorDestino = await ObterRateiosPorDestino(centroCustoFiltro);
                ViewBag.RateiosPorDestino = rateiosPorDestino;

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

        // =====================================================
        // DASHBOARD ESPECÍFICO PARA PASTORES
        // =====================================================
        [AuthorizeRoles(Roles.Pastor)]
        public async Task<IActionResult> DashboardPastor()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                _logger.LogInformation($"Dashboard Pastor acessado por: {user.NomeCompleto}");

                var hoje = DateTime.Now.Date;
                var inicioMes = new DateTime(hoje.Year, hoje.Month, 1);
                var fimMes = inicioMes.AddMonths(1).AddDays(-1);

                var viewModel = new ViewModels.DashboardPastorViewModel
                {
                    NomePastor = user.NomeCompleto,
                    DataReferencia = hoje,
                    PeriodoReferencia = $"{inicioMes:MMMM yyyy}".ToUpper()
                };

                // =====================================================
                // BUSCAR TODOS OS CENTROS DE CUSTO ATIVOS (CONGREGAÇÕES)
                // =====================================================
                var centrosCusto = await _context.CentrosCusto
                    .Where(c => c.Ativo)
                    .OrderBy(c => c.Nome)
                    .ToListAsync();

                viewModel.QuantidadeCongregacoes = centrosCusto.Count;

                // =====================================================
                // PROCESSAR INDICADORES POR CONGREGAÇÃO
                // =====================================================
                var indicadoresCongregacoes = new List<ViewModels.IndicadoresCongregacaoViewModel>();

                foreach (var centro in centrosCusto)
                {
                    var indicador = await CalcularIndicadoresCongregacao(centro.Id, inicioMes, fimMes);
                    indicadoresCongregacoes.Add(indicador);

                    // Acumular totais gerais
                    viewModel.TotalReceitasGeral += indicador.ReceitasAcumuladas;
                    viewModel.TotalDespesasGeral += indicador.DespesasAcumuladas;
                    viewModel.ReceitasMesAtual += indicador.ReceitasMesAtual;
                    viewModel.DespesasMesAtual += indicador.DespesasMesAtual;
                    viewModel.TotalRateiosEnviados += indicador.RateiosEnviados;
                }

                viewModel.SaldoGeralAtual = viewModel.TotalReceitasGeral - viewModel.TotalDespesasGeral - viewModel.TotalRateiosEnviados;
                viewModel.SaldoMesAtual = viewModel.ReceitasMesAtual - viewModel.DespesasMesAtual;
                viewModel.Congregacoes = indicadoresCongregacoes.OrderByDescending(c => c.ReceitasAcumuladas).ToList();

                // =====================================================
                // MAIORES DESPESAS CONSOLIDADAS
                // =====================================================
                viewModel.MaioresDespesas = await ObterMaioresDespesas(inicioMes, fimMes);

                // =====================================================
                // RANKINGS
                // =====================================================
                viewModel.RankingReceitas = indicadoresCongregacoes
                    .OrderByDescending(c => c.ReceitasMesAtual)
                    .Take(5)
                    .Select((c, index) => new ViewModels.RankingCongregacaoViewModel
                    {
                        Posicao = index + 1,
                        NomeCongregacao = c.NomeCongregacao,
                        Valor = c.ReceitasMesAtual,
                        CorBarra = index == 0 ? "success" : index < 3 ? "primary" : "info"
                    })
                    .ToList();

                viewModel.RankingDespesas = indicadoresCongregacoes
                    .OrderByDescending(c => c.DespesasMesAtual)
                    .Take(5)
                    .Select((c, index) => new ViewModels.RankingCongregacaoViewModel
                    {
                        Posicao = index + 1,
                        NomeCongregacao = c.NomeCongregacao,
                        Valor = c.DespesasMesAtual,
                        CorBarra = index == 0 ? "danger" : index < 3 ? "warning" : "secondary"
                    })
                    .ToList();

                // =====================================================
                // TENDÊNCIAS (ÚLTIMOS 6 MESES)
                // =====================================================
                viewModel.TendenciasReceitas = await ObterTendenciasMensais();

                // =====================================================
                // ALERTAS E OBSERVAÇÕES
                // =====================================================
                viewModel.Alertas = GerarAlertas(indicadoresCongregacoes);

                await _auditService.LogAsync("DASHBOARD_PASTOR_ACCESS", "Home",
                    $"Dashboard Pastor acessado por {user.NomeCompleto}");

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao carregar Dashboard Pastor");
                TempData["Erro"] = "Erro ao carregar dashboard.";
                return View("Error", new ErrorViewModel
                {
                    RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
                });
            }
        }

        // =====================================================
        // MÉTODOS AUXILIARES PARA DASHBOARD PASTOR
        // =====================================================

        private async Task<ViewModels.IndicadoresCongregacaoViewModel> CalcularIndicadoresCongregacao(
            int centroCustoId, DateTime inicioMes, DateTime fimMes)
        {
            var centroCusto = await _context.CentrosCusto.FindAsync(centroCustoId);

            var indicador = new ViewModels.IndicadoresCongregacaoViewModel
            {
                CentroCustoId = centroCustoId,
                NomeCongregacao = centroCusto?.Nome ?? "Desconhecido",
                TipoCongregacao = centroCusto?.Tipo.ToString() ?? "N/A"
            };

            // RECEITAS ACUMULADAS (aprovadas/processadas de todos os tempos)
            var queryReceitasAcumuladas = _context.Entradas
                .Where(e => e.CentroCustoId == centroCustoId);

            var idsReceitasAcumuladas = await queryReceitasAcumuladas
                .Where(e => _context.FechamentosPeriodo.Any(f =>
                    f.CentroCustoId == e.CentroCustoId &&
                    (f.Status == StatusFechamentoPeriodo.Aprovado || f.Status == StatusFechamentoPeriodo.Processado) &&
                    e.Data >= f.DataInicio &&
                    e.Data <= f.DataFim))
                .Select(e => e.Id)
                .ToListAsync();

            indicador.ReceitasAcumuladas = idsReceitasAcumuladas.Any()
                ? await _context.Entradas
                    .Where(e => idsReceitasAcumuladas.Contains(e.Id))
                    .SumAsync(e => (decimal?)e.Valor) ?? 0
                : 0;

            // RECEITAS DO MÊS ATUAL
            var queryReceitasMes = _context.Entradas
                .Where(e => e.CentroCustoId == centroCustoId &&
                           e.Data >= inicioMes && e.Data <= fimMes);

            var idsReceitasMes = await queryReceitasMes
                .Where(e => _context.FechamentosPeriodo.Any(f =>
                    f.CentroCustoId == e.CentroCustoId &&
                    (f.Status == StatusFechamentoPeriodo.Aprovado || f.Status == StatusFechamentoPeriodo.Processado) &&
                    e.Data >= f.DataInicio &&
                    e.Data <= f.DataFim))
                .Select(e => e.Id)
                .ToListAsync();

            indicador.ReceitasMesAtual = idsReceitasMes.Any()
                ? await _context.Entradas
                    .Where(e => idsReceitasMes.Contains(e.Id))
                    .SumAsync(e => (decimal?)e.Valor) ?? 0
                : 0;

            // DESPESAS ACUMULADAS
            var queryDespesasAcumuladas = _context.Saidas
                .Where(s => s.CentroCustoId == centroCustoId);

            var idsDespesasAcumuladas = await queryDespesasAcumuladas
                .Where(s => _context.FechamentosPeriodo.Any(f =>
                    f.CentroCustoId == s.CentroCustoId &&
                    (f.Status == StatusFechamentoPeriodo.Aprovado || f.Status == StatusFechamentoPeriodo.Processado) &&
                    s.Data >= f.DataInicio &&
                    s.Data <= f.DataFim))
                .Select(s => s.Id)
                .ToListAsync();

            indicador.DespesasAcumuladas = idsDespesasAcumuladas.Any()
                ? await _context.Saidas
                    .Where(s => idsDespesasAcumuladas.Contains(s.Id))
                    .SumAsync(s => (decimal?)s.Valor) ?? 0
                : 0;

            // DESPESAS DO MÊS ATUAL
            var queryDespesasMes = _context.Saidas
                .Where(s => s.CentroCustoId == centroCustoId &&
                           s.Data >= inicioMes && s.Data <= fimMes);

            var idsDespesasMes = await queryDespesasMes
                .Where(s => _context.FechamentosPeriodo.Any(f =>
                    f.CentroCustoId == s.CentroCustoId &&
                    (f.Status == StatusFechamentoPeriodo.Aprovado || f.Status == StatusFechamentoPeriodo.Processado) &&
                    s.Data >= f.DataInicio &&
                    s.Data <= f.DataFim))
                .Select(s => s.Id)
                .ToListAsync();

            indicador.DespesasMesAtual = idsDespesasMes.Any()
                ? await _context.Saidas
                    .Where(s => idsDespesasMes.Contains(s.Id))
                    .SumAsync(s => (decimal?)s.Valor) ?? 0
                : 0;

            // RATEIOS ENVIADOS
            indicador.RateiosEnviados = await _context.ItensRateioFechamento
                .Include(i => i.FechamentoPeriodo)
                .Where(i => i.FechamentoPeriodo.CentroCustoId == centroCustoId &&
                           (i.FechamentoPeriodo.Status == StatusFechamentoPeriodo.Aprovado ||
                            i.FechamentoPeriodo.Status == StatusFechamentoPeriodo.Processado))
                .SumAsync(i => (decimal?)i.ValorRateio) ?? 0;

            // RATEIOS RECEBIDOS (destino é este centro de custo)
            indicador.RateiosRecebidos = await _context.ItensRateioFechamento
                .Include(i => i.RegraRateio)
                .Include(i => i.FechamentoPeriodo)
                .Where(i => i.RegraRateio.CentroCustoDestinoId == centroCustoId &&
                           (i.FechamentoPeriodo.Status == StatusFechamentoPeriodo.Aprovado ||
                            i.FechamentoPeriodo.Status == StatusFechamentoPeriodo.Processado))
                .SumAsync(i => (decimal?)i.ValorRateio) ?? 0;

            // SALDO ATUAL
            indicador.SaldoAtual = indicador.ReceitasAcumuladas - indicador.DespesasAcumuladas - indicador.RateiosEnviados + indicador.RateiosRecebidos;

            // PERCENTUAL DE LUCRO
            if (indicador.ReceitasAcumuladas > 0)
            {
                indicador.PercentualLucro = ((indicador.ReceitasAcumuladas - indicador.DespesasAcumuladas) / indicador.ReceitasAcumuladas) * 100;
            }

            // STATUS DE SAÚDE FINANCEIRA
            if (indicador.SaldoAtual < 0)
            {
                indicador.StatusSaude = "Crítico";
                indicador.CorIndicador = "danger";
                indicador.IconeStatus = "bi-exclamation-triangle-fill";
            }
            else if (indicador.PercentualLucro < 20)
            {
                indicador.StatusSaude = "Regular";
                indicador.CorIndicador = "warning";
                indicador.IconeStatus = "bi-exclamation-circle";
            }
            else if (indicador.PercentualLucro < 40)
            {
                indicador.StatusSaude = "Bom";
                indicador.CorIndicador = "info";
                indicador.IconeStatus = "bi-info-circle";
            }
            else
            {
                indicador.StatusSaude = "Ótimo";
                indicador.CorIndicador = "success";
                indicador.IconeStatus = "bi-check-circle-fill";
            }

            return indicador;
        }

        private async Task<List<ViewModels.MaiorDespesaViewModel>> ObterMaioresDespesas(
            DateTime inicioMes, DateTime fimMes)
        {
            var querySaidasAprovadas = _context.Saidas
                .Include(s => s.PlanoDeContas)
                .Include(s => s.CentroCusto)
                .Where(s => s.Data >= inicioMes && s.Data <= fimMes);

            var idsSaidasAprovadas = await querySaidasAprovadas
                .Where(s => _context.FechamentosPeriodo.Any(f =>
                    f.CentroCustoId == s.CentroCustoId &&
                    (f.Status == StatusFechamentoPeriodo.Aprovado || f.Status == StatusFechamentoPeriodo.Processado) &&
                    s.Data >= f.DataInicio &&
                    s.Data <= f.DataFim))
                .Select(s => s.Id)
                .ToListAsync();

            if (!idsSaidasAprovadas.Any())
                return new List<ViewModels.MaiorDespesaViewModel>();

            var maioresDespesas = await _context.Saidas
                .Include(s => s.PlanoDeContas)
                .Include(s => s.CentroCusto)
                .Where(s => idsSaidasAprovadas.Contains(s.Id))
                .GroupBy(s => new
                {
                    Categoria = s.PlanoDeContas!.Nome
                })
                .Select(g => new ViewModels.MaiorDespesaViewModel
                {
                    CategoriaDespesa = g.Key.Categoria,
                    ValorTotal = g.Sum(s => s.Valor),
                    QuantidadeOcorrencias = g.Count(),
                    CongregacoesEnvolvidas = g.Select(s => s.CentroCusto!.Nome).Distinct().ToList()
                })
                .OrderByDescending(m => m.ValorTotal)
                .Take(10)
                .ToListAsync();

            return maioresDespesas;
        }

        private async Task<List<ViewModels.TendenciaMensalViewModel>> ObterTendenciasMensais()
        {
            var tendencias = new List<ViewModels.TendenciaMensalViewModel>();
            var hoje = DateTime.Now.Date;

            for (int i = 5; i >= 0; i--)
            {
                var mesInicio = hoje.AddMonths(-i).AddDays(-(hoje.Day - 1));
                var mesFim = mesInicio.AddMonths(1).AddDays(-1);

                var queryEntradas = _context.Entradas
                    .Where(e => e.Data >= mesInicio && e.Data <= mesFim);

                var idsEntradas = await queryEntradas
                    .Where(e => _context.FechamentosPeriodo.Any(f =>
                        f.CentroCustoId == e.CentroCustoId &&
                        (f.Status == StatusFechamentoPeriodo.Aprovado || f.Status == StatusFechamentoPeriodo.Processado) &&
                        e.Data >= f.DataInicio &&
                        e.Data <= f.DataFim))
                    .Select(e => e.Id)
                    .ToListAsync();

                var totalReceitas = idsEntradas.Any()
                    ? await _context.Entradas
                        .Where(e => idsEntradas.Contains(e.Id))
                        .SumAsync(e => (decimal?)e.Valor) ?? 0
                    : 0;

                var querySaidas = _context.Saidas
                    .Where(s => s.Data >= mesInicio && s.Data <= mesFim);

                var idsSaidas = await querySaidas
                    .Where(s => _context.FechamentosPeriodo.Any(f =>
                        f.CentroCustoId == s.CentroCustoId &&
                        (f.Status == StatusFechamentoPeriodo.Aprovado || f.Status == StatusFechamentoPeriodo.Processado) &&
                        s.Data >= f.DataInicio &&
                        s.Data <= f.DataFim))
                    .Select(s => s.Id)
                    .ToListAsync();

                var totalDespesas = idsSaidas.Any()
                    ? await _context.Saidas
                        .Where(s => idsSaidas.Contains(s.Id))
                        .SumAsync(s => (decimal?)s.Valor) ?? 0
                    : 0;

                tendencias.Add(new ViewModels.TendenciaMensalViewModel
                {
                    MesAno = mesInicio.ToString("MMM/yy", new System.Globalization.CultureInfo("pt-BR")),
                    TotalReceitas = totalReceitas,
                    TotalDespesas = totalDespesas,
                    Saldo = totalReceitas - totalDespesas
                });
            }

            return tendencias;
        }

        private List<ViewModels.AlertaDashboardViewModel> GerarAlertas(
            List<ViewModels.IndicadoresCongregacaoViewModel> congregacoes)
        {
            var alertas = new List<ViewModels.AlertaDashboardViewModel>();

            // Alerta: Congregações com saldo negativo
            var congregacoesNegativas = congregacoes.Where(c => c.SaldoAtual < 0).ToList();
            if (congregacoesNegativas.Any())
            {
                alertas.Add(new ViewModels.AlertaDashboardViewModel
                {
                    Tipo = "danger",
                    Icone = "bi-exclamation-triangle-fill",
                    Titulo = "Atenção: Saldo Negativo",
                    Mensagem = $"{congregacoesNegativas.Count} congregação(ões) com saldo negativo requer atenção imediata.",
                    CongregacaoRelacionada = string.Join(", ", congregacoesNegativas.Select(c => c.NomeCongregacao))
                });
            }

            // Alerta: Congregações com despesas acima de 80% das receitas
            var congregacoesAltaDespesa = congregacoes
                .Where(c => c.ReceitasMesAtual > 0 && (c.DespesasMesAtual / c.ReceitasMesAtual) > 0.8m)
                .ToList();

            if (congregacoesAltaDespesa.Any())
            {
                alertas.Add(new ViewModels.AlertaDashboardViewModel
                {
                    Tipo = "warning",
                    Icone = "bi-exclamation-circle",
                    Titulo = "Despesas Elevadas",
                    Mensagem = $"{congregacoesAltaDespesa.Count} congregação(ões) com despesas acima de 80% das receitas.",
                    CongregacaoRelacionada = string.Join(", ", congregacoesAltaDespesa.Select(c => c.NomeCongregacao))
                });
            }

            // Alerta positivo: Congregações com melhor desempenho
            var melhorDesempenho = congregacoes
                .Where(c => c.ReceitasMesAtual > 0)
                .OrderByDescending(c => c.PercentualLucro)
                .FirstOrDefault();

            if (melhorDesempenho != null && melhorDesempenho.PercentualLucro > 40)
            {
                alertas.Add(new ViewModels.AlertaDashboardViewModel
                {
                    Tipo = "success",
                    Icone = "bi-trophy-fill",
                    Titulo = "Destaque do Mês",
                    Mensagem = $"{melhorDesempenho.NomeCongregacao} apresentou excelente desempenho com {melhorDesempenho.PercentualLucro:F1}% de rentabilidade.",
                    CongregacaoRelacionada = melhorDesempenho.NomeCongregacao
                });
            }

            return alertas;
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
            ViewBag.TotalRateiosEnviados = "R$ 0,00";
            ViewBag.DizimosMes = "R$ 0,00";
            ViewBag.FluxoCaixaData = new List<object>();
            ViewBag.DespesasData = new List<object>();
            ViewBag.RateiosPorDestino = new List<object>();
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

        /// <summary>
        /// Obtém o detalhamento dos rateios enviados agrupados por centro de custo destino
        /// </summary>
        private async Task<List<object>> ObterRateiosPorDestino(int? centroCustoFiltro)
        {
            try
            {
                var queryRateios = _context.ItensRateioFechamento
                    .Include(i => i.FechamentoPeriodo)
                    .Include(i => i.RegraRateio)
                        .ThenInclude(r => r.CentroCustoDestino)
                    .Where(i => (i.FechamentoPeriodo.Status == StatusFechamentoPeriodo.Aprovado ||
                                i.FechamentoPeriodo.Status == StatusFechamentoPeriodo.Processado));

                // Filtrar por centro de custo se necessário
                if (centroCustoFiltro.HasValue)
                {
                    queryRateios = queryRateios.Where(i => i.FechamentoPeriodo.CentroCustoId == centroCustoFiltro.Value);
                }

                var rateiosPorDestino = await queryRateios
                    .GroupBy(i => new
                    {
                        CentroCustoDestinoId = i.RegraRateio.CentroCustoDestinoId,
                        CentroCustoDestinoNome = i.RegraRateio.CentroCustoDestino.Nome
                    })
                    .Select(g => new
                    {
                        CentroCustoDestino = g.Key.CentroCustoDestinoNome,
                        TotalAcumulado = g.Sum(i => i.ValorRateio),
                        QuantidadeFechamentos = g.Select(i => i.FechamentoPeriodoId).Distinct().Count(),
                        UltimoRateio = g.Max(i => i.FechamentoPeriodo.DataAprovacao)
                    })
                    .OrderByDescending(r => r.TotalAcumulado)
                    .ToListAsync<object>();

                return rateiosPorDestino;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar rateios por destino");
                return new List<object>();
            }
        }

        // ✅ NOVO: Gerar PDF do Dashboard Pastor
        [AuthorizeRoles(Roles.Pastor)]
        [HttpGet]
        public async Task<IActionResult> GerarDashboardPdf()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                _logger.LogInformation($"Geração de PDF do Dashboard Pastor solicitada por: {user.NomeCompleto}");

                var hoje = DateTime.Now.Date;
                var inicioMes = new DateTime(hoje.Year, hoje.Month, 1);
                var fimMes = inicioMes.AddMonths(1).AddDays(-1);

                // Recarregar os dados do dashboard
                var viewModel = new ViewModels.DashboardPastorViewModel
                {
                    NomePastor = user.NomeCompleto,
                    DataReferencia = hoje,
                    PeriodoReferencia = $"{inicioMes:MMMM yyyy}".ToUpper()
                };

                var centrosCusto = await _context.CentrosCusto
                    .Where(c => c.Ativo)
                    .OrderBy(c => c.Nome)
                    .ToListAsync();

                viewModel.QuantidadeCongregacoes = centrosCusto.Count;

                var indicadoresCongregacoes = new List<ViewModels.IndicadoresCongregacaoViewModel>();

                foreach (var centro in centrosCusto)
                {
                    var indicador = await CalcularIndicadoresCongregacao(centro.Id, inicioMes, fimMes);
                    indicadoresCongregacoes.Add(indicador);

                    viewModel.TotalReceitasGeral += indicador.ReceitasAcumuladas;
                    viewModel.TotalDespesasGeral += indicador.DespesasAcumuladas;
                    viewModel.ReceitasMesAtual += indicador.ReceitasMesAtual;
                    viewModel.DespesasMesAtual += indicador.DespesasMesAtual;
                    viewModel.TotalRateiosEnviados += indicador.RateiosEnviados;
                }

                viewModel.SaldoGeralAtual = viewModel.TotalReceitasGeral - viewModel.TotalDespesasGeral - viewModel.TotalRateiosEnviados;
                viewModel.SaldoMesAtual = viewModel.ReceitasMesAtual - viewModel.DespesasMesAtual;
                viewModel.Congregacoes = indicadoresCongregacoes.OrderByDescending(c => c.ReceitasAcumuladas).ToList();
                viewModel.MaioresDespesas = await ObterMaioresDespesas(inicioMes, fimMes);

                viewModel.RankingReceitas = indicadoresCongregacoes
                    .OrderByDescending(c => c.ReceitasMesAtual)
                    .Take(5)
                    .Select((c, index) => new ViewModels.RankingCongregacaoViewModel
                    {
                        Posicao = index + 1,
                        NomeCongregacao = c.NomeCongregacao,
                        Valor = c.ReceitasMesAtual,
                        CorBarra = index == 0 ? "success" : index < 3 ? "primary" : "info"
                    })
                    .ToList();

                viewModel.RankingDespesas = indicadoresCongregacoes
                    .OrderByDescending(c => c.DespesasMesAtual)
                    .Take(5)
                    .Select((c, index) => new ViewModels.RankingCongregacaoViewModel
                    {
                        Posicao = index + 1,
                        NomeCongregacao = c.NomeCongregacao,
                        Valor = c.DespesasMesAtual,
                        CorBarra = index == 0 ? "danger" : index < 3 ? "warning" : "secondary"
                    })
                    .ToList();

                viewModel.TendenciasReceitas = await ObterTendenciasMensais();
                viewModel.Alertas = GerarAlertas(indicadoresCongregacoes);

                // Gerar PDF
                var pdfBytes = Helpers.DashboardPastorPdfHelper.GerarPdfDashboard(viewModel);

                // Registrar auditoria
                await _auditService.LogAsync("PDF_DASHBOARD_PASTOR", "Home",
                    $"PDF do Dashboard Pastor gerado por {user.NomeCompleto} - Período: {viewModel.PeriodoReferencia}");

                _logger.LogInformation($"PDF do Dashboard Pastor gerado com sucesso - Tamanho: {pdfBytes.Length} bytes");

                var nomeArquivo = $"Dashboard_Pastor_{inicioMes:yyyyMM}.pdf";

                return File(pdfBytes, "application/pdf", nomeArquivo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao gerar PDF do Dashboard Pastor");
                TempData["ErrorMessage"] = $"Erro ao gerar PDF: {ex.Message}";
                return RedirectToAction(nameof(DashboardPastor));
            }
        }
    }
}