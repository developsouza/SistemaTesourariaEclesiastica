using SistemaTesourariaEclesiastica.Enums;

namespace SistemaTesourariaEclesiastica.ViewModels
{
    /// <summary>
    /// ViewModel para relatório completo de consistência do sistema
    /// </summary>
    public class RelatorioConsistenciaViewModel
    {
        public DateTime DataExecucao { get; set; }
        public int TotalInconsistencias { get; set; }
        public int InconsistenciasCriticas { get; set; }
        public int InconsistenciasAvisos { get; set; }
        public int InconsistenciasInformacoes { get; set; }
        public List<InconsistenciaViewModel> Inconsistencias { get; set; } = new List<InconsistenciaViewModel>();

        /// <summary>
        /// Retorna resumo agrupado por tipo
        /// </summary>
        public Dictionary<string, int> ResumoPorTipo
        {
            get
            {
                return Inconsistencias
                    .GroupBy(i => i.Tipo)
                    .ToDictionary(g => g.Key, g => g.Count());
            }
        }

        /// <summary>
        /// Retorna resumo agrupado por categoria
        /// </summary>
        public Dictionary<string, int> ResumoPorCategoria
        {
            get
            {
                return Inconsistencias
                    .GroupBy(i => i.Categoria)
                    .ToDictionary(g => g.Key, g => g.Count());
            }
        }

        /// <summary>
        /// Indica se há inconsistências críticas
        /// </summary>
        public bool TemInconsistenciasCriticas => InconsistenciasCriticas > 0;

        /// <summary>
        /// Percentual de saúde do sistema (0-100)
        /// </summary>
        public double PercentualSaude
        {
            get
            {
                if (TotalInconsistencias == 0) return 100;

                // Peso: Críticas -10, Avisos -3, Informações -1
                var pontuacaoNegativa = (InconsistenciasCriticas * 10) + (InconsistenciasAvisos * 3) + InconsistenciasInformacoes;
                var saude = Math.Max(0, 100 - pontuacaoNegativa);
                return saude;
            }
        }

        /// <summary>
        /// Classificação da saúde do sistema
        /// </summary>
        public string ClassificacaoSaude
        {
            get
            {
                if (PercentualSaude >= 90) return "Excelente";
                if (PercentualSaude >= 70) return "Bom";
                if (PercentualSaude >= 50) return "Regular";
                if (PercentualSaude >= 30) return "Ruim";
                return "Crítico";
            }
        }

        /// <summary>
        /// Cor do badge de saúde
        /// </summary>
        public string CorSaude
        {
            get
            {
                if (PercentualSaude >= 90) return "success";
                if (PercentualSaude >= 70) return "info";
                if (PercentualSaude >= 50) return "warning";
                return "danger";
            }
        }
    }

    /// <summary>
    /// ViewModel para uma inconsistência individual encontrada
    /// </summary>
    public class InconsistenciaViewModel
    {
        /// <summary>
        /// Tipo da inconsistência (ex: "Lançamento Duplicado", "Total Incorreto")
        /// </summary>
        public string Tipo { get; set; } = string.Empty;

        /// <summary>
        /// Categoria (ex: "Entradas", "Saídas", "Fechamentos")
        /// </summary>
        public string Categoria { get; set; } = string.Empty;

        /// <summary>
        /// Descrição detalhada da inconsistência
        /// </summary>
        public string Descricao { get; set; } = string.Empty;

        /// <summary>
        /// Severidade da inconsistência
        /// </summary>
        public SeveridadeInconsistencia Severidade { get; set; }

        /// <summary>
        /// ID da entidade afetada (quando aplicável)
        /// </summary>
        public string? EntidadeId { get; set; }

        /// <summary>
        /// Tipo da entidade afetada (ex: "Entrada", "Saida", "FechamentoPeriodo")
        /// </summary>
        public string? EntidadeTipo { get; set; }

        /// <summary>
        /// Sugestão de ação corretiva
        /// </summary>
        public string? AcaoCorretivaSugerida { get; set; }

        /// <summary>
        /// Ícone do Bootstrap para o tipo de inconsistência
        /// </summary>
        public string Icone
        {
            get
            {
                return Tipo switch
                {
                    "Lançamento Duplicado" => "bi-copy",
                    "Total Incorreto em Fechamento" => "bi-calculator",
                    "Divergência Caixa Físico/Digital" => "bi-cash-coin",
                    "Saldo Final Incorreto" => "bi-exclamation-triangle",
                    "Referência Inválida" => "bi-link-45deg",
                    "Lançamento Órfão" => "bi-question-circle",
                    "Transferência Inválida" => "bi-arrow-left-right",
                    "Rateio Incorreto" => "bi-pie-chart",
                    "Saldo Negativo" => "bi-dash-circle",
                    "Tipo de Caixa Incoerente" => "bi-cash-stack",
                    "Data Futura Excessiva" => "bi-calendar-x",
                    "Valor Inválido" => "bi-currency-dollar",
                    _ => "bi-exclamation-circle"
                };
            }
        }

        /// <summary>
        /// Cor do badge de severidade
        /// </summary>
        public string CorSeveridade
        {
            get
            {
                return Severidade switch
                {
                    SeveridadeInconsistencia.Critica => "danger",
                    SeveridadeInconsistencia.Aviso => "warning",
                    SeveridadeInconsistencia.Informacao => "info",
                    _ => "secondary"
                };
            }
        }

        /// <summary>
        /// Texto da severidade
        /// </summary>
        public string TextoSeveridade
        {
            get
            {
                return Severidade switch
                {
                    SeveridadeInconsistencia.Critica => "CRÍTICO",
                    SeveridadeInconsistencia.Aviso => "AVISO",
                    SeveridadeInconsistencia.Informacao => "INFO",
                    _ => "DESCONHECIDO"
                };
            }
        }
    }
}
