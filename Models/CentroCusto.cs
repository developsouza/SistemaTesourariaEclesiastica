using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using SistemaTesourariaEclesiastica.Enums;
using SistemaTesourariaEclesiastica.Helpers;
using System.ComponentModel.DataAnnotations;

namespace SistemaTesourariaEclesiastica.Models
{
    public class CentroCusto
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "O nome da unidade financeira é obrigatório.")]
        [StringLength(100, ErrorMessage = "O nome deve ter no máximo 100 caracteres.")]
        [Display(Name = "Nome")]
        public string Nome { get; set; } = string.Empty;

        [Required(ErrorMessage = "O tipo da unidade financeira é obrigatório.")]
        [Display(Name = "Tipo")]
        public TipoCentroCusto Tipo { get; set; }

        [StringLength(250, ErrorMessage = "A descrição deve ter no máximo 250 caracteres.")]
        [Display(Name = "Descrição")]
        public string? Descricao { get; set; }

        [Display(Name = "Ativo")]
        public bool Ativo { get; set; } = true;

        [Display(Name = "Data de Criação")]
        public DateTime DataCriacao { get; set; } = DateTimeHelper.Now;

        // Navigation properties
        [ValidateNever]
        public virtual ICollection<Membro> Membros { get; set; } = new List<Membro>();
        [ValidateNever]
        public virtual ICollection<ApplicationUser> Usuarios { get; set; } = new List<ApplicationUser>();
        [ValidateNever]
        public virtual ICollection<ContaBancaria> ContasBancarias { get; set; } = new List<ContaBancaria>();
        [ValidateNever]
        public virtual ICollection<Entrada> Entradas { get; set; } = new List<Entrada>();
        [ValidateNever]
        public virtual ICollection<Saida> Saidas { get; set; } = new List<Saida>();
        [ValidateNever]
        public virtual ICollection<TransferenciaInterna> TransferenciasOrigem { get; set; } = new List<TransferenciaInterna>();
        [ValidateNever]
        public virtual ICollection<TransferenciaInterna> TransferenciasDestino { get; set; } = new List<TransferenciaInterna>();
        [ValidateNever]
        public virtual ICollection<FechamentoPeriodo> FechamentosPeriodo { get; set; } = new List<FechamentoPeriodo>();
    }
}

