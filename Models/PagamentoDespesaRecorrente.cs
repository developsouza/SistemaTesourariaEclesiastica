using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaTesourariaEclesiastica.Models
{
    /// <summary>
    /// Representa o controle de pagamento individual de uma despesa recorrente
    /// </summary>
    public class PagamentoDespesaRecorrente
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "A despesa recorrente é obrigatória.")]
        [Display(Name = "Despesa Recorrente")]
        public int DespesaRecorrenteId { get; set; }

        [Required(ErrorMessage = "A data de vencimento é obrigatória.")]
        [Display(Name = "Data de Vencimento")]
        [DataType(DataType.Date)]
        public DateTime DataVencimento { get; set; }

        [Display(Name = "Data de Pagamento")]
        [DataType(DataType.Date)]
        public DateTime? DataPagamento { get; set; }

        [Required(ErrorMessage = "O valor previsto é obrigatório.")]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Valor Previsto")]
        public decimal ValorPrevisto { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Valor Pago")]
        public decimal? ValorPago { get; set; }

        [Display(Name = "Pago?")]
        public bool Pago { get; set; } = false;

        [Display(Name = "Saída Gerada?")]
        public bool SaidaGerada { get; set; } = false;

        [Display(Name = "ID da Saída Gerada")]
        public int? SaidaId { get; set; }

        [StringLength(500, ErrorMessage = "As observações devem ter no máximo 500 caracteres.")]
        [Display(Name = "Observações do Pagamento")]
        public string? Observacoes { get; set; }

        [Display(Name = "Data de Registro")]
        public DateTime DataRegistro { get; set; } = DateTime.Now;

        // Navigation properties
        [ValidateNever]
        public virtual DespesaRecorrente DespesaRecorrente { get; set; } = null!;

        [ValidateNever]
        public virtual Saida? Saida { get; set; }

        // Propriedades computadas
        [NotMapped]
        [Display(Name = "Status")]
        public string Status
        {
            get
            {
                if (Pago)
                    return "Pago";
                if (DataVencimento < DateTime.Today)
                    return "Atrasado";
                if (DataVencimento == DateTime.Today)
                    return "Vence Hoje";
                return "Pendente";
            }
        }

        [NotMapped]
        [Display(Name = "Dias de Atraso")]
        public int? DiasAtraso
        {
            get
            {
                if (!Pago && DataVencimento < DateTime.Today)
                    return (DateTime.Today - DataVencimento).Days;
                return null;
            }
        }
    }
}
