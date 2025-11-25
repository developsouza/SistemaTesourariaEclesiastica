using Microsoft.EntityFrameworkCore;
using SistemaTesourariaEclesiastica.Data;
using SistemaTesourariaEclesiastica.Enums;
using SistemaTesourariaEclesiastica.Models;
using SistemaTesourariaEclesiastica.ViewModels;

namespace SistemaTesourariaEclesiastica.Services
{
    public class EscalaPorteiroService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<EscalaPorteiroService> _logger;

        public EscalaPorteiroService(ApplicationDbContext context, ILogger<EscalaPorteiroService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Gera a escala de porteiros baseado nos dias selecionados
        /// Sistema de variedade máxima - evita repetições consecutivas de duplas
        /// </summary>
        public async Task<List<EscalaPorteiro>> GerarEscalaAsync(
            List<DiasCultoViewModel> diasSelecionados,
            string? usuarioId = null)
        {
            if (diasSelecionados == null || !diasSelecionados.Any())
            {
                throw new ArgumentException("É necessário selecionar pelo menos um dia para gerar a escala.");
            }

            // Buscar porteiros e responsáveis ativos
            var porteiros = await _context.Porteiros
                .Where(p => p.Ativo)
                .OrderBy(p => p.Nome)
                .ToListAsync();

            var responsaveis = await _context.ResponsaveisPorteiros
                .Where(r => r.Ativo)
                .OrderBy(r => r.Nome)
                .ToListAsync();

            if (!porteiros.Any())
            {
                throw new InvalidOperationException("Não há porteiros ativos cadastrados.");
            }

            if (!responsaveis.Any())
            {
                throw new InvalidOperationException("Não há responsáveis ativos cadastrados.");
            }

            // Carregar disponibilidade dos porteiros
            foreach (var porteiro in porteiros)
            {
                porteiro.CarregarDisponibilidade();
            }

            // Ordenar dias por data e horário
            var diasOrdenados = diasSelecionados.OrderBy(d => d.Data).ThenBy(d => d.Horario).ToList();

            // Sistema de pontuação inicial
            var pontuacaoPorteiros = porteiros.ToDictionary(p => p.Id, p => 0);

            // NOVO: Histórico das últimas 5 duplas usadas (com peso decrescente)
            var historicoDuplas = new Queue<string>();

            // Rastrear frequência global de duplas
            var frequenciaDuplasGeracao = new Dictionary<string, int>();

            // Identificar porteiros com disponibilidade limitada
            var porteirosLimitados = new HashSet<int>();
            foreach (var porteiro in porteiros)
            {
                var diasDisponiveis = 0;
                if (porteiro.DisponibilidadeDomingo) diasDisponiveis++;
                if (porteiro.DisponibilidadeSegunda) diasDisponiveis++;
                if (porteiro.DisponibilidadeTerca) diasDisponiveis++;
                if (porteiro.DisponibilidadeQuarta) diasDisponiveis++;
                if (porteiro.DisponibilidadeQuinta) diasDisponiveis++;
                if (porteiro.DisponibilidadeSexta) diasDisponiveis++;
                if (porteiro.DisponibilidadeSabado) diasDisponiveis++;

                if (diasDisponiveis == 0) diasDisponiveis = 7;

                var horariosDisponiveis = 1;
                if (!string.IsNullOrWhiteSpace(porteiro.HorariosDisponiveis))
                {
                    horariosDisponiveis = porteiro.HorariosDisponiveis.Split(',', StringSplitOptions.RemoveEmptyEntries).Length;
                }

                if (diasDisponiveis <= 2 || (horariosDisponiveis == 1 && !string.IsNullOrWhiteSpace(porteiro.HorariosDisponiveis)))
                {
                    porteirosLimitados.Add(porteiro.Id);
                }
            }

            // Controle de descanso entre dias - REMOVIDO para maximizar variedade
            // Com apenas 4 porteiros disponíveis, o controle de descanso limita demais as combinações
            var porteirosDiaAtualTodosHorarios = new HashSet<int>();
            DateTime? dataUltimoDiaProcessado = null;

            var escalasGeradas = new List<EscalaPorteiro>();
            var random = new Random(DateTime.Now.Millisecond);
            var responsavelSelecionado = responsaveis.First();

            // Rastrear últimas 3 escalas (para porteiros individuais)
            var historicoEscalasGeracao = new Queue<List<int>>();

            _logger.LogInformation($"Iniciando geração com {porteiros.Count} porteiros ativos");

            // Processar cada dia
            foreach (var dia in diasOrdenados)
            {
                // Mudança de dia - resetar controle de horários (mantém apenas controle dentro do mesmo dia)
                if (dataUltimoDiaProcessado == null || dataUltimoDiaProcessado.Value.Date != dia.Data.Date)
                {
                    porteirosDiaAtualTodosHorarios.Clear();
                    dataUltimoDiaProcessado = dia.Data.Date;
                }

                // Verificar se já existe escala para este dia/horário
                var escalaExistente = await _context.EscalasPorteiros
                    .AnyAsync(e => e.DataCulto.Date == dia.Data.Date && e.Horario == dia.Horario);

                if (escalaExistente)
                {
                    var horarioStr = dia.Horario.HasValue ? $" às {dia.Horario.Value:hh\\:mm}" : "";
                    _logger.LogWarning($"Já existe escala para o dia {dia.Data:dd/MM/yyyy}{horarioStr}. Pulando...");
                    continue;
                }

                // Determinar quantidade de porteiros necessária
                int quantidadePorteiros = 1;

                if (dia.Horario.HasValue && dia.Horario.Value >= new TimeSpan(19, 0, 0))
                {
                    quantidadePorteiros = 2;
                }
                else if (dia.TipoCulto == TipoCulto.EscolaBiblica)
                {
                    quantidadePorteiros = 1;
                }

                // IDs bloqueados apenas por já terem trabalhado no mesmo dia em outro horário
                var bloqueadosTotais = new HashSet<int>(porteirosDiaAtualTodosHorarios);

                // Filtrar porteiros elegíveis
                var elegiveisBase = porteiros.Where(p => p.EstaDisponivelNodia(dia.Data.DayOfWeek)).ToList();
                if (dia.Horario.HasValue)
                {
                    elegiveisBase = elegiveisBase.Where(p => string.IsNullOrWhiteSpace(p.HorariosDisponiveis) || p.EstaDisponivelNoHorario(dia.Horario.Value)).ToList();
                }

                var porteirosElegiveis = elegiveisBase.Where(p => !bloqueadosTotais.Contains(p.Id)).ToList();

                // NOVO: Randomizar a ordem dos porteiros elegíveis para aumentar variedade
                porteirosElegiveis = porteirosElegiveis.OrderBy(_ => random.Next()).ToList();

                // Fallback: relaxar bloqueio se necessário
                if (porteirosElegiveis.Count < quantidadePorteiros)
                {
                    _logger.LogWarning($"Relaxando bloqueio em {dia.Data:dd/MM/yyyy} por falta de porteiros (necessário {quantidadePorteiros}, disponíveis {porteirosElegiveis.Count}).");

                    var complementares = elegiveisBase
                        .Where(p => bloqueadosTotais.Contains(p.Id))
                        .OrderBy(p => pontuacaoPorteiros[p.Id])
                        .ThenBy(_ => random.Next()) // Adicionar randomização para variedade
                        .ToList();

                    foreach (var p in complementares)
                    {
                        if (!porteirosElegiveis.Contains(p)) porteirosElegiveis.Add(p);
                        if (porteirosElegiveis.Count >= quantidadePorteiros) break;
                    }
                }

                if (!porteirosElegiveis.Any())
                {
                    _logger.LogError($"Sem porteiros elegíveis para {dia.Data:dd/MM/yyyy}. Dia será ignorado.");
                    continue;
                }

                // NOVA SELEÇÃO: Baseada em variedade de duplas
                // IMPORTANTE: Se teve que relaxar o bloqueio, ignorar histórico recente para permitir mais variedade
                var precisouRelaxar = porteirosElegiveis.Any(p => bloqueadosTotais.Contains(p.Id));
                
                var selecionados = SelecionarPorteirosComVariedade(
                    porteirosElegiveis,
                    quantidadePorteiros,
                    historicoEscalasGeracao,
                    pontuacaoPorteiros,
                    porteirosLimitados,
                    frequenciaDuplasGeracao,
                    historicoDuplas,
                    random,
                    precisouRelaxar); // Novo parâmetro

                if (!selecionados.Any())
                {
                    _logger.LogError($"Falha ao selecionar porteiros para {dia.Data:dd/MM/yyyy}.");
                    continue;
                }

                // Registrar pontuação e dia atual
                foreach (var id in selecionados)
                {
                    pontuacaoPorteiros[id]++;
                    porteirosDiaAtualTodosHorarios.Add(id);
                }

                // Atualizar rastreamento de duplas
                if (selecionados.Count == 2)
                {
                    var p1 = selecionados[0];
                    var p2 = selecionados[1];

                    var chaveDupla = GerarChaveDupla(p1, p2);
                    frequenciaDuplasGeracao[chaveDupla] = frequenciaDuplasGeracao.GetValueOrDefault(chaveDupla) + 1;

                    // Adicionar ao histórico de duplas recentes
                    historicoDuplas.Enqueue(chaveDupla);
                    if (historicoDuplas.Count > 5)
                        historicoDuplas.Dequeue();

                    // Se uma dupla está sendo usada demais, reduzir o histórico para permitir mais variedade
                    if (frequenciaDuplasGeracao[chaveDupla] >= 3 && historicoDuplas.Count > 3)
                    {
                        var temp = historicoDuplas.Skip(historicoDuplas.Count - 2).ToList();
                        historicoDuplas.Clear();
                        foreach (var d in temp)
                        {
                            historicoDuplas.Enqueue(d);
                        }
                    }
                }

                // Criar escala
                var escala = new EscalaPorteiro
                {
                    DataCulto = dia.Data,
                    Horario = dia.Horario,
                    TipoCulto = dia.TipoCulto,
                    PorteiroId = selecionados[0],
                    Porteiro2Id = selecionados.Count > 1 ? selecionados[1] : null,
                    ResponsavelId = responsavelSelecionado.Id,
                    Observacao = dia.Observacao,
                    DataGeracao = DateTime.Now,
                    UsuarioGeracaoId = usuarioId
                };

                escalasGeradas.Add(escala);

                historicoEscalasGeracao.Enqueue(selecionados);
                if (historicoEscalasGeracao.Count > 3)
                    historicoEscalasGeracao.Dequeue();
            }

            _logger.LogInformation($"Geração concluída: {escalasGeradas.Count} escalas criadas");
            LogarEstatisticasGeracao(pontuacaoPorteiros, frequenciaDuplasGeracao, porteiros);

            return escalasGeradas;
        }

        /// <summary>
        /// Gera uma chave única para identificar uma dupla de porteiros (independente da ordem)
        /// </summary>
        private string GerarChaveDupla(int porteiro1Id, int porteiro2Id)
        {
            var menor = Math.Min(porteiro1Id, porteiro2Id);
            var maior = Math.Max(porteiro1Id, porteiro2Id);
            return $"{menor}-{maior}";
        }

        /// <summary>
        /// NOVO v2: Sistema de seleção baseado em VARIEDADE MÁXIMA
        /// Prioriza duplas que não trabalharam juntas recentemente
        /// </summary>
        private List<int> SelecionarPorteirosComVariedade(
            List<Porteiro> porteirosElegiveis,
            int quantidadeNecessaria,
            Queue<List<int>> historicoEscalasGeracao,
            Dictionary<int, int> pontuacaoPorteiros,
            HashSet<int> porteirosLimitados,
            Dictionary<string, int> frequenciaDuplasGeracao,
            Queue<string> historicoDuplas,
            Random random,
            bool ignorarHistorico = false) // NOVO parâmetro
        {
            // Porteiros das últimas 3 escalas para evitar repetição imediata
            var porteirosRecentes = historicoEscalasGeracao
                .SelectMany(h => h)
                .Distinct()
                .ToList();

            if (quantidadeNecessaria == 1)
            {
                // Para 1 porteiro (Ex: Escola Bíblica), a lógica é simples
                var candidatoSolo = porteirosElegiveis
                    .Where(p => !porteirosRecentes.Contains(p.Id))
                    .OrderBy(p => pontuacaoPorteiros[p.Id])
                    .ThenBy(p => porteirosLimitados.Contains(p.Id) ? 0 : 1)
                    .ThenBy(_ => random.Next())
                    .FirstOrDefault();

                if (candidatoSolo != null)
                {
                    return new List<int> { candidatoSolo.Id };
                }

                // Fallback
                var fallbackSolo = porteirosElegiveis
                    .OrderBy(p => pontuacaoPorteiros[p.Id])
                    .First();
                return new List<int> { fallbackSolo.Id };
            }

            // LÓGICA PARA DUPLAS - Variedade é PRIORIDADE MÁXIMA
            var candidatos = porteirosElegiveis.Where(p => !porteirosRecentes.Contains(p.Id)).ToList();
            
            // Se não tiver candidatos suficientes sem recentes, relaxa
            if (candidatos.Count < 2)
            {
                candidatos = porteirosElegiveis.ToList();
            }

            // Avaliar TODAS as duplas possíveis e escolher a MELHOR
            var duplasAvaliadas = new List<(int p1, int p2, int score)>();

            for (int i = 0; i < candidatos.Count; i++)
            {
                for (int j = i + 1; j < candidatos.Count; j++)
                {
                    var p1 = candidatos[i];
                    var p2 = candidatos[j];
                    var chaveDupla = GerarChaveDupla(p1.Id, p2.Id);

                    int score = 0;

                    // 1. PENALIZAÇÃO PESADA por estar no histórico recente
                    // MAS: Se ignorarHistorico=true (situação crítica), reduz drasticamente a penalização
                    if (!ignorarHistorico)
                    {
                        var historicoLista = historicoDuplas.Reverse().ToList();
                        for (int k = 0; k < historicoLista.Count; k++)
                        {
                            if (historicoLista[k] == chaveDupla)
                            {
                                // Última dupla = +1000, penúltima = +800, etc
                                score += 1000 - (k * 200);
                            }
                        }
                    }
                    else
                    {
                        // Em situação crítica, penalização muito menor (apenas 10% do normal)
                        var historicoLista = historicoDuplas.Reverse().ToList();
                        for (int k = 0; k < historicoLista.Count; k++)
                        {
                            if (historicoLista[k] == chaveDupla)
                            {
                                score += (1000 - (k * 200)) / 10; // Reduz para 10%
                            }
                        }
                    }

                    // 2. Penalização por frequência total de uso (PESO AUMENTADO)
                    var frequencia = frequenciaDuplasGeracao.GetValueOrDefault(chaveDupla, 0);
                    score += frequencia * 300; // Aumentado de 100 para 300

                    // 3. Penalização por pontuação individual (balanceamento secundário)
                    score += pontuacaoPorteiros[p1.Id] * 10;
                    score += pontuacaoPorteiros[p2.Id] * 10;

                    // 4. BÔNUS para porteiros limitados (prioridade)
                    if (porteirosLimitados.Contains(p1.Id)) score -= 300;
                    if (porteirosLimitados.Contains(p2.Id)) score -= 300;

                    duplasAvaliadas.Add((p1.Id, p2.Id, score));
                }
            }

            if (!duplasAvaliadas.Any())
            {
                _logger.LogWarning("Nenhuma dupla pôde ser formada!");
                return new List<int> { candidatos[0].Id, candidatos[1].Id };
            }

            // Ordenar por score (menor = melhor) e pegar a melhor
            var melhorDupla = duplasAvaliadas.OrderBy(d => d.score).First();

            return new List<int> { melhorDupla.p1, melhorDupla.p2 };
        }

        /// <summary>
        /// Loga estatísticas da geração para análise
        /// </summary>
        private void LogarEstatisticasGeracao(
            Dictionary<int, int> pontuacaoPorteiros,
            Dictionary<string, int> frequenciaDuplasGeracao,
            List<Porteiro> porteiros)
        {
            _logger.LogInformation("=== ESTATÍSTICAS DA GERAÇÃO ===");

            // Distribuição de trabalho
            _logger.LogInformation("Distribuição de escalas por porteiro:");
            foreach (var kvp in pontuacaoPorteiros.OrderByDescending(x => x.Value))
            {
                var porteiro = porteiros.First(p => p.Id == kvp.Key);
                _logger.LogInformation($"  - {porteiro.Nome}: {kvp.Value} escalas");
            }

            // Frequência de duplas
            if (frequenciaDuplasGeracao.Any())
            {
                _logger.LogInformation("Frequência de duplas:");
                foreach (var kvp in frequenciaDuplasGeracao.OrderByDescending(x => x.Value).Take(10))
                {
                    _logger.LogInformation($"  - Dupla {kvp.Key}: {kvp.Value} vezes");
                }
            }
        }

        /// <summary>
        /// Busca as configurações padrão de cultos (semanais e datas específicas)
        /// </summary>
        public async Task<List<ConfiguracaoCulto>> ObterConfiguracoesPadraoAsync()
        {
            return await _context.ConfiguracoesCultos
                .Where(c => c.Ativo)
                .OrderBy(c => c.DataEspecifica.HasValue ? 0 : 1)
                .ThenBy(c => c.DataEspecifica)
                .ThenBy(c => c.DiaSemana)
                .ToListAsync();
        }

        /// <summary>
        /// Gera sugestões de dias baseado nas configurações padrão (semanais) e datas específicas
        /// </summary>
        public async Task<List<DiasCultoViewModel>> SugerirDiasAsync(DateTime dataInicio, DateTime dataFim)
        {
            var configuracoes = await ObterConfiguracoesPadraoAsync();
            var diasSugeridos = new List<DiasCultoViewModel>();

            if (!configuracoes.Any())
            {
                configuracoes = ObterConfiguracoesPadrao();
            }

            var configuracoesSemanais = configuracoes.Where(c => c.DiaSemana.HasValue).ToList();
            var configuracoesDataEspecifica = configuracoes.Where(c => c.DataEspecifica.HasValue).ToList();

            // Adicionar datas específicas que estão no período
            foreach (var config in configuracoesDataEspecifica)
            {
                if (config.DataEspecifica!.Value.Date >= dataInicio.Date &&
                    config.DataEspecifica.Value.Date <= dataFim.Date)
                {
                    diasSugeridos.Add(new DiasCultoViewModel
                    {
                        Data = config.DataEspecifica.Value,
                        Horario = config.Horario,
                        TipoCulto = config.TipoCulto,
                        Observacao = config.Observacao ?? $"Evento Especial - {config.TipoCulto}"
                    });
                }
            }

            // Adicionar todos os cultos semanais (múltiplos horários no mesmo dia)
            if (configuracoesSemanais.Any())
            {
                for (var data = dataInicio; data <= dataFim; data = data.AddDays(1))
                {
                    var configsDoDia = configuracoesSemanais
                        .Where(c => c.DiaSemana == data.DayOfWeek)
                        .ToList();

                    foreach (var config in configsDoDia)
                    {
                        var temDataEspecificaMesmoHorario = diasSugeridos.Any(d =>
                            d.Data.Date == data.Date &&
                            d.Horario == config.Horario);

                        if (!temDataEspecificaMesmoHorario)
                        {
                            diasSugeridos.Add(new DiasCultoViewModel
                            {
                                Data = data,
                                Horario = config.Horario,
                                TipoCulto = config.TipoCulto,
                                Observacao = config.Observacao
                            });
                        }
                    }
                }
            }

            return diasSugeridos.OrderBy(d => d.Data).ThenBy(d => d.Horario).ToList();
        }

        /// <summary>
        /// Retorna as configurações padrão de cultos (fallback)
        /// </summary>
        private List<ConfiguracaoCulto> ObterConfiguracoesPadrao()
        {
            return new List<ConfiguracaoCulto>
            {
                new ConfiguracaoCulto
                {
                    DiaSemana = DayOfWeek.Sunday,
                    Horario = new TimeSpan(9, 0, 0),
                    TipoCulto = TipoCulto.EscolaBiblica,
                    Ativo = true
                },
                new ConfiguracaoCulto
                {
                    DiaSemana = DayOfWeek.Sunday,
                    Horario = new TimeSpan(19, 0, 0),
                    TipoCulto = TipoCulto.Evangelistico,
                    Ativo = true
                },
                new ConfiguracaoCulto
                {
                    DiaSemana = DayOfWeek.Wednesday,
                    Horario = new TimeSpan(19, 30, 0),
                    TipoCulto = TipoCulto.DaFamilia,
                    Ativo = true
                },
                new ConfiguracaoCulto
                {
                    DiaSemana = DayOfWeek.Friday,
                    Horario = new TimeSpan(19, 30, 0),
                    TipoCulto = TipoCulto.DeDoutrina,
                    Ativo = true
                }
            };
        }
    }
}