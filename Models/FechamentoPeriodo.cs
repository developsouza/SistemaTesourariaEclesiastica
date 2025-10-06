using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
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

        [Display(Name = "Tipo de Fechamento")]
        public TipoFechamento TipoFechamento { get; set; } = TipoFechamento.Mensal;

        [Range(2000, 2100, ErrorMessage = "Ano deve estar entre 2000 e 2100.")]
        [Display(Name = "Ano")]
        public int? Ano { get; set; }

        [Range(1, 12, ErrorMessage = "Mês deve estar entre 1 e 12.")]
        [Display(Name = "Mês")]
        public int? Mes { get; set; }

        [Required(ErrorMessage = "A data de início é obrigatória.")]
        [Display(Name = "Data de Início")]
        [DataType(DataType.Date)]
        public DateTime DataInicio { get; set; }

        [Required(ErrorMessage = "A data de fim é obrigatória.")]
        [Display(Name = "Data de Fim")]
        [DataType(DataType.Date)]
        public DateTime DataFim { get; set; }

        [Display(Name = "Total de Entradas")]
        [Column(TypeName = "decimal(18,2)")]
        [DataType(DataType.Currency)]
        public decimal TotalEntradas { get; set; }

        [Display(Name = "Total de Saídas")]
        [Column(TypeName = "decimal(18,2)")]
        [DataType(DataType.Currency)]
        public decimal TotalSaidas { get; set; }

        [Display(Name = "Total Entradas Físicas")]
        [Column(TypeName = "decimal(18,2)")]
        [DataType(DataType.Currency)]
        public decimal TotalEntradasFisicas { get; set; }

        [Display(Name = "Total Saídas Físicas")]
        [Column(TypeName = "decimal(18,2)")]
        [DataType(DataType.Currency)]
        public decimal TotalSaidasFisicas { get; set; }

        [Display(Name = "Total Entradas Digitais")]
        [Column(TypeName = "decimal(18,2)")]
        [DataType(DataType.Currency)]
        public decimal TotalEntradasDigitais { get; set; }

        [Display(Name = "Total Saídas Digitais")]
        [Column(TypeName = "decimal(18,2)")]
        [DataType(DataType.Currency)]
        public decimal TotalSaidasDigitais { get; set; }

        [Display(Name = "Balanço Digital")]
        [Column(TypeName = "decimal(18,2)")]
        [DataType(DataType.Currency)]
        public decimal BalancoDigital { get; set; }

        [Display(Name = "Balanço Físico")]
        [Column(TypeName = "decimal(18,2)")]
        [DataType(DataType.Currency)]
        public decimal BalancoFisico { get; set; }

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

        /// <summary>
        /// Indica se este fechamento de congregação já foi incluído em um fechamento da SEDE
        /// </summary>
        [Display(Name = "Foi Processado pela Sede?")]
        public bool FoiProcessadoPelaSede { get; set; } = false;

        /// <summary>
        /// ID do fechamento da SEDE que processou este fechamento (se aplicável)
        /// </summary>
        [Display(Name = "Fechamento da Sede que Processou")]
        public int? FechamentoSedeProcessadorId { get; set; }

        /// <summary>
        /// Data em que foi processado pela SEDE
        /// </summary>
        [Display(Name = "Data de Processamento pela Sede")]
        public DateTime? DataProcessamentoPelaSede { get; set; }

        // Navigation Property
        [ValidateNever]
        public virtual FechamentoPeriodo? FechamentoSedeProcessador { get; set; }

        /// <summary>
        /// Fechamentos de congregações que foram incluídos neste fechamento da SEDE
        /// </summary>
        [ValidateNever]
        public virtual ICollection<FechamentoPeriodo> FechamentosCongregacoesIncluidos { get; set; }
            = new List<FechamentoPeriodo>();

        // Navigation properties
        [ValidateNever]
        public virtual CentroCusto CentroCusto { get; set; } = null!;
        [ValidateNever]
        public virtual ApplicationUser UsuarioSubmissao { get; set; } = null!;
        [ValidateNever]
        public virtual ApplicationUser? UsuarioAprovacao { get; set; }
        [ValidateNever]
        public virtual ICollection<ItemRateioFechamento> ItensRateio { get; set; } = new List<ItemRateioFechamento>();
        [ValidateNever]
        public virtual ICollection<DetalheFechamento> DetalhesFechamento { get; set; } = new List<DetalheFechamento>();
    }
}