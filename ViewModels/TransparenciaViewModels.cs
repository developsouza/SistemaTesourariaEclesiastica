using SistemaTesourariaEclesiastica.Models;
using System.ComponentModel.DataAnnotations;

namespace SistemaTesourariaEclesiastica.ViewModels
{
    /// <summary>
    /// ViewModel para validação de acesso à transparência
    /// </summary>
    public class TransparenciaValidacaoViewModel
    {
        [Required(ErrorMessage = "O nome completo é obrigatório.")]
        [Display(Name = "Nome Completo")]
        [StringLength(100, ErrorMessage = "O nome deve ter no máximo 100 caracteres.")]
        public string? NomeCompleto { get; set; }

        [Required(ErrorMessage = "O CPF é obrigatório.")]
        [Display(Name = "CPF")]
        [StringLength(14, ErrorMessage = "CPF inválido.")]
        public string? CPF { get; set; }

        [Required(ErrorMessage = "A data de nascimento é obrigatória.")]
        [Display(Name = "Data de Nascimento")]
        [DataType(DataType.Date)]
        public DateTime? DataNascimento { get; set; }
    }

    /// <summary>
    /// ViewModel para exibição do histórico de contribuições
    /// </summary>
    public class TransparenciaHistoricoViewModel
    {
        public string MembroNome { get; set; } = string.Empty;
        public string? MembroApelido { get; set; }
        public string? CentroCustoNome { get; set; }
        public DateTime DataCadastro { get; set; }
        public List<Entrada> Contribuicoes { get; set; } = new List<Entrada>();
        public decimal TotalContribuido { get; set; }
        public int QuantidadeContribuicoes { get; set; }
        public DateTime? UltimaContribuicao { get; set; }
    }
}
