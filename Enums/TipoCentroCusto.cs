using System.ComponentModel.DataAnnotations;

namespace SistemaTesourariaEclesiastica.Enums
{
    /// <summary>
    /// Tipos de Unidade Financeira disponíveis no sistema
    /// </summary>
    public enum TipoCentroCusto
    {
        [Display(Name = "Sede")]
        Sede = 1,

        [Display(Name = "Congregação")]
        Congregacao = 2,

        [Display(Name = "Departamento")]
        Departamento = 3,

        [Display(Name = "Projeto")]
        Projeto = 4,

        [Display(Name = "Ministério")]
        Ministerio = 5,

        [Display(Name = "Missão")]
        Missao = 6,

        [Display(Name = "Educacional")]
        Educacional = 7,

        [Display(Name = "Social")]
        Social = 8,

        [Display(Name = "Evangelístico")]
        Evangelistico = 9,

        [Display(Name = "Administrativo")]
        Administrativo = 10,

        [Display(Name = "Infraestrutura")]
        Infraestrutura = 11,

        [Display(Name = "Financeiro")]
        Financeiro = 12,

        [Display(Name = "Outro")]
        Outro = 99
    }
}