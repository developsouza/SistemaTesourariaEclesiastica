using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace SistemaTesourariaEclesiastica.Attributes
{
    public class CpfValidationAttribute : ValidationAttribute
    {
        public override bool IsValid(object? value)
        {
            if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
                return true; // Permite valores nulos/vazios - use [Required] para obrigar preenchimento

            var cpf = value.ToString()!.Replace(".", "").Replace("-", "").Replace(" ", "");

            return IsValidCpf(cpf);
        }

        private static bool IsValidCpf(string cpf)
        {
            // Verifica se tem 11 dígitos
            if (cpf.Length != 11)
                return false;

            // Verifica se todos os dígitos são iguais
            if (cpf.All(c => c == cpf[0]))
                return false;

            // Verifica se contém apenas números
            if (!Regex.IsMatch(cpf, @"^\d{11}$"))
                return false;

            // Calcula o primeiro dígito verificador
            int soma = 0;
            for (int i = 0; i < 9; i++)
            {
                soma += int.Parse(cpf[i].ToString()) * (10 - i);
            }
            int resto = soma % 11;
            int digitoVerificador1 = resto < 2 ? 0 : 11 - resto;

            // Verifica o primeiro dígito
            if (int.Parse(cpf[9].ToString()) != digitoVerificador1)
                return false;

            // Calcula o segundo dígito verificador
            soma = 0;
            for (int i = 0; i < 10; i++)
            {
                soma += int.Parse(cpf[i].ToString()) * (11 - i);
            }
            resto = soma % 11;
            int digitoVerificador2 = resto < 2 ? 0 : 11 - resto;

            // Verifica o segundo dígito
            return int.Parse(cpf[10].ToString()) == digitoVerificador2;
        }

        public override string FormatErrorMessage(string name)
        {
            return $"O campo {name} deve conter um CPF válido.";
        }
    }
}
