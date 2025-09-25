using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaTesourariaEclesiastica.Models
{
    public class TransferenciaInterna
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "A data é obrigatória.")]
        [Display(Name = "Data")]
        [DataType(DataType.Date)]
        public DateTime Data { get; set; }

        [Required(ErrorMessage = "O valor é obrigatório.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "O valor deve ser maior que zero.")]
        [Display(Name = "Valor")]
        [Column(TypeName = "decimal(18,2)")]
        [DataType(DataType.Currency)]
        public decimal Valor { get; set; }

        [StringLength(250, ErrorMessage = "A descrição deve ter no máximo 250 caracteres.")]
        [Display(Name = "Descrição")]
        public string? Descricao { get; set; }

        [Required(ErrorMessage = "O meio de pagamento de origem é obrigatório.")]
        [Display(Name = "Meio de Pagamento Origem")]
        public int MeioDePagamentoOrigemId { get; set; }

        [Required(ErrorMessage = "O meio de pagamento de destino é obrigatório.")]
        [Display(Name = "Meio de Pagamento Destino")]
        public int MeioDePagamentoDestinoId { get; set; }

        [Required(ErrorMessage = "O centro de custo de origem é obrigatório.")]
        [Display(Name = "Centro de Custo Origem")]
        public int CentroCustoOrigemId { get; set; }

        [Required(ErrorMessage = "O centro de custo de destino é obrigatório.")]
        [Display(Name = "Centro de Custo Destino")]
        public int CentroCustoDestinoId { get; set; }

        [Display(Name = "Quitada")]
        public bool Quitada { get; set; } = false;

        [Required(ErrorMessage = "O usuário é obrigatório.")]
        public string UsuarioId { get; set; } = string.Empty;

        [Display(Name = "Data de Criação")]
        public DateTime DataCriacao { get; set; } = DateTime.Now;

        // Navigation properties
        public virtual MeioDePagamento MeioDePagamentoOrigem { get; set; } = null!;
        public virtual MeioDePagamento MeioDePagamentoDestino { get; set; } = null!;
        public virtual CentroCusto CentroCustoOrigem { get; set; } = null!;
        public virtual CentroCusto CentroCustoDestino { get; set; } = null!;
        public virtual ApplicationUser Usuario { get; set; } = null!;
    }
}

