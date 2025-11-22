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

            // Buscar as últimas escalas para fazer distribuição justa
            var ultimasEscalas = await _context.EscalasPorteiros
                .Where(e => e.DataCulto < diasOrdenados.First().Data)
                .OrderByDescending(e => e.DataCulto)
                .Take(20)
                .Include(e => e.Porteiro)
                .Include(e => e.Porteiro2) // ? INCLUIR PORTEIRO2
                .ToListAsync();

            // Criar lista para controlar distribuição
            var distribuicaoPorteiros = porteiros.ToDictionary(
                p => p.Id,
                p => ultimasEscalas.Count(e => e.PorteiroId == p.Id || e.Porteiro2Id == p.Id)
            );

            var escalasGeradas = new List<EscalaPorteiro>();
            var random = new Random();

            // Selecionar um responsável (pode ser rotativo ou fixo)
            var responsavelSelecionado = responsaveis.First();

            // Variável para rastrear os últimos porteiros escalados
            var ultimosPorteirosIds = new List<int>();
            if (ultimasEscalas.Any())
            {
                ultimosPorteirosIds.Add(ultimasEscalas.First().PorteiroId);
                if (ultimasEscalas.First().Porteiro2Id.HasValue)
                    ultimosPorteirosIds.Add(ultimasEscalas.First().Porteiro2Id.Value);
            }

            foreach (var dia in diasOrdenados)
            {
                // ? CORREÇÃO: Verificar se já existe escala para este dia E HORÁRIO
                var escalaExistente = await _context.EscalasPorteiros
                    .AnyAsync(e => e.DataCulto.Date == dia.Data.Date &&
                                   e.Horario == dia.Horario);

                if (escalaExistente)
                {
                    // ? CORREÇÃO: Formatação manual de TimeSpan
                    var horarioStr = dia.Horario.HasValue ? $" às {dia.Horario.Value.Hours:D2}:{dia.Horario.Value.Minutes:D2}" : "";
                    _logger.LogWarning($"Já existe escala para o dia {dia.Data:dd/MM/yyyy}{horarioStr}. Pulando...");
                    continue;
                }

                // Determinar quantidade de porteiros necessários
                // Escola Bíblica (domingo manhã) = 1 porteiro
                // Demais cultos = 2 porteiros
                int quantidadePorteiros = (dia.TipoCulto == TipoCulto.EscolaBiblica) ? 1 : 2;

                // ? LOG DETALHADO: Informações do dia/horário sendo processado
                var horarioInfo = dia.Horario.HasValue ? $" às {dia.Horario.Value.Hours:D2}:{dia.Horario.Value.Minutes:D2}" : " (sem horário)";
                _logger.LogInformation($"?? Processando escala para {dia.Data:dd/MM/yyyy} ({dia.Data.DayOfWeek}){horarioInfo} - Tipo: {dia.TipoCulto}");

                // ? CORREÇÃO: Filtrar porteiros elegíveis para este dia E horário específico
                var porteirosElegiveis = porteiros
                    .Where(p =>
                    {
                        // ? LOG: Verificando cada porteiro
                        _logger.LogDebug($"   ?? Verificando porteiro: {p.Nome}");
                        _logger.LogDebug($"      - DiasDisponiveis: '{p.DiasDisponiveis ?? "(vazio)"}'");
                        _logger.LogDebug($"      - HorariosDisponiveis: '{p.HorariosDisponiveis ?? "(vazio)"}'");

                        // 1. Verificar disponibilidade de dia da semana
                        var disponivelNoDia = p.EstaDisponivelNodia(dia.Data.DayOfWeek);
                        _logger.LogDebug($"      - Disponível no dia {dia.Data.DayOfWeek}? {(disponivelNoDia ? "? SIM" : "? NÃO")}");

                        if (!disponivelNoDia)
                        {
                            return false;
                        }

                        // 2. ? CORREÇÃO CRÍTICA: Verificar disponibilidade de horário (se o culto tiver horário)
                        if (dia.Horario.HasValue)
                        {
                            var horarioFormatado = $"{dia.Horario.Value.Hours:D2}:{dia.Horario.Value.Minutes:D2}";
                            _logger.LogDebug($"      - Validando horário {horarioFormatado}...");

                            // Se o porteiro tem horários configurados, deve validar
                            if (!string.IsNullOrWhiteSpace(p.HorariosDisponiveis))
                            {
                                var disponivelNoHorario = p.EstaDisponivelNoHorario(dia.Horario.Value);
                                _logger.LogDebug($"      - Disponível no horário {horarioFormatado}? {(disponivelNoHorario ? "? SIM" : "? NÃO")}");

                                if (!disponivelNoHorario)
                                {
                                    return false;
                                }
                            }
                            else
                            {
                                _logger.LogDebug($"      - ? Sem horários configurados = disponível em todos os horários");
                            }
                        }
                        else
                        {
                            _logger.LogDebug($"      - ? Culto sem horário específico");
                        }

                        _logger.LogDebug($"      ? Porteiro {p.Nome} é ELEGÍVEL");
                        return true;
                    })
                    .ToList();

                if (!porteirosElegiveis.Any())
                {
                    _logger.LogWarning($"?? NENHUM porteiro disponível para {dia.Data:dd/MM/yyyy}{horarioInfo} ({dia.Data.DayOfWeek}). Usando todos os porteiros como fallback.");
                    porteirosElegiveis = porteiros.ToList();
                }
                else
                {
                    var nomesElegiveis = string.Join(", ", porteirosElegiveis.Select(p => p.Nome));
                    _logger.LogInformation($"? Porteiros elegíveis para {dia.Data:dd/MM/yyyy}{horarioInfo}: {nomesElegiveis}");
                }

                // Selecionar porteiros evitando repetição consecutiva
                var porteirosSelecionados = new List<int>();

                for (int i = 0; i < quantidadePorteiros; i++)
                {
                    var candidatos = porteirosElegiveis
                        .Where(p => !porteirosSelecionados.Contains(p.Id)) // Não selecionar o mesmo porteiro duas vezes
                        .Where(p => !ultimosPorteirosIds.Contains(p.Id)) // Evitar últimos escalados
                        .OrderBy(p => distribuicaoPorteiros[p.Id])
                        .ThenBy(_ => random.Next())
                        .ToList();

                    // Se não houver candidatos, permitir qualquer um
                    if (!candidatos.Any())
                    {
                        candidatos = porteirosElegiveis
                            .Where(p => !porteirosSelecionados.Contains(p.Id))
                            .OrderBy(p => distribuicaoPorteiros[p.Id])
                            .ThenBy(_ => random.Next())
                            .ToList();
                    }

                    if (candidatos.Any())
                    {
                        var porteiroSelecionado = candidatos.First();
                        porteirosSelecionados.Add(porteiroSelecionado.Id);
                        distribuicaoPorteiros[porteiroSelecionado.Id]++;
                    }
                }

                if (!porteirosSelecionados.Any())
                {
                    _logger.LogError($"? Não foi possível selecionar porteiros para {dia.Data:dd/MM/yyyy}");
                    continue;
                }

                // Criar a escala
                var escala = new EscalaPorteiro
                {
                    DataCulto = dia.Data,
                    Horario = dia.Horario, // ? GARANTIR QUE O HORÁRIO É SALVO
                    TipoCulto = dia.TipoCulto,
                    PorteiroId = porteirosSelecionados[0],
                    Porteiro2Id = porteirosSelecionados.Count > 1 ? porteirosSelecionados[1] : null,
                    ResponsavelId = responsavelSelecionado.Id,
                    Observacao = dia.Observacao,
                    DataGeracao = DateTime.Now,
                    UsuarioGeracaoId = usuarioId
                };

                escalasGeradas.Add(escala);

                // Atualizar rastreamento
                ultimosPorteirosIds = porteirosSelecionados;

                var nomes = string.Join(" e ", porteirosSelecionados.Select(id => porteiros.First(p => p.Id == id).Nome));
                var horarioLog = dia.Horario.HasValue ? $" às {dia.Horario.Value.Hours:D2}:{dia.Horario.Value.Minutes:D2}" : "";
                _logger.LogInformation($"? Porteiro(s) selecionado(s) para {dia.Data:dd/MM/yyyy}{horarioLog}: {nomes}");
            }

            return escalasGeradas;
        }

        /// <summary>
        /// Busca as configurações padrão de cultos (semanais e datas específicas)
        /// </summary>
        public async Task<List<ConfiguracaoCulto>> ObterConfiguracoesPadraoAsync()
        {
            return await _context.ConfiguracoesCultos
                .Where(c => c.Ativo)
                .OrderBy(c => c.DataEspecifica.HasValue ? 0 : 1) // Datas específicas primeiro
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
                // Se não houver configurações, usar padrões
                configuracoes = ObterConfiguracoesPadrao();
            }

            // Separar configurações por tipo
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

            // ? CORREÇÃO: Adicionar TODOS os cultos semanais (múltiplos horários no mesmo dia)
            if (configuracoesSemanais.Any())
            {
                // Iterar por todos os dias no período
                for (var data = dataInicio; data <= dataFim; data = data.AddDays(1))
                {
                    // ? CORREÇÃO: Buscar TODAS as configurações para este dia da semana
                    var configsDoDia = configuracoesSemanais
                        .Where(c => c.DiaSemana == data.DayOfWeek)
                        .ToList();

                    // Para cada configuração encontrada, adicionar um culto
                    foreach (var config in configsDoDia)
                    {
                        // ? Verificar se não há data específica para este dia E horário
                        var temDataEspecificaMesmoHorario = diasSugeridos.Any(d =>
                            d.Data.Date == data.Date &&
                            d.Horario == config.Horario);

                        if (!temDataEspecificaMesmoHorario)
                        {
                            diasSugeridos.Add(new DiasCultoViewModel
                            {
                                Data = data,
                                Horario = config.Horario,
                                TipoCulto = config.TipoCulto, // ? Agora mantém o tipo correto (EscolaBiblica ou Evangelistico)
                                Observacao = config.Observacao
                            });
                        }
                    }
                }
            }

            // Ordenar por data E horário
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
                    Horario = new TimeSpan(9, 0, 0), // 09:00
                    TipoCulto = TipoCulto.EscolaBiblica,
                    Ativo = true
                },
                new ConfiguracaoCulto
                {
                    DiaSemana = DayOfWeek.Sunday,
                    Horario = new TimeSpan(19, 0, 0), // 19:00
                    TipoCulto = TipoCulto.Evangelistico,
                    Ativo = true
                },
                new ConfiguracaoCulto
                {
                    DiaSemana = DayOfWeek.Wednesday,
                    Horario = new TimeSpan(19, 30, 0), // 19:30
                    TipoCulto = TipoCulto.DaFamilia,
                    Ativo = true
                },
                new ConfiguracaoCulto
                {
                    DiaSemana = DayOfWeek.Friday,
                    Horario = new TimeSpan(19, 30, 0), // 19:30
                    TipoCulto = TipoCulto.DeDoutrina,
                    Ativo = true
                }
            };
        }

        /// <summary>
        /// ?? DIAGNÓSTICO: Verifica configuração de disponibilidade de todos os porteiros
        /// </summary>
        public async Task<string> DiagnosticarDisponibilidadePorteirosAsync()
        {
            var porteiros = await _context.Porteiros
                .Where(p => p.Ativo)
                .OrderBy(p => p.Nome)
                .ToListAsync();

            var diagnostico = new System.Text.StringBuilder();
            diagnostico.AppendLine("=== ?? DIAGNÓSTICO DE DISPONIBILIDADE DE PORTEIROS ===\n");
            diagnostico.AppendLine($"Data/Hora: {DateTime.Now:dd/MM/yyyy HH:mm:ss}\n");
            diagnostico.AppendLine($"Total de porteiros ativos: {porteiros.Count}\n");
            diagnostico.AppendLine("?????????????????????????????????????????????????????\n");

            foreach (var porteiro in porteiros)
            {
                porteiro.CarregarDisponibilidade();

                diagnostico.AppendLine($"?? PORTEIRO: {porteiro.Nome}");
                diagnostico.AppendLine($"   ID: {porteiro.Id}");
                diagnostico.AppendLine($"   Telefone: {porteiro.Telefone}");
                diagnostico.AppendLine($"   Ativo: {(porteiro.Ativo ? "? SIM" : "? NÃO")}");
                diagnostico.AppendLine();

                // Dias disponíveis (raw)
                diagnostico.AppendLine($"   ?? DiasDisponiveis (banco): '{porteiro.DiasDisponiveis ?? "(NULL/VAZIO)"}'");

                // Dias disponíveis (interpretado)
                var diasInterpretados = new List<string>();
                if (porteiro.DisponibilidadeDomingo) diasInterpretados.Add("Domingo");
                if (porteiro.DisponibilidadeSegunda) diasInterpretados.Add("Segunda");
                if (porteiro.DisponibilidadeTerca) diasInterpretados.Add("Terça");
                if (porteiro.DisponibilidadeQuarta) diasInterpretados.Add("Quarta");
                if (porteiro.DisponibilidadeQuinta) diasInterpretados.Add("Quinta");
                if (porteiro.DisponibilidadeSexta) diasInterpretados.Add("Sexta");
                if (porteiro.DisponibilidadeSabado) diasInterpretados.Add("Sábado");

                if (diasInterpretados.Any())
                {
                    diagnostico.AppendLine($"   ?? Dias interpretados: {string.Join(", ", diasInterpretados)}");
                }
                else
                {
                    diagnostico.AppendLine($"   ?? Dias interpretados: ? NENHUM DIA SELECIONADO (disponível em todos)");
                }
                diagnostico.AppendLine();

                // Horários disponíveis (raw)
                diagnostico.AppendLine($"   ? HorariosDisponiveis (banco): '{porteiro.HorariosDisponiveis ?? "(NULL/VAZIO)"}'");

                // Horários disponíveis (interpretado)
                if (!string.IsNullOrWhiteSpace(porteiro.HorariosDisponiveis))
                {
                    var horarios = porteiro.HorariosDisponiveis.Split(',', StringSplitOptions.RemoveEmptyEntries);
                    diagnostico.AppendLine($"   ? Horários interpretados: {string.Join(", ", horarios.Select(h => h.Trim()))}");
                }
                else
                {
                    diagnostico.AppendLine($"   ? Horários interpretados: ? NENHUM HORÁRIO ESPECIFICADO (disponível em todos)");
                }
                diagnostico.AppendLine();

                // Testes de disponibilidade
                diagnostico.AppendLine($"   ?? TESTES DE VALIDAÇÃO:");

                // Teste de dias
                var testesDias = new[] { DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday };
                foreach (var dia in testesDias)
                {
                    var disponivel = porteiro.EstaDisponivelNodia(dia);
                    diagnostico.AppendLine($"      - {dia}: {(disponivel ? "? Disponível" : "? Indisponível")}");
                }
                diagnostico.AppendLine();

                // Teste de horários
                if (!string.IsNullOrWhiteSpace(porteiro.HorariosDisponiveis))
                {
                    var testesHorarios = new[] { new TimeSpan(9, 0, 0), new TimeSpan(19, 0, 0), new TimeSpan(19, 30, 0) };
                    foreach (var horario in testesHorarios)
                    {
                        var disponivel = porteiro.EstaDisponivelNoHorario(horario);
                        // ? CORREÇÃO: Formatação manual de TimeSpan
                        var horarioFormatado = $"{horario.Hours:D2}:{horario.Minutes:D2}";
                        diagnostico.AppendLine($"      - {horarioFormatado}: {(disponivel ? "? Disponível" : "? Indisponível")}");
                    }
                }

                diagnostico.AppendLine();
                diagnostico.AppendLine("?????????????????????????????????????????????????????\n");
            }

            diagnostico.AppendLine("=== FIM DO DIAGNÓSTICO ===");
            return diagnostico.ToString();
        }
    }
}
