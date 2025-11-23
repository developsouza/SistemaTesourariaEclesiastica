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

            // Buscar as últimas 50 escalas para ter um histórico mais amplo
            var dataMinima = diasOrdenados.First().Data.AddMonths(-3);
            var ultimasEscalas = await _context.EscalasPorteiros
                .Where(e => e.DataCulto >= dataMinima && e.DataCulto < diasOrdenados.First().Data)
                .OrderByDescending(e => e.DataCulto)
                .ThenByDescending(e => e.Horario)
                .Take(50)
                .Include(e => e.Porteiro)
                .Include(e => e.Porteiro2)
                .ToListAsync();

            // Criar dicionário de distribuição baseado no histórico real
            var distribuicaoPorteiros = porteiros.ToDictionary(
                p => p.Id,
                p => ultimasEscalas.Count(e => e.PorteiroId == p.Id || e.Porteiro2Id == p.Id)
            );

            // Identificar porteiros com disponibilidade limitada (apenas 1 dia ou 1 horário)
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

            var escalasGeradas = new List<EscalaPorteiro>();
            var random = new Random(DateTime.Now.Millisecond);
            var responsavelSelecionado = responsaveis.First();

            // Rastrear os últimos 3 porteiros de cada escala
            var historicoEscalas = new Queue<List<int>>();

            foreach (var escala in ultimasEscalas.Take(3))
            {
                var porteirosEscala = new List<int> { escala.PorteiroId };
                if (escala.Porteiro2Id.HasValue)
                    porteirosEscala.Add(escala.Porteiro2Id.Value);
                historicoEscalas.Enqueue(porteirosEscala);
            }

            foreach (var dia in diasOrdenados)
            {
                var escalaExistente = await _context.EscalasPorteiros
                    .AnyAsync(e => e.DataCulto.Date == dia.Data.Date && e.Horario == dia.Horario);

                if (escalaExistente)
                {
                    var horarioStr = dia.Horario.HasValue ? $" às {dia.Horario.Value.Hours:D2}:{dia.Horario.Value.Minutes:D2}" : "";
                    _logger.LogWarning($"Já existe escala para o dia {dia.Data:dd/MM/yyyy}{horarioStr}. Pulando...");
                    continue;
                }

                int quantidadePorteiros = (dia.TipoCulto == TipoCulto.EscolaBiblica) ? 1 : 2;

                // Filtrar porteiros elegíveis para este dia e horário
                var porteirosElegiveis = porteiros
                    .Where(p =>
                    {
                        if (!p.EstaDisponivelNodia(dia.Data.DayOfWeek))
                            return false;

                        if (dia.Horario.HasValue && !string.IsNullOrWhiteSpace(p.HorariosDisponiveis))
                        {
                            if (!p.EstaDisponivelNoHorario(dia.Horario.Value))
                                return false;
                        }

                        return true;
                    })
                    .ToList();

                if (!porteirosElegiveis.Any())
                {
                    var horarioInfo = dia.Horario.HasValue ? $" às {dia.Horario.Value.Hours:D2}:{dia.Horario.Value.Minutes:D2}" : "";
                    _logger.LogWarning($"Nenhum porteiro disponível para {dia.Data:dd/MM/yyyy}{horarioInfo}. Usando todos como fallback.");
                    porteirosElegiveis = porteiros.ToList();
                }

                var porteirosSelecionados = SelecionarPorteirosBalanceados(
                    porteirosElegiveis,
                    quantidadePorteiros,
                    historicoEscalas,
                    distribuicaoPorteiros,
                    porteirosLimitados,
                    random
                );

                if (!porteirosSelecionados.Any())
                {
                    _logger.LogError($"Não foi possível selecionar porteiros para {dia.Data:dd/MM/yyyy}");
                    continue;
                }

                foreach (var porteiroId in porteirosSelecionados)
                {
                    distribuicaoPorteiros[porteiroId]++;
                }

                var escala = new EscalaPorteiro
                {
                    DataCulto = dia.Data,
                    Horario = dia.Horario,
                    TipoCulto = dia.TipoCulto,
                    PorteiroId = porteirosSelecionados[0],
                    Porteiro2Id = porteirosSelecionados.Count > 1 ? porteirosSelecionados[1] : null,
                    ResponsavelId = responsavelSelecionado.Id,
                    Observacao = dia.Observacao,
                    DataGeracao = DateTime.Now,
                    UsuarioGeracaoId = usuarioId
                };

                escalasGeradas.Add(escala);

                historicoEscalas.Enqueue(porteirosSelecionados);
                if (historicoEscalas.Count > 3)
                    historicoEscalas.Dequeue();
            }

            return escalasGeradas;
        }

        /// <summary>
        /// Seleciona porteiros de forma balanceada, evitando repetições consecutivas
        /// </summary>
        private List<int> SelecionarPorteirosBalanceados(
            List<Porteiro> porteirosElegiveis,
            int quantidadeNecessaria,
            Queue<List<int>> historicoEscalas,
            Dictionary<int, int> distribuicaoPorteiros,
            HashSet<int> porteirosLimitados,
            Random random)
        {
            var porteirosSelecionados = new List<int>();

            // Obter IDs dos porteiros que estiveram nas últimas 2 escalas (exceto limitados)
            var porteirosRecentesNaoLimitados = historicoEscalas
                .SelectMany(h => h)
                .Where(id => !porteirosLimitados.Contains(id))
                .Distinct()
                .ToList();

            for (int i = 0; i < quantidadeNecessaria; i++)
            {
                // Fase 1: Tentar selecionar entre porteiros que NÃO estiveram nas últimas 2 escalas
                var candidatosFase1 = porteirosElegiveis
                    .Where(p => !porteirosSelecionados.Contains(p.Id))
                    .Where(p => !porteirosRecentesNaoLimitados.Contains(p.Id))
                    .OrderBy(p => distribuicaoPorteiros[p.Id])
                    .ThenBy(_ => random.Next())
                    .ToList();

                if (candidatosFase1.Any())
                {
                    porteirosSelecionados.Add(candidatosFase1.First().Id);
                    continue;
                }

                // Fase 2: Aceitar porteiros da penúltima escala (não da última)
                var porteirosUltimaEscala = historicoEscalas.LastOrDefault() ?? new List<int>();
                var candidatosFase2 = porteirosElegiveis
                    .Where(p => !porteirosSelecionados.Contains(p.Id))
                    .Where(p => !porteirosUltimaEscala.Contains(p.Id))
                    .OrderBy(p => distribuicaoPorteiros[p.Id])
                    .ThenBy(_ => random.Next())
                    .ToList();

                if (candidatosFase2.Any())
                {
                    porteirosSelecionados.Add(candidatosFase2.First().Id);
                    continue;
                }

                // Fase 3: Último recurso - aceitar qualquer um
                var candidatosFase3 = porteirosElegiveis
                    .Where(p => !porteirosSelecionados.Contains(p.Id))
                    .OrderBy(p => distribuicaoPorteiros[p.Id])
                    .ThenBy(_ => random.Next())
                    .ToList();

                if (candidatosFase3.Any())
                {
                    porteirosSelecionados.Add(candidatosFase3.First().Id);
                }
            }

            return porteirosSelecionados;
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
