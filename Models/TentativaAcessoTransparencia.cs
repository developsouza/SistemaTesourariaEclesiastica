using System.ComponentModel.DataAnnotations;

namespace SistemaTesourariaEclesiastica.Models
{
    /// <summary>
    /// Modelo para rastrear tentativas de acesso ao Portal de Transparência
    /// Usado para implementar Rate Limiting e prevenir ataques de força bruta
    /// </summary>
    public class TentativaAcessoTransparencia
    {
        public int Id { get; set; }

        [Required]
        [StringLength(11)]
        [Display(Name = "CPF")]
        public string CPF { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Data/Hora da Tentativa")]
        public DateTime DataHoraTentativa { get; set; } = DateTime.Now;

        [Display(Name = "Sucesso")]
        public bool Sucesso { get; set; }

        [StringLength(45)]
        [Display(Name = "Endereço IP")]
        public string? EnderecoIP { get; set; }

        [StringLength(500)]
        [Display(Name = "User Agent")]
        public string? UserAgent { get; set; }

        [StringLength(200)]
        [Display(Name = "Mensagem")]
        public string? Mensagem { get; set; }
    }
}
