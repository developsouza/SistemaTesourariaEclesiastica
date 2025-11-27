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

            // Histórico ampliado das últimas 10 duplas usadas (com peso decrescente)
            var historicoDuplas = new Queue<string>();

            // Rastrear frequência global de duplas
            var frequenciaDuplasGeracao = new Dictionary<string, int>();
            // Rastrear última data de trabalho de cada porteiro (SOLO ou DUPLA)
            var ultimaDataTrabalho = porteiros.ToDictionary(p => p.Id, p => (DateTime?)null);

            // NOVO: Rastrear horários trabalhados no dia atual
            var horariosTrabalhados = porteiros.ToDictionary(p => p.Id, p => new List<TimeSpan>());

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

            var escalasGeradas = new List<EscalaPorteiro>();
            var random = new Random(DateTime.Now.Millisecond);
            var responsavelSelecionado = responsaveis.First();

            // Rastrear últimas 5 escalas (para porteiros individuais - SOLO ou DUPLA)
            var historicoEscalasGeracao = new Queue<List<int>>();

            DateTime? dataUltimoDiaProcessado = null;

            _logger.LogInformation($"Iniciando geração com {porteiros.Count} porteiros ativos");

            // CALCULAR DESCANSO MÍNIMO VIÁVEL baseado na demanda
            var totalEscalasNecessarias = diasOrdenados.Count;
            var totalPosicoesNecessarias = diasOrdenados.Sum(d => 
                (d.Horario.HasValue && d.Horario.Value >= new TimeSpan(19, 0, 0)) ? 2 : 1);
            
            var porteirosDisponiveis = porteiros.Count;
            var mediaEscalasPorPorteiro = (double)totalPosicoesNecessarias / porteirosDisponiveis;
            var diasNoMes = (diasOrdenados.Last().Data - diasOrdenados.First().Data).TotalDays + 1;
            var frequenciaMedia = diasNoMes / mediaEscalasPorPorteiro;
            
            // Calcular descanso mínimo viável
            int DIAS_MINIMO_DESCANSO;
            if (frequenciaMedia >= 7)
                DIAS_MINIMO_DESCANSO = 3; // Baixa demanda: pode exigir 3 dias
            else if (frequenciaMedia >= 5)
                DIAS_MINIMO_DESCANSO = 2; // Demanda média: 2 dias
            else
                DIAS_MINIMO_DESCANSO = 1; // Alta demanda: 1 dia (trabalha dia sim, dia não)
            
            _logger.LogInformation($"📊 Análise de Demanda:");
            _logger.LogInformation($"  - Total de escalas: {totalEscalasNecessarias}");
            _logger.LogInformation($"  - Total de posições: {totalPosicoesNecessarias}");
            _logger.LogInformation($"  - Porteiros disponíveis: {porteirosDisponiveis}");
            _logger.LogInformation($"  - Média por porteiro: {mediaEscalasPorPorteiro:F1} posições");
            _logger.LogInformation($"  - Frequência média: a cada {frequenciaMedia:F1} dias");
            _logger.LogInformation($"  - ⚙️ Descanso mínimo definido: {DIAS_MINIMO_DESCANSO} dia(s)");
            _logger.LogInformation("");

            // Processar cada dia
            foreach (var dia in diasOrdenados)
            {
                // Mudança de dia - resetar controle de horários
                if (dataUltimoDiaProcessado == null || dataUltimoDiaProcessado.Value.Date != dia.Data.Date)
                {
                    // Limpar horários trabalhados de todos os porteiros
                    foreach (var porteiroId in horariosTrabalhados.Keys.ToList())
                    {
                        horariosTrabalhados[porteiroId].Clear();
                    }
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

                // BLOQUEIO RÍGIDO: Calcular porteiros bloqueados por descanso insuficiente
                var bloqueadosPorDescanso = new HashSet<int>();

                foreach (var porteiro in porteiros)
                {
                    if (ultimaDataTrabalho[porteiro.Id].HasValue)
                    {
                        var diasDescanso = (dia.Data - ultimaDataTrabalho[porteiro.Id].Value).TotalDays;
                        
                        // Bloqueio ABSOLUTO se descanso < mínimo calculado
                        if (diasDescanso < DIAS_MINIMO_DESCANSO)
                        {
                            bloqueadosPorDescanso.Add(porteiro.Id);
                            _logger.LogDebug($"  {porteiro.Nome} BLOQUEADO por descanso insuficiente: {diasDescanso:F1} dias (mín: {DIAS_MINIMO_DESCANSO})");
                        }
                    }
                }

                // BLOQUEIO ADICIONAL: Porteiros que já trabalharam em outro horário HOJE
                var bloqueadosPorHorarioHoje = new HashSet<int>();
                if (dia.Horario.HasValue)
                {
                    foreach (var porteiro in porteiros)
                    {
                        // Se já trabalhou hoje em qualquer horário, bloqueia
                        if (horariosTrabalhados[porteiro.Id].Any())
                        {
                            bloqueadosPorHorarioHoje.Add(porteiro.Id);
                            _logger.LogDebug($"  {porteiro.Nome} BLOQUEADO por já ter trabalhado hoje às {horariosTrabalhados[porteiro.Id][0]:hh\\:mm}");
                        }
                    }
                }

                // Combinar todos os bloqueios
                var bloqueadosTotais = new HashSet<int>(bloqueadosPorDescanso);
                bloqueadosTotais.UnionWith(bloqueadosPorHorarioHoje);

                // Filtrar porteiros elegíveis
                var elegiveisBase = porteiros.Where(p => p.EstaDisponivelNodia(dia.Data.DayOfWeek)).ToList();
                if (dia.Horario.HasValue)
                {
                    elegiveisBase = elegiveisBase.Where(p => string.IsNullOrWhiteSpace(p.HorariosDisponiveis) || p.EstaDisponivelNoHorario(dia.Horario.Value)).ToList();
                }

                var porteirosElegiveis = elegiveisBase.Where(p => !bloqueadosTotais.Contains(p.Id)).ToList();

                _logger.LogInformation($"Data {dia.Data:dd/MM/yyyy} {dia.Horario?.ToString(@"hh\:mm") ?? "sem horário"}: {porteirosElegiveis.Count} elegíveis de {elegiveisBase.Count} disponíveis (bloqueados: {bloqueadosTotais.Count})");

                // Randomizar a ordem dos porteiros elegíveis para aumentar variedade
                porteirosElegiveis = porteirosElegiveis.OrderBy(_ => random.Next()).ToList();

                // Fallback SOMENTE em situação CRÍTICA
                var precisouRelaxar = false;
                if (porteirosElegiveis.Count < quantidadePorteiros)
                {
                    _logger.LogWarning($"⚠️ SITUAÇÃO CRÍTICA em {dia.Data:dd/MM/yyyy}: necessário {quantidadePorteiros}, disponíveis {porteirosElegiveis.Count}");
                    
                    // Relaxar APENAS bloqueio por descanso (mantém bloqueio de horário do mesmo dia)
                    var complementares = elegiveisBase
                        .Where(p => bloqueadosPorDescanso.Contains(p.Id) && !bloqueadosPorHorarioHoje.Contains(p.Id))
                        .OrderBy(p => ultimaDataTrabalho[p.Id].HasValue ? (dia.Data - ultimaDataTrabalho[p.Id].Value).TotalDays : 999)
                        .ThenBy(p => pontuacaoPorteiros[p.Id])
                        .ThenBy(_ => random.Next())
                        .ToList();

                    foreach (var p in complementares)
                    {
                        if (!porteirosElegiveis.Contains(p))
                        {
                            var diasDescanso = ultimaDataTrabalho[p.Id].HasValue 
                                ? (dia.Data - ultimaDataTrabalho[p.Id].Value).TotalDays 
                                : 999;
                            _logger.LogWarning($"    Relaxando bloqueio para {p.Nome} (descanso: {diasDescanso:F1} dias)");
                            porteirosElegiveis.Add(p);
                            precisouRelaxar = true;
                        }
                        if (porteirosElegiveis.Count >= quantidadePorteiros) break;
                    }
                }

                if (!porteirosElegiveis.Any())
                {
                    _logger.LogError($"❌ Sem porteiros elegíveis para {dia.Data:dd/MM/yyyy}. Dia será ignorado.");
                    continue;
                }

                // SELEÇÃO: Baseada em variedade de duplas e espaçamento de datas
                var selecionados = SelecionarPorteirosComVariedade(
                    porteirosElegiveis,
                    quantidadePorteiros,
                    historicoEscalasGeracao,
                    pontuacaoPorteiros,
                    porteirosLimitados,
                    frequenciaDuplasGeracao,
                    historicoDuplas,
                    ultimaDataTrabalho,
                    dia.Data,
                    random,
                    precisouRelaxar);

                if (!selecionados.Any())
                {
                    _logger.LogError($"Falha ao selecionar porteiros para {dia.Data:dd/MM/yyyy}.");
                    continue;
                }

                // Registrar pontuação, última data de trabalho e horários trabalhados
                foreach (var id in selecionados)
                {
                    pontuacaoPorteiros[id]++;
                    ultimaDataTrabalho[id] = dia.Data;
                    
                    // Registrar horário trabalhado hoje
                    if (dia.Horario.HasValue)
                    {
                        horariosTrabalhados[id].Add(dia.Horario.Value);
                    }
                    
                    var nomePorteiro = porteiros.First(p => p.Id == id).Nome;
                    _logger.LogInformation($"  ✓ {nomePorteiro} escalado para {dia.Data:dd/MM/yyyy} {dia.Horario?.ToString(@"hh\:mm") ?? ""}");
                }

                // Atualizar rastreamento de duplas (somente para duplas)
                if (selecionados.Count == 2)
                {
                    var p1 = selecionados[0];
                    var p2 = selecionados[1];

                    var chaveDupla = GerarChaveDupla(p1, p2);
                    frequenciaDuplasGeracao[chaveDupla] = frequenciaDuplasGeracao.GetValueOrDefault(chaveDupla) + 1;

                    // Adicionar ao histórico de duplas recentes (aumentado para 10)
                    historicoDuplas.Enqueue(chaveDupla);
                    if (historicoDuplas.Count > 10)
                        historicoDuplas.Dequeue();
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

                // Atualizar histórico de escalas (PARA TODOS - solo ou dupla)
                historicoEscalasGeracao.Enqueue(selecionados);
                if (historicoEscalasGeracao.Count > 5)
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
        /// Sistema de seleção baseado em VARIEDADE MÁXIMA com espaçamento temporal
        /// Prioriza duplas que não trabalharam juntas recentemente e porteiros descansados
        /// </summary>
        private List<int> SelecionarPorteirosComVariedade(
            List<Porteiro> porteirosElegiveis,
            int quantidadeNecessaria,
            Queue<List<int>> historicoEscalasGeracao,
            Dictionary<int, int> pontuacaoPorteiros,
            HashSet<int> porteirosLimitados,
            Dictionary<string, int> frequenciaDuplasGeracao,
            Queue<string> historicoDuplas,
            Dictionary<int, DateTime?> ultimaDataTrabalho,
            DateTime dataAtual,
            Random random,
            bool ignorarHistorico = false)
        {
            // Porteiros das últimas 5 escalas para evitar repetição imediata (inclui solo E dupla)
            var porteirosRecentes = historicoEscalasGeracao
                .SelectMany(h => h)
                .Distinct()
                .ToList();

            if (quantidadeNecessaria == 1)
            {
                // Para 1 porteiro, prioriza quem está descansado há mais tempo
                // IMPORTANTE: Agora considera TODOS os trabalhos anteriores (solo ou dupla)
                var candidatoSolo = porteirosElegiveis
                    .Where(p => !porteirosRecentes.Contains(p.Id))
                    .OrderByDescending(p => ultimaDataTrabalho[p.Id].HasValue ? (dataAtual - ultimaDataTrabalho[p.Id].Value).TotalDays : double.MaxValue)
                    .ThenBy(p => pontuacaoPorteiros[p.Id])
                    .ThenBy(p => porteirosLimitados.Contains(p.Id) ? 0 : 1)
                    .ThenBy(_ => random.Next())
                    .FirstOrDefault();

                if (candidatoSolo != null)
                {
                    var diasDescanso = ultimaDataTrabalho[candidatoSolo.Id].HasValue 
                        ? (dataAtual - ultimaDataTrabalho[candidatoSolo.Id].Value).TotalDays 
                        : 999;
                    _logger.LogDebug($"Porteiro SOLO selecionado: {candidatoSolo.Nome} (descanso: {diasDescanso:F1} dias)");
                    return new List<int> { candidatoSolo.Id };
                }

                // Fallback
                var fallbackSolo = porteirosElegiveis
                    .OrderByDescending(p => ultimaDataTrabalho[p.Id].HasValue ? (dataAtual - ultimaDataTrabalho[p.Id].Value).TotalDays : double.MaxValue)
                    .ThenBy(p => pontuacaoPorteiros[p.Id])
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
            var duplasAvaliadas = new List<(int p1, int p2, double score)>();

            for (int i = 0; i < candidatos.Count; i++)
            {
                for (int j = i + 1; j < candidatos.Count; j++)
                {
                    var p1 = candidatos[i];
                    var p2 = candidatos[j];
                    var chaveDupla = GerarChaveDupla(p1.Id, p2.Id);

                    double score = 0;

                    // 1. PENALIZAÇÃO MUITO PESADA por estar no histórico recente de duplas
                    if (!ignorarHistorico)
                    {
                        var historicoLista = historicoDuplas.Reverse().ToList();
                        for (int k = 0; k < historicoLista.Count; k++)
                        {
                            if (historicoLista[k] == chaveDupla)
                            {
                                // Última dupla = +8000, penúltima = +7000, etc (peso MUITO maior)
                                score += 8000 - (k * 800);
                            }
                        }
                    }
                    else
                    {
                        // Em situação crítica, penalização reduzida
                        var historicoLista = historicoDuplas.Reverse().ToList();
                        for (int k = 0; k < historicoLista.Count; k++)
                        {
                            if (historicoLista[k] == chaveDupla)
                            {
                                score += (8000 - (k * 800)) / 5;
                            }
                        }
                    }

                    // 2. Penalização FORTE por frequência total de uso da dupla
                    var frequencia = frequenciaDuplasGeracao.GetValueOrDefault(chaveDupla, 0);
                    score += frequencia * 1200; // Aumentado ainda mais

                    // 3. PENALIZAÇÃO CRÍTICA por trabalho recente individual
                    // ESTE É O AJUSTE PRINCIPAL - considera trabalhos solo E dupla
                    if (ultimaDataTrabalho[p1.Id].HasValue)
                    {
                        var diasDescanso = (dataAtual - ultimaDataTrabalho[p1.Id].Value).TotalDays;
                        
                        // PENALIZAÇÃO GRADUADA (ajustada dinamicamente):
                        if (diasDescanso < 1.5)
                            score += 15000; // Bloqueio muito pesado
                        else if (diasDescanso < 2.5)
                            score += 8000;  // Pesado
                        else if (diasDescanso < 3.5)
                            score += 3000;  // Moderado
                        else if (diasDescanso < 5)
                            score += 1000;  // Leve
                        else if (diasDescanso < 7)
                            score += 300;   // Muito leve
                        // 7+ dias = sem penalização
                    }
                    else
                    {
                        // Bônus grande para quem nunca trabalhou
                        score -= 1500;
                    }

                    if (ultimaDataTrabalho[p2.Id].HasValue)
                    {
                        var diasDescanso = (dataAtual - ultimaDataTrabalho[p2.Id].Value).TotalDays;
                        
                        if (diasDescanso < 1.5)
                            score += 15000;
                        else if (diasDescanso < 2.5)
                            score += 8000;
                        else if (diasDescanso < 3.5)
                            score += 3000;
                        else if (diasDescanso < 5)
                            score += 1000;
                        else if (diasDescanso < 7)
                            score += 300;
                    }
                    else
                    {
                        score -= 1500;
                    }

                    // 4. Penalização por pontuação individual (balanceamento secundário)
                    score += pontuacaoPorteiros[p1.Id] * 100;
                    score += pontuacaoPorteiros[p2.Id] * 100;

                    // 5. BÔNUS para porteiros limitados (prioridade)
                    if (porteirosLimitados.Contains(p1.Id)) score -= 500;
                    if (porteirosLimitados.Contains(p2.Id)) score -= 500;

                    // 6. BÔNUS GRANDE para porteiros que nunca trabalharam juntos
                    if (frequencia == 0) score -= 2000;

                    // 7. BÔNUS para porteiros bem descansados (7+ dias)
                    if (ultimaDataTrabalho[p1.Id].HasValue && (dataAtual - ultimaDataTrabalho[p1.Id].Value).TotalDays >= 7)
                        score -= 300;
                    if (ultimaDataTrabalho[p2.Id].HasValue && (dataAtual - ultimaDataTrabalho[p2.Id].Value).TotalDays >= 7)
                        score -= 300;

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

            var p1Nome = porteirosElegiveis.First(p => p.Id == melhorDupla.p1).Nome;
            var p2Nome = porteirosElegiveis.First(p => p.Id == melhorDupla.p2).Nome;
            
            var p1Descanso = ultimaDataTrabalho[melhorDupla.p1].HasValue 
                ? (dataAtual - ultimaDataTrabalho[melhorDupla.p1].Value).TotalDays 
                : 999;
            var p2Descanso = ultimaDataTrabalho[melhorDupla.p2].HasValue 
                ? (dataAtual - ultimaDataTrabalho[melhorDupla.p2].Value).TotalDays 
                : 999;
            
            _logger.LogDebug($"Dupla selecionada para {dataAtual:dd/MM/yyyy}: {p1Nome} ({p1Descanso:F1}d) + {p2Nome} ({p2Descanso:F1}d) (score: {melhorDupla.score:F2})");

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