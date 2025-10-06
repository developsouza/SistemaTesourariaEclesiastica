using SistemaTesourariaEclesiastica.Data;
using SistemaTesourariaEclesiastica.Enums;

namespace SistemaTesourariaEclesiastica.Services
{
    public class LancamentoAprovadoService
    {
        private readonly ApplicationDbContext _context;

        public LancamentoAprovadoService(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Obtém os IDs de todas as Entradas que estão em fechamentos aprovados
        /// </summary>
        public async Task<List<int>> ObterEntradasAprovadasIdsAsync(int? centroCustoId = null, DateTime? dataInicio = null, DateTime? dataFim = null)
        {
            var query = _context.Entradas.AsQueryable();

            if (centroCustoId.HasValue)
            {
                query = query.Where(e => e.CentroCustoId == centroCustoId.Value);
            }

            if (dataInicio.HasValue)
            {
                query = query.Where(e => e.Data >= dataInicio.Value);
            }

            if (dataFim.HasValue)
            {
                query = query.Where(e => e.Data <= dataFim.Value);
            }

            // Buscar fechamentos aprovados que incluem essas entradas
            var entradasAprovadas = await query
                .Where(e => _context.FechamentosPeriodo.Any(f =>
                    f.CentroCustoId == e.CentroCustoId &&
                    f.Status == StatusFechamentoPeriodo.Aprovado &&
                    e.Data >= f.DataInicio &&
                    e.Data <= f.DataFim))
                .Select(e => e.Id)
                .ToListAsync();

            return entradasAprovadas;
        }

        /// <summary>
        /// Obtém os IDs de todas as Saídas que estão em fechamentos aprovados
        /// </summary>
        public async Task<List<int>> ObterSaidasAprovadasIdsAsync(int? centroCustoId = null, DateTime? dataInicio = null, DateTime? dataFim = null)
        {
            var query = _context.Saidas.AsQueryable();

            if (centroCustoId.HasValue)
            {
                query = query.Where(s => s.CentroCustoId == centroCustoId.Value);
            }

            if (dataInicio.HasValue)
            {
                query = query.Where(s => s.Data >= dataInicio.Value);
            }

            if (dataFim.HasValue)
            {
                query = query.Where(s => s.Data <= dataFim.Value);
            }

            // Buscar fechamentos aprovados que incluem essas saídas
            var saidasAprovadas = await query
                .Where(s => _context.FechamentosPeriodo.Any(f =>
                    f.CentroCustoId == s.CentroCustoId &&
                    f.Status == StatusFechamentoPeriodo.Aprovado &&
                    s.Data >= f.DataInicio &&
                    s.Data <= f.DataFim))
                .Select(s => s.Id)
                .ToListAsync();

            return saidasAprovadas;
        }

        /// <summary>
        /// Calcula o total de entradas aprovadas
        /// </summary>
        public async Task<decimal> ObterTotalEntradasAprovadasAsync(int? centroCustoId = null, DateTime? dataInicio = null, DateTime? dataFim = null)
        {
            var idsAprovados = await ObterEntradasAprovadasIdsAsync(centroCustoId, dataInicio, dataFim);

            if (!idsAprovados.Any())
                return 0;

            return await _context.Entradas
                .Where(e => idsAprovados.Contains(e.Id))
                .SumAsync(e => e.Valor);
        }

        /// <summary>
        /// Calcula o total de saídas aprovadas
        /// </summary>
        public async Task<decimal> ObterTotalSaidasAprovadasAsync(int? centroCustoId = null, DateTime? dataInicio = null, DateTime? dataFim = null)
        {
            var idsAprovados = await ObterSaidasAprovadasIdsAsync(centroCustoId, dataInicio, dataFim);

            if (!idsAprovados.Any())
                return 0;

            return await _context.Saidas
                .Where(s => idsAprovados.Contains(s.Id))
                .SumAsync(s => s.Valor);
        }
    }
}
