using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using SistemaTesourariaEclesiastica.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaTesourariaEclesiastica.Models
{
    /// <summary>
    /// Representa uma despesa fixa/recorrente cadastrada no sistema
    /// </summary>
    public class DespesaRecorrente
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "O nome da despesa é obrigatório.")]
        [StringLength(150, ErrorMessage = "O nome deve ter no máximo 150 caracteres.")]
        [Display(Name = "Nome da Despesa")]
        public string Nome { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "A descrição deve ter no máximo 500 caracteres.")]
        [Display(Name = "Descrição")]
        public string? Descricao { get; set; }

        [Required(ErrorMessage = "O valor é obrigatório.")]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, 999999.99, ErrorMessage = "O valor deve estar entre R$ 0,01 e R$ 999.999,99.")]
        [Display(Name = "Valor Padrão")]
        public decimal ValorPadrao { get; set; }

        [Required(ErrorMessage = "A periodicidade é obrigatória.")]
        [Display(Name = "Periodicidade")]
        public Periodicidade Periodicidade { get; set; }

        [Required(ErrorMessage = "O centro de custo é obrigatório.")]
        [Display(Name = "Centro de Custo")]
        public int CentroCustoId { get; set; }

        [Required(ErrorMessage = "A categoria da despesa é obrigatória.")]
        [Display(Name = "Categoria da Despesa")]
        public int PlanoDeContasId { get; set; }

        [Display(Name = "Fornecedor")]
        public int? FornecedorId { get; set; }

        [Display(Name = "Meio de Pagamento Padrão")]
        public int? MeioDePagamentoId { get; set; }

        [Display(Name = "Dia de Vencimento")]
        [Range(1, 31, ErrorMessage = "O dia deve estar entre 1 e 31.")]
        public int? DiaVencimento { get; set; }

        [Display(Name = "Ativa")]
        public bool Ativa { get; set; } = true;

        [Display(Name = "Data de Cadastro")]
        public DateTime DataCadastro { get; set; } = DateTime.Now;

        [Display(Name = "Data de Início")]
        [DataType(DataType.Date)]
        public DateTime? DataInicio { get; set; }

        [Display(Name = "Data de Término")]
        [DataType(DataType.Date)]
        public DateTime? DataTermino { get; set; }

        [StringLength(500, ErrorMessage = "As observações devem ter no máximo 500 caracteres.")]
        [Display(Name = "Observações")]
        public string? Observacoes { get; set; }

        // Navigation properties
        [ValidateNever]
        public virtual CentroCusto CentroCusto { get; set; } = null!;

        [ValidateNever]
        public virtual PlanoDeContas PlanoDeContas { get; set; } = null!;

        [ValidateNever]
        public virtual Fornecedor? Fornecedor { get; set; }

        [ValidateNever]
        public virtual MeioDePagamento? MeioDePagamento { get; set; }

        [ValidateNever]
        public virtual ICollection<PagamentoDespesaRecorrente> Pagamentos { get; set; } = new List<PagamentoDespesaRecorrente>();
    }
}
