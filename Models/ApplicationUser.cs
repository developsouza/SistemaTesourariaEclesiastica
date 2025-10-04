using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace SistemaTesourariaEclesiastica.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required(ErrorMessage = "O nome completo é obrigatório.")]
        [StringLength(100, ErrorMessage = "O nome completo deve ter no máximo 100 caracteres.")]
        [Display(Name = "Nome Completo")]
        public string NomeCompleto { get; set; } = string.Empty;

        [Display(Name = "Centro de Custo")]
        public int? CentroCustoId { get; set; }

        [Display(Name = "Ativo")]
        public bool Ativo { get; set; } = true;

        [Display(Name = "Data de Criação")]
        public DateTime DataCriacao { get; set; } = DateTime.Now;

        [Display(Name = "Último Acesso")]
        public DateTime? UltimoAcesso { get; set; }

        // Navigation property
        [ValidateNever]
        public virtual CentroCusto? CentroCusto { get; set; }
    }
}