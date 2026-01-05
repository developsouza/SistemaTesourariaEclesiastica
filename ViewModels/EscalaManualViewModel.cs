using SistemaTesourariaEclesiastica.Enums;
using SistemaTesourariaEclesiastica.Models;

namespace SistemaTesourariaEclesiastica.ViewModels
{
    /// <summary>
    /// ViewModel para criação manual de escalas com drag-and-drop
    /// </summary>
    public class EscalaManualViewModel
    {
        public DateTime DataInicio { get; set; }
        public DateTime DataFim { get; set; }
        public List<DiaEscalaManual> DiasDisponiveis { get; set; } = new();
        public List<Porteiro> PorteirosDisponiveis { get; set; } = new();
        public List<ResponsavelPorteiro> ResponsaveisDisponiveis { get; set; } = new();
        public int ResponsavelSelecionadoId { get; set; }
    }

    /// <summary>
    /// Representa um dia disponível para atribuição manual de porteiros
    /// </summary>
    public class DiaEscalaManual
    {
        public DateTime Data { get; set; }
        public TimeSpan? Horario { get; set; }
        public TipoCulto TipoCulto { get; set; }
        public string DiaSemana => Data.ToString("dddd", new System.Globalization.CultureInfo("pt-BR"));
        public string DataFormatada => Data.ToString("dd/MM/yyyy");
        public string HorarioFormatado => Horario?.ToString(@"hh\:mm") ?? "";
        public int QuantidadePorteirosNecessaria => Horario.HasValue && Horario.Value >= new TimeSpan(19, 0, 0) ? 2 : 1;

        // Porteiros já atribuídos a este dia
        public List<PorteiroAtribuido> PorteirosAtribuidos { get; set; } = new();
    }

    /// <summary>
    /// Representa um porteiro atribuído a um dia específico
    /// </summary>
    public class PorteiroAtribuido
    {
        public int PorteiroId { get; set; }
        public string NomePorteiro { get; set; } = string.Empty;
        public int Posicao { get; set; } // 1 = Porteiro Principal, 2 = Porteiro 2
    }

    /// <summary>
    /// Modelo para salvar a escala manual via AJAX
    /// </summary>
    public class SalvarEscalaManualRequest
    {
        public DateTime DataInicio { get; set; }
        public DateTime DataFim { get; set; }
        public int ResponsavelId { get; set; }
        public List<EscalaDiaManual> Escalas { get; set; } = new();
    }

    /// <summary>
    /// Representa uma escala de um dia específico para salvar
    /// </summary>
    public class EscalaDiaManual
    {
        public DateTime Data { get; set; }
        public TimeSpan? Horario { get; set; }
        public TipoCulto TipoCulto { get; set; }
        public int? PorteiroId { get; set; }
        public int? Porteiro2Id { get; set; }
        public string? Observacao { get; set; }
    }
}
