using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using SistemaTesourariaEclesiastica.Enums;
using System.ComponentModel.DataAnnotations;

namespace SistemaTesourariaEclesiastica.Models
{
    public class EscalaPorteiro
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "A data do culto é obrigatória.")]
        [DataType(DataType.Date)]
        [Display(Name = "Data do Culto")]
        public DateTime DataCulto { get; set; }

        [Display(Name = "Horário")]
        [DataType(DataType.Time)]
        public TimeSpan? Horario { get; set; }

        [Required(ErrorMessage = "O tipo de culto é obrigatório.")]
        [Display(Name = "Tipo de Culto")]
        public TipoCulto TipoCulto { get; set; }

        [Required(ErrorMessage = "O porteiro é obrigatório.")]
        [Display(Name = "Porteiro")]
        public int PorteiroId { get; set; }

        [Display(Name = "Porteiro 2")]
        public int? Porteiro2Id { get; set; }

        [Required(ErrorMessage = "O responsável é obrigatório.")]
        [Display(Name = "Responsável")]
        public int ResponsavelId { get; set; }

        [Display(Name = "Data de Geração")]
        public DateTime DataGeracao { get; set; } = DateTime.Now;

        [StringLength(200, ErrorMessage = "A observação deve ter no máximo 200 caracteres.")]
        [Display(Name = "Observação")]
        public string? Observacao { get; set; }

        [Display(Name = "Usuário que Gerou")]
        public string? UsuarioGeracaoId { get; set; }

        // Navigation properties
        [ValidateNever]
        public virtual Porteiro Porteiro { get; set; } = null!;

        [ValidateNever]
        public virtual Porteiro? Porteiro2 { get; set; }

        [ValidateNever]
        public virtual ResponsavelPorteiro Responsavel { get; set; } = null!;

        [ValidateNever]
        public virtual ApplicationUser? UsuarioGeracao { get; set; }
    }
}
