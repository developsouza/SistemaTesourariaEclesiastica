using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace SistemaTesourariaEclesiastica.Models
{
    public class LogAuditoria
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "O usuário é obrigatório.")]
        public string UsuarioId { get; set; } = string.Empty;

        [Required(ErrorMessage = "A ação é obrigatória.")]
        [StringLength(100, ErrorMessage = "A ação deve ter no máximo 100 caracteres.")]
        [Display(Name = "Ação")]
        public string Acao { get; set; } = string.Empty;

        [Required(ErrorMessage = "A entidade é obrigatória.")]
        [StringLength(100, ErrorMessage = "A entidade deve ter no máximo 100 caracteres.")]
        [Display(Name = "Entidade")]
        public string Entidade { get; set; } = string.Empty;

        [Required(ErrorMessage = "O ID da entidade é obrigatório.")]
        [StringLength(50, ErrorMessage = "O ID da entidade deve ter no máximo 50 caracteres.")]
        [Display(Name = "ID da Entidade")]
        public string EntidadeId { get; set; } = string.Empty;

        [Required(ErrorMessage = "A data e hora são obrigatórias.")]
        [Display(Name = "Data e Hora")]
        public DateTime DataHora { get; set; } = DateTime.Now;

        [Display(Name = "Detalhes")]
        public string? Detalhes { get; set; }

        [StringLength(45, ErrorMessage = "O endereço IP deve ter no máximo 45 caracteres.")]
        [Display(Name = "Endereço IP")]
        public string? EnderecoIP { get; set; }

        [StringLength(500, ErrorMessage = "O User Agent deve ter no máximo 500 caracteres.")]
        [Display(Name = "User Agent")]
        public string? UserAgent { get; set; }

        // Navigation properties
        [ValidateNever]
        public virtual ApplicationUser Usuario { get; set; } = null!;
    }
}

