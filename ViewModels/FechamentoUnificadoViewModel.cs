using SistemaTesourariaEclesiastica.Enums;

namespace SistemaTesourariaEclesiastica.ViewModels
{
    public class FechamentoUnificadoViewModel
    {
        // Identificação do Centro de Custo
        public int CentroCustoId { get; set; }
        public string NomeCentroCusto { get; set; } = string.Empty;
        public bool EhSede { get; set; }
        
        // Dados do Fechamento
        public int? Ano { get; set; }
        public int? Mes { get; set; }
        public DateTime DataInicio { get; set; }
        public DateTime DataFim { get; set; }
        public string? Observacoes { get; set; }
        public TipoFechamento TipoFechamento { get; set; }

        // Fechamentos das Congregações Aprovados e Disponíveis (apenas para SEDE)
        public List<FechamentoCongregacaoDisponivel> FechamentosDisponiveis { get; set; }
            = new List<FechamentoCongregacaoDisponivel>();

        // IDs dos fechamentos selecionados para incluir (apenas para SEDE)
        public List<int> FechamentosIncluidos { get; set; } = new List<int>();
    }
}
