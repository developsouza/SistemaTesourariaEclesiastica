using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace SistemaTesourariaEclesiastica.Models
{
    public class ContaBancaria
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "O nome do banco é obrigatório.")]
        [StringLength(100, ErrorMessage = "O nome do banco deve ter no máximo 100 caracteres.")]
        [Display(Name = "Banco")]
        public string Banco { get; set; } = string.Empty;

        [Required(ErrorMessage = "A agência é obrigatória.")]
        [StringLength(20, ErrorMessage = "A agência deve ter no máximo 20 caracteres.")]
        [Display(Name = "Agência")]
        public string Agencia { get; set; } = string.Empty;

        [Required(ErrorMessage = "A conta é obrigatória.")]
        [StringLength(20, ErrorMessage = "A conta deve ter no máximo 20 caracteres.")]
        [Display(Name = "Conta")]
        public string Conta { get; set; } = string.Empty;

        [Required(ErrorMessage = "O centro de custo é obrigatório.")]
        [Display(Name = "Centro de Custo")]
        public int CentroCustoId { get; set; }

        // Navigation properties
        [ValidateNever]
        public virtual CentroCusto CentroCusto { get; set; } = null!;
    }
}

