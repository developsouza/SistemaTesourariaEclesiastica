using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using SistemaTesourariaEclesiastica.Enums;
using System.ComponentModel.DataAnnotations;

namespace SistemaTesourariaEclesiastica.Models
{
    public class ConfiguracaoCulto
    {
        public int Id { get; set; }

        [Display(Name = "Dia da Semana")]
        public DayOfWeek? DiaSemana { get; set; }

        [Display(Name = "Data Específica")]
        [DataType(DataType.Date)]
        public DateTime? DataEspecifica { get; set; }

        [Display(Name = "Horário")]
        [DataType(DataType.Time)]
        public TimeSpan? Horario { get; set; }

        [Required(ErrorMessage = "O tipo de culto é obrigatório.")]
        [Display(Name = "Tipo de Culto")]
        public TipoCulto TipoCulto { get; set; }

        [Display(Name = "Ativo")]
        public bool Ativo { get; set; } = true;

        [Display(Name = "Data de Cadastro")]
        public DateTime DataCadastro { get; set; } = DateTime.Now;

        [StringLength(200, ErrorMessage = "A observação deve ter no máximo 200 caracteres.")]
        [Display(Name = "Observação")]
        public string? Observacao { get; set; }

        // Propriedade computada para identificar o tipo de configuração
        [ValidateNever]
        public bool EhDataEspecifica => DataEspecifica.HasValue;

        // Propriedade computada para exibição
        [ValidateNever]
        public string DescricaoConfiguracao
        {
            get
            {
                var descricao = "";
                if (DataEspecifica.HasValue)
                {
                    descricao = $"{DataEspecifica.Value:dd/MM/yyyy} ({DataEspecifica.Value:dddd})";
                }
                else if (DiaSemana.HasValue)
                {
                    descricao = Helpers.LocalizacaoHelper.ObterNomeDiaSemana(DiaSemana.Value);
                }
                else
                {
                    descricao = "Não configurado";
                }

                if (Horario.HasValue)
                {
                    descricao += $" às {Horario.Value:hh\\:mm}";
                }

                return descricao;
            }
        }
    }
}
