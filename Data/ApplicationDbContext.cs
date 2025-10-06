using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SistemaTesourariaEclesiastica.Models;

namespace SistemaTesourariaEclesiastica.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // DbSets Existentes
        public DbSet<CentroCusto> CentrosCusto { get; set; }
        public DbSet<Membro> Membros { get; set; }
        public DbSet<PlanoDeContas> PlanosDeContas { get; set; }
        public DbSet<MeioDePagamento> MeiosDePagamento { get; set; }
        public DbSet<ModeloRateioEntrada> ModelosRateioEntrada { get; set; }
        public DbSet<Fornecedor> Fornecedores { get; set; }
        public DbSet<ContaBancaria> ContasBancarias { get; set; }
        public DbSet<Entrada> Entradas { get; set; }
        public DbSet<Saida> Saidas { get; set; }
        public DbSet<TransferenciaInterna> TransferenciasInternas { get; set; }
        public DbSet<FechamentoPeriodo> FechamentosPeriodo { get; set; }
        public DbSet<LogAuditoria> LogsAuditoria { get; set; }
        public DbSet<RegraRateio> RegrasRateio { get; set; }
        public DbSet<ItemRateioFechamento> ItensRateioFechamento { get; set; }
        public DbSet<DetalheFechamento> DetalhesFechamento { get; set; }
        public DbSet<Emprestimo> Emprestimos { get; set; }
        public DbSet<DevolucaoEmprestimo> DevolucaoEmprestimos { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // ========================================
            // CONFIGURAÇÕES GERAIS
            // ========================================

            // ApplicationUser -> CentroCusto
            builder.Entity<ApplicationUser>()
                .HasOne(u => u.CentroCusto)
                .WithMany(c => c.Usuarios)
                .HasForeignKey(u => u.CentroCustoId)
                .OnDelete(DeleteBehavior.SetNull);

            // Membro -> CentroCusto
            builder.Entity<Membro>()
                .HasOne(m => m.CentroCusto)
                .WithMany(c => c.Membros)
                .HasForeignKey(m => m.CentroCustoId)
                .OnDelete(DeleteBehavior.Restrict);

            // ContaBancaria -> CentroCusto
            builder.Entity<ContaBancaria>()
                .HasOne(cb => cb.CentroCusto)
                .WithMany(c => c.ContasBancarias)
                .HasForeignKey(cb => cb.CentroCustoId)
                .OnDelete(DeleteBehavior.Restrict);

            // ========================================
            // 💰 CONFIGURAÇÃO CRÍTICA - MEIO DE PAGAMENTO
            // ========================================
            builder.Entity<MeioDePagamento>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Nome)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.Descricao)
                    .HasMaxLength(250);

                entity.Property(e => e.TipoCaixa)
                    .HasConversion<int>()
                    .IsRequired();

                entity.Property(e => e.Ativo)
                    .IsRequired()
                    .HasDefaultValue(true);
            });

            // Entrada -> MeioDePagamento
            builder.Entity<Entrada>()
                .HasOne(e => e.MeioDePagamento)
                .WithMany(mp => mp.Entradas)
                .HasForeignKey(e => e.MeioDePagamentoId)
                .OnDelete(DeleteBehavior.Restrict);

            // Entrada -> CentroCusto
            builder.Entity<Entrada>()
                .HasOne(e => e.CentroCusto)
                .WithMany(c => c.Entradas)
                .HasForeignKey(e => e.CentroCustoId)
                .OnDelete(DeleteBehavior.Restrict);

            // Entrada -> PlanoDeContas
            builder.Entity<Entrada>()
                .HasOne(e => e.PlanoDeContas)
                .WithMany(pc => pc.Entradas)
                .HasForeignKey(e => e.PlanoDeContasId)
                .OnDelete(DeleteBehavior.Restrict);

            // Entrada -> Membro
            builder.Entity<Entrada>()
                .HasOne(e => e.Membro)
                .WithMany(m => m.Entradas)
                .HasForeignKey(e => e.MembroId)
                .OnDelete(DeleteBehavior.SetNull);

            // Entrada -> ModeloRateioEntrada
            builder.Entity<Entrada>()
                .HasOne(e => e.ModeloRateioEntrada)
                .WithMany(mre => mre.Entradas)
                .HasForeignKey(e => e.ModeloRateioEntradaId)
                .OnDelete(DeleteBehavior.SetNull);

            // Entrada -> ApplicationUser
            builder.Entity<Entrada>()
                .HasOne(e => e.Usuario)
                .WithMany()
                .HasForeignKey(e => e.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);

            // Saida -> MeioDePagamento
            builder.Entity<Saida>()
                .HasOne(s => s.MeioDePagamento)
                .WithMany(mp => mp.Saidas)
                .HasForeignKey(s => s.MeioDePagamentoId)
                .OnDelete(DeleteBehavior.Restrict);

            // Saida -> CentroCusto
            builder.Entity<Saida>()
                .HasOne(s => s.CentroCusto)
                .WithMany(c => c.Saidas)
                .HasForeignKey(s => s.CentroCustoId)
                .OnDelete(DeleteBehavior.Restrict);

            // Saida -> PlanoDeContas
            builder.Entity<Saida>()
                .HasOne(s => s.PlanoDeContas)
                .WithMany(pc => pc.Saidas)
                .HasForeignKey(s => s.PlanoDeContasId)
                .OnDelete(DeleteBehavior.Restrict);

            // Saida -> Fornecedor
            builder.Entity<Saida>()
                .HasOne(s => s.Fornecedor)
                .WithMany(f => f.Saidas)
                .HasForeignKey(s => s.FornecedorId)
                .OnDelete(DeleteBehavior.SetNull);

            // Saida -> ApplicationUser
            builder.Entity<Saida>()
                .HasOne(s => s.Usuario)
                .WithMany()
                .HasForeignKey(s => s.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);

            // TransferenciaInterna -> MeioDePagamento (Origem)
            builder.Entity<TransferenciaInterna>()
                .HasOne(ti => ti.MeioDePagamentoOrigem)
                .WithMany(mp => mp.TransferenciasOrigem)
                .HasForeignKey(ti => ti.MeioDePagamentoOrigemId)
                .OnDelete(DeleteBehavior.Restrict);

            // TransferenciaInterna -> MeioDePagamento (Destino)
            builder.Entity<TransferenciaInterna>()
                .HasOne(ti => ti.MeioDePagamentoDestino)
                .WithMany(mp => mp.TransferenciasDestino)
                .HasForeignKey(ti => ti.MeioDePagamentoDestinoId)
                .OnDelete(DeleteBehavior.Restrict);

            // TransferenciaInterna -> CentroCusto (Origem)
            builder.Entity<TransferenciaInterna>()
                .HasOne(ti => ti.CentroCustoOrigem)
                .WithMany(c => c.TransferenciasOrigem)
                .HasForeignKey(ti => ti.CentroCustoOrigemId)
                .OnDelete(DeleteBehavior.Restrict);

            // TransferenciaInterna -> CentroCusto (Destino)
            builder.Entity<TransferenciaInterna>()
                .HasOne(ti => ti.CentroCustoDestino)
                .WithMany(c => c.TransferenciasDestino)
                .HasForeignKey(ti => ti.CentroCustoDestinoId)
                .OnDelete(DeleteBehavior.Restrict);

            // TransferenciaInterna -> ApplicationUser
            builder.Entity<TransferenciaInterna>()
                .HasOne(ti => ti.Usuario)
                .WithMany()
                .HasForeignKey(ti => ti.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);

            // ========================================
            // 🎯 CONFIGURAÇÃO CRÍTICA - FECHAMENTO PERÍODO
            // ========================================
            builder.Entity<FechamentoPeriodo>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.TipoFechamento)
                    .HasConversion<int>()
                    .IsRequired();

                entity.Property(e => e.Ano)
                    .IsRequired(false);

                entity.Property(e => e.Mes)
                    .IsRequired(false);

                entity.Property(e => e.DataInicio)
                    .IsRequired();

                entity.Property(e => e.DataFim)
                    .IsRequired();

                // Novos campos para cálculos detalhados
                entity.Property(e => e.TotalEntradas)
                    .HasColumnType("decimal(18,2)")
                    .HasDefaultValue(0);

                entity.Property(e => e.TotalSaidas)
                    .HasColumnType("decimal(18,2)")
                    .HasDefaultValue(0);

                entity.Property(e => e.TotalEntradasFisicas)
                    .HasColumnType("decimal(18,2)")
                    .HasDefaultValue(0);

                entity.Property(e => e.TotalSaidasFisicas)
                    .HasColumnType("decimal(18,2)")
                    .HasDefaultValue(0);

                entity.Property(e => e.TotalEntradasDigitais)
                    .HasColumnType("decimal(18,2)")
                    .HasDefaultValue(0);

                entity.Property(e => e.TotalSaidasDigitais)
                    .HasColumnType("decimal(18,2)")
                    .HasDefaultValue(0);

                entity.Property(e => e.BalancoDigital)
                    .HasColumnType("decimal(18,2)")
                    .HasDefaultValue(0);

                entity.Property(e => e.BalancoFisico)
                    .HasColumnType("decimal(18,2)")
                    .HasDefaultValue(0);

                entity.Property(e => e.TotalRateios)
                    .HasColumnType("decimal(18,2)")
                    .HasDefaultValue(0);

                entity.Property(e => e.SaldoFinal)
                    .HasColumnType("decimal(18,2)")
                    .HasDefaultValue(0);

                entity.Property(e => e.Status)
                    .HasConversion<int>()
                    .IsRequired();

                entity.Property(e => e.UsuarioSubmissaoId)
                    .IsRequired();

                entity.Property(e => e.UsuarioAprovacaoId)
                    .IsRequired(false);
            });

            // FechamentoPeriodo -> CentroCusto
            builder.Entity<FechamentoPeriodo>()
                .HasOne(fp => fp.CentroCusto)
                .WithMany(c => c.FechamentosPeriodo)
                .HasForeignKey(fp => fp.CentroCustoId)
                .OnDelete(DeleteBehavior.Restrict);

            // FechamentoPeriodo -> UsuarioSubmissao
            builder.Entity<FechamentoPeriodo>()
                .HasOne(fp => fp.UsuarioSubmissao)
                .WithMany()
                .HasForeignKey(fp => fp.UsuarioSubmissaoId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(true);

            // FechamentoPeriodo -> UsuarioAprovacao
            builder.Entity<FechamentoPeriodo>()
                .HasOne(fp => fp.UsuarioAprovacao)
                .WithMany()
                .HasForeignKey(fp => fp.UsuarioAprovacaoId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);

            // LogAuditoria -> ApplicationUser
            builder.Entity<LogAuditoria>()
                .HasOne(la => la.Usuario)
                .WithMany()
                .HasForeignKey(la => la.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);

            // RegraRateio -> CentroCusto (Origem)
            builder.Entity<RegraRateio>()
                .HasOne(rr => rr.CentroCustoOrigem)
                .WithMany()
                .HasForeignKey(rr => rr.CentroCustoOrigemId)
                .OnDelete(DeleteBehavior.Restrict);

            // RegraRateio -> CentroCusto (Destino)
            builder.Entity<RegraRateio>()
                .HasOne(rr => rr.CentroCustoDestino)
                .WithMany()
                .HasForeignKey(rr => rr.CentroCustoDestinoId)
                .OnDelete(DeleteBehavior.Restrict);

            // ItemRateioFechamento -> FechamentoPeriodo
            builder.Entity<ItemRateioFechamento>()
                .HasOne(irf => irf.FechamentoPeriodo)
                .WithMany(fp => fp.ItensRateio)
                .HasForeignKey(irf => irf.FechamentoPeriodoId)
                .OnDelete(DeleteBehavior.Cascade);

            // ItemRateioFechamento -> RegraRateio
            builder.Entity<ItemRateioFechamento>()
                .HasOne(irf => irf.RegraRateio)
                .WithMany(rr => rr.ItensRateio)
                .HasForeignKey(irf => irf.RegraRateioId)
                .OnDelete(DeleteBehavior.Restrict);

            // DetalheFechamento -> FechamentoPeriodo
            builder.Entity<DetalheFechamento>()
                .HasOne(df => df.FechamentoPeriodo)
                .WithMany(fp => fp.DetalhesFechamento)
                .HasForeignKey(df => df.FechamentoPeriodoId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relacionamento de Fechamentos: Sede → Congregações Processadas
            builder.Entity<FechamentoPeriodo>()
                .HasOne(f => f.FechamentoSedeProcessador)
                .WithMany(f => f.FechamentosCongregacoesIncluidos)
                .HasForeignKey(f => f.FechamentoSedeProcessadorId)
                .OnDelete(DeleteBehavior.Restrict);

            // ========================================
            // 💰 CONFIGURAÇÕES - MÓDULO DE EMPRÉSTIMOS
            // ========================================
            builder.Entity<Emprestimo>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.DataEmprestimo)
                    .IsRequired()
                    .HasDefaultValueSql("GETDATE()");

                entity.Property(e => e.ValorTotal)
                    .HasColumnType("decimal(18,2)")
                    .IsRequired();

                entity.Property(e => e.Justificativa)
                    .HasMaxLength(500)
                    .IsRequired();

                entity.Property(e => e.Status)
                    .HasConversion<int>()
                    .IsRequired();

                entity.Property(e => e.DataQuitacao)
                    .IsRequired(false);

                entity.HasMany(e => e.Devolucoes)
                    .WithOne(d => d.Emprestimo)
                    .HasForeignKey(d => d.EmprestimoId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<DevolucaoEmprestimo>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.EmprestimoId)
                    .IsRequired();

                entity.Property(e => e.DataDevolucao)
                    .IsRequired()
                    .HasDefaultValueSql("GETDATE()");

                entity.Property(e => e.ValorDevolvido)
                    .HasColumnType("decimal(18,2)")
                    .IsRequired();

                entity.Property(e => e.Observacoes)
                    .HasMaxLength(300)
                    .IsRequired(false);
            });

            // ========================================
            // ÍNDICES ÚNICOS
            // ========================================
            builder.Entity<Membro>()
                .HasIndex(m => m.CPF)
                .IsUnique();

            builder.Entity<CentroCusto>()
                .HasIndex(c => c.Nome)
                .IsUnique();

            builder.Entity<PlanoDeContas>()
                .HasIndex(pc => pc.Descricao)
                .IsUnique();

            builder.Entity<MeioDePagamento>()
                .HasIndex(mp => mp.Nome)
                .IsUnique();

            builder.Entity<ModeloRateioEntrada>()
                .HasIndex(mre => mre.Nome)
                .IsUnique();

            builder.Entity<Fornecedor>()
                .HasIndex(f => f.Nome)
                .IsUnique();

            // Índices para Empréstimos
            builder.Entity<DevolucaoEmprestimo>()
                .HasIndex(d => d.EmprestimoId)
                .HasDatabaseName("IX_DevolucaoEmprestimos_EmprestimoId");

            builder.Entity<Emprestimo>()
                .HasIndex(e => e.Status)
                .HasDatabaseName("IX_Emprestimos_Status");

            builder.Entity<Emprestimo>()
                .HasIndex(e => e.DataEmprestimo)
                .IsDescending()
                .HasDatabaseName("IX_Emprestimos_DataEmprestimo");

            builder.Entity<Emprestimo>()
                .HasIndex(e => new { e.Status, e.DataEmprestimo })
                .HasDatabaseName("IX_Emprestimos_Status_DataEmprestimo");
        }
    }
}