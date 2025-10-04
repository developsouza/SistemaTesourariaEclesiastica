using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using SistemaTesourariaEclesiastica.Enums;
using System.ComponentModel.DataAnnotations;

namespace SistemaTesourariaEclesiastica.Models
{
    public class MeioDePagamento
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "O nome é obrigatório.")]
        [StringLength(100, ErrorMessage = "O nome deve ter no máximo 100 caracteres.")]
        [Display(Name = "Nome")]
        public string Nome { get; set; } = string.Empty;

        [StringLength(250, ErrorMessage = "A descrição deve ter no máximo 250 caracteres.")]
        [Display(Name = "Descrição")]
        public string? Descricao { get; set; }

        [Required(ErrorMessage = "O tipo de caixa é obrigatório.")]
        [Display(Name = "Tipo de Caixa")]
        public TipoCaixa TipoCaixa { get; set; } = TipoCaixa.Digital;

        [Display(Name = "Ativo")]
        public bool Ativo { get; set; } = true;

        // Navigation properties
        [ValidateNever]
        public virtual ICollection<Entrada> Entradas { get; set; } = new List<Entrada>();
        [ValidateNever]
        public virtual ICollection<Saida> Saidas { get; set; } = new List<Saida>();
        [ValidateNever]
        public virtual ICollection<TransferenciaInterna> TransferenciasOrigem { get; set; } = new List<TransferenciaInterna>();
        [ValidateNever]
        public virtual ICollection<TransferenciaInterna> TransferenciasDestino { get; set; } = new List<TransferenciaInterna>();
    }
}