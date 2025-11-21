namespace SistemaTesourariaEclesiastica.Helpers
{
    /// <summary>
    /// Helper para formatação de dados em português brasileiro
    /// </summary>
    public static class LocalizacaoHelper
    {
        /// <summary>
        /// Retorna o nome do dia da semana em português
        /// </summary>
        public static string ObterNomeDiaSemana(DayOfWeek diaSemana)
        {
            return diaSemana switch
            {
                DayOfWeek.Sunday => "Domingo",
                DayOfWeek.Monday => "Segunda-feira",
                DayOfWeek.Tuesday => "Terça-feira",
                DayOfWeek.Wednesday => "Quarta-feira",
                DayOfWeek.Thursday => "Quinta-feira",
                DayOfWeek.Friday => "Sexta-feira",
                DayOfWeek.Saturday => "Sábado",
                _ => diaSemana.ToString()
            };
        }

        /// <summary>
        /// Retorna o nome abreviado do dia da semana em português
        /// </summary>
        public static string ObterNomeDiaSemanaAbreviado(DayOfWeek diaSemana)
        {
            return diaSemana switch
            {
                DayOfWeek.Sunday => "Dom",
                DayOfWeek.Monday => "Seg",
                DayOfWeek.Tuesday => "Ter",
                DayOfWeek.Wednesday => "Qua",
                DayOfWeek.Thursday => "Qui",
                DayOfWeek.Friday => "Sex",
                DayOfWeek.Saturday => "Sáb",
                _ => diaSemana.ToString()
            };
        }

        /// <summary>
        /// Formata uma data no padrão brasileiro (dd/MM/yyyy)
        /// </summary>
        public static string FormatarData(DateTime data)
        {
            return data.ToString("dd/MM/yyyy");
        }

        /// <summary>
        /// Formata uma data com hora no padrão brasileiro (dd/MM/yyyy HH:mm)
        /// </summary>
        public static string FormatarDataHora(DateTime dataHora)
        {
            return dataHora.ToString("dd/MM/yyyy HH:mm");
        }

        /// <summary>
        /// Formata uma data por extenso (ex: Segunda-feira, 20 de janeiro de 2025)
        /// </summary>
        public static string FormatarDataPorExtenso(DateTime data)
        {
            var cultura = new System.Globalization.CultureInfo("pt-BR");
            var diaSemana = ObterNomeDiaSemana(data.DayOfWeek);
            var dia = data.Day;
            var mes = cultura.DateTimeFormat.GetMonthName(data.Month);
            var ano = data.Year;

            return $"{diaSemana}, {dia} de {mes} de {ano}";
        }

        /// <summary>
        /// Formata um telefone no padrão brasileiro
        /// Celular: (00) 00000-0000
        /// Fixo: (00) 0000-0000
        /// </summary>
        public static string FormatarTelefone(string? telefone)
        {
            if (string.IsNullOrWhiteSpace(telefone))
                return string.Empty;

            // Remove todos os caracteres não numéricos
            var numeros = new string(telefone.Where(char.IsDigit).ToArray());

            if (numeros.Length == 11) // Celular
            {
                return $"({numeros.Substring(0, 2)}) {numeros.Substring(2, 5)}-{numeros.Substring(7)}";
            }
            else if (numeros.Length == 10) // Fixo
            {
                return $"({numeros.Substring(0, 2)}) {numeros.Substring(2, 4)}-{numeros.Substring(6)}";
            }
            else if (numeros.Length == 9) // Celular sem DDD
            {
                return $"{numeros.Substring(0, 5)}-{numeros.Substring(5)}";
            }
            else if (numeros.Length == 8) // Fixo sem DDD
            {
                return $"{numeros.Substring(0, 4)}-{numeros.Substring(4)}";
            }

            // Se não se encaixa em nenhum formato, retorna o original
            return telefone;
        }
    }
}
