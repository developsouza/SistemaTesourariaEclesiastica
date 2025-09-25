using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaTesourariaEclesiastica.Models
{
    public class ModeloRateioEntrada
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "O nome do modelo de rateio é obrigatório.")]
        [StringLength(100, ErrorMessage = "O nome deve ter no máximo 100 caracteres.")]
        [Display(Name = "Nome")]
        public string Nome { get; set; } = string.Empty;

        [Required(ErrorMessage = "O percentual da sede é obrigatório.")]
        [Range(0, 100, ErrorMessage = "O percentual deve ser entre 0 e 100.")]
        [Display(Name = "Percentual Sede (%)")]
        [Column(TypeName = "decimal(5,2)")]
        public decimal PercentualSede { get; set; }

        [Required(ErrorMessage = "O percentual da congregação é obrigatório.")]
        [Range(0, 100, ErrorMessage = "O percentual deve ser entre 0 e 100.")]
        [Display(Name = "Percentual Congregação (%)")]
        [Column(TypeName = "decimal(5,2)")]
        public decimal PercentualCongregacao { get; set; }

        // Navigation properties
        public virtual ICollection<Entrada> Entradas { get; set; } = new List<Entrada>();
    }
}

