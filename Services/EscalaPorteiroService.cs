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
        /// ? OTIMIZADO: Funciona sem depender de histórico no banco (ideal para escalas mensais que são deletadas)
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

            // ? NOVO: Sistema de pontuação ZERO inicial (sem histórico)
            var pontuacaoPorteiros = porteiros.ToDictionary(p => p.Id, p => 0);

            // ? NOVO: Rastrear parceiros apenas da geração atual
            var parceirosDaGeracao = new Dictionary<int, HashSet<int>>();
            foreach (var porteiro in porteiros)
            {
                parceirosDaGeracao[porteiro.Id] = new HashSet<int>();
            }

            // ? NOVO: Rastrear frequência de duplas apenas nesta geração
            var frequenciaDuplasGeracao = new Dictionary<string, int>();

            // ? NOVO: Janela de duplas recentes menor (últimas 10 duplas)
            var duplasRecentesQueue = new Queue<string>();
            var duplasUtilizadas = new HashSet<string>();

            _logger.LogInformation($"Iniciando geração de escala para {diasOrdenados.Count} dias (SEM histórico do banco)");

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

                if (diasDisponiveis == 0)
                    diasDisponiveis = 7;

                var horariosDisponiveis = 1;
                if (!string.IsNullOrWhiteSpace(porteiro.HorariosDisponiveis))
                {
                    horariosDisponiveis = porteiro.HorariosDisponiveis.Split(',', StringSplitOptions.RemoveEmptyEntries).Length;
                }

                if (diasDisponiveis == 1 || (horariosDisponiveis == 1 && !string.IsNullOrWhiteSpace(porteiro.HorariosDisponiveis)))
                {
                    porteirosLimitados.Add(porteiro.Id);
                }
            }

            // ? SIMPLIFICADO: Controle de descanso entre dias (2 dias de intervalo)
            const int DIAS_DESCANSO_CONSECUTIVO = 2;
            var filaDiasAnteriores = new Queue<HashSet<int>>();
            var porteirosDiaAtualTodosHorarios = new HashSet<int>();
            DateTime? dataUltimoDiaProcessado = null;

            var escalasGeradas = new List<EscalaPorteiro>();
            var random = new Random(DateTime.Now.Millisecond);
            var responsavelSelecionado = responsaveis.First();

            // ? SIMPLIFICADO: Rastrear apenas últimas 3 escalas (não do banco, da geração)
            var historicoEscalasGeracao = new Queue<List<int>>();

            // Processar cada dia
            foreach (var dia in diasOrdenados)
            {
                // Mudança de dia - resetar controle de horários
                if (dataUltimoDiaProcessado == null || dataUltimoDiaProcessado.Value.Date != dia.Data.Date)
                {
                    if (porteirosDiaAtualTodosHorarios.Any())
                    {
                        filaDiasAnteriores.Enqueue(new HashSet<int>(porteirosDiaAtualTodosHorarios));
                        if (filaDiasAnteriores.Count > DIAS_DESCANSO_CONSECUTIVO)
                            filaDiasAnteriores.Dequeue();
                    }
                    
                    porteirosDiaAtualTodosHorarios.Clear();
                    dataUltimoDiaProcessado = dia.Data.Date;
                    
                    var bloqueados = filaDiasAnteriores.SelectMany(s => s).Distinct().ToList();
                    _logger.LogInformation($"Novo dia {dataUltimoDiaProcessado:dd/MM/yyyy}. Porteiros bloqueados por descanso ({DIAS_DESCANSO_CONSECUTIVO} dias): {string.Join(", ", bloqueados)}");
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
                    quantidadePorteiros = 2; // Cultos noturnos: 2 porteiros
                }
                else if (dia.TipoCulto == TipoCulto.EscolaBiblica)
                {
                    quantidadePorteiros = 1; // Escola Bíblica: 1 porteiro
                }

                // IDs bloqueados por descanso
                var bloqueadosDias = filaDiasAnteriores.SelectMany(s => s).Distinct().ToHashSet();
                var bloqueadosTotais = new HashSet<int>(bloqueadosDias);
                bloqueadosTotais.UnionWith(porteirosDiaAtualTodosHorarios); // Bloquear quem já trabalhou hoje

                // Filtrar porteiros elegíveis
                var elegiveisBase = porteiros.Where(p => p.EstaDisponivelNodia(dia.Data.DayOfWeek)).ToList();
                if (dia.Horario.HasValue)
                {
                    elegiveisBase = elegiveisBase.Where(p => string.IsNullOrWhiteSpace(p.HorariosDisponiveis) || p.EstaDisponivelNoHorario(dia.Horario.Value)).ToList();
                }

                var porteirosElegiveis = elegiveisBase.Where(p => !bloqueadosTotais.Contains(p.Id)).ToList();

                // Fallback: relaxar bloqueio se necessário
                if (porteirosElegiveis.Count < quantidadePorteiros)
                {
                    _logger.LogWarning($"Relaxando bloqueio em {dia.Data:dd/MM/yyyy} por falta de porteiros (necessário {quantidadePorteiros}, disponíveis {porteirosElegiveis.Count}).");
                    
                    var complementares = elegiveisBase
                        .Where(p => bloqueadosTotais.Contains(p.Id))
                        .OrderBy(p => pontuacaoPorteiros[p.Id])
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

                // ? OTIMIZADO: Seleção focada APENAS na variedade da geração atual
                var selecionados = SelecionarPorteirosComMaximaVariedade(
                    porteirosElegiveis,
                    quantidadePorteiros,
                    historicoEscalasGeracao,
                    pontuacaoPorteiros,
                    porteirosLimitados,
                    duplasUtilizadas,
                    frequenciaDuplasGeracao,
                    parceirosDaGeracao,
                    random);

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
                    
                    // Registrar parceiros
                    parceirosDaGeracao[p1].Add(p2);
                    parceirosDaGeracao[p2].Add(p1);
                    
                    // Registrar frequência da dupla
                    var chaveDupla = GerarChaveDupla(p1, p2);
                    frequenciaDuplasGeracao[chaveDupla] = frequenciaDuplasGeracao.GetValueOrDefault(chaveDupla) + 1;
                    
                    // Adicionar na janela de duplas recentes
                    duplasRecentesQueue.Enqueue(chaveDupla);
                    duplasUtilizadas.Add(chaveDupla);
                    
                    // Manter janela de 10 duplas
                    while (duplasRecentesQueue.Count > 10)
                    {
                        var removida = duplasRecentesQueue.Dequeue();
                        duplasUtilizadas.Remove(removida);
                    }
                    
                    _logger.LogInformation($"Dupla escalada: {p1} + {p2} (frequência nesta geração: {frequenciaDuplasGeracao[chaveDupla]})");
                }
                else
                {
                    _logger.LogInformation($"Porteiro escalado: {selecionados[0]}");
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
                
                // Atualizar histórico da geração
                historicoEscalasGeracao.Enqueue(selecionados);
                if (historicoEscalasGeracao.Count > 3)
                    historicoEscalasGeracao.Dequeue();
            }

            _logger.LogInformation($"Geração concluída: {escalasGeradas.Count} escalas criadas");
            
            // Log de estatísticas
            LogarEstatisticasGeracao(pontuacaoPorteiros, parceirosDaGeracao, frequenciaDuplasGeracao, porteiros);

            return escalasGeradas;
        }

        /// <summary>
        /// ? NOVO: Loga estatísticas da geração para análise
        /// </summary>
        private void LogarEstatisticasGeracao(
            Dictionary<int, int> pontuacaoPorteiros,
            Dictionary<int, HashSet<int>> parceirosDaGeracao,
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
            
            // Variedade de parceiros
            _logger.LogInformation("Variedade de parceiros:");
            foreach (var kvp in parceirosDaGeracao.Where(x => x.Value.Any()))
            {
                var porteiro = porteiros.First(p => p.Id == kvp.Key);
                var parceiros = string.Join(", ", kvp.Value.Select(id => porteiros.First(p => p.Id == id).Nome));
                _logger.LogInformation($"  - {porteiro.Nome} trabalhou com: {parceiros}");
            }
            
            // Frequência de duplas
            if (frequenciaDuplasGeracao.Any())
            {
                _logger.LogInformation("Frequência de duplas:");
                foreach (var kvp in frequenciaDuplasGeracao.OrderByDescending(x => x.Value))
                {
                    _logger.LogInformation($"  - Dupla {kvp.Key}: {kvp.Value} vezes");
                }
            }
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
        /// ? OTIMIZADO: Seleciona porteiros com foco em MÁXIMA VARIEDADE dentro da geração atual
        /// </summary>
        private List<int> SelecionarPorteirosComMaximaVariedade(
            List<Porteiro> porteirosElegiveis,
            int quantidadeNecessaria,
            Queue<List<int>> historicoEscalasGeracao,
            Dictionary<int, int> pontuacaoPorteiros,
            HashSet<int> porteirosLimitados,
            HashSet<string> duplasUtilizadas,
            Dictionary<string, int> frequenciaDuplasGeracao,
            Dictionary<int, HashSet<int>> parceirosDaGeracao,
            Random random)
        {
            var porteirosSelecionados = new List<int>();

            // Porteiros das últimas 3 escalas geradas
            var porteirosRecentes = historicoEscalasGeracao
                .Take(3)
                .SelectMany(h => h)
                .Distinct()
                .ToList();

            if (quantidadeNecessaria == 2)
            {
                // ? ESTRATÉGIA OTIMIZADA: Priorizar duplas inéditas e variedade máxima
                
                // Fase 1: Candidatos que NÃO estão nas últimas 3 escalas
                var candidatosFase1 = porteirosElegiveis
                    .Where(p => !porteirosRecentes.Contains(p.Id))
                    .ToList();

                if (candidatosFase1.Count >= 2)
                {
                    var melhorDupla = EncontrarMelhorDuplaVariedade(
                        candidatosFase1,
                        pontuacaoPorteiros,
                        parceirosDaGeracao,
                        frequenciaDuplasGeracao,
                        duplasUtilizadas,
                        porteirosLimitados,
                        random,
                        prioridadeMaxima: true);

                    if (melhorDupla != null && melhorDupla.Count == 2)
                    {
                        _logger.LogInformation($"? Dupla selecionada (Fase 1): {melhorDupla[0]} + {melhorDupla[1]}");
                        return melhorDupla;
                    }
                }

                // Fase 2: Aceitar porteiros das últimas 2 escalas (não da última)
                var porteirosUltima1 = historicoEscalasGeracao.Take(1).SelectMany(h => h).Distinct().ToList();
                var candidatosFase2 = porteirosElegiveis
                    .Where(p => !porteirosUltima1.Contains(p.Id))
                    .ToList();

                if (candidatosFase2.Count >= 2)
                {
                    var melhorDupla = EncontrarMelhorDuplaVariedade(
                        candidatosFase2,
                        pontuacaoPorteiros,
                        parceirosDaGeracao,
                        frequenciaDuplasGeracao,
                        duplasUtilizadas,
                        porteirosLimitados,
                        random,
                        prioridadeMaxima: true);

                    if (melhorDupla != null && melhorDupla.Count == 2)
                    {
                        _logger.LogInformation($"? Dupla selecionada (Fase 2): {melhorDupla[0]} + {melhorDupla[1]}");
                        return melhorDupla;
                    }
                }

                // Fase 3: Aceitar qualquer dupla disponível (última opção)
                var melhorDuplaFallback = EncontrarMelhorDuplaVariedade(
                    porteirosElegiveis.ToList(),
                    pontuacaoPorteiros,
                    parceirosDaGeracao,
                    frequenciaDuplasGeracao,
                    duplasUtilizadas,
                    porteirosLimitados,
                    random,
                    prioridadeMaxima: false);

                if (melhorDuplaFallback != null && melhorDuplaFallback.Count == 2)
                {
                    _logger.LogWarning($"? Dupla selecionada (Fallback): {melhorDuplaFallback[0]} + {melhorDuplaFallback[1]}");
                    return melhorDuplaFallback;
                }

                // Último recurso
                var p1 = porteirosElegiveis.OrderBy(p => pontuacaoPorteiros[p.Id]).First();
                var p2 = porteirosElegiveis.Where(p => p.Id != p1.Id).OrderBy(p => pontuacaoPorteiros[p.Id]).First();
                _logger.LogWarning($"? Dupla de emergência: {p1.Id} + {p2.Id}");
                return new List<int> { p1.Id, p2.Id };
            }

            // Para Escola Bíblica (1 porteiro)
            var candidatoSolo = porteirosElegiveis
                .Where(p => !porteirosRecentes.Contains(p.Id))
                .OrderBy(p => pontuacaoPorteiros[p.Id])
                .ThenBy(p => porteirosLimitados.Contains(p.Id) ? 0 : 1)
                .ThenBy(_ => random.Next())
                .FirstOrDefault();

            if (candidatoSolo != null)
            {
                _logger.LogInformation($"? Porteiro solo: {candidatoSolo.Id} ({pontuacaoPorteiros[candidatoSolo.Id]} escalas)");
                return new List<int> { candidatoSolo.Id };
            }

            var fallbackSolo = porteirosElegiveis.OrderBy(p => pontuacaoPorteiros[p.Id]).First();
            return new List<int> { fallbackSolo.Id };
        }

        /// <summary>
        /// ? OTIMIZADO: Encontra a melhor dupla focando em VARIEDADE dentro da geração
        /// </summary>
        private List<int>? EncontrarMelhorDuplaVariedade(
            List<Porteiro> candidatos,
            Dictionary<int, int> pontuacaoPorteiros,
            Dictionary<int, HashSet<int>> parceirosDaGeracao,
            Dictionary<string, int> frequenciaDuplasGeracao,
            HashSet<string> duplasUtilizadas,
            HashSet<int> porteirosLimitados,
            Random random,
            bool prioridadeMaxima)
        {
            if (candidatos.Count < 2) return null;

            // ?? LOG DETALHADO: Estado atual
            _logger.LogInformation($"[DEBUG] Candidatos disponíveis: {string.Join(", ", candidatos.Select(c => $"{c.Id}({c.Nome})"))}");
            _logger.LogInformation($"[DEBUG] Prioridade Máxima: {prioridadeMaxima}");
            
            // Log de quem já trabalhou com quem
            foreach (var kvp in parceirosDaGeracao.Where(x => x.Value.Any()))
            {
                var porteiro = candidatos.FirstOrDefault(p => p.Id == kvp.Key);
                if (porteiro != null)
                {
                    var parceirosNomes = string.Join(", ", kvp.Value.Select(id => {
                        var p = candidatos.FirstOrDefault(x => x.Id == id);
                        return p != null ? $"{id}({p.Nome})" : id.ToString();
                    }));
                    _logger.LogInformation($"[DEBUG] Porteiro {kvp.Key}({porteiro.Nome}) já trabalhou com: {parceirosNomes}");
                }
            }

            var duplasAvaliadas = new List<(int p1, int p2, double score, string motivo)>();

            foreach (var p1 in candidatos)
            {
                foreach (var p2 in candidatos.Where(p => p.Id != p1.Id))
                {
                    if (p1.Id >= p2.Id) continue; // Evitar duplicatas (A-B = B-A)

                    var chaveDupla = GerarChaveDupla(p1.Id, p2.Id);
                    double score = 0;
                    var motivos = new List<string>();

                    // ? Critério 1: DUPLAS INÉDITAS na geração atual (peso MASSIVO)
                    var trabalhramJuntosNaGeracao = parceirosDaGeracao[p1.Id].Contains(p2.Id);
                    
                    if (prioridadeMaxima)
                    {
                        if (!trabalhramJuntosNaGeracao)
                        {
                            score -= 100000; // ?? AUMENTADO 10X - PRIORIDADE ABSOLUTA
                            motivos.Add($"INÉDITA (-100000)");
                        }
                        else
                        {
                            score += 50000; // ?? AUMENTADO 10X - PENALIDADE ENORME
                            motivos.Add($"JÁ TRABALHARAM (+50000)");
                        }
                    }

                    // ? Critério 2: Frequência da dupla na geração
                    var freqDupla = frequenciaDuplasGeracao.GetValueOrDefault(chaveDupla, 0);
                    if (freqDupla > 0)
                    {
                        score += freqDupla * 10000; // ?? AUMENTADO 20X
                        motivos.Add($"Freq:{freqDupla} (+{freqDupla * 10000})");
                    }

                    // ? Critério 3: Dupla na janela recente? (últimas 10)
                    if (duplasUtilizadas.Contains(chaveDupla))
                    {
                        score += 5000; // ?? AUMENTADO
                        motivos.Add($"Recente (+5000)");
                    }

                    // ? Critério 4: Balancear carga de trabalho (PESO MENOR)
                    var pontuacaoTotal = pontuacaoPorteiros[p1.Id] + pontuacaoPorteiros[p2.Id];
                    score += pontuacaoTotal * 1; // ?? REDUZIDO PARA DAR MAIS PESO À VARIEDADE
                    motivos.Add($"Carga:{pontuacaoTotal} (+{pontuacaoTotal})");

                    // ? Critério 5: Priorizar porteiros limitados
                    if (porteirosLimitados.Contains(p1.Id) || porteirosLimitados.Contains(p2.Id))
                    {
                        score -= 1000; // ?? AUMENTADO
                        motivos.Add($"Limitado (-1000)");
                    }

                    // ? Critério 6: Variedade de parceiros individuais
                    // Quanto MENOS parceiros cada um teve, MELHOR (bônus para novatos em parceria)
                    var variedadeP1 = parceirosDaGeracao[p1.Id].Count;
                    var variedadeP2 = parceirosDaGeracao[p2.Id].Count;
                    
                    // ?? INVERTIDO: Dar BÔNUS para quem trabalhou com MENOS pessoas
                    var penalidade = (variedadeP1 + variedadeP2) * 2000; // Penalizar quem já tem muitos parceiros
                    score += penalidade;
                    motivos.Add($"Variedade:{variedadeP1}+{variedadeP2} (+{penalidade})");

                    // ? Critério 7: Aleatoriedade para desempate (peso MÍNIMO)
                    var aleatorio = random.NextDouble() * 0.1;
                    score += aleatorio;

                    duplasAvaliadas.Add((p1.Id, p2.Id, score, string.Join(", ", motivos)));
                }
            }

            if (!duplasAvaliadas.Any()) 
            {
                _logger.LogWarning("[DEBUG] Nenhuma dupla avaliada!");
                return null;
            }

            // ?? LOG: Top 5 duplas avaliadas
            _logger.LogInformation("[DEBUG] === TOP 5 DUPLAS AVALIADAS ===");
            foreach (var dupla in duplasAvaliadas.OrderBy(d => d.score).Take(5))
            {
                var p1Nome = candidatos.First(p => p.Id == dupla.p1).Nome;
                var p2Nome = candidatos.First(p => p.Id == dupla.p2).Nome;
                var chaveDupla = GerarChaveDupla(dupla.p1, dupla.p2);
                var freqAtual = frequenciaDuplasGeracao.GetValueOrDefault(chaveDupla, 0);
                var saoIneditos = !parceirosDaGeracao[dupla.p1].Contains(dupla.p2);
                
                _logger.LogInformation(
                    $"  {dupla.p1}({p1Nome}) + {dupla.p2}({p2Nome}) ? " +
                    $"Score: {dupla.score:F2} | Freq: {freqAtual} | Inédita: {saoIneditos} | {dupla.motivo}");
            }

            var melhor = duplasAvaliadas.OrderBy(d => d.score).First();
            
            var melhorP1Nome = candidatos.First(p => p.Id == melhor.p1).Nome;
            var melhorP2Nome = candidatos.First(p => p.Id == melhor.p2).Nome;
            var melhorChave = GerarChaveDupla(melhor.p1, melhor.p2);
            var melhorFreq = frequenciaDuplasGeracao.GetValueOrDefault(melhorChave, 0);
            var melhorInedita = !parceirosDaGeracao[melhor.p1].Contains(melhor.p2);
            
            _logger.LogInformation(
                $"[DEBUG] ? DUPLA ESCOLHIDA: {melhor.p1}({melhorP1Nome}) + {melhor.p2}({melhorP2Nome}) ? " +
                $"Score: {melhor.score:F2} | Freq: {melhorFreq} | Inédita: {melhorInedita}");
            
            return new List<int> { melhor.p1, melhor.p2 };
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
