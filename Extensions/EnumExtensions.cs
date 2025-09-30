using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace SistemaTesourariaEclesiastica.Extensions
{
    /// <summary>
    /// M�todos de extens�o para Enums
    /// </summary>
    public static class EnumExtensions
    {
        /// <summary>
        /// Obt�m o nome de exibi��o do enum usando o atributo Display
        /// </summary>
        public static string GetDisplayName(this Enum enumValue)
        {
            if (enumValue == null)
                return string.Empty;

            var memberInfo = enumValue.GetType().GetMember(enumValue.ToString());
            if (memberInfo.Length > 0)
            {
                var displayAttribute = memberInfo[0].GetCustomAttribute<DisplayAttribute>();
                if (displayAttribute != null)
                {
                    return displayAttribute.Name ?? enumValue.ToString();
                }
            }
            return enumValue.ToString();
        }

        /// <summary>
        /// Obt�m a descri��o do enum usando o atributo Display
        /// </summary>
        public static string GetDescription(this Enum enumValue)
        {
            if (enumValue == null)
                return string.Empty;

            var displayAttribute = enumValue.GetType()
                .GetMember(enumValue.ToString())
                .FirstOrDefault()?
                .GetCustomAttribute<DisplayAttribute>();

            return displayAttribute?.Description ?? enumValue.ToString();
        }

        /// <summary>
        /// Obt�m o valor inteiro do enum
        /// </summary>
        public static int GetValue(this Enum enumValue)
        {
            return Convert.ToInt32(enumValue);
        }
    }
}