using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaTesourariaEclesiastica.Models
{
    public class ItemRateioFechamento
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "O fechamento de período é obrigatório.")]
        [Display(Name = "Fechamento de Período")]
        public int FechamentoPeriodoId { get; set; }

        [Required(ErrorMessage = "A regra de rateio é obrigatória.")]
        [Display(Name = "Regra de Rateio")]
        public int RegraRateioId { get; set; }

        [Required(ErrorMessage = "O valor base é obrigatório.")]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Valor Base")]
        public decimal ValorBase { get; set; }

        [Required(ErrorMessage = "O percentual é obrigatório.")]
        [Column(TypeName = "decimal(5,2)")]
        [Display(Name = "Percentual (%)")]
        public decimal Percentual { get; set; }

        [Required(ErrorMessage = "O valor do rateio é obrigatório.")]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Valor do Rateio")]
        public decimal ValorRateio { get; set; }

        [StringLength(250, ErrorMessage = "As observações devem ter no máximo 250 caracteres.")]
        [Display(Name = "Observações")]
        public string? Observacoes { get; set; }

        [Display(Name = "Data de Criação")]
        public DateTime DataCriacao { get; set; } = DateTime.Now;

        // Navigation properties
        public virtual FechamentoPeriodo FechamentoPeriodo { get; set; } = null!;
        public virtual RegraRateio RegraRateio { get; set; } = null!;
    }
}
