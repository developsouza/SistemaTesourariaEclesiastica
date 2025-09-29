using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SistemaTesourariaEclesiastica.Enums;
using SistemaTesourariaEclesiastica.Models;

namespace SistemaTesourariaEclesiastica.Data
{
    public static class SeedData
    {
        public static async Task Initialize(IServiceProvider serviceProvider, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            try
            {
                // Criar roles
                await CreateRoles(roleManager);

                // Criar centro de custo padrão
                using var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
                var sedeCentroCusto = await CreateDefaultCentroCusto(context);

                // Criar usuários padrão
                await CreateDefaultUsers(userManager, sedeCentroCusto.Id);

                // Criar dados básicos
                await CreateBasicData(context);
            }
            catch (Exception ex)
            {
                var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
                logger.LogError(ex, "Erro durante a inicialização dos dados");
                throw;
            }
        }

        private static async Task CreateRoles(RoleManager<IdentityRole> roleManager)
        {
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
                }
            }
        }

        private static async Task<CentroCusto> CreateDefaultCentroCusto(ApplicationDbContext context)
        {
            var sede = await context.CentrosCusto.FirstOrDefaultAsync(c => c.Tipo == TipoCentroCusto.Sede);

            if (sede == null)
            {
                sede = new CentroCusto
                {
                    Nome = "Sede Principal",
                    Tipo = TipoCentroCusto.Sede,
                    Descricao = "Centro de custo principal da igreja",
                    Ativo = true,
                    DataCriacao = DateTime.Now
                };

                context.CentrosCusto.Add(sede);
                await context.SaveChangesAsync();
            }

            return sede;
        }

        private static async Task CreateDefaultUsers(UserManager<ApplicationUser> userManager, int sedeCentroCustoId)
        {
            // Administrador
            if (await userManager.FindByEmailAsync("admin@tesouraria.com") == null)
            {
                var admin = new ApplicationUser
                {
                    UserName = "admin@tesouraria.com",
                    Email = "admin@tesouraria.com",
                    NomeCompleto = "Administrador do Sistema",
                    CentroCustoId = sedeCentroCustoId,
                    Ativo = true,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(admin, "Admin@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(admin, "Administrador");
                }
            }

            // Tesoureiro Geral
            if (await userManager.FindByEmailAsync("tesoureiro@tesouraria.com") == null)
            {
                var tesoureiro = new ApplicationUser
                {
                    UserName = "tesoureiro@tesouraria.com",
                    Email = "tesoureiro@tesouraria.com",
                    NomeCompleto = "Tesoureiro Geral",
                    CentroCustoId = sedeCentroCustoId,
                    Ativo = true,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(tesoureiro, "Tesoureiro@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(tesoureiro, "TesoureiroGeral");
                }
            }

            // Tesoureiro Local
            if (await userManager.FindByEmailAsync("local@tesouraria.com") == null)
            {
                var local = new ApplicationUser
                {
                    UserName = "local@tesouraria.com",
                    Email = "local@tesouraria.com",
                    NomeCompleto = "Tesoureiro Local",
                    CentroCustoId = sedeCentroCustoId,
                    Ativo = true,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(local, "Local@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(local, "TesoureiroLocal");
                }
            }

            // Pastor
            if (await userManager.FindByEmailAsync("pastor@tesouraria.com") == null)
            {
                var pastor = new ApplicationUser
                {
                    UserName = "pastor@tesouraria.com",
                    Email = "pastor@tesouraria.com",
                    NomeCompleto = "Pastor",
                    CentroCustoId = sedeCentroCustoId,
                    Ativo = true,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(pastor, "Pastor@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(pastor, "Pastor");
                }
            }
        }

        private static async Task CreateBasicData(ApplicationDbContext context)
        {
            // Criar planos de contas básicos
            await CreateBasicPlanosContas(context);

            // Criar meios de pagamento básicos
            await CreateBasicMeiosPagamento(context);

            await context.SaveChangesAsync();
        }

        private static async Task CreateBasicPlanosContas(ApplicationDbContext context)
        {
            var planosExistentes = await context.PlanosDeContas.AnyAsync();
            if (planosExistentes) return;

            var planosReceitas = new[]
            {
                new PlanoDeContas { Nome = "Dízimos", Descricao = "Dízimos dos membros", Tipo = TipoPlanoContas.Receita },
                new PlanoDeContas { Nome = "Ofertas", Descricao = "Ofertas especiais e cultos", Tipo = TipoPlanoContas.Receita },
                new PlanoDeContas { Nome = "Eventos", Descricao = "Receitas de eventos e festivais", Tipo = TipoPlanoContas.Receita },
                new PlanoDeContas { Nome = "Doações", Descricao = "Doações diversas", Tipo = TipoPlanoContas.Receita },
                new PlanoDeContas { Nome = "Vendas", Descricao = "Vendas de produtos e materiais", Tipo = TipoPlanoContas.Receita }
            };

            var planosDespesas = new[]
            {
                new PlanoDeContas { Nome = "Salários", Descricao = "Salários e honorários", Tipo = TipoPlanoContas.Despesa },
                new PlanoDeContas { Nome = "Encargos Sociais", Descricao = "INSS, FGTS e outros encargos", Tipo = TipoPlanoContas.Despesa },
                new PlanoDeContas { Nome = "Aluguel", Descricao = "Aluguel e condomínio", Tipo = TipoPlanoContas.Despesa },
                new PlanoDeContas { Nome = "Energia Elétrica", Descricao = "Conta de energia elétrica", Tipo = TipoPlanoContas.Despesa },
                new PlanoDeContas { Nome = "Água e Esgoto", Descricao = "Conta de água e esgoto", Tipo = TipoPlanoContas.Despesa },
                new PlanoDeContas { Nome = "Telefone/Internet", Descricao = "Comunicações", Tipo = TipoPlanoContas.Despesa },
                new PlanoDeContas { Nome = "Material de Limpeza", Descricao = "Produtos de limpeza e higiene", Tipo = TipoPlanoContas.Despesa },
                new PlanoDeContas { Nome = "Material de Escritório", Descricao = "Materiais de escritório e papelaria", Tipo = TipoPlanoContas.Despesa },
                new PlanoDeContas { Nome = "Manutenção", Descricao = "Manutenção e reparos", Tipo = TipoPlanoContas.Despesa },
                new PlanoDeContas { Nome = "Equipamentos", Descricao = "Compra de equipamentos", Tipo = TipoPlanoContas.Despesa }
            };

            context.PlanosDeContas.AddRange(planosReceitas);
            context.PlanosDeContas.AddRange(planosDespesas);
        }

        private static async Task CreateBasicMeiosPagamento(ApplicationDbContext context)
        {
            var meiosExistentes = await context.MeiosDePagamento.AnyAsync();
            if (meiosExistentes) return;

            var meiosPagamento = new[]
            {
                new MeioDePagamento { Nome = "Dinheiro", Descricao = "Pagamento em espécie" },
                new MeioDePagamento { Nome = "PIX", Descricao = "Transferência via PIX" },
                new MeioDePagamento { Nome = "Transferência Bancária", Descricao = "TED/DOC" },
                new MeioDePagamento { Nome = "Cartão de Débito", Descricao = "Pagamento com cartão de débito" },
                new MeioDePagamento { Nome = "Cartão de Crédito", Descricao = "Pagamento com cartão de crédito" },
                new MeioDePagamento { Nome = "Cheque", Descricao = "Pagamento em cheque" }
            };

            context.MeiosDePagamento.AddRange(meiosPagamento);
        }
    }
}