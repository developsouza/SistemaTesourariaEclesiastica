using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace SistemaTesourariaEclesiastica.Attributes
{
    public class CnpjValidationAttribute : ValidationAttribute
    {
        public override bool IsValid(object? value)
        {
            if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
                return true; // Permite valores nulos/vazios - use [Required] para obrigar preenchimento

            var cnpj = value.ToString()!.Replace(".", "").Replace("/", "").Replace("-", "").Replace(" ", "");

            return IsValidCnpj(cnpj);
        }

        private static bool IsValidCnpj(string cnpj)
        {
            // Verifica se tem 14 dígitos
            if (cnpj.Length != 14)
                return false;

            // Verifica se todos os dígitos são iguais
            if (cnpj.All(c => c == cnpj[0]))
                return false;

            // Verifica se contém apenas números
            if (!Regex.IsMatch(cnpj, @"^\d{14}$"))
                return false;

            // Calcula o primeiro dígito verificador
            int[] multiplicador1 = { 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
            int soma = 0;
            for (int i = 0; i < 12; i++)
            {
                soma += int.Parse(cnpj[i].ToString()) * multiplicador1[i];
            }
            int resto = soma % 11;
            int digitoVerificador1 = resto < 2 ? 0 : 11 - resto;

            // Verifica o primeiro dígito
            if (int.Parse(cnpj[12].ToString()) != digitoVerificador1)
                return false;

            // Calcula o segundo dígito verificador
            int[] multiplicador2 = { 6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
            soma = 0;
            for (int i = 0; i < 13; i++)
            {
                soma += int.Parse(cnpj[i].ToString()) * multiplicador2[i];
            }
            resto = soma % 11;
            int digitoVerificador2 = resto < 2 ? 0 : 11 - resto;

            // Verifica o segundo dígito
            return int.Parse(cnpj[13].ToString()) == digitoVerificador2;
        }

        public override string FormatErrorMessage(string name)
        {
            return $"O campo {name} deve conter um CNPJ válido.";
        }
    }
}
