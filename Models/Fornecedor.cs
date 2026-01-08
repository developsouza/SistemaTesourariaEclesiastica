using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using SistemaTesourariaEclesiastica.Helpers;

namespace SistemaTesourariaEclesiastica.Models
{
    public class Fornecedor
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "O nome do fornecedor é obrigatório.")]
        [StringLength(100, ErrorMessage = "O nome deve ter no máximo 100 caracteres.")]
        [Display(Name = "Nome")]
        public string Nome { get; set; } = string.Empty;

        [StringLength(18, ErrorMessage = "O CNPJ deve ter no máximo 18 caracteres.")]
        [RegularExpression(@"^\d{2}\.\d{3}\.\d{3}\/\d{4}\-\d{2}$", ErrorMessage = "CNPJ deve estar no formato XX.XXX.XXX/XXXX-XX")]
        [Display(Name = "CNPJ")]
        public string? CNPJ { get; set; }

        [StringLength(14, ErrorMessage = "O CPF deve ter no máximo 14 caracteres.")]
        [RegularExpression(@"^\d{3}\.\d{3}\.\d{3}\-\d{2}$", ErrorMessage = "CPF deve estar no formato XXX.XXX.XXX-XX")]
        [Display(Name = "CPF")]
        public string? CPF { get; set; }

        [StringLength(20, ErrorMessage = "O telefone deve ter no máximo 20 caracteres.")]
        [Display(Name = "Telefone")]
        public string? Telefone { get; set; }

        [StringLength(100, ErrorMessage = "O email deve ter no máximo 100 caracteres.")]
        [EmailAddress(ErrorMessage = "Email inválido.")]
        [Display(Name = "Email")]
        public string? Email { get; set; }

        [StringLength(200, ErrorMessage = "O endereço deve ter no máximo 200 caracteres.")]
        [Display(Name = "Endereço")]
        public string? Endereco { get; set; }

        [StringLength(100, ErrorMessage = "A cidade deve ter no máximo 100 caracteres.")]
        [Display(Name = "Cidade")]
        public string? Cidade { get; set; }

        [StringLength(2, ErrorMessage = "O estado deve ter 2 caracteres.")]
        [Display(Name = "Estado")]
        public string? Estado { get; set; }

        [StringLength(9, ErrorMessage = "O CEP deve ter no máximo 9 caracteres.")]
        [RegularExpression(@"^\d{5}\-?\d{3}$", ErrorMessage = "CEP deve estar no formato XXXXX-XXX")]
        [Display(Name = "CEP")]
        public string? CEP { get; set; }

        [StringLength(500, ErrorMessage = "As observações devem ter no máximo 500 caracteres.")]
        [Display(Name = "Observações")]
        public string? Observacoes { get; set; }

        [Display(Name = "Ativo")]
        public bool Ativo { get; set; } = true;

        [Display(Name = "Data de Cadastro")]
        public DateTime DataCadastro { get; set; } = DateTimeHelper.Now; // ✅ ALTERADO

        // Navigation properties
        [ValidateNever]
        public virtual ICollection<Saida> Saidas { get; set; } = new List<Saida>();
    }
}

