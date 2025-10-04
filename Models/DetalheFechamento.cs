using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaTesourariaEclesiastica.Models
{
    public class DetalheFechamento
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "O fechamento de período é obrigatório.")]
        [Display(Name = "Fechamento de Período")]
        public int FechamentoPeriodoId { get; set; }

        [Required(ErrorMessage = "O tipo de movimento é obrigatório.")]
        [StringLength(20, ErrorMessage = "O tipo deve ter no máximo 20 caracteres.")]
        [Display(Name = "Tipo de Movimento")]
        public string TipoMovimento { get; set; } = string.Empty; // "Entrada" ou "Saida"

        [Required(ErrorMessage = "A descrição é obrigatória.")]
        [StringLength(250, ErrorMessage = "A descrição deve ter no máximo 250 caracteres.")]
        [Display(Name = "Descrição")]
        public string Descricao { get; set; } = string.Empty;

        [Required(ErrorMessage = "O valor é obrigatório.")]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Valor")]
        public decimal Valor { get; set; }

        [Required(ErrorMessage = "A data é obrigatória.")]
        [Display(Name = "Data")]
        [DataType(DataType.Date)]
        public DateTime Data { get; set; }

        [StringLength(100, ErrorMessage = "O plano de contas deve ter no máximo 100 caracteres.")]
        [Display(Name = "Plano de Contas")]
        public string? PlanoContas { get; set; }

        [StringLength(100, ErrorMessage = "O meio de pagamento deve ter no máximo 100 caracteres.")]
        [Display(Name = "Meio de Pagamento")]
        public string? MeioPagamento { get; set; }

        [StringLength(100, ErrorMessage = "O membro deve ter no máximo 100 caracteres.")]
        [Display(Name = "Membro")]
        public string? Membro { get; set; }

        [StringLength(100, ErrorMessage = "O fornecedor deve ter no máximo 100 caracteres.")]
        [Display(Name = "Fornecedor")]
        public string? Fornecedor { get; set; }

        [StringLength(250, ErrorMessage = "As observações devem ter no máximo 250 caracteres.")]
        [Display(Name = "Observações")]
        public string? Observacoes { get; set; }

        // Navigation property
        [ValidateNever]
        public virtual FechamentoPeriodo FechamentoPeriodo { get; set; } = null!;
    }
}
