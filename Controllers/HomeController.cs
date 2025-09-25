using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaTesourariaEclesiastica.Models;
using SistemaTesourariaEclesiastica.Data;
using SistemaTesourariaEclesiastica.ViewModels;

namespace SistemaTesourariaEclesiastica.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationDbContext _context;

    public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var hoje = DateTime.Now.Date;
        var inicioMes = new DateTime(hoje.Year, hoje.Month, 1);
        var fimMes = inicioMes.AddMonths(1).AddDays(-1);

        // Estatísticas do mês atual
        var entradasMes = await _context.Entradas
            .Where(e => e.Data >= inicioMes && e.Data <= fimMes)
            .SumAsync(e => e.Valor);

        var saidasMes = await _context.Saidas
            .Where(s => s.Data >= inicioMes && s.Data <= fimMes)
            .SumAsync(s => s.Valor);

        // Saldo total (todas as contas)
        var totalEntradas = await _context.Entradas.SumAsync(e => e.Valor);
        var totalSaidas = await _context.Saidas.SumAsync(s => s.Valor);
        var saldoTotal = totalEntradas - totalSaidas;

        // Dízimos do mês (assumindo que existe um plano de contas específico para dízimos)
        var dizimosMes = await _context.Entradas
            .Include(e => e.PlanoDeContas)
            .Where(e => e.Data >= inicioMes && e.Data <= fimMes && 
                       e.PlanoDeContas.Nome.ToLower().Contains("dízimo"))
            .SumAsync(e => e.Valor);

        // Dados para gráfico de fluxo de caixa (últimos 6 meses)
        var fluxoCaixaData = new List<object>();
        var despesasData = new List<object>();
        
        for (int i = 5; i >= 0; i--)
        {
            var mesInicio = hoje.AddMonths(-i).AddDays(-(hoje.Day - 1));
            var mesFim = mesInicio.AddMonths(1).AddDays(-1);

            var entradasMesGrafico = await _context.Entradas
                .Where(e => e.Data >= mesInicio && e.Data <= mesFim)
                .SumAsync(e => e.Valor);

            var saidasMesGrafico = await _context.Saidas
                .Where(s => s.Data >= mesInicio && s.Data <= mesFim)
                .SumAsync(s => s.Valor);

            fluxoCaixaData.Add(new
            {
                mes = mesInicio.ToString("MMM"),
                entradas = entradasMesGrafico,
                saidas = saidasMesGrafico
            });
        }

        // Distribuição de despesas por centro de custo (mês atual)
        var despesasPorCentroCusto = await _context.Saidas
            .Include(s => s.CentroCusto)
            .Where(s => s.Data >= inicioMes && s.Data <= fimMes && s.CentroCusto != null)
            .GroupBy(s => s.CentroCusto.Nome)
            .Select(g => new
            {
                centroCusto = g.Key,
                valor = g.Sum(s => s.Valor)
            })
            .ToListAsync(); // Traz os dados para memória primeiro

        // Depois ordena e pega os top 5
        var resultado = despesasPorCentroCusto
            .OrderByDescending(x => x.valor)
            .Take(5)
            .ToList();

        // Atividades recentes (últimas 10)
        var atividadesRecentes = new List<object>();

        var ultimasEntradas = await _context.Entradas
            .Include(e => e.Membro)
            .Include(e => e.PlanoDeContas)
            .OrderByDescending(e => e.Data)
            .Take(5)
            .Select(e => new
            {
                tipo = "entrada",
                titulo = "Nova entrada registrada",
                descricao = $"{e.PlanoDeContas.Nome} - {(e.Membro != null ? e.Membro.Nome : "Anônimo")} - {e.Valor:C}",
                data = e.Data,
                icone = "success"
            })
            .ToListAsync();

        var ultimasSaidas = await _context.Saidas
            .Include(s => s.Fornecedor)
            .Include(s => s.PlanoDeContas)
            .OrderByDescending(s => s.Data)
            .Take(5)
            .Select(s => new
            {
                tipo = "saida",
                titulo = "Nova despesa registrada",
                descricao = $"{s.PlanoDeContas.Nome} - {(s.Fornecedor != null ? s.Fornecedor.Nome : "Sem fornecedor")} - {s.Valor:C}",
                data = s.Data,
                icone = "danger"
            })
            .ToListAsync();

        atividadesRecentes.AddRange(ultimasEntradas);
        atividadesRecentes.AddRange(ultimasSaidas);
        atividadesRecentes = atividadesRecentes.OrderByDescending(a => ((dynamic)a).data).Take(10).ToList();

        // Alertas importantes
        var alertas = new List<object>();

        // Verificar se há centros de custo sem movimentação no mês
        var centrosSemMovimentacao = await _context.CentrosCusto
            .Where(c => c.Ativo && !_context.Entradas.Any(e => e.CentroCustoId == c.Id && e.Data >= inicioMes) &&
                       !_context.Saidas.Any(s => s.CentroCustoId == c.Id && s.Data >= inicioMes))
            .CountAsync();

        if (centrosSemMovimentacao > 0)
        {
            alertas.Add(new
            {
                tipo = "warning",
                titulo = "Centros de custo sem movimentação",
                descricao = $"{centrosSemMovimentacao} centro(s) de custo sem movimentação neste mês."
            });
        }

        // Verificar saldo baixo (menos de R$ 1000)
        if (saldoTotal < 1000)
        {
            alertas.Add(new
            {
                tipo = "danger",
                titulo = "Saldo baixo",
                descricao = "O saldo atual está abaixo de R$ 1.000,00. Considere revisar as despesas."
            });
        }

        ViewBag.EntradasMes = entradasMes.ToString("C");
        ViewBag.SaidasMes = saidasMes.ToString("C");
        ViewBag.SaldoTotal = saldoTotal.ToString("C");
        ViewBag.DizimosMes = dizimosMes.ToString("C");
        ViewBag.FluxoCaixaData = fluxoCaixaData;
        ViewBag.DespesasPorCentroCusto = despesasPorCentroCusto;
        ViewBag.AtividadesRecentes = atividadesRecentes;
        ViewBag.Alertas = alertas;

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
}
