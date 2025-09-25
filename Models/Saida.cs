using SistemaTesourariaEclesiastica.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SistemaTesourariaEclesiastica.Attributes;

namespace SistemaTesourariaEclesiastica.Models
{
    public class Saida
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "A data é obrigatória.")]
        [Display(Name = "Data")]
        [DataType(DataType.Date)]
        [DataValidation(PermitirDataFutura = true, DiasMaximoFuturo = 30, DiasMaximoPassado = 730, ErrorMessage = "Data deve estar entre 2 anos no passado e 30 dias no futuro.")]
        public DateTime Data { get; set; }

        [Required(ErrorMessage = "O valor é obrigatório.")]
        [Display(Name = "Valor")]
        [Column(TypeName = "decimal(18,2)")]
        [DataType(DataType.Currency)]
        [ValorMonetarioValidation(ValorMinimo = 0.01, ValorMaximo = 999999.99, ErrorMessage = "Valor deve estar entre R$ 0,01 e R$ 999.999,99.")]
        public decimal Valor { get; set; }

        [Required(ErrorMessage = "A descrição é obrigatória.")]
        [StringLength(250, ErrorMessage = "A descrição deve ter no máximo 250 caracteres.")]
        [Display(Name = "Descrição")]
        public string Descricao { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "As observações devem ter no máximo 500 caracteres.")]
        [Display(Name = "Observações")]
        public string? Observacoes { get; set; }

        [Required(ErrorMessage = "O meio de pagamento é obrigatório.")]
        [Display(Name = "Meio de Pagamento")]
        public int MeioDePagamentoId { get; set; }

        [Required(ErrorMessage = "O centro de custo é obrigatório.")]
        [Display(Name = "Centro de Custo")]
        public int CentroCustoId { get; set; }

        [Required(ErrorMessage = "A categoria da despesa é obrigatória.")]
        [Display(Name = "Categoria da Despesa")]
        public int PlanoDeContasId { get; set; }

        [Display(Name = "Fornecedor")]
        public int? FornecedorId { get; set; }

        [Required(ErrorMessage = "O tipo de despesa é obrigatório.")]
        [Display(Name = "Tipo de Despesa")]
        public TipoDespesa TipoDespesa { get; set; }

        [StringLength(50, ErrorMessage = "O número do documento deve ter no máximo 50 caracteres.")]
        [Display(Name = "Número do Documento")]
        public string? NumeroDocumento { get; set; }

        [Display(Name = "Data de Vencimento")]
        [DataType(DataType.Date)]
        public DateTime? DataVencimento { get; set; }

        [Display(Name = "Comprovante")]
        public string? ComprovanteUrl { get; set; }

        [Required(ErrorMessage = "O usuário é obrigatório.")]
        public string UsuarioId { get; set; } = string.Empty;

        [Display(Name = "Data de Criação")]
        public DateTime DataCriacao { get; set; } = DateTime.Now;

        // Navigation properties
        public virtual MeioDePagamento MeioDePagamento { get; set; } = null!;
        public virtual CentroCusto CentroCusto { get; set; } = null!;
        public virtual PlanoDeContas PlanoDeContas { get; set; } = null!;
        public virtual Fornecedor? Fornecedor { get; set; }
        public virtual ApplicationUser Usuario { get; set; } = null!;
    }
}

