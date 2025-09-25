using Microsoft.EntityFrameworkCore;
using SistemaTesourariaEclesiastica.Data;
using SistemaTesourariaEclesiastica.Models;

namespace SistemaTesourariaEclesiastica.Services
{
    public class BusinessRulesService
    {
        private readonly ApplicationDbContext _context;

        public BusinessRulesService(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Valida se uma entrada pode ser criada/editada
        /// </summary>
        public async Task<(bool IsValid, string ErrorMessage)> ValidateEntradaAsync(Entrada entrada, int? entradaIdToIgnore = null)
        {
            // Verifica se o valor é positivo
            if (entrada.Valor <= 0)
                return (false, "O valor da entrada deve ser maior que zero.");

            // Verifica se a data não é muito antiga (mais de 2 anos)
            if (entrada.Data < DateTime.Now.AddYears(-2))
                return (false, "Não é possível registrar entradas com mais de 2 anos.");

            // Verifica se a data não é muito futura (mais de 1 mês)
            if (entrada.Data > DateTime.Now.AddMonths(1))
                return (false, "Não é possível registrar entradas com mais de 1 mês no futuro.");

            // Verifica se o centro de custo está ativo
            var centroCusto = await _context.CentrosCusto.FindAsync(entrada.CentroCustoId);
            if (centroCusto == null || !centroCusto.Ativo)
                return (false, "O centro de custo selecionado não está ativo.");

            // Verifica se o plano de contas está ativo
            var planoContas = await _context.PlanosDeContas.FindAsync(entrada.PlanoDeContasId);
            if (planoContas == null || !planoContas.Ativo)
                return (false, "O plano de contas selecionado não está ativo.");

            // Verifica se o meio de pagamento está ativo
            var meioPagamento = await _context.MeiosDePagamento.FindAsync(entrada.MeioDePagamentoId);
            if (meioPagamento == null || !meioPagamento.Ativo)
                return (false, "O meio de pagamento selecionado não está ativo.");

            // Verifica duplicatas (mesmo valor, data, centro de custo e plano de contas)
            var duplicata = await _context.Entradas
                .Where(e => e.Valor == entrada.Valor &&
                           e.Data.Date == entrada.Data.Date &&
                           e.CentroCustoId == entrada.CentroCustoId &&
                           e.PlanoDeContasId == entrada.PlanoDeContasId &&
                           (entradaIdToIgnore == null || e.Id != entradaIdToIgnore))
                .FirstOrDefaultAsync();

            if (duplicata != null)
                return (false, "Já existe uma entrada com os mesmos dados (valor, data, centro de custo e plano de contas).");

            return (true, string.Empty);
        }

        /// <summary>
        /// Valida se uma saída pode ser criada/editada
        /// </summary>
        public async Task<(bool IsValid, string ErrorMessage)> ValidateSaidaAsync(Saida saida, int? saidaIdToIgnore = null)
        {
            // Verifica se o valor é positivo
            if (saida.Valor <= 0)
                return (false, "O valor da saída deve ser maior que zero.");

            // Verifica se a data não é muito antiga (mais de 2 anos)
            if (saida.Data < DateTime.Now.AddYears(-2))
                return (false, "Não é possível registrar saídas com mais de 2 anos.");

            // Verifica se a data não é muito futura (mais de 1 mês)
            if (saida.Data > DateTime.Now.AddMonths(1))
                return (false, "Não é possível registrar saídas com mais de 1 mês no futuro.");

            // Verifica se o centro de custo está ativo
            var centroCusto = await _context.CentrosCusto.FindAsync(saida.CentroCustoId);
            if (centroCusto == null || !centroCusto.Ativo)
                return (false, "O centro de custo selecionado não está ativo.");

            // Verifica se o plano de contas está ativo
            var planoContas = await _context.PlanosDeContas.FindAsync(saida.PlanoDeContasId);
            if (planoContas == null || !planoContas.Ativo)
                return (false, "O plano de contas selecionado não está ativo.");

            // Verifica se o meio de pagamento está ativo
            var meioPagamento = await _context.MeiosDePagamento.FindAsync(saida.MeioDePagamentoId);
            if (meioPagamento == null || !meioPagamento.Ativo)
                return (false, "O meio de pagamento selecionado não está ativo.");

            // Verifica se há saldo suficiente no meio de pagamento
            var saldoMeioPagamento = await CalcularSaldoMeioPagamentoAsync(saida.MeioDePagamentoId, saida.Data);
            var valorConsiderar = saidaIdToIgnore.HasValue ? 
                saida.Valor - (await _context.Saidas.Where(s => s.Id == saidaIdToIgnore.Value).Select(s => s.Valor).FirstOrDefaultAsync()) : 
                saida.Valor;

            if (saldoMeioPagamento < valorConsiderar)
                return (false, $"Saldo insuficiente no meio de pagamento. Saldo disponível: {saldoMeioPagamento:C}");

            return (true, string.Empty);
        }

        /// <summary>
        /// Valida se uma transferência interna pode ser criada/editada
        /// </summary>
        public async Task<(bool IsValid, string ErrorMessage)> ValidateTransferenciaInternaAsync(TransferenciaInterna transferencia, int? transferenciaIdToIgnore = null)
        {
            // Verifica se o valor é positivo
            if (transferencia.Valor <= 0)
                return (false, "O valor da transferência deve ser maior que zero.");

            // Verifica se origem e destino são diferentes
            if (transferencia.MeioDePagamentoOrigemId == transferencia.MeioDePagamentoDestinoId)
                return (false, "O meio de pagamento de origem deve ser diferente do destino.");

            // Verifica se a data não é muito antiga (mais de 1 ano)
            if (transferencia.Data < DateTime.Now.AddYears(-1))
                return (false, "Não é possível registrar transferências com mais de 1 ano.");

            // Verifica se a data não é muito futura (mais de 1 semana)
            if (transferencia.Data > DateTime.Now.AddDays(7))
                return (false, "Não é possível registrar transferências com mais de 1 semana no futuro.");

            // Verifica se os meios de pagamento estão ativos
            var meioOrigem = await _context.MeiosDePagamento.FindAsync(transferencia.MeioDePagamentoOrigemId);
            if (meioOrigem == null || !meioOrigem.Ativo)
                return (false, "O meio de pagamento de origem não está ativo.");

            var meioDestino = await _context.MeiosDePagamento.FindAsync(transferencia.MeioDePagamentoDestinoId);
            if (meioDestino == null || !meioDestino.Ativo)
                return (false, "O meio de pagamento de destino não está ativo.");

            // Verifica se há saldo suficiente no meio de pagamento de origem
            var saldoOrigem = await CalcularSaldoMeioPagamentoAsync(transferencia.MeioDePagamentoOrigemId, transferencia.Data);
            var valorConsiderar = transferenciaIdToIgnore.HasValue ? 
                transferencia.Valor - (await _context.TransferenciasInternas.Where(t => t.Id == transferenciaIdToIgnore.Value).Select(t => t.Valor).FirstOrDefaultAsync()) : 
                transferencia.Valor;

            if (saldoOrigem < valorConsiderar)
                return (false, $"Saldo insuficiente no meio de pagamento de origem. Saldo disponível: {saldoOrigem:C}");

            return (true, string.Empty);
        }

        /// <summary>
        /// Calcula o saldo de um meio de pagamento até uma determinada data
        /// </summary>
        public async Task<decimal> CalcularSaldoMeioPagamentoAsync(int meioDePagamentoId, DateTime? dataLimite = null)
        {
            dataLimite ??= DateTime.Now;

            var entradas = await _context.Entradas
                .Where(e => e.MeioDePagamentoId == meioDePagamentoId && e.Data <= dataLimite)
                .SumAsync(e => e.Valor);

            var saidas = await _context.Saidas
                .Where(s => s.MeioDePagamentoId == meioDePagamentoId && s.Data <= dataLimite)
                .SumAsync(s => s.Valor);

            var transferenciasEntrada = await _context.TransferenciasInternas
                .Where(t => t.MeioDePagamentoDestinoId == meioDePagamentoId && t.Data <= dataLimite)
                .SumAsync(t => t.Valor);

            var transferenciasSaida = await _context.TransferenciasInternas
                .Where(t => t.MeioDePagamentoOrigemId == meioDePagamentoId && t.Data <= dataLimite)
                .SumAsync(t => t.Valor);

            return entradas + transferenciasEntrada - saidas - transferenciasSaida;
        }

        /// <summary>
        /// Verifica se um período pode ser fechado
        /// </summary>
        public async Task<(bool CanClose, string ErrorMessage)> CanClosePeriodAsync(int centroCustoId, int ano, int mes)
        {
            // Verifica se já existe fechamento para o período
            var fechamentoExistente = await _context.FechamentosPeriodo
                .Where(f => f.CentroCustoId == centroCustoId && f.Ano == ano && f.Mes == mes)
                .FirstOrDefaultAsync();

            if (fechamentoExistente != null)
                return (false, "Já existe um fechamento para este período.");

            // Verifica se há movimentações no período
            var inicioMes = new DateTime(ano, mes, 1);
            var fimMes = inicioMes.AddMonths(1).AddDays(-1);

            var temEntradas = await _context.Entradas
                .AnyAsync(e => e.CentroCustoId == centroCustoId && e.Data >= inicioMes && e.Data <= fimMes);

            var temSaidas = await _context.Saidas
                .AnyAsync(s => s.CentroCustoId == centroCustoId && s.Data >= inicioMes && s.Data <= fimMes);

            if (!temEntradas && !temSaidas)
                return (false, "Não há movimentações para fechar neste período.");

            // Verifica se o período anterior está fechado (se não for o primeiro mês)
            if (mes > 1 || ano > DateTime.Now.Year - 10)
            {
                var mesAnterior = mes == 1 ? 12 : mes - 1;
                var anoAnterior = mes == 1 ? ano - 1 : ano;

                var fechamentoAnterior = await _context.FechamentosPeriodo
                    .Where(f => f.CentroCustoId == centroCustoId && f.Ano == anoAnterior && f.Mes == mesAnterior)
                    .FirstOrDefaultAsync();

                if (fechamentoAnterior == null)
                    return (false, "O período anterior deve ser fechado primeiro.");
            }

            return (true, string.Empty);
        }

        /// <summary>
        /// Verifica se um membro pode ser inativado
        /// </summary>
        public async Task<(bool CanDeactivate, string ErrorMessage)> CanDeactivateMemberAsync(int membroId)
        {
            // Verifica se há entradas vinculadas ao membro nos últimos 12 meses
            var dataLimite = DateTime.Now.AddMonths(-12);
            var temEntradasRecentes = await _context.Entradas
                .AnyAsync(e => e.MembroId == membroId && e.Data >= dataLimite);

            if (temEntradasRecentes)
                return (false, "Não é possível inativar um membro que possui entradas nos últimos 12 meses.");

            return (true, string.Empty);
        }

        /// <summary>
        /// Verifica se um centro de custo pode ser inativado
        /// </summary>
        public async Task<(bool CanDeactivate, string ErrorMessage)> CanDeactivateCentroCustoAsync(int centroCustoId)
        {
            // Verifica se há movimentações vinculadas ao centro de custo nos últimos 6 meses
            var dataLimite = DateTime.Now.AddMonths(-6);
            
            var temEntradasRecentes = await _context.Entradas
                .AnyAsync(e => e.CentroCustoId == centroCustoId && e.Data >= dataLimite);

            var temSaidasRecentes = await _context.Saidas
                .AnyAsync(s => s.CentroCustoId == centroCustoId && s.Data >= dataLimite);

            if (temEntradasRecentes || temSaidasRecentes)
                return (false, "Não é possível inativar um centro de custo que possui movimentações nos últimos 6 meses.");

            return (true, string.Empty);
        }
    }
}
