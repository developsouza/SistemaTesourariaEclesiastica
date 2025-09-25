using System.ComponentModel.DataAnnotations;

namespace SistemaTesourariaEclesiastica.Models
{
    public class MeioDePagamento
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "O nome do meio de pagamento é obrigatório.")]
        [StringLength(100, ErrorMessage = "O nome deve ter no máximo 100 caracteres.")]
        [Display(Name = "Nome")]
        public string Nome { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "A descrição deve ter no máximo 500 caracteres.")]
        [Display(Name = "Descrição")]
        public string? Descricao { get; set; }

        [Display(Name = "Ativo")]
        public bool Ativo { get; set; } = true;

        // Navigation properties
        public virtual ICollection<Entrada> Entradas { get; set; } = new List<Entrada>();
        public virtual ICollection<Saida> Saidas { get; set; } = new List<Saida>();
        public virtual ICollection<TransferenciaInterna> TransferenciasOrigem { get; set; } = new List<TransferenciaInterna>();
        public virtual ICollection<TransferenciaInterna> TransferenciasDestino { get; set; } = new List<TransferenciaInterna>();
    }
}

