using System;

namespace SistemaTesourariaEclesiastica.ViewModels
{
    public class FluxoDeCaixaItem
    {
        public DateTime Data { get; set; }
        public decimal Entradas { get; set; }
        public decimal Saidas { get; set; }
        public decimal SaldoDia { get; set; }
        public decimal SaldoAcumulado { get; set; }
    }
}

