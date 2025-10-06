using System.ComponentModel.DataAnnotations;

namespace SistemaTesourariaEclesiastica.Enums
{
    public enum StatusFechamentoPeriodo
    {
        [Display(Name = "Pendente")]
        Pendente = 1,

        [Display(Name = "Aprovado")]
        Aprovado = 2,

        [Display(Name = "Rejeitado")]
        Rejeitado = 3,

        [Display(Name = "Incluído em Prestação da Sede")]
        Processado = 4  // NOVO STATUS
    }
}

