using Microsoft.EntityFrameworkCore;
using SistemaTesourariaEclesiastica.Data;
using SistemaTesourariaEclesiastica.Enums;
using SistemaTesourariaEclesiastica.ViewModels;

namespace SistemaTesourariaEclesiastica.Services
{
    /// <summary>
    /// Serviço responsável por diagnosticar problemas de consistência no sistema financeiro.
    /// Identifica discrepâncias, lançamentos duplicados, valores incorretos e problemas de integridade.
    /// </summary>
    public class ConsistenciaService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ConsistenciaService> _logger;

        public ConsistenciaService(ApplicationDbContext context, ILogger<ConsistenciaService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Executa diagnóstico completo de consistência do sistema
        /// </summary>
        public async Task<RelatorioConsistenciaViewModel> ExecutarDiagnosticoCompleto()
        {
            _logger.LogInformation("Iniciando diagnóstico completo de consistência");

            var relatorio = new RelatorioConsistenciaViewModel
            {
                DataExecucao = DateTime.Now,
                Inconsistencias = new List<InconsistenciaViewModel>()
            };

            try
            {
                // 1. Validar lançamentos duplicados
                await ValidarLancamentosDuplicados(relatorio);

                // 2. Validar fechamentos com totais incorretos
                await ValidarTotaisFechamentos(relatorio);

                // 3. Validar integridade referencial
                await ValidarIntegridadeReferencial(relatorio);

                // 4. Validar lançamentos órfãos (incluídos em fechamentos que não existem mais)
                await ValidarLancamentosOrfaos(relatorio);

                // 5. Validar transferências internas incorretas
                await ValidarTransferenciasInternas(relatorio);

                // 6. Validar rateios aplicados
                await ValidarRateiosAplicados(relatorio);

                // 7. Validar saldos de centros de custo
                await ValidarSaldosCentrosCusto(relatorio);

                // 8. Validar consistência de meios de pagamento (Físico vs Digital)
                await ValidarConsistenciaMeiosPagamento(relatorio);

                // 9. Validar datas futuras excessivas
                await ValidarDatasFuturas(relatorio);

                // 10. Validar valores negativos ou zero
                await ValidarValoresInvalidos(relatorio);

                relatorio.TotalInconsistencias = relatorio.Inconsistencias.Count;
                relatorio.InconsistenciasCriticas = relatorio.Inconsistencias.Count(i => i.Severidade == SeveridadeInconsistencia.Critica);
                relatorio.InconsistenciasAvisos = relatorio.Inconsistencias.Count(i => i.Severidade == SeveridadeInconsistencia.Aviso);
                relatorio.InconsistenciasInformacoes = relatorio.Inconsistencias.Count(i => i.Severidade == SeveridadeInconsistencia.Informacao);

                _logger.LogInformation($"Diagnóstico concluído: {relatorio.TotalInconsistencias} inconsistências encontradas");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao executar diagnóstico de consistência");
                relatorio.Inconsistencias.Add(new InconsistenciaViewModel
                {
                    Tipo = "Erro do Sistema",
                    Descricao = $"Erro ao executar diagnóstico: {ex.Message}",
                    Severidade = SeveridadeInconsistencia.Critica
                });
            }

            return relatorio;
        }

        #region Validações Individuais

        /// <summary>
        /// Valida se existem lançamentos duplicados (mesma data, valor, centro de custo)
        /// </summary>
        private async Task ValidarLancamentosDuplicados(RelatorioConsistenciaViewModel relatorio)
        {
            _logger.LogInformation("Validando lançamentos duplicados");

            // Entradas duplicadas
            var entradasDuplicadas = await _context.Entradas
                .GroupBy(e => new { e.Data.Date, e.Valor, e.CentroCustoId, e.PlanoDeContasId })
                .Where(g => g.Count() > 1)
                .Select(g => new
                {
                    g.Key.Date,
                    g.Key.Valor,
                    g.Key.CentroCustoId,
                    g.Key.PlanoDeContasId,
                    Quantidade = g.Count(),
                    Ids = g.Select(e => e.Id).ToList()
                })
                .ToListAsync();

            foreach (var duplicata in entradasDuplicadas)
            {
                var centroCusto = await _context.CentrosCusto.FindAsync(duplicata.CentroCustoId);
                var planoContas = await _context.PlanosDeContas.FindAsync(duplicata.PlanoDeContasId);

                relatorio.Inconsistencias.Add(new InconsistenciaViewModel
                {
                    Tipo = "Lançamento Duplicado",
                    Categoria = "Entradas",
                    Descricao = $"Encontradas {duplicata.Quantidade} entradas idênticas: Data {duplicata.Date:dd/MM/yyyy}, " +
                                $"Valor {duplicata.Valor:C}, Centro: {centroCusto?.Nome}, Plano: {planoContas?.Nome}",
                    Severidade = SeveridadeInconsistencia.Aviso,
                    EntidadeId = string.Join(", ", duplicata.Ids),
                    EntidadeTipo = "Entrada",
                    AcaoCorretivaSugerida = "Verifique se os lançamentos são realmente duplicados e exclua os extras manualmente."
                });
            }

            // Saídas duplicadas
            var saidasDuplicadas = await _context.Saidas
                .GroupBy(s => new { s.Data.Date, s.Valor, s.CentroCustoId, s.PlanoDeContasId })
                .Where(g => g.Count() > 1)
                .Select(g => new
                {
                    g.Key.Date,
                    g.Key.Valor,
                    g.Key.CentroCustoId,
                    g.Key.PlanoDeContasId,
                    Quantidade = g.Count(),
                    Ids = g.Select(s => s.Id).ToList()
                })
                .ToListAsync();

            foreach (var duplicata in saidasDuplicadas)
            {
                var centroCusto = await _context.CentrosCusto.FindAsync(duplicata.CentroCustoId);
                var planoContas = await _context.PlanosDeContas.FindAsync(duplicata.PlanoDeContasId);

                relatorio.Inconsistencias.Add(new InconsistenciaViewModel
                {
                    Tipo = "Lançamento Duplicado",
                    Categoria = "Saídas",
                    Descricao = $"Encontradas {duplicata.Quantidade} saídas idênticas: Data {duplicata.Date:dd/MM/yyyy}, " +
                                $"Valor {duplicata.Valor:C}, Centro: {centroCusto?.Nome}, Plano: {planoContas?.Nome}",
                    Severidade = SeveridadeInconsistencia.Aviso,
                    EntidadeId = string.Join(", ", duplicata.Ids),
                    EntidadeTipo = "Saida",
                    AcaoCorretivaSugerida = "Verifique se os lançamentos são realmente duplicados e exclua os extras manualmente."
                });
            }
        }

        /// <summary>
        /// Valida se os totais dos fechamentos batem com os lançamentos incluídos
        /// </summary>
        private async Task ValidarTotaisFechamentos(RelatorioConsistenciaViewModel relatorio)
        {
            _logger.LogInformation("Validando totais de fechamentos");

            var fechamentos = await _context.FechamentosPeriodo
                .Include(f => f.CentroCusto)
                .Include(f => f.DetalhesFechamento)
                .Where(f => f.Status == StatusFechamentoPeriodo.Aprovado || f.Status == StatusFechamentoPeriodo.Pendente)
                .ToListAsync();

            foreach (var fechamento in fechamentos)
            {
                // Calcular totais a partir dos lançamentos incluídos
                var entradas = await _context.Entradas
                    .Include(e => e.MeioDePagamento)
                    .Where(e => e.FechamentoQueIncluiuId == fechamento.Id)
                    .ToListAsync();

                var saidas = await _context.Saidas
                    .Include(s => s.MeioDePagamento)
                    .Where(s => s.FechamentoQueIncluiuId == fechamento.Id)
                    .ToListAsync();

                var totalEntradasReal = entradas.Sum(e => e.Valor);
                var totalSaidasReal = saidas.Sum(s => s.Valor);

                var totalEntradasFisicasReal = entradas.Where(e => e.MeioDePagamento.TipoCaixa == TipoCaixa.Fisico).Sum(e => e.Valor);
                var totalEntradasDigitaisReal = entradas.Where(e => e.MeioDePagamento.TipoCaixa == TipoCaixa.Digital).Sum(e => e.Valor);
                var totalSaidasFisicasReal = saidas.Where(s => s.MeioDePagamento.TipoCaixa == TipoCaixa.Fisico).Sum(s => s.Valor);
                var totalSaidasDigitaisReal = saidas.Where(s => s.MeioDePagamento.TipoCaixa == TipoCaixa.Digital).Sum(s => s.Valor);

                // Comparar com os valores armazenados no fechamento
                var diferencaEntradas = fechamento.TotalEntradas - totalEntradasReal;
                var diferencaSaidas = fechamento.TotalSaidas - totalSaidasReal;
                var diferencaEntradasFisicas = fechamento.TotalEntradasFisicas - totalEntradasFisicasReal;
                var diferencaEntradasDigitais = fechamento.TotalEntradasDigitais - totalEntradasDigitaisReal;
                var diferencaSaidasFisicas = fechamento.TotalSaidasFisicas - totalSaidasFisicasReal;
                var diferencaSaidasDigitais = fechamento.TotalSaidasDigitais - totalSaidasDigitaisReal;

                if (Math.Abs(diferencaEntradas) > 0.01m || Math.Abs(diferencaSaidas) > 0.01m)
                {
                    relatorio.Inconsistencias.Add(new InconsistenciaViewModel
                    {
                        Tipo = "Total Incorreto em Fechamento",
                        Categoria = "Fechamentos",
                        Descricao = $"Fechamento #{fechamento.Id} ({fechamento.CentroCusto.Nome} - {fechamento.Mes}/{fechamento.Ano}): " +
                                    $"Total Entradas Registrado: {fechamento.TotalEntradas:C}, Real: {totalEntradasReal:C} (Diferença: {diferencaEntradas:C}). " +
                                    $"Total Saídas Registrado: {fechamento.TotalSaidas:C}, Real: {totalSaidasReal:C} (Diferença: {diferencaSaidas:C}).",
                        Severidade = SeveridadeInconsistencia.Critica,
                        EntidadeId = fechamento.Id.ToString(),
                        EntidadeTipo = "FechamentoPeriodo",
                        AcaoCorretivaSugerida = "Recalcular totais do fechamento ou verificar lançamentos incluídos incorretamente."
                    });
                }

                if (Math.Abs(diferencaEntradasFisicas) > 0.01m || Math.Abs(diferencaEntradasDigitais) > 0.01m ||
                    Math.Abs(diferencaSaidasFisicas) > 0.01m || Math.Abs(diferencaSaidasDigitais) > 0.01m)
                {
                    relatorio.Inconsistencias.Add(new InconsistenciaViewModel
                    {
                        Tipo = "Divergência Caixa Físico/Digital",
                        Categoria = "Fechamentos",
                        Descricao = $"Fechamento #{fechamento.Id} ({fechamento.CentroCusto.Nome}): " +
                                    $"Entradas Físicas: Registrado {fechamento.TotalEntradasFisicas:C}, Real {totalEntradasFisicasReal:C} ({diferencaEntradasFisicas:C}). " +
                                    $"Entradas Digitais: Registrado {fechamento.TotalEntradasDigitais:C}, Real {totalEntradasDigitaisReal:C} ({diferencaEntradasDigitais:C}). " +
                                    $"Saídas Físicas: Registrado {fechamento.TotalSaidasFisicas:C}, Real {totalSaidasFisicasReal:C} ({diferencaSaidasFisicas:C}). " +
                                    $"Saídas Digitais: Registrado {fechamento.TotalSaidasDigitais:C}, Real {totalSaidasDigitaisReal:C} ({diferencaSaidasDigitais:C}).",
                        Severidade = SeveridadeInconsistencia.Critica,
                        EntidadeId = fechamento.Id.ToString(),
                        EntidadeTipo = "FechamentoPeriodo",
                        AcaoCorretivaSugerida = "Recalcular balanços físico e digital do fechamento."
                    });
                }

                // Validar cálculo do saldo final
                var balancoCalculado = (fechamento.BalancoFisico + fechamento.BalancoDigital) - fechamento.TotalRateios;
                if (Math.Abs(fechamento.SaldoFinal - balancoCalculado) > 0.01m)
                {
                    relatorio.Inconsistencias.Add(new InconsistenciaViewModel
                    {
                        Tipo = "Saldo Final Incorreto",
                        Categoria = "Fechamentos",
                        Descricao = $"Fechamento #{fechamento.Id}: Saldo Final registrado {fechamento.SaldoFinal:C}, " +
                                    $"deveria ser {balancoCalculado:C} (Diferença: {fechamento.SaldoFinal - balancoCalculado:C})",
                        Severidade = SeveridadeInconsistencia.Critica,
                        EntidadeId = fechamento.Id.ToString(),
                        EntidadeTipo = "FechamentoPeriodo",
                        AcaoCorretivaSugerida = "Recalcular saldo final do fechamento."
                    });
                }
            }
        }

        /// <summary>
        /// Valida integridade referencial (FKs órfãs)
        /// </summary>
        private async Task ValidarIntegridadeReferencial(RelatorioConsistenciaViewModel relatorio)
        {
            _logger.LogInformation("Validando integridade referencial");

            // Entradas sem MeioDePagamento válido
            var entradasSemMeio = await _context.Entradas
                .Where(e => !_context.MeiosDePagamento.Any(m => m.Id == e.MeioDePagamentoId))
                .Select(e => e.Id)
                .ToListAsync();

            if (entradasSemMeio.Any())
            {
                relatorio.Inconsistencias.Add(new InconsistenciaViewModel
                {
                    Tipo = "Referência Inválida",
                    Categoria = "Entradas",
                    Descricao = $"{entradasSemMeio.Count} entrada(s) com MeioDePagamento inválido. IDs: {string.Join(", ", entradasSemMeio.Take(10))}",
                    Severidade = SeveridadeInconsistencia.Critica,
                    AcaoCorretivaSugerida = "Corrigir ou remover lançamentos com referências inválidas."
                });
            }

            // Entradas sem CentroCusto válido
            var entradasSemCentro = await _context.Entradas
                .Where(e => !_context.CentrosCusto.Any(c => c.Id == e.CentroCustoId))
                .Select(e => e.Id)
                .ToListAsync();

            if (entradasSemCentro.Any())
            {
                relatorio.Inconsistencias.Add(new InconsistenciaViewModel
                {
                    Tipo = "Referência Inválida",
                    Categoria = "Entradas",
                    Descricao = $"{entradasSemCentro.Count} entrada(s) com CentroCusto inválido. IDs: {string.Join(", ", entradasSemCentro.Take(10))}",
                    Severidade = SeveridadeInconsistencia.Critica,
                    AcaoCorretivaSugerida = "Corrigir ou remover lançamentos com referências inválidas."
                });
            }

            // Entradas sem PlanoDeContas válido
            var entradasSemPlano = await _context.Entradas
                .Where(e => !_context.PlanosDeContas.Any(p => p.Id == e.PlanoDeContasId))
                .Select(e => e.Id)
                .ToListAsync();

            if (entradasSemPlano.Any())
            {
                relatorio.Inconsistencias.Add(new InconsistenciaViewModel
                {
                    Tipo = "Referência Inválida",
                    Categoria = "Entradas",
                    Descricao = $"{entradasSemPlano.Count} entrada(s) com PlanoDeContas inválido. IDs: {string.Join(", ", entradasSemPlano.Take(10))}",
                    Severidade = SeveridadeInconsistencia.Critica,
                    AcaoCorretivaSugerida = "Corrigir ou remover lançamentos com referências inválidas."
                });
            }

            // Saídas sem MeioDePagamento válido
            var saidasSemMeio = await _context.Saidas
                .Where(s => !_context.MeiosDePagamento.Any(m => m.Id == s.MeioDePagamentoId))
                .Select(s => s.Id)
                .ToListAsync();

            if (saidasSemMeio.Any())
            {
                relatorio.Inconsistencias.Add(new InconsistenciaViewModel
                {
                    Tipo = "Referência Inválida",
                    Categoria = "Saídas",
                    Descricao = $"{saidasSemMeio.Count} saída(s) com MeioDePagamento inválido. IDs: {string.Join(", ", saidasSemMeio.Take(10))}",
                    Severidade = SeveridadeInconsistencia.Critica,
                    AcaoCorretivaSugerida = "Corrigir ou remover lançamentos com referências inválidas."
                });
            }

            // Saídas sem CentroCusto válido
            var saidasSemCentro = await _context.Saidas
                .Where(s => !_context.CentrosCusto.Any(c => c.Id == s.CentroCustoId))
                .Select(s => s.Id)
                .ToListAsync();

            if (saidasSemCentro.Any())
            {
                relatorio.Inconsistencias.Add(new InconsistenciaViewModel
                {
                    Tipo = "Referência Inválida",
                    Categoria = "Saídas",
                    Descricao = $"{saidasSemCentro.Count} saída(s) com CentroCusto inválido. IDs: {string.Join(", ", saidasSemCentro.Take(10))}",
                    Severidade = SeveridadeInconsistencia.Critica,
                    AcaoCorretivaSugerida = "Corrigir ou remover lançamentos com referências inválidas."
                });
            }
        }

        /// <summary>
        /// Valida lançamentos marcados como incluídos em fechamentos que não existem mais
        /// </summary>
        private async Task ValidarLancamentosOrfaos(RelatorioConsistenciaViewModel relatorio)
        {
            _logger.LogInformation("Validando lançamentos órfãos");

            // Entradas órfãs
            var entradasOrfas = await _context.Entradas
                .Where(e => e.IncluidaEmFechamento && e.FechamentoQueIncluiuId.HasValue &&
                           !_context.FechamentosPeriodo.Any(f => f.Id == e.FechamentoQueIncluiuId))
                .Select(e => e.Id)
                .ToListAsync();

            if (entradasOrfas.Any())
            {
                relatorio.Inconsistencias.Add(new InconsistenciaViewModel
                {
                    Tipo = "Lançamento Órfão",
                    Categoria = "Entradas",
                    Descricao = $"{entradasOrfas.Count} entrada(s) marcada(s) como incluída(s) em fechamento inexistente. IDs: {string.Join(", ", entradasOrfas.Take(10))}",
                    Severidade = SeveridadeInconsistencia.Critica,
                    AcaoCorretivaSugerida = "Desmarcar flag IncluidaEmFechamento e limpar FechamentoQueIncluiuId."
                });
            }

            // Saídas órfãs
            var saidasOrfas = await _context.Saidas
                .Where(s => s.IncluidaEmFechamento && s.FechamentoQueIncluiuId.HasValue &&
                           !_context.FechamentosPeriodo.Any(f => f.Id == s.FechamentoQueIncluiuId))
                .Select(s => s.Id)
                .ToListAsync();

            if (saidasOrfas.Any())
            {
                relatorio.Inconsistencias.Add(new InconsistenciaViewModel
                {
                    Tipo = "Lançamento Órfão",
                    Categoria = "Saídas",
                    Descricao = $"{saidasOrfas.Count} saída(s) marcada(s) como incluída(s) em fechamento inexistente. IDs: {string.Join(", ", saidasOrfas.Take(10))}",
                    Severidade = SeveridadeInconsistencia.Critica,
                    AcaoCorretivaSugerida = "Desmarcar flag IncluidaEmFechamento e limpar FechamentoQueIncluiuId."
                });
            }
        }

        /// <summary>
        /// Valida transferências internas com origem/destino iguais ou valores inválidos
        /// </summary>
        private async Task ValidarTransferenciasInternas(RelatorioConsistenciaViewModel relatorio)
        {
            _logger.LogInformation("Validando transferências internas");

            // Transferências com origem e destino iguais
            var transferenciasInvalidas = await _context.TransferenciasInternas
                .Where(t => t.CentroCustoOrigemId == t.CentroCustoDestinoId ||
                           t.MeioDePagamentoOrigemId == t.MeioDePagamentoDestinoId)
                .Select(t => new { t.Id, t.CentroCustoOrigemId, t.CentroCustoDestinoId, t.Valor })
                .ToListAsync();

            foreach (var transferencia in transferenciasInvalidas)
            {
                relatorio.Inconsistencias.Add(new InconsistenciaViewModel
                {
                    Tipo = "Transferência Inválida",
                    Categoria = "Transferências",
                    Descricao = $"Transferência #{transferencia.Id}: Origem e destino são iguais (Valor: {transferencia.Valor:C})",
                    Severidade = SeveridadeInconsistencia.Critica,
                    EntidadeId = transferencia.Id.ToString(),
                    EntidadeTipo = "TransferenciaInterna",
                    AcaoCorretivaSugerida = "Corrigir origem/destino ou excluir transferência."
                });
            }
        }

        /// <summary>
        /// Valida se os rateios foram aplicados corretamente
        /// </summary>
        private async Task ValidarRateiosAplicados(RelatorioConsistenciaViewModel relatorio)
        {
            _logger.LogInformation("Validando rateios aplicados");

            var fechamentosComRateio = await _context.FechamentosPeriodo
                .Include(f => f.ItensRateio)
                .Include(f => f.CentroCusto)
                .Where(f => f.Status == StatusFechamentoPeriodo.Aprovado && f.TotalRateios > 0)
                .ToListAsync();

            foreach (var fechamento in fechamentosComRateio)
            {
                var totalRateiosCalculado = fechamento.ItensRateio.Sum(i => i.ValorRateio);

                if (Math.Abs(fechamento.TotalRateios - totalRateiosCalculado) > 0.01m)
                {
                    relatorio.Inconsistencias.Add(new InconsistenciaViewModel
                    {
                        Tipo = "Rateio Incorreto",
                        Categoria = "Fechamentos",
                        Descricao = $"Fechamento #{fechamento.Id} ({fechamento.CentroCusto.Nome}): " +
                                    $"Total Rateios registrado {fechamento.TotalRateios:C}, " +
                                    $"soma dos itens {totalRateiosCalculado:C} (Diferença: {fechamento.TotalRateios - totalRateiosCalculado:C})",
                        Severidade = SeveridadeInconsistencia.Aviso,
                        EntidadeId = fechamento.Id.ToString(),
                        EntidadeTipo = "FechamentoPeriodo",
                        AcaoCorretivaSugerida = "Recalcular rateios do fechamento."
                    });
                }

                // Validar se percentuais batem
                foreach (var item in fechamento.ItensRateio)
                {
                    var valorEsperado = item.ValorBase * (item.Percentual / 100);
                    if (Math.Abs(item.ValorRateio - valorEsperado) > 0.01m)
                    {
                        relatorio.Inconsistencias.Add(new InconsistenciaViewModel
                        {
                            Tipo = "Cálculo de Rateio Incorreto",
                            Categoria = "Rateios",
                            Descricao = $"Item Rateio #{item.Id} (Fechamento #{fechamento.Id}): " +
                                        $"Valor calculado {valorEsperado:C}, registrado {item.ValorRateio:C}",
                            Severidade = SeveridadeInconsistencia.Aviso,
                            EntidadeId = item.Id.ToString(),
                            EntidadeTipo = "ItemRateioFechamento",
                            AcaoCorretivaSugerida = "Recalcular valor do rateio."
                        });
                    }
                }
            }
        }

        /// <summary>
        /// Valida saldos de centros de custo comparando com lançamentos
        /// </summary>
        private async Task ValidarSaldosCentrosCusto(RelatorioConsistenciaViewModel relatorio)
        {
            _logger.LogInformation("Validando saldos de centros de custo");

            var centros = await _context.CentrosCusto.Where(c => c.Ativo).ToListAsync();

            foreach (var centro in centros)
            {
                // Calcular saldo real
                var totalEntradas = await _context.Entradas
                    .Where(e => e.CentroCustoId == centro.Id)
                    .SumAsync(e => e.Valor);

                var totalSaidas = await _context.Saidas
                    .Where(s => s.CentroCustoId == centro.Id)
                    .SumAsync(s => s.Valor);

                var saldoReal = totalEntradas - totalSaidas;

                // Verificar se saldo é negativo (aviso)
                if (saldoReal < 0)
                {
                    relatorio.Inconsistencias.Add(new InconsistenciaViewModel
                    {
                        Tipo = "Saldo Negativo",
                        Categoria = "Centros de Custo",
                        Descricao = $"Centro de Custo '{centro.Nome}' (ID: {centro.Id}) possui saldo negativo: {saldoReal:C}",
                        Severidade = SeveridadeInconsistencia.Aviso,
                        EntidadeId = centro.Id.ToString(),
                        EntidadeTipo = "CentroCusto",
                        AcaoCorretivaSugerida = "Verificar se há despesas não provisionadas ou lançamentos incorretos."
                    });
                }
            }
        }

        /// <summary>
        /// Valida consistência entre tipos de caixa dos meios de pagamento
        /// </summary>
        private async Task ValidarConsistenciaMeiosPagamento(RelatorioConsistenciaViewModel relatorio)
        {
            _logger.LogInformation("Validando consistência de meios de pagamento");

            // Verificar se todos os meios de pagamento têm TipoCaixa definido corretamente
            var meiosSemTipo = await _context.MeiosDePagamento
                .Where(m => m.Ativo)
                .ToListAsync();

            // Validação adicional: verificar coerência entre nome e tipo
            var meiosIncoerentes = meiosSemTipo.Where(m =>
                (m.Nome.ToLower().Contains("dinheiro") || m.Nome.ToLower().Contains("espécie")) && m.TipoCaixa == TipoCaixa.Digital ||
                (m.Nome.ToLower().Contains("pix") || m.Nome.ToLower().Contains("transferência") || m.Nome.ToLower().Contains("cartão")) && m.TipoCaixa == TipoCaixa.Fisico
            ).ToList();

            foreach (var meio in meiosIncoerentes)
            {
                relatorio.Inconsistencias.Add(new InconsistenciaViewModel
                {
                    Tipo = "Tipo de Caixa Incoerente",
                    Categoria = "Meios de Pagamento",
                    Descricao = $"Meio de Pagamento '{meio.Nome}' (ID: {meio.Id}) possui tipo '{meio.TipoCaixa}' que pode estar incorreto",
                    Severidade = SeveridadeInconsistencia.Informacao,
                    EntidadeId = meio.Id.ToString(),
                    EntidadeTipo = "MeioDePagamento",
                    AcaoCorretivaSugerida = "Verificar e corrigir tipo de caixa (Físico/Digital)."
                });
            }
        }

        /// <summary>
        /// Valida datas futuras excessivas
        /// </summary>
        private async Task ValidarDatasFuturas(RelatorioConsistenciaViewModel relatorio)
        {
            _logger.LogInformation("Validando datas futuras");

            var dataLimite = DateTime.Now.AddDays(30);

            var entradasFuturas = await _context.Entradas
                .Where(e => e.Data > dataLimite)
                .Select(e => new { e.Id, e.Data, e.Valor })
                .ToListAsync();

            if (entradasFuturas.Any())
            {
                relatorio.Inconsistencias.Add(new InconsistenciaViewModel
                {
                    Tipo = "Data Futura Excessiva",
                    Categoria = "Entradas",
                    Descricao = $"{entradasFuturas.Count} entrada(s) com data superior a 30 dias no futuro. " +
                                $"Exemplo: ID {entradasFuturas.First().Id}, Data: {entradasFuturas.First().Data:dd/MM/yyyy}",
                    Severidade = SeveridadeInconsistencia.Aviso,
                    AcaoCorretivaSugerida = "Verificar se as datas estão corretas."
                });
            }

            var saidasFuturas = await _context.Saidas
                .Where(s => s.Data > dataLimite)
                .Select(s => new { s.Id, s.Data, s.Valor })
                .ToListAsync();

            if (saidasFuturas.Any())
            {
                relatorio.Inconsistencias.Add(new InconsistenciaViewModel
                {
                    Tipo = "Data Futura Excessiva",
                    Categoria = "Saídas",
                    Descricao = $"{saidasFuturas.Count} saída(s) com data superior a 30 dias no futuro. " +
                                $"Exemplo: ID {saidasFuturas.First().Id}, Data: {saidasFuturas.First().Data:dd/MM/yyyy}",
                    Severidade = SeveridadeInconsistencia.Aviso,
                    AcaoCorretivaSugerida = "Verificar se as datas estão corretas."
                });
            }
        }

        /// <summary>
        /// Valida valores negativos ou zero em lançamentos
        /// </summary>
        private async Task ValidarValoresInvalidos(RelatorioConsistenciaViewModel relatorio)
        {
            _logger.LogInformation("Validando valores inválidos");

            var entradasInvalidas = await _context.Entradas
                .Where(e => e.Valor <= 0)
                .Select(e => new { e.Id, e.Valor })
                .ToListAsync();

            if (entradasInvalidas.Any())
            {
                relatorio.Inconsistencias.Add(new InconsistenciaViewModel
                {
                    Tipo = "Valor Inválido",
                    Categoria = "Entradas",
                    Descricao = $"{entradasInvalidas.Count} entrada(s) com valor <= 0. IDs: {string.Join(", ", entradasInvalidas.Take(10).Select(e => e.Id))}",
                    Severidade = SeveridadeInconsistencia.Critica,
                    AcaoCorretivaSugerida = "Corrigir valores ou excluir lançamentos inválidos."
                });
            }

            var saidasInvalidas = await _context.Saidas
                .Where(s => s.Valor <= 0)
                .Select(s => new { s.Id, s.Valor })
                .ToListAsync();

            if (saidasInvalidas.Any())
            {
                relatorio.Inconsistencias.Add(new InconsistenciaViewModel
                {
                    Tipo = "Valor Inválido",
                    Categoria = "Saídas",
                    Descricao = $"{saidasInvalidas.Count} saída(s) com valor <= 0. IDs: {string.Join(", ", saidasInvalidas.Take(10).Select(s => s.Id))}",
                    Severidade = SeveridadeInconsistencia.Critica,
                    AcaoCorretivaSugerida = "Corrigir valores ou excluir lançamentos inválidos."
                });
            }
        }

        #endregion
    }
}
