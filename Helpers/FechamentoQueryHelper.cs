using Microsoft.EntityFrameworkCore;
using SistemaTesourariaEclesiastica.Data;
using SistemaTesourariaEclesiastica.Enums;
using SistemaTesourariaEclesiastica.Models;
using System.Linq.Expressions;

namespace SistemaTesourariaEclesiastica.Helpers
{
    /// <summary>
    /// Helper para queries relacionadas a fechamentos de período.
    /// Centraliza lógicas comuns e evita duplicação de código.
    /// </summary>
    public static class FechamentoQueryHelper
    {
        /// <summary>
        /// Expressão para filtrar entradas que NÃO foram incluídas em fechamentos APROVADOS.
        /// </summary>
        public static Expression<Func<Entrada, bool>> EntradasNaoIncluidasEmFechamentosAprovados(
            int centroCustoId, DateTime dataInicio, DateTime dataFim, int? fechamentoAtualId = null)
        {
            return e => e.CentroCustoId == centroCustoId &&
                        e.Data >= dataInicio &&
                        e.Data <= dataFim &&
                        (!e.IncluidaEmFechamento ||
                         (fechamentoAtualId.HasValue && e.FechamentoQueIncluiuId == fechamentoAtualId));
        }

        /// <summary>
        /// Expressão para filtrar saídas que NÃO foram incluídas em fechamentos APROVADOS.
        /// </summary>
        public static Expression<Func<Saida, bool>> SaidasNaoIncluidasEmFechamentosAprovados(
            int centroCustoId, DateTime dataInicio, DateTime dataFim, int? fechamentoAtualId = null)
        {
            return s => s.CentroCustoId == centroCustoId &&
                        s.Data >= dataInicio &&
                        s.Data <= dataFim &&
                        (!s.IncluidaEmFechamento ||
                         (fechamentoAtualId.HasValue && s.FechamentoQueIncluiuId == fechamentoAtualId));
        }

        /// <summary>
        /// Verifica se existem lançamentos novos (não incluídos em fechamentos aprovados) no período.
        /// ✅ OTIMIZADO: Usa short-circuit evaluation e AnyAsync para máxima performance.
        /// </summary>
        public static async Task<bool> TemLancamentosNovos(
            ApplicationDbContext context,
            int centroCustoId,
            DateTime dataInicio,
            DateTime dataFim,
            int? fechamentoAtualId = null)
        {
            // ✅ Short-circuit: se houver entradas, não precisa verificar saídas
            return await context.Entradas
                .AsNoTracking()
                .AnyAsync(EntradasNaoIncluidasEmFechamentosAprovados(centroCustoId, dataInicio, dataFim, fechamentoAtualId))
                || await context.Saidas
                    .AsNoTracking()
                    .AnyAsync(SaidasNaoIncluidasEmFechamentosAprovados(centroCustoId, dataInicio, dataFim, fechamentoAtualId));
        }

        /// <summary>
        /// Calcula totais de entradas e saídas (APENAS lançamentos não incluídos em fechamentos aprovados).
        /// ✅ CORRIGIDO: Executa queries SEQUENCIALMENTE para evitar erro de concorrência no DbContext.
        /// O EF Core não permite múltiplas operações simultâneas no mesmo contexto.
        /// </summary>
        public static async Task<TotaisFechamento> CalcularTotais(
            ApplicationDbContext context,
            int centroCustoId,
            DateTime dataInicio,
            DateTime dataFim,
            int? fechamentoAtualId = null)
        {
            // ✅ EXECUTAR SEQUENCIALMENTE - DbContext não suporta operações paralelas
            var totaisEntradas = await context.Entradas
                .AsNoTracking()
                .Where(EntradasNaoIncluidasEmFechamentosAprovados(centroCustoId, dataInicio, dataFim, fechamentoAtualId))
                .GroupBy(e => e.MeioDePagamento.TipoCaixa)
                .Select(g => new { TipoCaixa = g.Key, Total = g.Sum(e => e.Valor) })
                .ToListAsync();

            var totaisSaidas = await context.Saidas
                .AsNoTracking()
                .Where(SaidasNaoIncluidasEmFechamentosAprovados(centroCustoId, dataInicio, dataFim, fechamentoAtualId))
                .GroupBy(s => s.MeioDePagamento.TipoCaixa)
                .Select(g => new { TipoCaixa = g.Key, Total = g.Sum(s => s.Valor) })
                .ToListAsync();

            // Processar resultados
            var totais = new TotaisFechamento
            {
                EntradasFisicas = totaisEntradas.FirstOrDefault(t => t.TipoCaixa == TipoCaixa.Fisico)?.Total ?? 0,
                EntradasDigitais = totaisEntradas.FirstOrDefault(t => t.TipoCaixa == TipoCaixa.Digital)?.Total ?? 0,
                SaidasFisicas = totaisSaidas.FirstOrDefault(t => t.TipoCaixa == TipoCaixa.Fisico)?.Total ?? 0,
                SaidasDigitais = totaisSaidas.FirstOrDefault(t => t.TipoCaixa == TipoCaixa.Digital)?.Total ?? 0
            };

            // Calcular totais
            totais.TotalEntradas = totais.EntradasFisicas + totais.EntradasDigitais;
            totais.TotalSaidas = totais.SaidasFisicas + totais.SaidasDigitais;
            totais.BalancoFisico = totais.EntradasFisicas - totais.SaidasFisicas;
            totais.BalancoDigital = totais.EntradasDigitais - totais.SaidasDigitais;

            return totais;
        }

        /// <summary>
        /// Verifica se já existe fechamento aprovado no período.
        /// ✅ OTIMIZADO: Usa AsNoTracking para consultas somente leitura.
        /// </summary>
        public static async Task<FechamentoPeriodo?> BuscarFechamentoAprovadoNoPeriodo(
            ApplicationDbContext context,
            int centroCustoId,
            DateTime dataInicio,
            DateTime dataFim,
            int? excluirFechamentoId = null)
        {
            var query = context.FechamentosPeriodo
                .AsNoTracking()
                .Where(f => f.CentroCustoId == centroCustoId &&
                            f.Status == StatusFechamentoPeriodo.Aprovado &&
                            f.DataInicio == dataInicio &&
                            f.DataFim == dataFim);

            if (excluirFechamentoId.HasValue)
            {
                query = query.Where(f => f.Id != excluirFechamentoId.Value);
            }

            return await query.FirstOrDefaultAsync();
        }
    }

    /// <summary>
    /// DTO para retorno de totais calculados.
    /// </summary>
    public class TotaisFechamento
    {
        public decimal EntradasFisicas { get; set; }
        public decimal EntradasDigitais { get; set; }
        public decimal SaidasFisicas { get; set; }
        public decimal SaidasDigitais { get; set; }
        public decimal TotalEntradas { get; set; }
        public decimal TotalSaidas { get; set; }
        public decimal BalancoFisico { get; set; }
        public decimal BalancoDigital { get; set; }
    }
}
