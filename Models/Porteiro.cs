using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaTesourariaEclesiastica.Models
{
    public class Porteiro
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "O nome do porteiro é obrigatório.")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "O nome deve ter entre 3 e 100 caracteres.")]
        [Display(Name = "Nome Completo")]
        public string Nome { get; set; } = string.Empty;

        [Required(ErrorMessage = "O telefone é obrigatório.")]
        [Phone(ErrorMessage = "Telefone inválido.")]
        [StringLength(20, ErrorMessage = "O telefone deve ter no máximo 20 caracteres.")]
        [Display(Name = "Telefone")]
        public string Telefone { get; set; } = string.Empty;

        [Display(Name = "Ativo")]
        public bool Ativo { get; set; } = true;

        [Display(Name = "Data de Cadastro")]
        public DateTime DataCadastro { get; set; } = DateTime.Now;

        // Disponibilidade de Dias da Semana (armazenado como JSON ou string separada por vírgulas)
        [Display(Name = "Dias Disponíveis")]
        [StringLength(100)]
        public string? DiasDisponiveis { get; set; }

        // Horários disponíveis (armazenado como JSON ou string)
        [Display(Name = "Horários Disponíveis")]
        [StringLength(200)]
        public string? HorariosDisponiveis { get; set; }

        // Navigation properties
        [ValidateNever]
        public virtual ICollection<EscalaPorteiro> Escalas { get; set; } = new List<EscalaPorteiro>();

        // Propriedades não mapeadas para facilitar o uso
        [NotMapped]
        [Display(Name = "Domingo")]
        public bool DisponibilidadeDomingo { get; set; }

        [NotMapped]
        [Display(Name = "Segunda-feira")]
        public bool DisponibilidadeSegunda { get; set; }

        [NotMapped]
        [Display(Name = "Terça-feira")]
        public bool DisponibilidadeTerca { get; set; }

        [NotMapped]
        [Display(Name = "Quarta-feira")]
        public bool DisponibilidadeQuarta { get; set; }

        [NotMapped]
        [Display(Name = "Quinta-feira")]
        public bool DisponibilidadeQuinta { get; set; }

        [NotMapped]
        [Display(Name = "Sexta-feira")]
        public bool DisponibilidadeSexta { get; set; }

        [NotMapped]
        [Display(Name = "Sábado")]
        public bool DisponibilidadeSabado { get; set; }

        // Métodos auxiliares
        public void CarregarDisponibilidade()
        {
            if (string.IsNullOrEmpty(DiasDisponiveis))
                return;

            var dias = DiasDisponiveis.Split(',', StringSplitOptions.RemoveEmptyEntries);
            DisponibilidadeDomingo = dias.Contains("0");
            DisponibilidadeSegunda = dias.Contains("1");
            DisponibilidadeTerca = dias.Contains("2");
            DisponibilidadeQuarta = dias.Contains("3");
            DisponibilidadeQuinta = dias.Contains("4");
            DisponibilidadeSexta = dias.Contains("5");
            DisponibilidadeSabado = dias.Contains("6");
        }

        public void SalvarDisponibilidade()
        {
            var diasSelecionados = new List<string>();
            if (DisponibilidadeDomingo) diasSelecionados.Add("0");
            if (DisponibilidadeSegunda) diasSelecionados.Add("1");
            if (DisponibilidadeTerca) diasSelecionados.Add("2");
            if (DisponibilidadeQuarta) diasSelecionados.Add("3");
            if (DisponibilidadeQuinta) diasSelecionados.Add("4");
            if (DisponibilidadeSexta) diasSelecionados.Add("5");
            if (DisponibilidadeSabado) diasSelecionados.Add("6");

            DiasDisponiveis = string.Join(",", diasSelecionados);
        }

        public bool EstaDisponivelNodia(DayOfWeek dia)
        {
            // ? Se não configurou dias, retorna TRUE (disponível em todos os dias)
            if (string.IsNullOrWhiteSpace(DiasDisponiveis))
                return true;

            return DiasDisponiveis.Contains(((int)dia).ToString());
        }

        public bool EstaDisponivelNoHorario(TimeSpan horario)
        {
            // ? Se NÃO configurou horários, retorna TRUE (disponível em todos os horários)
            if (string.IsNullOrWhiteSpace(HorariosDisponiveis))
                return true;

            var horarios = HorariosDisponiveis.Split(',', StringSplitOptions.RemoveEmptyEntries);

            // ? CORREÇÃO: TimeSpan não aceita formato "HH:mm"
            // Usar o formato de string padrão: "hh:mm" para comparação
            var horarioStr = $"{horario.Hours:D2}:{horario.Minutes:D2}";

            // ? Comparar com Trim e validar múltiplos formatos
            return horarios.Any(h =>
            {
                var horarioConfig = h.Trim();

                // Tentar comparar diretamente
                if (horarioConfig == horarioStr)
                    return true;

                // Tentar parsear e comparar (para lidar com formatos diferentes)
                if (TimeSpan.TryParse(horarioConfig, out var horarioConfigurado))
                {
                    return horarioConfigurado.Hours == horario.Hours &&
                           horarioConfigurado.Minutes == horario.Minutes;
                }

                return false;
            });
        }
    }
}
