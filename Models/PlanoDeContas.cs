using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using SistemaTesourariaEclesiastica.Enums;
using System.ComponentModel.DataAnnotations;
using SistemaTesourariaEclesiastica.Helpers;

namespace SistemaTesourariaEclesiastica.Models
{
    public class PlanoDeContas
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "O nome é obrigatório.")]
        [StringLength(100, ErrorMessage = "O nome deve ter no máximo 100 caracteres.")]
        [Display(Name = "Nome")]
        public string Nome { get; set; } = string.Empty;

        [Required(ErrorMessage = "A descrição é obrigatória.")]
        [StringLength(250, ErrorMessage = "A descrição deve ter no máximo 250 caracteres.")]
        [Display(Name = "Descrição")]
        public string Descricao { get; set; } = string.Empty;

        [Required(ErrorMessage = "O tipo é obrigatório.")]
        [Display(Name = "Tipo")]
        public TipoPlanoContas Tipo { get; set; }

        [Display(Name = "Ativo")]
        public bool Ativo { get; set; } = true;

        [Display(Name = "Data de Criação")]
        public DateTime DataCriacao { get; set; } = DateTimeHelper.Now; // ✅ ALTERADO

        // Navigation properties
        [ValidateNever]
        public virtual ICollection<Entrada> Entradas { get; set; } = new List<Entrada>();
        [ValidateNever]
        public virtual ICollection<Saida> Saidas { get; set; } = new List<Saida>();
    }
}