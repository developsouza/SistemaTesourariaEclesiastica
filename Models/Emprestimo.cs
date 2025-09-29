using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaTesourariaEclesiastica.Models
{
    public class Emprestimo
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "A data do empréstimo é obrigatória")]
        [Display(Name = "Data do Empréstimo")]
        [DataType(DataType.Date)]
        public DateTime DataEmprestimo { get; set; }

        [Required(ErrorMessage = "O valor do empréstimo é obrigatório")]
        [Display(Name = "Valor do Empréstimo")]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, double.MaxValue, ErrorMessage = "O valor deve ser maior que zero")]
        public decimal ValorTotal { get; set; }

        [Required(ErrorMessage = "A justificativa é obrigatória")]
        [Display(Name = "Justificativa")]
        [StringLength(500, ErrorMessage = "A justificativa não pode exceder 500 caracteres")]
        public string Justificativa { get; set; }

        [Display(Name = "Status")]
        public StatusEmprestimo Status { get; set; }

        [Display(Name = "Data de Quitação")]
        [DataType(DataType.Date)]
        public DateTime? DataQuitacao { get; set; }

        // Relacionamento com as devoluções
        public virtual ICollection<DevolucaoEmprestimo> Devolucoes { get; set; }

        // Propriedades calculadas
        [NotMapped]
        [Display(Name = "Valor Devolvido")]
        public decimal ValorDevolvido
        {
            get
            {
                return Devolucoes?.Sum(d => d.ValorDevolvido) ?? 0;
            }
        }

        [NotMapped]
        [Display(Name = "Saldo Devedor")]
        public decimal SaldoDevedor
        {
            get
            {
                return ValorTotal - ValorDevolvido;
            }
        }

        [NotMapped]
        [Display(Name = "Percentual Devolvido")]
        public decimal PercentualDevolvido
        {
            get
            {
                if (ValorTotal == 0) return 0;
                return Math.Round(ValorDevolvido / ValorTotal * 100, 2);
            }
        }

        public Emprestimo()
        {
            Devolucoes = new List<DevolucaoEmprestimo>();
            DataEmprestimo = DateTime.Now;
            Status = StatusEmprestimo.Ativo;
        }
    }

    public class DevolucaoEmprestimo
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Empréstimo")]
        public int EmprestimoId { get; set; }

        [ForeignKey("EmprestimoId")]
        public virtual Emprestimo Emprestimo { get; set; }

        [Required(ErrorMessage = "A data da devolução é obrigatória")]
        [Display(Name = "Data da Devolução")]
        [DataType(DataType.Date)]
        public DateTime DataDevolucao { get; set; }

        [Required(ErrorMessage = "O valor da devolução é obrigatório")]
        [Display(Name = "Valor Devolvido")]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, double.MaxValue, ErrorMessage = "O valor deve ser maior que zero")]
        public decimal ValorDevolvido { get; set; }

        [Display(Name = "Observações")]
        [StringLength(300)]
        public string Observacoes { get; set; }

        public DevolucaoEmprestimo()
        {
            DataDevolucao = DateTime.Now;
        }
    }

    public enum StatusEmprestimo
    {
        [Display(Name = "Ativo")]
        Ativo = 1,

        [Display(Name = "Quitado")]
        Quitado = 2,

        [Display(Name = "Cancelado")]
        Cancelado = 3
    }
}