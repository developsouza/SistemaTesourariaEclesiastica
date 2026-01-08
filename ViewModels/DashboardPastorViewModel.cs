namespace SistemaTesourariaEclesiastica.ViewModels
{
    /// <summary>
    /// ViewModel principal para o Dashboard do Pastor
    /// Contém visão consolidada de todas as congregações
    /// </summary>
    public class DashboardPastorViewModel
    {
        public string NomePastor { get; set; } = string.Empty;
        public DateTime DataReferencia { get; set; }
        public string PeriodoReferencia { get; set; } = string.Empty;

        // Totais gerais
        public decimal TotalReceitasGeral { get; set; }
        public decimal TotalDespesasGeral { get; set; }
        public decimal SaldoGeralAtual { get; set; }
        public decimal TotalRateiosEnviados { get; set; }

        // Indicadores do mês atual
        public decimal ReceitasMesAtual { get; set; }
        public decimal DespesasMesAtual { get; set; }
        public decimal SaldoMesAtual { get; set; }
        public int QuantidadeCongregacoes { get; set; }

        // Lista de congregações com seus indicadores
        public List<IndicadoresCongregacaoViewModel> Congregacoes { get; set; } = new();

        // Maiores despesas consolidadas
        public List<MaiorDespesaViewModel> MaioresDespesas { get; set; } = new();

        // Ranking de congregações
        public List<RankingCongregacaoViewModel> RankingReceitas { get; set; } = new();
        public List<RankingCongregacaoViewModel> RankingDespesas { get; set; } = new();

        // Tendências (últimos 6 meses)
        public List<TendenciaMensalViewModel> TendenciasReceitas { get; set; } = new();
        public List<TendenciaMensalViewModel> TendenciasDespesas { get; set; } = new();

        // Alertas e observações
        public List<AlertaDashboardViewModel> Alertas { get; set; } = new();
    }

    /// <summary>
    /// Indicadores financeiros por congregação
    /// </summary>
    public class IndicadoresCongregacaoViewModel
    {
        public int CentroCustoId { get; set; }
        public string NomeCongregacao { get; set; } = string.Empty;
        public string TipoCongregacao { get; set; } = string.Empty;

        // Receitas
        public decimal ReceitasAcumuladas { get; set; }
        public decimal ReceitasMesAtual { get; set; }
        public decimal PercentualReceitaTotal { get; set; }

        // Despesas
        public decimal DespesasAcumuladas { get; set; }
        public decimal DespesasMesAtual { get; set; }
        public decimal PercentualDespesaTotal { get; set; }

        // Saldo e Rentabilidade
        public decimal SaldoAtual { get; set; }
        public decimal PercentualLucro { get; set; }

        // Rateios
        public decimal RateiosEnviados { get; set; }
        public decimal RateiosRecebidos { get; set; }

        // Status e saúde financeira
        public string StatusSaude { get; set; } = string.Empty; // Ótimo, Bom, Regular, Crítico
        public string CorIndicador { get; set; } = string.Empty; // success, warning, danger
        public string IconeStatus { get; set; } = string.Empty;

        // Crescimento
        public decimal CrescimentoReceitas { get; set; }
        public decimal CrescimentoDespesas { get; set; }
    }

    /// <summary>
    /// Maiores despesas consolidadas
    /// </summary>
    public class MaiorDespesaViewModel
    {
        public string CategoriaDespesa { get; set; } = string.Empty;
        public decimal ValorTotal { get; set; }
        public int QuantidadeOcorrencias { get; set; }
        public List<string> CongregacoesEnvolvidas { get; set; } = new();
    }

    /// <summary>
    /// Ranking de congregações por diferentes critérios
    /// </summary>
    public class RankingCongregacaoViewModel
    {
        public int Posicao { get; set; }
        public string NomeCongregacao { get; set; } = string.Empty;
        public decimal Valor { get; set; }
        public string CorBarra { get; set; } = string.Empty;
    }

    /// <summary>
    /// Tendência mensal (últimos 6 meses)
    /// </summary>
    public class TendenciaMensalViewModel
    {
        public string MesAno { get; set; } = string.Empty;
        public decimal TotalReceitas { get; set; }
        public decimal TotalDespesas { get; set; }
        public decimal Saldo { get; set; }
    }

    /// <summary>
    /// Alertas e observações para o pastor
    /// </summary>
    public class AlertaDashboardViewModel
    {
        public string Tipo { get; set; } = string.Empty; // info, warning, danger, success
        public string Icone { get; set; } = string.Empty;
        public string Titulo { get; set; } = string.Empty;
        public string Mensagem { get; set; } = string.Empty;
        public string CongregacaoRelacionada { get; set; } = string.Empty;
        public DateTime? DataReferencia { get; set; }
    }
}
