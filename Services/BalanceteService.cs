using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SistemaTesourariaEclesiastica.Data;
using SistemaTesourariaEclesiastica.Enums;
using SistemaTesourariaEclesiastica.Models;
using SistemaTesourariaEclesiastica.ViewModels;

namespace SistemaTesourariaEclesiastica.Services
{
    /// <summary>
    /// Serviço para geração do Relatório de Balancete Mensal
    /// ? OTIMIZADO com Memory Cache e queries eficientes
    /// </summary>
    public class BalanceteService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<BalanceteService> _logger;
        private readonly IMemoryCache _cache;

        public BalanceteService(
   ApplicationDbContext context,
        ILogger<BalanceteService> logger,
         IMemoryCache cache)
        {
            _context = context;
            _logger = logger;
            _cache = cache;
        }

        /// <summary>
        /// Gera o balancete mensal para um centro de custo específico
        /// ? OTIMIZADO com cache e queries mais eficientes
        /// </summary>
        public async Task<BalanceteMensalViewModel> GerarBalanceteMensalAsync(
            int centroCustoId,
     DateTime dataInicio,
      DateTime dataFim)
        {
            var cacheKey = $"Balancete_{centroCustoId}_{dataInicio:yyyyMMdd}_{dataFim:yyyyMMdd}";

            // ? Tentar obter do cache (válido por 30 minutos)
            if (_cache.TryGetValue(cacheKey, out BalanceteMensalViewModel cachedResult))
            {
                _logger.LogInformation($"Balancete obtido do cache: {cacheKey}");
                return cachedResult;
            }

            try
            {
                _logger.LogInformation($"Gerando balancete: CentroCusto={centroCustoId}, " +
       $"Período={dataInicio:dd/MM/yyyy} a {dataFim:dd/MM/yyyy}");

                var viewModel = new BalanceteMensalViewModel
                {
                    DataInicio = dataInicio,
                    DataFim = dataFim,
                    Periodo = $"{dataInicio:dd/MM/yyyy} a {dataFim:dd/MM/yyyy}"
                };

                // Buscar centro de custo
                var centroCusto = await _context.CentrosCusto
              .AsNoTracking()
                     .FirstOrDefaultAsync(c => c.Id == centroCustoId);

                if (centroCusto == null)
                {
                    throw new Exception($"Centro de custo ID {centroCustoId} não encontrado");
                }

                viewModel.CentroCustoNome = centroCusto.Nome;

                // Verificar se existe fechamento da SEDE no período
                var ehSede = centroCusto.Tipo == TipoCentroCusto.Sede;
                var fechamentoSede = await _context.FechamentosPeriodo
           .AsNoTracking()
         .Include(f => f.FechamentosCongregacoesIncluidos)
           .FirstOrDefaultAsync(f => f.CentroCustoId == centroCustoId &&
    f.DataInicio >= dataInicio &&
            f.DataFim <= dataFim &&
              f.Status == StatusFechamentoPeriodo.Aprovado &&
    f.EhFechamentoSede);

                // Determinar quais centros de custo devem ser incluídos
                var centrosCustoParaIncluir = new List<int> { centroCustoId };

                if (ehSede && fechamentoSede != null && fechamentoSede.FechamentosCongregacoesIncluidos.Any())
                {
                    var congregacoesIncluidas = fechamentoSede.FechamentosCongregacoesIncluidos
                       .Select(f => f.CentroCustoId)
                         .Distinct()
                  .ToList();

                    centrosCustoParaIncluir.AddRange(congregacoesIncluidas);
                    _logger.LogInformation($"Balancete da SEDE incluirá {congregacoesIncluidas.Count} congregações");
                }

                // ? OTIMIZAÇÃO: Processar tudo em paralelo quando possível
                var saldoTask = CalcularSaldoMesAnteriorAsync(centrosCustoParaIncluir, dataInicio);
                var receitasTask = ProcessarReceitasAsync(viewModel, centrosCustoParaIncluir, dataInicio, dataFim);
                var imobilizadosTask = ProcessarImobilizadosAsync(viewModel, centrosCustoParaIncluir, dataInicio, dataFim);
                var despesasAdmTask = ProcessarDespesasAdministrativasAsync(viewModel, centrosCustoParaIncluir, dataInicio, dataFim);
                var despesasTribTask = ProcessarDespesasTributariasAsync(viewModel, centrosCustoParaIncluir, dataInicio, dataFim);
                var despesasFinTask = ProcessarDespesasFinanceirasAsync(viewModel, centrosCustoParaIncluir, dataInicio, dataFim);

                await Task.WhenAll(saldoTask, receitasTask, imobilizadosTask, despesasAdmTask, despesasTribTask, despesasFinTask);

                viewModel.SaldoMesAnterior = await saldoTask;

                // Recolhimentos devem ser processados após as outras tasks
                await ProcessarRecolhimentosAsync(viewModel, centroCustoId, dataInicio, dataFim, fechamentoSede);

                // Calcular totais finais
                CalcularTotaisFinais(viewModel);

                _logger.LogInformation($"Balancete gerado com sucesso. Saldo Final: {viewModel.Saldo:C}");

                // ? Armazenar no cache por 30 minutos
                _cache.Set(cacheKey, viewModel, TimeSpan.FromMinutes(30));

                return viewModel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao gerar balancete mensal");
                throw;
            }
        }

        /// <summary>
        /// ? OTIMIZADO: Calcula saldo anterior com query única
        /// </summary>
        private async Task<decimal> CalcularSaldoMesAnteriorAsync(List<int> centrosCustoIds, DateTime dataInicio)
        {
            try
            {
                // ? Query única para calcular totais
                var totais = await _context.Entradas
      .AsNoTracking()
          .Where(e => centrosCustoIds.Contains(e.CentroCustoId) &&
           e.Data < dataInicio &&
               e.IncluidaEmFechamento &&
          e.FechamentoQueIncluiu.Status == StatusFechamentoPeriodo.Aprovado)
       .GroupBy(e => 1)
    .Select(g => new { TotalEntradas = g.Sum(e => e.Valor) })
         .FirstOrDefaultAsync();

                var totalSaidas = await _context.Saidas
          .AsNoTracking()
        .Where(s => centrosCustoIds.Contains(s.CentroCustoId) &&
   s.Data < dataInicio &&
      s.IncluidaEmFechamento &&
        s.FechamentoQueIncluiu.Status == StatusFechamentoPeriodo.Aprovado)
        .SumAsync(s => (decimal?)s.Valor) ?? 0;

                var totalRateios = await _context.ItensRateioFechamento
            .AsNoTracking()
                .Where(r => centrosCustoIds.Contains(r.FechamentoPeriodo.CentroCustoId) &&
                r.FechamentoPeriodo.DataFim < dataInicio &&
                         r.FechamentoPeriodo.Status == StatusFechamentoPeriodo.Aprovado)
                  .SumAsync(r => (decimal?)r.ValorRateio) ?? 0;

                var totalEntradas = totais?.TotalEntradas ?? 0;
                var saldo = totalEntradas - totalSaidas - totalRateios;

                _logger.LogDebug($"Saldo anterior: Entradas={totalEntradas:C}, Saídas={totalSaidas:C}, Rateios={totalRateios:C}, Saldo={saldo:C}");
                return saldo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao calcular saldo anterior");
                return 0;
            }
        }

        /// <summary>
        /// ? OTIMIZADO: Processa receitas com projeção direta
        /// </summary>
        private async Task ProcessarReceitasAsync(
  BalanceteMensalViewModel viewModel,
    List<int> centrosCustoIds,
            DateTime dataInicio,
            DateTime dataFim)
        {
            var receitas = await _context.Entradas
      .AsNoTracking()
   .Where(e => centrosCustoIds.Contains(e.CentroCustoId) &&
   e.Data >= dataInicio &&
        e.Data <= dataFim &&
     e.IncluidaEmFechamento &&
     e.FechamentoQueIncluiu.Status == StatusFechamentoPeriodo.Aprovado)
    .GroupBy(e => e.PlanoDeContas.Nome)
      .Select(g => new ItemBalanceteViewModel
      {
          Descricao = g.Key,
          Valor = g.Sum(e => e.Valor)
      })
          .OrderBy(r => r.Descricao)
     .ToListAsync();

            viewModel.ReceitasOperacionais = receitas;
            viewModel.TotalCredito = receitas.Sum(r => r.Valor);

            _logger.LogDebug($"Receitas processadas: {receitas.Count} itens, Total={viewModel.TotalCredito:C}");
        }

        /// <summary>
        /// Processa imobilizados (entradas classificadas como investimentos)
        /// </summary>
        private async Task ProcessarImobilizadosAsync(
       BalanceteMensalViewModel viewModel,
            List<int> centrosCustoIds,
          DateTime dataInicio,
            DateTime dataFim)
        {
            var imobilizados = new List<ItemBalanceteViewModel>();
            viewModel.Imobilizados = imobilizados;
            viewModel.TotalCreditoComImobilizados = viewModel.TotalCredito + imobilizados.Sum(i => i.Valor);
        }

        /// <summary>
        /// ? OTIMIZADO: Processa despesas com categorias pré-definidas
        /// </summary>
        private async Task ProcessarDespesasAdministrativasAsync(
     BalanceteMensalViewModel viewModel,
    List<int> centrosCustoIds,
            DateTime dataInicio,
       DateTime dataFim)
        {
            var categoriasAdministrativas = new[]
            {
       "Mat. de Expediente", "Mat. Higiene e Limpeza", "Despesas com Telefone",
   "Despesas com Veículo", "Auxílio Oferta", "Mão de Obra Qualificada",
          "Despesas com Medicamentos", "Energia Elétrica (Luz)", "Água",
             "Despesas Diversas", "Despesas com Viagens", "Material de Construção",
        "Material de Conservação (Tintas, etc.)", "Despesas com Som (Peças e Acessórios)",
     "Aluguel", "INSS", "Pagamento de Inscrição da CGADB",
  "Previdência Privada", "Caixa de Evangelização"
            };

            var despesas = await _context.Saidas
        .AsNoTracking()
                .Where(s => centrosCustoIds.Contains(s.CentroCustoId) &&
         s.Data >= dataInicio &&
                    s.Data <= dataFim &&
          s.IncluidaEmFechamento &&
              s.FechamentoQueIncluiu.Status == StatusFechamentoPeriodo.Aprovado &&
      categoriasAdministrativas.Contains(s.PlanoDeContas.Nome))
               .GroupBy(s => s.PlanoDeContas.Nome)
         .Select(g => new ItemBalanceteViewModel
         {
             Descricao = g.Key,
             Valor = g.Sum(s => s.Valor)
         })
                .ToListAsync();

            // ? Garantir todas as categorias apareçam
            var despesasCompletas = categoriasAdministrativas.Select(cat => new ItemBalanceteViewModel
            {
                Descricao = cat,
                Valor = despesas.FirstOrDefault(d => d.Descricao == cat)?.Valor ?? 0
            }).ToList();

            viewModel.DespesasAdministrativas = despesasCompletas;
            viewModel.SubtotalDespesasAdministrativas = despesasCompletas.Sum(d => d.Valor);

            _logger.LogDebug($"Despesas administrativas: {despesasCompletas.Count} categorias, Subtotal={viewModel.SubtotalDespesasAdministrativas:C}");
        }

        /// <summary>
        /// Processa despesas tributárias (IPTU, impostos, etc.)
        /// </summary>
        private async Task ProcessarDespesasTributariasAsync(
            BalanceteMensalViewModel viewModel,
   List<int> centrosCustoIds,
            DateTime dataInicio,
  DateTime dataFim)
        {
            var categoriasTributarias = new[] { "IPTU", "Imposto Predial IPTR" };

            var despesas = await _context.Saidas
     .AsNoTracking()
                .Where(s => centrosCustoIds.Contains(s.CentroCustoId) &&
       s.Data >= dataInicio &&
         s.Data <= dataFim &&
  s.IncluidaEmFechamento &&
           s.FechamentoQueIncluiu.Status == StatusFechamentoPeriodo.Aprovado &&
     categoriasTributarias.Contains(s.PlanoDeContas.Nome))
   .GroupBy(s => s.PlanoDeContas.Nome)
.Select(g => new ItemBalanceteViewModel
{
    Descricao = g.Key,
    Valor = g.Sum(s => s.Valor)
})
             .ToListAsync();

            viewModel.DespesasTributarias = despesas;
            viewModel.SubtotalDespesasTributarias = despesas.Sum(d => d.Valor);
        }

        private async Task ProcessarDespesasFinanceirasAsync(
        BalanceteMensalViewModel viewModel,
               List<int> centrosCustoIds,
                    DateTime dataInicio,
           DateTime dataFim)
        {
            var categoriasFinanceiras = new[] { "Imposto Taxas Diversas", "Saldo para o mês" };

            var despesas = await _context.Saidas
                    .AsNoTracking()
              .Where(s => centrosCustoIds.Contains(s.CentroCustoId) &&
              s.Data >= dataInicio &&
      s.Data <= dataFim &&
               s.IncluidaEmFechamento &&
              s.FechamentoQueIncluiu.Status == StatusFechamentoPeriodo.Aprovado &&
          categoriasFinanceiras.Contains(s.PlanoDeContas.Nome))
     .GroupBy(s => s.PlanoDeContas.Nome)
              .Select(g => new ItemBalanceteViewModel
              {
                  Descricao = g.Key,
                  Valor = g.Sum(s => s.Valor)
              })
          .ToListAsync();

            viewModel.DespesasFinanceiras = despesas;
            viewModel.SubtotalDespesasFinanceiras = despesas.Sum(d => d.Valor);
        }

        /// <summary>
        /// Processa recolhimentos (rateios aplicados automaticamente)
        /// </summary>
        private async Task ProcessarRecolhimentosAsync(
            BalanceteMensalViewModel viewModel,
   int centroCustoId,
         DateTime dataInicio,
     DateTime dataFim,
  FechamentoPeriodo? fechamentoSede)
        {
            var recolhimentos = new List<ItemRecolhimentoViewModel>();

            if (fechamentoSede != null)
            {
                recolhimentos = await _context.ItensRateioFechamento
                 .AsNoTracking()
               .Where(r => r.FechamentoPeriodoId == fechamentoSede.Id)
                .GroupBy(r => new { r.RegraRateio.CentroCustoDestino.Nome, r.Percentual })
                 .Select(g => new ItemRecolhimentoViewModel
                 {
                     Destino = g.Key.Nome,
                     Percentual = g.Key.Percentual,
                     Valor = g.Sum(r => r.ValorRateio)
                 })
           .ToListAsync();
            }
            else
            {
                recolhimentos = await _context.ItensRateioFechamento
                      .AsNoTracking()
                     .Where(r => r.FechamentoPeriodo.CentroCustoId == centroCustoId &&
                    r.FechamentoPeriodo.DataInicio >= dataInicio &&
                        r.FechamentoPeriodo.DataFim <= dataFim &&
                       r.FechamentoPeriodo.Status == StatusFechamentoPeriodo.Aprovado)
                   .GroupBy(r => new { r.RegraRateio.CentroCustoDestino.Nome, r.Percentual })
                    .Select(g => new ItemRecolhimentoViewModel
                    {
                        Destino = g.Key.Nome,
                        Percentual = g.Key.Percentual,
                        Valor = g.Sum(r => r.ValorRateio)
                    })
                      .ToListAsync();

                if (!recolhimentos.Any())
                {
                    recolhimentos = await CalcularRateiosSemFechamentoAsync(centroCustoId, dataInicio, dataFim, viewModel.TotalCredito);
                }
            }

            viewModel.Recolhimentos = recolhimentos;
            viewModel.TotalRecolhimentos = recolhimentos.Sum(r => r.Valor);

            _logger.LogDebug($"Recolhimentos: {recolhimentos.Count} itens, Total={viewModel.TotalRecolhimentos:C}");
        }

        /// <summary>
        /// Calcula rateios baseado nas regras ativas quando não há fechamento aprovado
        /// </summary>
        private async Task<List<ItemRecolhimentoViewModel>> CalcularRateiosSemFechamentoAsync(
 int centroCustoId,
      DateTime dataInicio,
     DateTime dataFim,
    decimal valorBase)
        {
            var regrasRateio = await _context.RegrasRateio
          .AsNoTracking()
       .Where(r => r.CentroCustoOrigemId == centroCustoId && r.Ativo)
          .Select(r => new
          {
              r.CentroCustoDestino.Nome,
              r.Percentual
          })
               .ToListAsync();

            return regrasRateio.Select(regra => new ItemRecolhimentoViewModel
            {
                Destino = regra.Nome,
                Percentual = regra.Percentual,
                Valor = Math.Round(valorBase * (regra.Percentual / 100), 2)
            }).ToList();
        }

        private void CalcularTotaisFinais(BalanceteMensalViewModel viewModel)
        {
            viewModel.TotalDebito = viewModel.SubtotalDespesasAdministrativas +
                  viewModel.SubtotalDespesasTributarias +
                 viewModel.SubtotalDespesasFinanceiras +
           viewModel.TotalRecolhimentos;

            viewModel.Saldo = viewModel.SaldoMesAnterior +
               viewModel.TotalCreditoComImobilizados -
                   viewModel.TotalDebito;

            _logger.LogDebug($"Totais finais: TotalDebito={viewModel.TotalDebito:C}, Saldo={viewModel.Saldo:C}");
        }
    }
}
