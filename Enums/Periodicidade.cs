using System.ComponentModel.DataAnnotations;

namespace SistemaTesourariaEclesiastica.Enums
{
    /// <summary>
    /// Periodicidade de despesas recorrentes
    /// </summary>
    public enum Periodicidade
    {
        [Display(Name = "Semanal")]
        Semanal = 1,

        [Display(Name = "Quinzenal")]
        Quinzenal = 2,

        [Display(Name = "Mensal")]
        Mensal = 3,

        [Display(Name = "Bimestral")]
        Bimestral = 4,

        [Display(Name = "Trimestral")]
        Trimestral = 5,

        [Display(Name = "Semestral")]
        Semestral = 6,

        [Display(Name = "Anual")]
        Anual = 7
    }
}
