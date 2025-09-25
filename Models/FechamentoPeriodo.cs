using SistemaTesourariaEclesiastica.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaTesourariaEclesiastica.Models
{
    public class FechamentoPeriodo
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "O centro de custo é obrigatório.")]
        [Display(Name = "Centro de Custo")]
        public int CentroCustoId { get; set; }

        [Required(ErrorMessage = "O ano é obrigatório.")]
        [Range(2000, 2100, ErrorMessage = "Ano deve estar entre 2000 e 2100.")]
        [Display(Name = "Ano")]
        public int Ano { get; set; }

        [Required(ErrorMessage = "O mês é obrigatório.")]
        [Range(1, 12, ErrorMessage = "Mês deve estar entre 1 e 12.")]
        [Display(Name = "Mês")]
        public int Mes { get; set; }

        [Required(ErrorMessage = "A data de início é obrigatória.")]
        [Display(Name = "Data de Início")]
        [DataType(DataType.Date)]
        public DateTime DataInicio { get; set; }

        [Required(ErrorMessage = "A data de fim é obrigatória.")]
        [Display(Name = "Data de Fim")]
        [DataType(DataType.Date)]
        public DateTime DataFim { get; set; }

        [Display(Name = "Balanço Digital")]
        [Column(TypeName = "decimal(18,2)")]
        [DataType(DataType.Currency)]
        public decimal BalancoDigital { get; set; }

        [Display(Name = "Balanço Físico")]
        [Column(TypeName = "decimal(18,2)")]
        [DataType(DataType.Currency)]
        public decimal BalancoFisico { get; set; }

        [Display(Name = "Total de Entradas")]
        [Column(TypeName = "decimal(18,2)")]
        [DataType(DataType.Currency)]
        public decimal TotalEntradas { get; set; }

        [Display(Name = "Total de Saídas")]
        [Column(TypeName = "decimal(18,2)")]
        [DataType(DataType.Currency)]
        public decimal TotalSaidas { get; set; }

        [Display(Name = "Total de Rateios")]
        [Column(TypeName = "decimal(18,2)")]
        [DataType(DataType.Currency)]
        public decimal TotalRateios { get; set; }

        [Display(Name = "Saldo Final")]
        [Column(TypeName = "decimal(18,2)")]
        [DataType(DataType.Currency)]
        public decimal SaldoFinal { get; set; }

        [StringLength(500, ErrorMessage = "As observações devem ter no máximo 500 caracteres.")]
        [Display(Name = "Observações")]
        public string? Observacoes { get; set; }

        [Display(Name = "Status")]
        public StatusFechamentoPeriodo Status { get; set; } = StatusFechamentoPeriodo.Pendente;

        [Display(Name = "Data de Submissão")]
        public DateTime DataSubmissao { get; set; } = DateTime.Now;

        [Display(Name = "Data de Aprovação")]
        public DateTime? DataAprovacao { get; set; }

        [Required(ErrorMessage = "O usuário de submissão é obrigatório.")]
        public string UsuarioSubmissaoId { get; set; } = string.Empty;

        public string? UsuarioAprovacaoId { get; set; }

        // Navigation properties
        public virtual CentroCusto CentroCusto { get; set; } = null!;
        public virtual ApplicationUser UsuarioSubmissao { get; set; } = null!;
        public virtual ApplicationUser? UsuarioAprovacao { get; set; }
        public virtual ICollection<DetalheFechamento> DetalhesFechamento { get; set; } = new List<DetalheFechamento>();
        public virtual ICollection<ItemRateioFechamento> ItensRateio { get; set; } = new List<ItemRateioFechamento>();
    }
}

