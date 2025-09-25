using System.ComponentModel.DataAnnotations;

namespace SistemaTesourariaEclesiastica.ViewModels
{
    public class ProfileViewModel
    {
        public string Id { get; set; } = string.Empty;

        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Nome Completo")]
        public string NomeCompleto { get; set; } = string.Empty;

        [Display(Name = "Centro de Custo")]
        public string? CentroCustoNome { get; set; }

        [Display(Name = "Data de Criação")]
        public DateTime DataCriacao { get; set; }

        [Display(Name = "Último Acesso")]
        public DateTime? UltimoAcesso { get; set; }

        [Display(Name = "Perfis de Acesso")]
        public List<string> Roles { get; set; } = new List<string>();
    }

    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "A senha atual é obrigatória.")]
        [DataType(DataType.Password)]
        [Display(Name = "Senha Atual")]
        public string SenhaAtual { get; set; } = string.Empty;

        [Required(ErrorMessage = "A nova senha é obrigatória.")]
        [StringLength(100, ErrorMessage = "A nova senha deve ter pelo menos {2} e no máximo {1} caracteres.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Nova Senha")]
        public string NovaSenha { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Confirmar Nova Senha")]
        [Compare("NovaSenha", ErrorMessage = "A nova senha e a confirmação não coincidem.")]
        public string ConfirmarNovaSenha { get; set; } = string.Empty;
    }

    public class UserStatsViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string NomeCompleto { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime UltimoAcesso { get; set; }
        public int TotalLogins { get; set; }
        public int LoginsMes { get; set; }
        public List<string> Roles { get; set; } = new List<string>();
        public string CentroCusto { get; set; } = string.Empty;
    }

    public class SecurityAuditViewModel
    {
        public DateTime DataInicio { get; set; }
        public DateTime DataFim { get; set; }
        public int TotalTentativasLogin { get; set; }
        public int LoginsSucesso { get; set; }
        public int LoginsFalha { get; set; }
        public int ContasBloqueadas { get; set; }
        public List<string> IPsSuspeitos { get; set; } = new List<string>();
        public List<UserStatsViewModel> UsuariosMaisAtivos { get; set; } = new List<UserStatsViewModel>();
    }
}