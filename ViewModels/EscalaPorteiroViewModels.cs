using SistemaTesourariaEclesiastica.Enums;
using SistemaTesourariaEclesiastica.Models;
using System.ComponentModel.DataAnnotations;

namespace SistemaTesourariaEclesiastica.ViewModels
{
    public class GerarEscalaViewModel
    {
        [Required(ErrorMessage = "A data de início é obrigatória.")]
        [DataType(DataType.Date)]
        [Display(Name = "Data de Início")]
        public DateTime DataInicio { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "A data de fim é obrigatória.")]
        [DataType(DataType.Date)]
        [Display(Name = "Data de Fim")]
        public DateTime DataFim { get; set; } = DateTime.Today.AddMonths(1);

        [Display(Name = "Dias Selecionados")]
        public List<DiasCultoViewModel> DiasSelecionados { get; set; } = new();

        public List<Porteiro> PorteirosDisponiveis { get; set; } = new();
        public List<ResponsavelPorteiro> ResponsaveisDisponiveis { get; set; } = new();
    }

    public class DiasCultoViewModel
    {
        [Required]
        [DataType(DataType.Date)]
        public DateTime Data { get; set; }

        [DataType(DataType.Time)]
        public TimeSpan? Horario { get; set; }

        [Required]
        public TipoCulto TipoCulto { get; set; }

        public string? Observacao { get; set; }
    }

    public class EscalaGeradaViewModel
    {
        public DateTime DataInicio { get; set; }
        public DateTime DataFim { get; set; }
        public List<EscalaPorteiro> Escalas { get; set; } = new();
        public List<Porteiro> TodosPorteiros { get; set; } = new();
        public ResponsavelPorteiro? Responsavel { get; set; }
    }
}
