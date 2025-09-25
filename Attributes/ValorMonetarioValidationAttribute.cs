using System.ComponentModel.DataAnnotations;

namespace SistemaTesourariaEclesiastica.Attributes
{
    public class ValorMonetarioValidationAttribute : ValidationAttribute
    {
        public double ValorMinimo { get; set; } = 0.01;
        public double ValorMaximo { get; set; } = 999999999.99;

        public override bool IsValid(object? value)
        {
            if (value == null)
                return false;

            if (value is decimal valorDecimal)
            {
                return (double)valorDecimal >= ValorMinimo && (double)valorDecimal <= ValorMaximo;
            }

            if (decimal.TryParse(value.ToString(), out decimal valorParsed))
            {
                return (double)valorParsed >= ValorMinimo && (double)valorParsed <= ValorMaximo;
            }

            return false;
        }

        public override string FormatErrorMessage(string name)
        {
            return $"O campo {name} deve estar entre {ValorMinimo:C} e {ValorMaximo:C}.";
        }
    }
}
