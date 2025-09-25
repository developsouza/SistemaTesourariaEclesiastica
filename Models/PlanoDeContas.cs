using SistemaTesourariaEclesiastica.Enums;
using System.ComponentModel.DataAnnotations;

namespace SistemaTesourariaEclesiastica.Models
{
    public class PlanoDeContas
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "O código é obrigatório.")]
        [StringLength(20, ErrorMessage = "O código deve ter no máximo 20 caracteres.")]
        [Display(Name = "Código")]
        public string Codigo { get; set; } = string.Empty;

        [Required(ErrorMessage = "O nome é obrigatório.")]
        [StringLength(100, ErrorMessage = "O nome deve ter no máximo 100 caracteres.")]
        [Display(Name = "Nome")]
        public string Nome { get; set; } = string.Empty;

        [Required(ErrorMessage = "A descrição é obrigatória.")]
        [StringLength(100, ErrorMessage = "A descrição deve ter no máximo 100 caracteres.")]
        [Display(Name = "Descrição")]
        public string Descricao { get; set; } = string.Empty;

        [Required(ErrorMessage = "O tipo é obrigatório.")]
        [Display(Name = "Tipo")]
        public TipoPlanoContas Tipo { get; set; }

        [Display(Name = "Ativo")]
        public bool Ativo { get; set; } = true;

        [Display(Name = "Data de Criação")]
        public DateTime DataCriacao { get; set; } = DateTime.Now;

        // Navigation properties
        public virtual ICollection<Entrada> Entradas { get; set; } = new List<Entrada>();
        public virtual ICollection<Saida> Saidas { get; set; } = new List<Saida>();
    }
}

