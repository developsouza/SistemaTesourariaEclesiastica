using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaTesourariaEclesiastica.Models
{
    public class RegraRateio
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "O nome da regra é obrigatório.")]
        [StringLength(100, ErrorMessage = "O nome deve ter no máximo 100 caracteres.")]
        [Display(Name = "Nome da Regra")]
        public string Nome { get; set; } = string.Empty;

        [StringLength(250, ErrorMessage = "A descrição deve ter no máximo 250 caracteres.")]
        [Display(Name = "Descrição")]
        public string? Descricao { get; set; }

        [Required(ErrorMessage = "O centro de custo origem é obrigatório.")]
        [Display(Name = "Centro de Custo Origem")]
        public int CentroCustoOrigemId { get; set; }

        [Required(ErrorMessage = "O centro de custo destino é obrigatório.")]
        [Display(Name = "Centro de Custo Destino")]
        public int CentroCustoDestinoId { get; set; }

        [Required(ErrorMessage = "O percentual é obrigatório.")]
        [Range(0.01, 100.00, ErrorMessage = "O percentual deve estar entre 0,01% e 100%.")]
        [Column(TypeName = "decimal(5,2)")]
        [Display(Name = "Percentual (%)")]
        public decimal Percentual { get; set; }

        [Display(Name = "Ativo")]
        public bool Ativo { get; set; } = true;

        [Display(Name = "Data de Criação")]
        public DateTime DataCriacao { get; set; } = DateTime.Now;

        // Navigation properties
        public virtual CentroCusto CentroCustoOrigem { get; set; } = null!;
        public virtual CentroCusto CentroCustoDestino { get; set; } = null!;
        public virtual ICollection<ItemRateioFechamento> ItensRateio { get; set; } = new List<ItemRateioFechamento>();
    }
}
