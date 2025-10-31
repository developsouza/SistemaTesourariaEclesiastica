using SistemaTesourariaEclesiastica.Enums;

namespace SistemaTesourariaEclesiastica.ViewModels
{
    public class FechamentoSedeViewModel
    {
        // Dados do Fechamento da SEDE
        public int? Ano { get; set; }
        public int? Mes { get; set; }
        public DateTime DataInicio { get; set; }
        public DateTime DataFim { get; set; }
        public string? Observacoes { get; set; }
        public TipoFechamento TipoFechamento { get; set; }

        // Fechamentos das Congregações Aprovados e Disponíveis
        public List<FechamentoCongregacaoDisponivel> FechamentosDisponiveis { get; set; }
            = new List<FechamentoCongregacaoDisponivel>();

        // IDs dos fechamentos selecionados para incluir
        public List<int> FechamentosIncluidos { get; set; } = new List<int>();
    }

    public class FechamentoCongregacaoDisponivel
    {
        public int Id { get; set; }
        public string NomeCongregacao { get; set; } = string.Empty;
        public DateTime DataInicio { get; set; }
        public DateTime DataFim { get; set; }
        public decimal TotalEntradas { get; set; }
        public decimal TotalSaidas { get; set; }
        public decimal BalancoFisico { get; set; }
        public decimal BalancoDigital { get; set; }
        public DateTime DataAprovacao { get; set; }
        public bool Selecionado { get; set; }

        // ✅ NOVOS: Detalhes para análise antes do fechamento
        public List<DetalhePrestacaoContas> DetalhesEntradas { get; set; } = new List<DetalhePrestacaoContas>();
        public List<DetalhePrestacaoContas> DetalhesSaidas { get; set; } = new List<DetalhePrestacaoContas>();
    }

    // Detalhe individual de entrada ou saída para análise
    public class DetalhePrestacaoContas
    {
        public DateTime Data { get; set; }
        public string Descricao { get; set; } = string.Empty;
        public decimal Valor { get; set; }
        public string? PlanoContas { get; set; }
        public string? MeioPagamento { get; set; }
        public string? MembroOuFornecedor { get; set; }
        public string? Observacoes { get; set; }
    }
}