using System.ComponentModel.DataAnnotations;

namespace SistemaTesourariaEclesiastica.Enums
{
    /// <summary>
    /// Tipo de caixa para meios de pagamento
    /// </summary>
    public enum TipoCaixa
    {
        [Display(Name = "Caixa Físico (Dinheiro)")]
        Fisico = 1,

        [Display(Name = "Caixa Digital/Bancário")]
        Digital = 2
    }
}