using Microsoft.EntityFrameworkCore;
using SistemaTesourariaEclesiastica.Data;
using SistemaTesourariaEclesiastica.Enums;
using SistemaTesourariaEclesiastica.ViewModels;

namespace SistemaTesourariaEclesiastica.Services
{
    /// <summary>
    /// Serviço para geração do Relatório de Balancete Mensal
    /// Implementa a lógica conforme especificação técnica do projeto
    /// </summary>
    public class BalanceteService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<BalanceteService> _logger;

        public BalanceteService(
            ApplicationDbContext context,
            ILogger<BalanceteService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Gera o balancete mensal para um centro de custo específico
        /// </summary>
        public async Task<BalanceteMensalViewModel> GerarBalanceteMensalAsync(
            int centroCustoId,
            DateTime dataInicio,
            DateTime dataFim)
        {
            try
            {
                _logger.LogInformation($"Iniciando geração de balancete: CentroCusto={centroCustoId}, " +
                    $"Período={dataInicio:dd/MM/yyyy} a {dataFim:dd/MM/yyyy}");

                var viewModel = new BalanceteMensalViewModel
                {
                    DataInicio = dataInicio,
                    DataFim = dataFim,
                    Periodo = $"{dataInicio:dd/MM/yyyy} a {dataFim:dd/MM/yyyy}"
                };

                // Buscar centro de custo
                var centroCusto = await _context.CentrosCusto
                    .FirstOrDefaultAsync(c => c.Id == centroCustoId);

                if (centroCusto == null)
                {
                    throw new Exception($"Centro de custo ID {centroCustoId} não encontrado");
                }

                viewModel.CentroCustoNome = centroCusto.Nome;

                // ==========================================
                // 1. CALCULAR SALDO DO MÊS ANTERIOR
                // ==========================================
                viewModel.SaldoMesAnterior = await CalcularSaldoMesAnteriorAsync(centroCustoId, dataInicio);

                // ==========================================
                // 2. CALCULAR RECEITAS OPERACIONAIS
                // ==========================================
                await ProcessarReceitasAsync(viewModel, centroCustoId, dataInicio, dataFim);

                // ==========================================
                // 3. CALCULAR IMOBILIZADOS
                // ==========================================
                await ProcessarImobilizadosAsync(viewModel, centroCustoId, dataInicio, dataFim);

                // ==========================================
                // 4. CALCULAR DESPESAS ADMINISTRATIVAS
                // ==========================================
                await ProcessarDespesasAdministrativasAsync(viewModel, centroCustoId, dataInicio, dataFim);

                // ==========================================
                // 5. CALCULAR DESPESAS TRIBUTÁRIAS
                // ==========================================
                await ProcessarDespesasTributariasAsync(viewModel, centroCustoId, dataInicio, dataFim);

                // ==========================================
                // 6. CALCULAR DESPESAS FINANCEIRAS
                // ==========================================
                await ProcessarDespesasFinanceirasAsync(viewModel, centroCustoId, dataInicio, dataFim);

                // ==========================================
                // 7. CALCULAR RECOLHIMENTOS (RATEIOS)
                // ==========================================
                await ProcessarRecolhimentosAsync(viewModel, centroCustoId, dataInicio, dataFim);

                // ==========================================
                // 8. CALCULAR TOTAIS FINAIS
                // ==========================================
                CalcularTotaisFinais(viewModel);

                _logger.LogInformation($"Balancete gerado com sucesso. Saldo Final: {viewModel.Saldo:C}");

                return viewModel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao gerar balancete mensal");
                throw;
            }
        }

        /// <summary>
        /// Calcula o saldo acumulado até o mês anterior ao período selecionado
        /// </summary>
        private async Task<decimal> CalcularSaldoMesAnteriorAsync(int centroCustoId, DateTime dataInicio)
        {
            try
            {
                // Buscar todas as entradas até o dia anterior ao início do período
                var totalEntradasAnteriores = await _context.Entradas
                    .Where(e => e.CentroCustoId == centroCustoId && e.Data < dataInicio)
                    .SumAsync(e => (decimal?)e.Valor) ?? 0;

                // Buscar todas as saídas até o dia anterior ao início do período
                var totalSaidasAnteriores = await _context.Saidas
                    .Where(s => s.CentroCustoId == centroCustoId && s.Data < dataInicio)
                    .SumAsync(s => (decimal?)s.Valor) ?? 0;

                // Buscar todos os rateios de fechamentos anteriores
                var totalRateiosAnteriores = await _context.ItensRateioFechamento
                    .Include(r => r.FechamentoPeriodo)
                    .Where(r => r.FechamentoPeriodo.CentroCustoId == centroCustoId &&
                               r.FechamentoPeriodo.DataFim < dataInicio &&
                               r.FechamentoPeriodo.Status == StatusFechamentoPeriodo.Aprovado)
                    .SumAsync(r => (decimal?)r.ValorRateio) ?? 0;

                var saldo = totalEntradasAnteriores - totalSaidasAnteriores - totalRateiosAnteriores;

                _logger.LogDebug($"Saldo do mês anterior: Entradas={totalEntradasAnteriores:C}, " +
                    $"Saídas={totalSaidasAnteriores:C}, Rateios={totalRateiosAnteriores:C}, Saldo={saldo:C}");

                return saldo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao calcular saldo do mês anterior");
                return 0;
            }
        }

        /// <summary>
        /// Processa as receitas operacionais (dízimos, ofertas, votos, etc.)
        /// </summary>
        private async Task ProcessarReceitasAsync(
            BalanceteMensalViewModel viewModel,
            int centroCustoId,
            DateTime dataInicio,
            DateTime dataFim)
        {
            var receitas = await _context.Entradas
                .Include(e => e.PlanoDeContas)
                .Where(e => e.CentroCustoId == centroCustoId &&
                           e.Data >= dataInicio &&
                           e.Data <= dataFim)
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
            int centroCustoId,
            DateTime dataInicio,
            DateTime dataFim)
        {
            // Buscar entradas que sejam consideradas imobilizados
            // Critério: podem ser entradas com plano de contas específico ou tag especial
            var imobilizados = new List<ItemBalanceteViewModel>();

            // Placeholder: você pode adaptar esta lógica conforme necessário
            // Por exemplo, criar um PlanoDeContas específico para imobilizados
            // ou adicionar um campo "EhImobilizado" na tabela Entrada

            viewModel.Imobilizados = imobilizados;
            viewModel.TotalCreditoComImobilizados = viewModel.TotalCredito + imobilizados.Sum(i => i.Valor);
        }

        /// <summary>
        /// Processa despesas administrativas conforme categorias da especificação
        /// </summary>
        private async Task ProcessarDespesasAdministrativasAsync(
            BalanceteMensalViewModel viewModel,
            int centroCustoId,
            DateTime dataInicio,
            DateTime dataFim)
        {
            // Lista de categorias administrativas conforme especificação
            var categoriasAdministrativas = new[]
            {
                "Mat. de Expediente",
                "Mat. Higiene e Limpeza",
                "Despesas com Telefone",
                "Despesas com Veículo",
                "Auxílio Oferta",
                "Mão de Obra Qualificada",
                "Despesas com Medicamentos",
                "Energia Elétrica (Luz)",
                "Água",
                "Despesas Diversas",
                "Despesas com Viagens",
                "Material de Construção",
                "Material de Conservação (Tintas, etc.)",
                "Despesas com Som (Peças e Acessórios)",
                "Aluguel",
                "INSS",
                "Pagamento de Inscrição da CGADB",
                "Previdência Privada",
                "Caixa de Evangelização"
            };

            var despesas = await _context.Saidas
                .Include(s => s.PlanoDeContas)
                .Where(s => s.CentroCustoId == centroCustoId &&
                           s.Data >= dataInicio &&
                           s.Data <= dataFim &&
                           categoriasAdministrativas.Contains(s.PlanoDeContas.Nome))
                .GroupBy(s => s.PlanoDeContas.Nome)
                .Select(g => new ItemBalanceteViewModel
                {
                    Descricao = g.Key,
                    Valor = g.Sum(s => s.Valor)
                })
                .ToListAsync();

            // Garantir que todas as categorias apareçam, mesmo com valor zero
            var despesasCompletas = categoriasAdministrativas.Select(cat => new ItemBalanceteViewModel
            {
                Descricao = cat,
                Valor = despesas.FirstOrDefault(d => d.Descricao == cat)?.Valor ?? 0
            }).ToList();

            viewModel.DespesasAdministrativas = despesasCompletas;
            viewModel.SubtotalDespesasAdministrativas = despesasCompletas.Sum(d => d.Valor);

            _logger.LogDebug($"Despesas administrativas: {despesasCompletas.Count} categorias, " +
                $"Subtotal={viewModel.SubtotalDespesasAdministrativas:C}");
        }

        /// <summary>
        /// Processa despesas tributárias (IPTU, impostos, etc.)
        /// </summary>
        private async Task ProcessarDespesasTributariasAsync(
            BalanceteMensalViewModel viewModel,
            int centroCustoId,
            DateTime dataInicio,
            DateTime dataFim)
        {
            var categoriasTributarias = new[] { "IPTU", "Imposto Predial IPTR" };

            var despesas = await _context.Saidas
                .Include(s => s.PlanoDeContas)
                .Where(s => s.CentroCustoId == centroCustoId &&
                           s.Data >= dataInicio &&
                           s.Data <= dataFim &&
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

        /// <summary>
        /// Processa despesas financeiras (taxas, juros, etc.)
        /// </summary>
        private async Task ProcessarDespesasFinanceirasAsync(
            BalanceteMensalViewModel viewModel,
            int centroCustoId,
            DateTime dataInicio,
            DateTime dataFim)
        {
            var categoriasFinanceiras = new[]
            {
                "Imposto Taxas Diversas",
                "Saldo para o mês"
            };

            var despesas = await _context.Saidas
                .Include(s => s.PlanoDeContas)
                .Where(s => s.CentroCustoId == centroCustoId &&
                           s.Data >= dataInicio &&
                           s.Data <= dataFim &&
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
            DateTime dataFim)
        {
            // Buscar rateios aplicados em fechamentos aprovados no período
            var recolhimentos = await _context.ItensRateioFechamento
                .Include(r => r.RegraRateio)
                .ThenInclude(rr => rr.CentroCustoDestino)
                .Include(r => r.FechamentoPeriodo)
                .Where(r => r.FechamentoPeriodo.CentroCustoId == centroCustoId &&
                           r.FechamentoPeriodo.DataInicio >= dataInicio &&
                           r.FechamentoPeriodo.DataFim <= dataFim &&
                           r.FechamentoPeriodo.Status == StatusFechamentoPeriodo.Aprovado)
                .GroupBy(r => new
                {
                    r.RegraRateio.CentroCustoDestino.Nome,
                    r.Percentual
                })
                .Select(g => new ItemRecolhimentoViewModel
                {
                    Destino = g.Key.Nome,
                    Percentual = g.Key.Percentual,
                    Valor = g.Sum(r => r.ValorRateio)
                })
                .ToListAsync();

            // Se não houver fechamentos, calcular rateios diretamente das regras
            if (!recolhimentos.Any())
            {
                recolhimentos = await CalcularRateiosSemFechamentoAsync(
                    centroCustoId,
                    dataInicio,
                    dataFim,
                    viewModel.TotalCredito);
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
                .Include(r => r.CentroCustoDestino)
                .Where(r => r.CentroCustoOrigemId == centroCustoId && r.Ativo)
                .ToListAsync();

            var recolhimentos = regrasRateio.Select(regra => new ItemRecolhimentoViewModel
            {
                Destino = regra.CentroCustoDestino.Nome,
                Percentual = regra.Percentual,
                Valor = Math.Round(valorBase * (regra.Percentual / 100), 2)
            }).ToList();

            return recolhimentos;
        }

        /// <summary>
        /// Calcula os totais finais do balancete
        /// </summary>
        private void CalcularTotaisFinais(BalanceteMensalViewModel viewModel)
        {
            // Total do Débito = Despesas + Recolhimentos
            viewModel.TotalDebito = viewModel.SubtotalDespesasAdministrativas +
                                   viewModel.SubtotalDespesasTributarias +
                                   viewModel.SubtotalDespesasFinanceiras +
                                   viewModel.TotalRecolhimentos;

            // Saldo = Saldo Anterior + Total Crédito - Total Débito
            viewModel.Saldo = viewModel.SaldoMesAnterior +
                             viewModel.TotalCreditoComImobilizados -
                             viewModel.TotalDebito;

            _logger.LogDebug($"Totais finais calculados: TotalDebito={viewModel.TotalDebito:C}, Saldo={viewModel.Saldo:C}");
        }
    }
}