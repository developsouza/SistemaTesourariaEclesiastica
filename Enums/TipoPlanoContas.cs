using System.ComponentModel.DataAnnotations;

namespace SistemaTesourariaEclesiastica.Enums
{
    public enum TipoPlanoContas
    {
        [Display(Name = "Receita")]
        Receita = 1,

        [Display(Name = "Despesa")]
        Despesa = 2
    }

    public enum TipoDespesa
    {
        [Display(Name = "Fixa")]
        Fixa = 1,

        [Display(Name = "Variável")]
        Variavel = 2
    }

    public enum TipoCentroCusto
    {
        [Display(Name = "Sede")]
        Sede = 1,

        [Display(Name = "Congregação")]
        Congregacao = 2
    }

    public enum StatusFechamento
    {
        [Display(Name = "Pendente")]
        Pendente = 1,

        [Display(Name = "Aprovado")]
        Aprovado = 2,

        [Display(Name = "Rejeitado")]
        Rejeitado = 3
    }
}