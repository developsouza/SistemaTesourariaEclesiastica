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

            return (true, string.Empty);
        }

        /// <summary>
        /// Retorna lista de erros de validação para uma entrada (compatibilidade com versão anterior)
        /// </summary>
        public async Task<List<string>> ValidateEntrada(Entrada entrada)
        {
            var errors = new List<string>();

            var result = await ValidateEntradaAsync(entrada);
            if (!result.IsValid)
            {
                errors.Add(result.ErrorMessage);
            }

            return errors;
        }

        /// <summary>
        /// Verifica se um fechamento de período pode ser criado
        /// </summary>
        public async Task<(bool IsValid, string ErrorMessage)> ValidateFechamentoPeriodoAsync(FechamentoPeriodo fechamento)
        {
            // Verifica se já existe fechamento para o período
            var fechamentoExistente = await _context.FechamentosPeriodo
                .Where(f => f.CentroCustoId == fechamento.CentroCustoId &&
                           f.Ano == fechamento.Ano &&
                           f.Mes == fechamento.Mes &&
                           f.Id != fechamento.Id)
                .FirstOrDefaultAsync();

            if (fechamentoExistente != null)
                return (false, "Já existe um fechamento para este período neste centro de custo.");

            // Verifica se o período é válido
            if (fechamento.Ano < 2020 || fechamento.Ano > DateTime.Now.Year + 1)
                return (false, "Ano inválido para fechamento.");

            if (fechamento.Mes < 1 || fechamento.Mes > 12)
                return (false, "Mês inválido para fechamento.");

            // Verifica se não está tentando fechar período muito futuro
            var dataFechamento = new DateTime(fechamento.Ano, fechamento.Mes, 1);
            if (dataFechamento > DateTime.Now.AddMonths(1))
                return (false, "Não é possível fechar períodos muito futuros.");

            return (true, string.Empty);
        }

        /// <summary>
        /// Valida transferência interna
        /// </summary>
        public async Task<(bool IsValid, string ErrorMessage)> ValidateTransferenciaInternaAsync(TransferenciaInterna transferencia)
        {
            // Verifica se os centros de custo são diferentes
            if (transferencia.CentroCustoOrigemId == transferencia.CentroCustoDestinoId)
                return (false, "Os centros de custo de origem e destino devem ser diferentes.");

            // Verifica se o valor é positivo
            if (transferencia.Valor <= 0)
                return (false, "O valor da transferência deve ser maior que zero.");

            // Verifica se os centros de custo estão ativos
            var centroCustoOrigem = await _context.CentrosCusto.FindAsync(transferencia.CentroCustoOrigemId);
            if (centroCustoOrigem == null || !centroCustoOrigem.Ativo)
                return (false, "O centro de custo de origem não está ativo.");

            var centroCustoDestino = await _context.CentrosCusto.FindAsync(transferencia.CentroCustoDestinoId);
            if (centroCustoDestino == null || !centroCustoDestino.Ativo)
                return (false, "O centro de custo de destino não está ativo.");

            // Verifica se os meios de pagamento estão ativos
            var meioPagamentoOrigem = await _context.MeiosDePagamento.FindAsync(transferencia.MeioDePagamentoOrigemId);
            if (meioPagamentoOrigem == null || !meioPagamentoOrigem.Ativo)
                return (false, "O meio de pagamento de origem não está ativo.");

            var meioPagamentoDestino = await _context.MeiosDePagamento.FindAsync(transferencia.MeioDePagamentoDestinoId);
            if (meioPagamentoDestino == null || !meioPagamentoDestino.Ativo)
                return (false, "O meio de pagamento de destino não está ativo.");

            return (true, string.Empty);
        }

        /// <summary>
        /// Verifica se um membro pode ser cadastrado/atualizado
        /// </summary>
        public async Task<(bool IsValid, string ErrorMessage)> ValidateMembroAsync(Membro membro, int? membroIdToIgnore = null)
        {
            // Verifica CPF duplicado
            if (!string.IsNullOrEmpty(membro.CPF))
            {
                var cpfExistente = await _context.Membros
                    .Where(m => m.CPF == membro.CPF &&
                               (membroIdToIgnore == null || m.Id != membroIdToIgnore))
                    .FirstOrDefaultAsync();

                if (cpfExistente != null)
                    return (false, "Já existe um membro cadastrado com este CPF.");
            }

            // Verifica se o centro de custo está ativo
            var centroCusto = await _context.CentrosCusto.FindAsync(membro.CentroCustoId);
            if (centroCusto == null || !centroCusto.Ativo)
                return (false, "O centro de custo selecionado não está ativo.");

            return (true, string.Empty);
        }

        /// <summary>
        /// Calcula saldo disponível em um centro de custo
        /// </summary>
        public async Task<decimal> CalcularSaldoCentroCustoAsync(int centroCustoId, DateTime? dataLimite = null)
        {
            var dataFim = dataLimite ?? DateTime.Now;

            var totalEntradas = await _context.Entradas
                .Where(e => e.CentroCustoId == centroCustoId && e.Data <= dataFim)
                .SumAsync(e => e.Valor);

            var totalSaidas = await _context.Saidas
                .Where(s => s.CentroCustoId == centroCustoId && s.Data <= dataFim)
                .SumAsync(s => s.Valor);

            return totalEntradas - totalSaidas;
        }

        /// <summary>
        /// Verifica se há saldo suficiente para uma saída
        /// </summary>
        public async Task<bool> VerificarSaldoSuficienteAsync(int centroCustoId, decimal valor, DateTime data)
        {
            var saldoDisponivel = await CalcularSaldoCentroCustoAsync(centroCustoId, data);
            return saldoDisponivel >= valor;
        }

        /// <summary>
        /// Verifica se um plano de contas pode ser desativado
        /// </summary>
        public async Task<(bool CanDeactivate, string ErrorMessage)> CanDeactivatePlanoContasAsync(int planoContasId)
        {
            // Verifica se há entradas ou saídas usando este plano
            var hasEntradas = await _context.Entradas.AnyAsync(e => e.PlanoDeContasId == planoContasId);
            var hasSaidas = await _context.Saidas.AnyAsync(s => s.PlanoDeContasId == planoContasId);

            if (hasEntradas || hasSaidas)
                return (false, "Não é possível desativar este plano de contas pois existem movimentações vinculadas a ele.");

            return (true, string.Empty);
        }

        /// <summary>
        /// Verifica se um centro de custo pode ser desativado
        /// </summary>
        public async Task<(bool CanDeactivate, string ErrorMessage)> CanDeactivateCentroCustoAsync(int centroCustoId)
        {
            // Verifica se há membros ativos
            var hasMembrosAtivos = await _context.Membros.AnyAsync(m => m.CentroCustoId == centroCustoId && m.Ativo);
            if (hasMembrosAtivos)
                return (false, "Não é possível desativar este centro de custo pois existem membros ativos vinculados a ele.");

            // Verifica se há usuários ativos
            var hasUsuariosAtivos = await _context.Users.AnyAsync(u => u.CentroCustoId == centroCustoId && u.Ativo);
            if (hasUsuariosAtivos)
                return (false, "Não é possível desativar este centro de custo pois existem usuários ativos vinculados a ele.");

            // Verifica se há movimentações recentes (últimos 3 meses)
            var tresMesesAtras = DateTime.Now.AddMonths(-3);
            var hasMovimentacaoRecente = await _context.Entradas.AnyAsync(e => e.CentroCustoId == centroCustoId && e.Data >= tresMesesAtras) ||
                                        await _context.Saidas.AnyAsync(s => s.CentroCustoId == centroCustoId && s.Data >= tresMesesAtras);

            if (hasMovimentacaoRecente)
                return (false, "Não é possível desativar este centro de custo pois existem movimentações recentes (últimos 3 meses).");

            return (true, string.Empty);
        }
    }
}