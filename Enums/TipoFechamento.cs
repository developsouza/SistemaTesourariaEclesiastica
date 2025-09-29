using System.ComponentModel.DataAnnotations;

namespace SistemaTesourariaEclesiastica.Enums
{
    /// <summary>
    /// Tipo de fechamento de período
    /// </summary>
    public enum TipoFechamento
    {
        [Display(Name = "Fechamento Diário")]
        Diario = 1,

        [Display(Name = "Fechamento Semanal")]
        Semanal = 2,

        [Display(Name = "Fechamento Mensal")]
        Mensal = 3
    }
}