using Microsoft.EntityFrameworkCore;
using SistemaTesourariaEclesiastica.Data;
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

            // Ordenar dias por data
            var diasOrdenados = diasSelecionados.OrderBy(d => d.Data).ToList();

            // Buscar as últimas escalas para fazer distribuição justa
            var ultimasEscalas = await _context.EscalasPorteiros
                .Where(e => e.DataCulto < diasOrdenados.First().Data)
                .OrderByDescending(e => e.DataCulto)
                .Take(10)
                .Include(e => e.Porteiro)
                .ToListAsync();

            // Criar lista para controlar distribuição
            var distribuicaoPorteiros = porteiros.ToDictionary(
                p => p.Id,
                p => ultimasEscalas.Count(e => e.PorteiroId == p.Id)
            );

            var escalasGeradas = new List<EscalaPorteiro>();
            var random = new Random();

            // Selecionar um responsável (pode ser rotativo ou fixo)
            // Aqui vamos usar sempre o primeiro responsável, mas pode ser melhorado
            var responsavelSelecionado = responsaveis.First();

            foreach (var dia in diasOrdenados)
            {
                // Verificar se já existe escala para este dia
                var escalaExistente = await _context.EscalasPorteiros
                    .AnyAsync(e => e.DataCulto.Date == dia.Data.Date);

                if (escalaExistente)
                {
                    _logger.LogWarning($"Já existe escala para o dia {dia.Data:dd/MM/yyyy}. Pulando...");
                    continue;
                }

                // Selecionar porteiro com menor número de escalas (distribuição justa)
                var porteiroSelecionadoId = distribuicaoPorteiros
                    .OrderBy(d => d.Value)
                    .ThenBy(_ => random.Next()) // Adiciona aleatoriedade em caso de empate
                    .First()
                    .Key;

                var porteiro = porteiros.First(p => p.Id == porteiroSelecionadoId);

                // Criar a escala
                var escala = new EscalaPorteiro
                {
                    DataCulto = dia.Data,
                    TipoCulto = dia.TipoCulto,
                    PorteiroId = porteiro.Id,
                    ResponsavelId = responsavelSelecionado.Id,
                    Observacao = dia.Observacao,
                    DataGeracao = DateTime.Now,
                    UsuarioGeracaoId = usuarioId
                };

                escalasGeradas.Add(escala);

                // Atualizar distribuição
                distribuicaoPorteiros[porteiroSelecionadoId]++;
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
                        TipoCulto = config.TipoCulto,
                        Observacao = config.Observacao ?? $"Evento Especial - {config.TipoCulto}"
                    });
                }
            }

            // Adicionar cultos semanais
            if (configuracoesSemanais.Any())
            {
                // Iterar por todos os dias no período
                for (var data = dataInicio; data <= dataFim; data = data.AddDays(1))
                {
                    // Verificar se não há data específica para este dia
                    var temDataEspecifica = diasSugeridos.Any(d => d.Data.Date == data.Date);

                    if (!temDataEspecifica)
                    {
                        var configDia = configuracoesSemanais.FirstOrDefault(c => c.DiaSemana == data.DayOfWeek);

                        if (configDia != null)
                        {
                            diasSugeridos.Add(new DiasCultoViewModel
                            {
                                Data = data,
                                TipoCulto = configDia.TipoCulto,
                                Observacao = configDia.Observacao
                            });
                        }
                    }
                }
            }

            // Ordenar por data
            return diasSugeridos.OrderBy(d => d.Data).ToList();
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
                    TipoCulto = Enums.TipoCulto.Evangelistico,
                    Ativo = true
                },
                new ConfiguracaoCulto
                {
                    DiaSemana = DayOfWeek.Wednesday,
                    TipoCulto = Enums.TipoCulto.DaFamilia,
                    Ativo = true
                },
                new ConfiguracaoCulto
                {
                    DiaSemana = DayOfWeek.Friday,
                    TipoCulto = Enums.TipoCulto.DeDoutrina,
                    Ativo = true
                }
            };
        }
    }
}
