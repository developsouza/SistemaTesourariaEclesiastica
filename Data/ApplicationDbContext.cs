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
        public DbSet<DespesaRecorrente> DespesasRecorrentes { get; set; }
        public DbSet<PagamentoDespesaRecorrente> PagamentosDespesasRecorrentes { get; set; }
        public DbSet<Porteiro> Porteiros { get; set; }
        public DbSet<ResponsavelPorteiro> ResponsaveisPorteiros { get; set; }
        public DbSet<EscalaPorteiro> EscalasPorteiros { get; set; }
        public DbSet<ConfiguracaoCulto> ConfiguracoesCultos { get; set; }
        public DbSet<TentativaAcessoTransparencia> TentativasAcessoTransparencia { get; set; }

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

            // ✅ ÍNDICE ÚNICO para Regras de Rateio - Evita duplicatas
            builder.Entity<RegraRateio>()
                .HasIndex(r => new { r.CentroCustoOrigemId, r.CentroCustoDestinoId, r.Ativo })
                .HasFilter("[Ativo] = 1")
                .IsUnique()
                .HasDatabaseName("IX_RegrasRateio_OrigemDestino_Ativo");

            // ========================================
            // ✅ ÍNDICES COMPOSTOS PARA MELHOR PERFORMANCE
            // ========================================

            // Entradas - Otimizar queries por centro de custo, data e status de fechamento
            builder.Entity<Entrada>()
               .HasIndex(e => new { e.CentroCustoId, e.Data, e.IncluidaEmFechamento })
                .HasDatabaseName("IX_Entradas_CentroCusto_Data_Fechamento");

            builder.Entity<Entrada>()
               .HasIndex(e => new { e.Data, e.CentroCustoId })
                     .IsDescending(true, false)
               .HasDatabaseName("IX_Entradas_Data_CentroCusto_Desc");

            builder.Entity<Entrada>()
             .HasIndex(e => new { e.PlanoDeContasId, e.Data })
                     .HasDatabaseName("IX_Entradas_PlanoContas_Data");

            builder.Entity<Entrada>()
          .HasIndex(e => new { e.MembroId, e.Data })
              .HasDatabaseName("IX_Entradas_Membro_Data");

            // Saídas - Otimizar queries por centro de custo, data e status de fechamento
            builder.Entity<Saida>()
   .HasIndex(s => new { s.CentroCustoId, s.Data, s.IncluidaEmFechamento })
     .HasDatabaseName("IX_Saidas_CentroCusto_Data_Fechamento");

            builder.Entity<Saida>()
          .HasIndex(s => new { s.Data, s.CentroCustoId })
   .IsDescending(true, false)
       .HasDatabaseName("IX_Saidas_Data_CentroCusto_Desc");

            builder.Entity<Saida>()
              .HasIndex(s => new { s.PlanoDeContasId, s.Data })
           .HasDatabaseName("IX_Saidas_PlanoContas_Data");

            builder.Entity<Saida>()
                        .HasIndex(s => new { s.FornecedorId, s.Data })
          .HasDatabaseName("IX_Saidas_Fornecedor_Data");

            // FechamentoPeriodo - Otimizar queries por status, centro de custo e período
            builder.Entity<FechamentoPeriodo>()
         .HasIndex(f => new { f.Status, f.CentroCustoId, f.DataInicio, f.DataFim })
     .HasDatabaseName("IX_Fechamentos_Status_CentroCusto_Periodo");

            builder.Entity<FechamentoPeriodo>()
     .HasIndex(f => new { f.CentroCustoId, f.DataInicio, f.DataFim, f.Status })
         .HasDatabaseName("IX_Fechamentos_CentroCusto_Periodo_Status");

            builder.Entity<FechamentoPeriodo>()
           .HasIndex(f => new { f.EhFechamentoSede, f.Status, f.DataAprovacao })
              .IsDescending(false, false, true)
             .HasDatabaseName("IX_Fechamentos_Sede_Status_DataAprovacao");

            builder.Entity<FechamentoPeriodo>()
        .HasIndex(f => new { f.FoiProcessadoPelaSede, f.Status })
       .HasDatabaseName("IX_Fechamentos_ProcessadoSede_Status");

            // LogAuditoria - Otimizar queries por data e usuário
            builder.Entity<LogAuditoria>()
                    .HasIndex(l => new { l.DataHora, l.UsuarioId })
         .IsDescending(true, false)
       .HasDatabaseName("IX_LogAuditoria_DataHora_Usuario_Desc");

            builder.Entity<LogAuditoria>()
            .HasIndex(l => new { l.Acao, l.DataHora })
             .IsDescending(false, true)
          .HasDatabaseName("IX_LogAuditoria_Acao_DataHora");

            // ItemRateioFechamento - Otimizar queries por fechamento
            builder.Entity<ItemRateioFechamento>()
                .HasIndex(i => new { i.FechamentoPeriodoId, i.RegraRateioId })
                      .HasDatabaseName("IX_ItemRateio_Fechamento_Regra");

            // DetalheFechamento - Otimizar queries por fechamento e tipo
            builder.Entity<DetalheFechamento>()
   .HasIndex(d => new { d.FechamentoPeriodoId, d.TipoMovimento, d.Data })
    .HasDatabaseName("IX_DetalheFechamento_Fechamento_Tipo_Data");

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

            // ========================================
            // 📋 CONFIGURAÇÕES - MÓDULO DE ESCALA DE PORTEIROS
            // ========================================

            // Porteiro
            builder.Entity<Porteiro>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Nome)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.Telefone)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(e => e.Ativo)
                    .IsRequired()
                    .HasDefaultValue(true);

                entity.Property(e => e.DataCadastro)
                    .IsRequired()
                    .HasDefaultValueSql("GETDATE()");

                entity.Property(e => e.DiasDisponiveis)
                    .HasMaxLength(100)
                    .IsRequired(false);

                entity.Property(e => e.HorariosDisponiveis)
                    .HasMaxLength(200)
                    .IsRequired(false);
            });

            // ResponsavelPorteiro
            builder.Entity<ResponsavelPorteiro>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Nome)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.Telefone)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(e => e.Ativo)
                    .IsRequired()
                    .HasDefaultValue(true);

                entity.Property(e => e.DataCadastro)
                    .IsRequired()
                    .HasDefaultValueSql("GETDATE()");
            });

            // ConfiguracaoCulto
            builder.Entity<ConfiguracaoCulto>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.DiaSemana)
                    .HasConversion<int?>()
                    .IsRequired(false); // ✅ Tornado opcional

                entity.Property(e => e.DataEspecifica)
                    .IsRequired(false); // ✅ Tornado opcional

                entity.Property(e => e.Horario)
                    .IsRequired(false);

                entity.Property(e => e.TipoCulto)
                    .HasConversion<int>()
                    .IsRequired();

                entity.Property(e => e.Ativo)
                    .IsRequired()
                    .HasDefaultValue(true);

                entity.Property(e => e.DataCadastro)
                    .IsRequired()
                    .HasDefaultValueSql("GETDATE()");

                entity.Property(e => e.Observacao)
                    .HasMaxLength(200);
            });

            // EscalaPorteiro
            builder.Entity<EscalaPorteiro>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.DataCulto)
                    .IsRequired();

                entity.Property(e => e.Horario)
                    .IsRequired(false);

                entity.Property(e => e.TipoCulto)
                    .HasConversion<int>()
                    .IsRequired();

                entity.Property(e => e.DataGeracao)
                    .IsRequired()
                    .HasDefaultValueSql("GETDATE()");

                entity.Property(e => e.Observacao)
                    .HasMaxLength(200);

                // Relacionamentos
                entity.HasOne(e => e.Porteiro)
                    .WithMany(p => p.Escalas)
                    .HasForeignKey(e => e.PorteiroId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Porteiro2)
                    .WithMany()
                    .HasForeignKey(e => e.Porteiro2Id)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Responsavel)
                    .WithMany(r => r.Escalas)
                    .HasForeignKey(e => e.ResponsavelId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.UsuarioGeracao)
                    .WithMany()
                    .HasForeignKey(e => e.UsuarioGeracaoId)
                    .OnDelete(DeleteBehavior.SetNull);

                // Índices
                entity.HasIndex(e => new { e.DataCulto, e.PorteiroId })
                    .HasDatabaseName("IX_EscalaPorteiro_Data_Porteiro");

                entity.HasIndex(e => e.DataCulto)
                    .HasDatabaseName("IX_EscalaPorteiro_DataCulto");
            });

            // ========================================
            // 🔒 CONFIGURAÇÕES - MÓDULO DE RATE LIMITING (LGPD)
            // ========================================
            
            // TentativaAcessoTransparencia
            builder.Entity<TentativaAcessoTransparencia>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.CPF)
                    .IsRequired()
                    .HasMaxLength(11);

                entity.Property(e => e.DataHoraTentativa)
                    .IsRequired()
                    .HasDefaultValueSql("GETDATE()");

                entity.Property(e => e.Sucesso)
                    .IsRequired();

                entity.Property(e => e.EnderecoIP)
                    .HasMaxLength(45);

                entity.Property(e => e.UserAgent)
                    .HasMaxLength(500);

                entity.Property(e => e.Mensagem)
                    .HasMaxLength(200);

                // Índice composto para otimizar verificação de rate limiting
                entity.HasIndex(e => new { e.CPF, e.DataHoraTentativa, e.Sucesso })
                    .HasDatabaseName("IX_TentativaAcesso_CPF_Data_Sucesso");

                // Índice para limpeza de registros antigos
                entity.HasIndex(e => e.DataHoraTentativa)
                    .HasDatabaseName("IX_TentativaAcesso_DataHora");
            });
        }
    }
}