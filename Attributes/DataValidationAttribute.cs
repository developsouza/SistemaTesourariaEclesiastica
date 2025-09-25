using System.ComponentModel.DataAnnotations;

namespace SistemaTesourariaEclesiastica.Attributes
{
    public class DataValidationAttribute : ValidationAttribute
    {
        public bool PermitirDataFutura { get; set; } = true;
        public bool PermitirDataPassada { get; set; } = true;
        public int DiasMaximoPassado { get; set; } = 365 * 10; // 10 anos
        public int DiasMaximoFuturo { get; set; } = 365; // 1 ano

        public override bool IsValid(object? value)
        {
            if (value == null)
                return false;

            if (value is DateTime data)
            {
                var hoje = DateTime.Now.Date;
                var dataValidacao = data.Date;

                // Verifica se é data futura e se é permitida
                if (dataValidacao > hoje && !PermitirDataFutura)
                    return false;

                // Verifica se é data passada e se é permitida
                if (dataValidacao < hoje && !PermitirDataPassada)
                    return false;

                // Verifica limite de dias no passado
                if (dataValidacao < hoje)
                {
                    var diasDiferenca = (hoje - dataValidacao).Days;
                    if (diasDiferenca > DiasMaximoPassado)
                        return false;
                }

                // Verifica limite de dias no futuro
                if (dataValidacao > hoje)
                {
                    var diasDiferenca = (dataValidacao - hoje).Days;
                    if (diasDiferenca > DiasMaximoFuturo)
                        return false;
                }

                return true;
            }

            return false;
        }

        public override string FormatErrorMessage(string name)
        {
            var mensagens = new List<string>();

            if (!PermitirDataFutura)
                mensagens.Add("não pode ser uma data futura");
            else if (DiasMaximoFuturo < 365)
                mensagens.Add($"não pode ser mais de {DiasMaximoFuturo} dias no futuro");

            if (!PermitirDataPassada)
                mensagens.Add("não pode ser uma data passada");
            else if (DiasMaximoPassado < 365 * 10)
                mensagens.Add($"não pode ser mais de {DiasMaximoPassado} dias no passado");

            var restricoes = string.Join(" e ", mensagens);
            return $"O campo {name} {restricoes}.";
        }
    }
}
