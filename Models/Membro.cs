using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using SistemaTesourariaEclesiastica.Attributes;
using System.ComponentModel.DataAnnotations;
using SistemaTesourariaEclesiastica.Helpers;

namespace SistemaTesourariaEclesiastica.Models
{
    public class Membro
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "O nome completo do membro é obrigatório.")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "O nome completo deve ter entre 3 e 100 caracteres.")]
        [Display(Name = "Nome Completo")]
        public string NomeCompleto { get; set; } = string.Empty;

        [StringLength(50, ErrorMessage = "O apelido deve ter no máximo 50 caracteres.")]
        [Display(Name = "Apelido")]
        public string? Apelido { get; set; }

        [Required(ErrorMessage = "O CPF é obrigatório.")]
        [CpfValidation(ErrorMessage = "CPF inválido.")]
        [Display(Name = "CPF")]
        public string CPF { get; set; } = string.Empty;

        [Display(Name = "Data de Nascimento")]
        [DataType(DataType.Date)]
        public DateTime? DataNascimento { get; set; }

        [Required(ErrorMessage = "O centro de custo é obrigatório.")]
        [Display(Name = "Centro de Custo")]
        public int CentroCustoId { get; set; }

        [Display(Name = "Data de Cadastro")]
        public DateTime DataCadastro { get; set; } = DateTimeHelper.Now;

        [Display(Name = "Ativo")]
        public bool Ativo { get; set; } = true;

        // Propriedade para compatibilidade
        [Display(Name = "Nome")]
        public string Nome => NomeCompleto;

        // Navigation properties - ADICIONAR [ValidateNever]
        [ValidateNever]
        public virtual CentroCusto CentroCusto { get; set; } = null!;

        [ValidateNever]
        public virtual ICollection<Entrada> Entradas { get; set; } = new List<Entrada>();
    }
}