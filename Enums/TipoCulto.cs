using System.ComponentModel.DataAnnotations;

namespace SistemaTesourariaEclesiastica.Enums
{
    public enum TipoCulto
    {
        [Display(Name = "Escola Bíblica")]
        EscolaBiblica = 0,

        [Display(Name = "Culto Evangelístico")]
        Evangelistico = 1,

        [Display(Name = "Culto da Família")]
        DaFamilia = 2,

        [Display(Name = "Culto de Doutrina")]
        DeDoutrina = 3,

        [Display(Name = "Culto Especial")]
        Especial = 4,

        [Display(Name = "Congresso")]
        Congresso = 5,

        [Display(Name = "Outro Tipo de Culto")]
        OutroTipoCulto = 6
    }
}
