using System;

namespace SistemaTesourariaEclesiastica.Helpers
{
    /// <summary>
    /// Helper para garantir que todas as datas sejam tratadas no fuso horário de Brasília
    /// </summary>
    public static class DateTimeHelper
    {
        // TimeZone do Brasil (Brasília) - UTC-3
        private static readonly TimeZoneInfo BrasilTimeZone = TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time");

        /// <summary>
        /// Retorna a data/hora atual no fuso horário de Brasília
        /// Use este método ao invés de DateTime.Now
        /// </summary>
        public static DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, BrasilTimeZone);

        /// <summary>
        /// Retorna a data atual (sem hora) no fuso horário de Brasília
        /// </summary>
        public static DateTime Today => Now.Date;

        /// <summary>
        /// Converte uma data UTC para o fuso horário de Brasília
        /// </summary>
        public static DateTime FromUtc(DateTime utcDateTime)
        {
            if (utcDateTime.Kind != DateTimeKind.Utc)
            {
                utcDateTime = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
            }
            return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, BrasilTimeZone);
        }

        /// <summary>
        /// Converte uma data do fuso horário de Brasília para UTC
        /// </summary>
        public static DateTime ToUtc(DateTime brasilDateTime)
        {
            return TimeZoneInfo.ConvertTimeToUtc(brasilDateTime, BrasilTimeZone);
        }

        /// <summary>
        /// Retorna informações sobre o fuso horário do Brasil
        /// </summary>
        public static TimeZoneInfo GetBrasilTimeZone() => BrasilTimeZone;

        /// <summary>
        /// Formata uma data no padrão brasileiro (dd/MM/yyyy)
        /// </summary>
        public static string FormatarData(DateTime data)
        {
            return data.ToString("dd/MM/yyyy");
        }

        /// <summary>
        /// Formata uma data/hora no padrão brasileiro (dd/MM/yyyy HH:mm)
        /// </summary>
        public static string FormatarDataHora(DateTime dataHora)
        {
            return dataHora.ToString("dd/MM/yyyy HH:mm");
        }

        /// <summary>
        /// Formata uma data/hora completa no padrão brasileiro (dd/MM/yyyy HH:mm:ss)
        /// </summary>
        public static string FormatarDataHoraCompleta(DateTime dataHora)
        {
            return dataHora.ToString("dd/MM/yyyy HH:mm:ss");
        }
    }
}
