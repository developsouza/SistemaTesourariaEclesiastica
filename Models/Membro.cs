using System.ComponentModel.DataAnnotations;
using SistemaTesourariaEclesiastica.Attributes;

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

        [StringLength(15, ErrorMessage = "O telefone deve ter no máximo 15 caracteres.")]
        [RegularExpression(@"^[\(\)\d\s\-\+]+$", ErrorMessage = "Formato de telefone inválido.")]
        [Display(Name = "Telefone")]
        public string? Telefone { get; set; }

        [StringLength(100, ErrorMessage = "O email deve ter no máximo 100 caracteres.")]
        [EmailAddress(ErrorMessage = "Email inválido.")]
        [Display(Name = "Email")]
        public string? Email { get; set; }

        [DataValidation(PermitirDataFutura = false, DiasMaximoPassado = 36500, ErrorMessage = "Data de nascimento inválida.")]
        [Display(Name = "Data de Nascimento")]
        public DateTime? DataNascimento { get; set; }

        [StringLength(200, ErrorMessage = "O endereço deve ter no máximo 200 caracteres.")]
        [Display(Name = "Endereço")]
        public string? Endereco { get; set; }

        [Display(Name = "Ativo")]
        public bool Ativo { get; set; } = true;

        [StringLength(20, ErrorMessage = "O RG deve ter no máximo 20 caracteres.")]
        [Display(Name = "RG")]
        public string? RG { get; set; }

        [StringLength(10, ErrorMessage = "O CEP deve ter no máximo 10 caracteres.")]
        [RegularExpression(@"^\d{5}-?\d{3}$", ErrorMessage = "Formato de CEP inválido.")]
        [Display(Name = "CEP")]
        public string? CEP { get; set; }

        [StringLength(10, ErrorMessage = "O número deve ter no máximo 10 caracteres.")]
        [Display(Name = "Número")]
        public string? Numero { get; set; }

        [StringLength(100, ErrorMessage = "O complemento deve ter no máximo 100 caracteres.")]
        [Display(Name = "Complemento")]
        public string? Complemento { get; set; }

        [StringLength(100, ErrorMessage = "O bairro deve ter no máximo 100 caracteres.")]
        [Display(Name = "Bairro")]
        public string? Bairro { get; set; }

        [StringLength(100, ErrorMessage = "A cidade deve ter no máximo 100 caracteres.")]
        [Display(Name = "Cidade")]
        public string? Cidade { get; set; }

        [StringLength(2, ErrorMessage = "A UF deve ter 2 caracteres.")]
        [Display(Name = "UF")]
        public string? UF { get; set; }

        [DataValidation(PermitirDataFutura = false, DiasMaximoPassado = 36500, ErrorMessage = "Data de batismo inválida.")]
        [Display(Name = "Data de Batismo")]
        public DateTime? DataBatismo { get; set; }

        [Display(Name = "Data de Cadastro")]
        public DateTime DataCadastro { get; set; } = DateTime.Now;

        // Propriedade para compatibilidade com views que usam "Nome"
        [Display(Name = "Nome")]
        public string Nome => NomeCompleto;

        [Required(ErrorMessage = "O centro de custo é obrigatório.")]
        [Display(Name = "Centro de Custo")]
        public int CentroCustoId { get; set; }

        // Navigation properties
        public virtual CentroCusto CentroCusto { get; set; } = null!;
        public virtual ICollection<Entrada> Entradas { get; set; } = new List<Entrada>();
    }
}

