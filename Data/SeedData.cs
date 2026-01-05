using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SistemaTesourariaEclesiastica.Enums;
using SistemaTesourariaEclesiastica.Models;

namespace SistemaTesourariaEclesiastica.Data
{
    /// <summary>
    /// Classe responsável pela inicialização de dados no banco
    /// Inclui: Roles, Usuários, Centros de Custo, Plano de Contas e Meios de Pagamento
    /// </summary>
    public static class SeedData
    {
        public static async Task Initialize(
          IServiceProvider serviceProvider,
            UserManager<ApplicationUser> userManager,
                RoleManager<IdentityRole> roleManager)
        {
            try
            {
                using var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
                var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

                logger.LogInformation("========================================");
                logger.LogInformation("INICIANDO SEED DE DADOS DO SISTEMA");
                logger.LogInformation("========================================");

                // 1. Criar roles
                await CreateRoles(roleManager, logger);

                // 2. Criar centros de custo
                var (sede, fundoRepasse) = await CreateCentrosCusto(context, logger);

                // 3. Criar usuários padrão
                await CreateDefaultUsers(userManager, sede.Id, logger);

                // 4. Criar Plano de Contas
                await CreatePlanoDeContas(context, logger);

                // 5. Criar Meios de Pagamento
                await CreateMeiosDePagamento(context, logger);

                // 6. Criar Regras de Rateio (Dízimo dos Dízimos)
                await CreateRegrasRateio(context, sede.Id, fundoRepasse.Id, logger);

                logger.LogInformation("========================================");
                logger.LogInformation("SEED DE DADOS CONCLUÍDO COM SUCESSO!");
                logger.LogInformation("========================================");
            }
            catch (Exception ex)
            {
                var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
                logger.LogError(ex, "ERRO CRÍTICO durante a inicialização dos dados");
                throw;
            }
        }

        // ==========================================
        // 1. CRIAÇÃO DE ROLES
        // ==========================================
        private static async Task CreateRoles(RoleManager<IdentityRole> roleManager, ILogger logger)
        {
            logger.LogInformation("1. Criando Roles...");

            var roles = new[]
  {
                "Administrador",
                "TesoureiroGeral",
                "TesoureiroLocal",
                "Pastor"
            };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                    logger.LogInformation($"   ✓ Role '{role}' criada");
                }
                else
                {
                    logger.LogInformation($"   ○ Role '{role}' já existe");
                }
            }
        }

        // ==========================================
        // 2. CRIAÇÃO DE CENTROS DE CUSTO
        // ==========================================
        private static async Task<(CentroCusto sede, CentroCusto fundoRepasse)> CreateCentrosCusto(
            ApplicationDbContext context,
            ILogger logger)
        {
            logger.LogInformation("2. Criando Centros de Custo...");

            // Sede
            var sede = await context.CentrosCusto.FirstOrDefaultAsync(c => c.Nome == "Sede");
            if (sede == null)
            {
                sede = new CentroCusto
                {
                    Nome = "Sede",
                    Tipo = TipoCentroCusto.Sede,
                    Descricao = "Centro de custo principal da igreja - Sede da Convenção",
                    Ativo = true,
                    DataCriacao = DateTime.Now
                };
                context.CentrosCusto.Add(sede);
                await context.SaveChangesAsync();
                logger.LogInformation($"   ✓ Centro de Custo 'Sede' criado (ID: {sede.Id})");
            }
            else
            {
                logger.LogInformation($"   ○ Centro de Custo 'Sede' já existe (ID: {sede.Id})");
            }

            // ✅ FUNDO 1: Repasse Templo Central (20%)
            var fundoRepasse = await context.CentrosCusto.FirstOrDefaultAsync(c =>
                c.Nome.Contains("FUNDO - Repasse Templo Central") ||
                c.Nome.Contains("Repasse Templo Central"));

            if (fundoRepasse == null)
            {
                fundoRepasse = new CentroCusto
                {
                    Nome = "FUNDO - Repasse Templo Central",
                    Tipo = TipoCentroCusto.Financeiro, // ✅ CORRIGIDO: Tipo Financeiro
                    Descricao = "Dízimo dos Dízimos (20%) - Valor repassado mensalmente para o Templo Central da Convenção",
                    Ativo = true,
                    DataCriacao = DateTime.Now
                };
                context.CentrosCusto.Add(fundoRepasse);
                await context.SaveChangesAsync();
                logger.LogInformation($"   ✓ Centro de Custo 'FUNDO - Repasse Templo Central' criado (ID: {fundoRepasse.Id}, Tipo: Financeiro)");
            }
            else
            {
                // ✅ Atualizar tipo se já existir mas estiver errado
                if (fundoRepasse.Tipo != TipoCentroCusto.Financeiro)
                {
                    logger.LogInformation($"   ⚠ Corrigindo tipo do 'FUNDO - Repasse Templo Central' de {fundoRepasse.Tipo} para Financeiro");
                    fundoRepasse.Tipo = TipoCentroCusto.Financeiro;
                    context.CentrosCusto.Update(fundoRepasse);
                    await context.SaveChangesAsync();
                    logger.LogInformation($"   ✓ Tipo atualizado para Financeiro (ID: {fundoRepasse.Id})");
                }
                else
                {
                    logger.LogInformation($"   ○ Centro de Custo 'FUNDO - Repasse Templo Central' já existe (ID: {fundoRepasse.Id}, Tipo: Financeiro)");
                }
            }

            // ✅ FUNDO 2: Despesas Administrativas (20%)
            var fundoDespesas = await context.CentrosCusto.FirstOrDefaultAsync(c =>
                c.Nome.Contains("FUNDO - Despesas Administrativas") ||
                c.Nome.Contains("Despesas Administrativas"));

            if (fundoDespesas == null)
            {
                fundoDespesas = new CentroCusto
                {
                    Nome = "FUNDO - Despesas Administrativas",
                    Tipo = TipoCentroCusto.Financeiro, // ✅ Tipo Financeiro
                    Descricao = "Despesas Administrativas (20%) - Fundo destinado ao pagamento das despesas administrativas mensais",
                    Ativo = true,
                    DataCriacao = DateTime.Now
                };
                context.CentrosCusto.Add(fundoDespesas);
                await context.SaveChangesAsync();
                logger.LogInformation($"   ✓ Centro de Custo 'FUNDO - Despesas Administrativas' criado (ID: {fundoDespesas.Id}, Tipo: Financeiro)");
            }
            else
            {
                // ✅ Atualizar tipo se já existir mas estiver errado
                if (fundoDespesas.Tipo != TipoCentroCusto.Financeiro)
                {
                    logger.LogInformation($"   ⚠ Corrigindo tipo do 'FUNDO - Despesas Administrativas' de {fundoDespesas.Tipo} para Financeiro");
                    fundoDespesas.Tipo = TipoCentroCusto.Financeiro;
                    context.CentrosCusto.Update(fundoDespesas);
                    await context.SaveChangesAsync();
                    logger.LogInformation($"   ✓ Tipo atualizado para Financeiro (ID: {fundoDespesas.Id})");
                }
                else
                {
                    logger.LogInformation($"   ○ Centro de Custo 'FUNDO - Despesas Administrativas' já existe (ID: {fundoDespesas.Id}, Tipo: Financeiro)");
                }
            }

            return (sede, fundoRepasse);
        }

        // ==========================================
        // 3. CRIAÇÃO DE USUÁRIOS PADRÃO
        // ==========================================
        private static async Task CreateDefaultUsers(
           UserManager<ApplicationUser> userManager,
                int sedeCentroCustoId,
        ILogger logger)
        {
            logger.LogInformation("3. Criando Usuários Padrão...");

            // Administrador
            var adminEmail = "admin@tesouraria.com";
            if (await userManager.FindByEmailAsync(adminEmail) == null)
            {
                var admin = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    NomeCompleto = "Administrador do Sistema",
                    CentroCustoId = sedeCentroCustoId,
                    Ativo = true,
                    EmailConfirmed = true,
                    DataCriacao = DateTime.Now
                };

                var result = await userManager.CreateAsync(admin, "Admin@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(admin, "Administrador");
                    logger.LogInformation($"   ✓ Usuário 'Administrador' criado (Email: {adminEmail})");
                }
                else
                {
                    logger.LogError($"   ✗ Erro ao criar Administrador: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }
            }
            else
            {
                logger.LogInformation($"   ○ Usuário 'Administrador' já existe");
            }

            // Apenas em desenvolvimento, criar usuários de teste
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            if (environment == "Development")
            {
                await CreateTestUser(userManager, sedeCentroCustoId, "tesoureiro@tesouraria.com",
                "Tesoureiro Geral", "TesoureiroGeral", "Tesoureiro@123", logger);

                await CreateTestUser(userManager, sedeCentroCustoId, "local@tesouraria.com",
                "Tesoureiro Local", "TesoureiroLocal", "Local@123", logger);

                await CreateTestUser(userManager, sedeCentroCustoId, "pastor@tesouraria.com",
                "Pastor da Igreja", "Pastor", "Pastor@123", logger);
            }
        }

        private static async Task CreateTestUser(
         UserManager<ApplicationUser> userManager,
            int centroCustoId,
                        string email,
                   string nomeCompleto,
               string role,
               string senha,
                        ILogger logger)
        {
            if (await userManager.FindByEmailAsync(email) == null)
            {
                var user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    NomeCompleto = nomeCompleto,
                    CentroCustoId = centroCustoId,
                    Ativo = true,
                    EmailConfirmed = true,
                    DataCriacao = DateTime.Now
                };

                var result = await userManager.CreateAsync(user, senha);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, role);
                    logger.LogInformation($"   ✓ Usuário de teste '{nomeCompleto}' criado (Email: {email})");
                }
            }
        }

        // ==========================================
        // 4. CRIAÇÃO DO PLANO DE CONTAS
        // ==========================================
        private static async Task CreatePlanoDeContas(ApplicationDbContext context, ILogger logger)
        {
            logger.LogInformation("4. Criando Plano de Contas...");

            // ==========================================
            // 4.1. RECEITAS (Tipo = 0)
            // ==========================================
            logger.LogInformation("   4.1. Fontes de Renda (Receitas)...");

            var receitas = new[]
            {
                new { Nome = "Dízimos", Descricao = "Dízimos e ofertas do Templo Central" },
                new { Nome = "Ofertas", Descricao = "Ofertas diversas" },
                new { Nome = "Votos", Descricao = "Votos e promessas" },
                new { Nome = "Ofertas (Círculo de Oração)", Descricao = "Ofertas do Círculo de Oração (Adultos e Mocidade)" },
                new { Nome = "Repasse de Congregação", Descricao = "Repasses financeiros das congregações para a Sede" }
            };

            foreach (var receita in receitas)
            {
                if (!await context.PlanosDeContas.AnyAsync(p => p.Nome == receita.Nome && p.Tipo == TipoPlanoContas.Receita))
                {
                    context.PlanosDeContas.Add(new PlanoDeContas
                    {
                        Nome = receita.Nome,
                        Descricao = receita.Descricao,
                        Tipo = TipoPlanoContas.Receita,
                        Ativo = true,
                        DataCriacao = DateTime.Now
                    });
                    logger.LogInformation($"      ✓ Receita '{receita.Nome}' criada");
                }
            }

            await context.SaveChangesAsync();

            // ==========================================
            // 4.2. DESPESAS (Tipo = 1)
            // ==========================================
            logger.LogInformation("   4.2. Categorias de Despesa...");

            var despesas = new[]
              {
       // Despesas Administrativas
        new { Nome = "Mat. de Expediente", Descricao = "Material de Expediente" },
        new { Nome = "Mat. Higiene e Limpeza", Descricao = "Material de Higiene e Limpeza" },
        new { Nome = "Despesas com Telefone", Descricao = "Despesas com Telefone" },
        new { Nome = "Despesas com Veículo", Descricao = "Despesas com Veículo" },
        new { Nome = "Auxílio Oferta", Descricao = "Auxílio Oferta" },
        new { Nome = "Mão de Obra Qualificada", Descricao = "Mão de Obra Qualificada" },
        new { Nome = "Despesas com Medicamentos", Descricao = "Despesas com Medicamentos" },
        new { Nome = "Energia Elétrica (Luz)", Descricao = "Energia Elétrica (Luz)" },
        new { Nome = "Água", Descricao = "Água" },
        new { Nome = "Despesas Diversas", Descricao = "Despesas Diversas" },
        new { Nome = "Despesas com Viagens", Descricao = "Despesas com Viagens" },
        new { Nome = "Material de Construção", Descricao = "Material de Construção" },
        new { Nome = "Material de Conservação (Tintas, etc.)", Descricao = "Material de Conservação (Tintas, etc.)" },
        new { Nome = "Despesas com Som (Peças e Acessórios)", Descricao = "Despesas com Som (Peças e Acessórios)" },
        new { Nome = "Aluguel", Descricao = "Aluguel" },
        new { Nome = "INSS", Descricao = "INSS" },
        new { Nome = "Pagamento de Inscrição da CGADB", Descricao = "Pagamento de Inscrição da CGADB" },
        new { Nome = "Previdência Privada", Descricao = "Previdência Privada" },
        
    // Despesas Tributárias
        new { Nome = "IPTU", Descricao = "IPTU" },
        new { Nome = "Imposto Predial IPTR", Descricao = "Imposto Predial IPTR" },
 
                // Despesas Financeiras
        new { Nome = "Imposto Taxas Diversas", Descricao = "Imposto Taxas Diversas" },
        new { Nome = "Saldo para o mês", Descricao = "Saldo para o mês" }
        };

            foreach (var despesa in despesas)
            {
                if (!await context.PlanosDeContas.AnyAsync(p => p.Nome == despesa.Nome && p.Tipo == TipoPlanoContas.Despesa))
                {
                    context.PlanosDeContas.Add(new PlanoDeContas
                    {
                        Nome = despesa.Nome,
                        Descricao = despesa.Descricao,
                        Tipo = TipoPlanoContas.Despesa,
                        Ativo = true,
                        DataCriacao = DateTime.Now
                    });
                    logger.LogInformation($"      ✓ Despesa '{despesa.Nome}' criada");
                }
            }

            await context.SaveChangesAsync();
            logger.LogInformation($"   ✓ Plano de Contas completo criado");
        }

        // ==========================================
        // 5. CRIAÇÃO DE MEIOS DE PAGAMENTO
        // ==========================================
        private static async Task CreateMeiosDePagamento(ApplicationDbContext context, ILogger logger)
        {
            logger.LogInformation("5. Criando Meios de Pagamento...");

            var meiosDePagamento = new[]
            {
                new { Nome = "Dinheiro", Descricao = "Pagamento em dinheiro (espécie)", TipoCaixa = TipoCaixa.Fisico },
                new { Nome = "PIX", Descricao = "Pagamento via PIX", TipoCaixa = TipoCaixa.Digital },
                new { Nome = "Transferência Bancária", Descricao = "Transferência bancária", TipoCaixa = TipoCaixa.Digital },
                new { Nome = "Débito", Descricao = "Cartão de Débito", TipoCaixa = TipoCaixa.Digital },
                new { Nome = "Crédito", Descricao = "Cartão de Crédito", TipoCaixa = TipoCaixa.Digital },
                new { Nome = "Boleto", Descricao = "Pagamento via Boleto Bancário", TipoCaixa = TipoCaixa.Digital },
                new { Nome = "Cheque", Descricao = "Pagamento em Cheque", TipoCaixa = TipoCaixa.Fisico }
            };

            foreach (var meio in meiosDePagamento)
            {
                if (!await context.MeiosDePagamento.AnyAsync(m => m.Nome == meio.Nome))
                {
                    context.MeiosDePagamento.Add(new MeioDePagamento
                    {
                        Nome = meio.Nome,
                        Descricao = meio.Descricao,
                        TipoCaixa = meio.TipoCaixa,
                        Ativo = true
                    });
                    logger.LogInformation($"   ✓ Meio de Pagamento '{meio.Nome}' criado ({meio.TipoCaixa})");
                }
            }

            await context.SaveChangesAsync();
        }

        // ==========================================
        // 6. CRIAÇÃO DE REGRAS DE RATEIO (DÍZIMO DOS DÍZIMOS + DESPESAS ADMINISTRATIVAS)
        // ==========================================
        private static async Task CreateRegrasRateio(
          ApplicationDbContext context,
            int sedeId,
                  int fundoRepasseId,
             ILogger logger)
        {
            logger.LogInformation("6. Criando Regras de Rateio...");

            // ✅ REGRA 1: Dízimo dos Dízimos (SEDE → FUNDO Repasse Templo Central - 20%)
            var regraRepasse = await context.RegrasRateio
                .FirstOrDefaultAsync(r =>
                    r.CentroCustoOrigemId == sedeId &&
                    r.CentroCustoDestinoId == fundoRepasseId &&
                    r.Ativo);

            if (regraRepasse == null)
            {
                regraRepasse = new RegraRateio
                {
                    Nome = "Dízimo dos Dízimos (20%)",
                    Descricao = "Repasse mensal de 20% das receitas da Sede Local para o Templo Central da Convenção",
                    CentroCustoOrigemId = sedeId,
                    CentroCustoDestinoId = fundoRepasseId,
                    Percentual = 20.00m, // 20%
                    Ativo = true,
                    DataCriacao = DateTime.Now
                };

                context.RegrasRateio.Add(regraRepasse);
                await context.SaveChangesAsync();

                logger.LogInformation($"   ✓ Regra de Rateio 'Dízimo dos Dízimos (20%)' criada:");
                logger.LogInformation($"      - Origem: Sede Local (ID: {sedeId})");
                logger.LogInformation($"      - Destino: FUNDO - Repasse Templo Central (ID: {fundoRepasseId})");
                logger.LogInformation($"      - Percentual: 20.00%");
            }
            else
            {
                logger.LogInformation($"   ○ Regra 'Dízimo dos Dízimos' já existe (ID: {regraRepasse.Id}, Percentual: {regraRepasse.Percentual:F2}%)");
            }

            // ✅ REGRA 2: Despesas Administrativas (SEDE → FUNDO Despesas Administrativas - 20%)
            var fundoDespesas = await context.CentrosCusto.FirstOrDefaultAsync(c =>
                c.Nome.Contains("FUNDO - Despesas Administrativas") ||
                c.Nome.Contains("Despesas Administrativas"));

            if (fundoDespesas != null)
            {
                var regraDespesas = await context.RegrasRateio
                    .FirstOrDefaultAsync(r =>
                        r.CentroCustoOrigemId == sedeId &&
                        r.CentroCustoDestinoId == fundoDespesas.Id &&
                        r.Ativo);

                if (regraDespesas == null)
                {
                    regraDespesas = new RegraRateio
                    {
                        Nome = "Despesas Administrativas (20%)",
                        Descricao = "Rateio mensal de 20% das receitas da Sede para o Fundo de Despesas Administrativas",
                        CentroCustoOrigemId = sedeId,
                        CentroCustoDestinoId = fundoDespesas.Id,
                        Percentual = 20.00m, // 20%
                        Ativo = true,
                        DataCriacao = DateTime.Now
                    };

                    context.RegrasRateio.Add(regraDespesas);
                    await context.SaveChangesAsync();

                    logger.LogInformation($"   ✓ Regra de Rateio 'Despesas Administrativas (20%)' criada:");
                    logger.LogInformation($"      - Origem: Sede Local (ID: {sedeId})");
                    logger.LogInformation($"      - Destino: FUNDO - Despesas Administrativas (ID: {fundoDespesas.Id})");
                    logger.LogInformation($"      - Percentual: 20.00%");
                }
                else
                {
                    logger.LogInformation($"   ○ Regra 'Despesas Administrativas' já existe (ID: {regraDespesas.Id}, Percentual: {regraDespesas.Percentual:F2}%)");
                }
            }
            else
            {
                logger.LogWarning("   ⚠ FUNDO - Despesas Administrativas não encontrado. Regra de rateio não criada.");
            }

            logger.LogInformation($"   ✓ Total de rateio configurado: 40% (20% Repasse + 20% Despesas)");
        }
    }
}