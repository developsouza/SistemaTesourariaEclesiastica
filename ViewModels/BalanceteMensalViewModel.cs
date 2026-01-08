using System.ComponentModel.DataAnnotations;
using SistemaTesourariaEclesiastica.Helpers;

namespace SistemaTesourariaEclesiastica.ViewModels
{
    /// <summary>
    /// ViewModel para o Relatório de Balancete Mensal conforme especificação
    /// </summary>
    public class BalanceteMensalViewModel
    {
        [Display(Name = "Centro de Custo")]
        public string CentroCustoNome { get; set; } = string.Empty;

        [Display(Name = "Período")]
        public string Periodo { get; set; } = string.Empty;

        [Display(Name = "Data Início")]
        public DateTime DataInicio { get; set; }

        [Display(Name = "Data Fim")]
        public DateTime DataFim { get; set; }

        [Display(Name = "Saldo do Mês Anterior")]
        public decimal SaldoMesAnterior { get; set; }

        // ==========================================
        // RECEITAS OPERACIONAIS
        // ==========================================
        [Display(Name = "Receitas Operacionais")]
        public List<ItemBalanceteViewModel> ReceitasOperacionais { get; set; } = new List<ItemBalanceteViewModel>();

        [Display(Name = "Total do Crédito")]
        public decimal TotalCredito { get; set; }

        // ==========================================
        // IMOBILIZADOS
        // ==========================================
        [Display(Name = "Imobilizados")]
        public List<ItemBalanceteViewModel> Imobilizados { get; set; } = new List<ItemBalanceteViewModel>();

        [Display(Name = "Total do Crédito (com Imobilizados)")]
        public decimal TotalCreditoComImobilizados { get; set; }

        // ==========================================
        // DESPESAS ADMINISTRATIVAS
        // ==========================================
        [Display(Name = "Despesas Administrativas")]
        public List<ItemBalanceteViewModel> DespesasAdministrativas { get; set; } = new List<ItemBalanceteViewModel>();

        [Display(Name = "Subtotal Despesas Administrativas")]
        public decimal SubtotalDespesasAdministrativas { get; set; }

        // ==========================================
        // DESPESAS TRIBUTÁRIAS
        // ==========================================
        [Display(Name = "Despesas Tributárias")]
        public List<ItemBalanceteViewModel> DespesasTributarias { get; set; } = new List<ItemBalanceteViewModel>();

        [Display(Name = "Subtotal Despesas Tributárias")]
        public decimal SubtotalDespesasTributarias { get; set; }

        // ==========================================
        // DESPESAS FINANCEIRAS
        // ==========================================
        [Display(Name = "Despesas Financeiras")]
        public List<ItemBalanceteViewModel> DespesasFinanceiras { get; set; } = new List<ItemBalanceteViewModel>();

        [Display(Name = "Subtotal Despesas Financeiras")]
        public decimal SubtotalDespesasFinanceiras { get; set; }

        // ==========================================
        // RECOLHIMENTOS (RATEIOS)
        // ==========================================
        [Display(Name = "Recolhimentos")]
        public List<ItemRecolhimentoViewModel> Recolhimentos { get; set; } = new List<ItemRecolhimentoViewModel>();

        [Display(Name = "Total de Recolhimentos")]
        public decimal TotalRecolhimentos { get; set; }

        // ==========================================
        // TOTALIZADORES FINAIS
        // ==========================================
        [Display(Name = "Total do Débito")]
        public decimal TotalDebito { get; set; }

        [Display(Name = "Saldo Final")]
        public decimal Saldo { get; set; }

        // Informações adicionais
        [Display(Name = "Tesoureiro Responsável")]
        public string? TesoureriroResponsavel { get; set; }

        [Display(Name = "Visto do Pastor")]
        public string? VistoDoPastor { get; set; }

        [Display(Name = "Data de Geração")]
        public DateTime DataGeracao { get; set; } = DateTimeHelper.Now;
    }

    /// <summary>
    /// Item individual do balancete (receita ou despesa)
    /// </summary>
    public class ItemBalanceteViewModel
    {
        [Display(Name = "Descrição")]
        public string Descricao { get; set; } = string.Empty;

        [Display(Name = "Valor")]
        public decimal Valor { get; set; }

        [Display(Name = "Tipo")]
        public string? Tipo { get; set; }
    }

    /// <summary>
    /// Item de recolhimento (rateio)
    /// </summary>
    public class ItemRecolhimentoViewModel
    {
        [Display(Name = "Destino")]
        public string Destino { get; set; } = string.Empty;

        [Display(Name = "Percentual")]
        public decimal Percentual { get; set; }

        [Display(Name = "Valor")]
        public decimal Valor { get; set; }
    }
}