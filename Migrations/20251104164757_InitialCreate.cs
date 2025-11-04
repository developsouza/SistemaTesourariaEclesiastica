using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaTesourariaEclesiastica.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CentrosCusto",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nome = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Tipo = table.Column<int>(type: "int", nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Ativo = table.Column<bool>(type: "bit", nullable: false),
                    DataCriacao = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CentrosCusto", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Emprestimos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DataEmprestimo = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    ValorTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Justificativa = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    DataQuitacao = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Emprestimos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Fornecedores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nome = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CNPJ = table.Column<string>(type: "nvarchar(18)", maxLength: 18, nullable: true),
                    CPF = table.Column<string>(type: "nvarchar(14)", maxLength: 14, nullable: true),
                    Telefone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Endereco = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Cidade = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Estado = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: true),
                    CEP = table.Column<string>(type: "nvarchar(9)", maxLength: 9, nullable: true),
                    Observacoes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Ativo = table.Column<bool>(type: "bit", nullable: false),
                    DataCadastro = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Fornecedores", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MeiosDePagamento",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nome = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    TipoCaixa = table.Column<int>(type: "int", nullable: false),
                    Ativo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeiosDePagamento", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ModelosRateioEntrada",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nome = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PercentualSede = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    PercentualCongregacao = table.Column<decimal>(type: "decimal(5,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModelosRateioEntrada", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlanosDeContas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nome = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Tipo = table.Column<int>(type: "int", nullable: false),
                    Ativo = table.Column<bool>(type: "bit", nullable: false),
                    DataCriacao = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanosDeContas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    NomeCompleto = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CentroCustoId = table.Column<int>(type: "int", nullable: true),
                    Ativo = table.Column<bool>(type: "bit", nullable: false),
                    DataCriacao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UltimoAcesso = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUsers_CentrosCusto_CentroCustoId",
                        column: x => x.CentroCustoId,
                        principalTable: "CentrosCusto",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ContasBancarias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Banco = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Agencia = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Conta = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CentroCustoId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContasBancarias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContasBancarias_CentrosCusto_CentroCustoId",
                        column: x => x.CentroCustoId,
                        principalTable: "CentrosCusto",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Membros",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NomeCompleto = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Apelido = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CPF = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CentroCustoId = table.Column<int>(type: "int", nullable: false),
                    DataCadastro = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Ativo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Membros", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Membros_CentrosCusto_CentroCustoId",
                        column: x => x.CentroCustoId,
                        principalTable: "CentrosCusto",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RegrasRateio",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nome = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    CentroCustoOrigemId = table.Column<int>(type: "int", nullable: false),
                    CentroCustoDestinoId = table.Column<int>(type: "int", nullable: false),
                    Percentual = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    Ativo = table.Column<bool>(type: "bit", nullable: false),
                    DataCriacao = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegrasRateio", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RegrasRateio_CentrosCusto_CentroCustoDestinoId",
                        column: x => x.CentroCustoDestinoId,
                        principalTable: "CentrosCusto",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RegrasRateio_CentrosCusto_CentroCustoOrigemId",
                        column: x => x.CentroCustoOrigemId,
                        principalTable: "CentrosCusto",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DevolucaoEmprestimos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmprestimoId = table.Column<int>(type: "int", nullable: false),
                    DataDevolucao = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    ValorDevolvido = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Observacoes = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DevolucaoEmprestimos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DevolucaoEmprestimos_Emprestimos_EmprestimoId",
                        column: x => x.EmprestimoId,
                        principalTable: "Emprestimos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DespesasRecorrentes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nome = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ValorPadrao = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Periodicidade = table.Column<int>(type: "int", nullable: false),
                    CentroCustoId = table.Column<int>(type: "int", nullable: false),
                    PlanoDeContasId = table.Column<int>(type: "int", nullable: false),
                    FornecedorId = table.Column<int>(type: "int", nullable: true),
                    MeioDePagamentoId = table.Column<int>(type: "int", nullable: true),
                    DiaVencimento = table.Column<int>(type: "int", nullable: true),
                    Ativa = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    DataCadastro = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    DataInicio = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DataTermino = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Observacoes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DespesasRecorrentes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DespesasRecorrentes_CentrosCusto_CentroCustoId",
                        column: x => x.CentroCustoId,
                        principalTable: "CentrosCusto",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DespesasRecorrentes_Fornecedores_FornecedorId",
                        column: x => x.FornecedorId,
                        principalTable: "Fornecedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_DespesasRecorrentes_MeiosDePagamento_MeioDePagamentoId",
                        column: x => x.MeioDePagamentoId,
                        principalTable: "MeiosDePagamento",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_DespesasRecorrentes_PlanosDeContas_PlanoDeContasId",
                        column: x => x.PlanoDeContasId,
                        principalTable: "PlanosDeContas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FechamentosPeriodo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CentroCustoId = table.Column<int>(type: "int", nullable: false),
                    TipoFechamento = table.Column<int>(type: "int", nullable: false),
                    Ano = table.Column<int>(type: "int", nullable: true),
                    Mes = table.Column<int>(type: "int", nullable: true),
                    DataInicio = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DataFim = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TotalEntradas = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m),
                    TotalSaidas = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m),
                    TotalEntradasFisicas = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m),
                    TotalSaidasFisicas = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m),
                    TotalEntradasDigitais = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m),
                    TotalSaidasDigitais = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m),
                    BalancoDigital = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m),
                    BalancoFisico = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m),
                    TotalRateios = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m),
                    SaldoFinal = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m),
                    Observacoes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    DataSubmissao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DataAprovacao = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UsuarioSubmissaoId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    UsuarioAprovacaoId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    EhFechamentoSede = table.Column<bool>(type: "bit", nullable: false),
                    QuantidadeCongregacoesIncluidas = table.Column<int>(type: "int", nullable: false),
                    FoiProcessadoPelaSede = table.Column<bool>(type: "bit", nullable: false),
                    FechamentoSedeProcessadorId = table.Column<int>(type: "int", nullable: true),
                    DataProcessamentoPelaSede = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FechamentosPeriodo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FechamentosPeriodo_AspNetUsers_UsuarioAprovacaoId",
                        column: x => x.UsuarioAprovacaoId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_FechamentosPeriodo_AspNetUsers_UsuarioSubmissaoId",
                        column: x => x.UsuarioSubmissaoId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FechamentosPeriodo_CentrosCusto_CentroCustoId",
                        column: x => x.CentroCustoId,
                        principalTable: "CentrosCusto",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FechamentosPeriodo_FechamentosPeriodo_FechamentoSedeProcessadorId",
                        column: x => x.FechamentoSedeProcessadorId,
                        principalTable: "FechamentosPeriodo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LogsAuditoria",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UsuarioId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Acao = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Entidade = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntidadeId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DataHora = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Detalhes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EnderecoIP = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogsAuditoria", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LogsAuditoria_AspNetUsers_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TransferenciasInternas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Data = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Valor = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    MeioDePagamentoOrigemId = table.Column<int>(type: "int", nullable: false),
                    MeioDePagamentoDestinoId = table.Column<int>(type: "int", nullable: false),
                    CentroCustoOrigemId = table.Column<int>(type: "int", nullable: false),
                    CentroCustoDestinoId = table.Column<int>(type: "int", nullable: false),
                    Quitada = table.Column<bool>(type: "bit", nullable: false),
                    UsuarioId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DataCriacao = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransferenciasInternas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TransferenciasInternas_AspNetUsers_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TransferenciasInternas_CentrosCusto_CentroCustoDestinoId",
                        column: x => x.CentroCustoDestinoId,
                        principalTable: "CentrosCusto",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TransferenciasInternas_CentrosCusto_CentroCustoOrigemId",
                        column: x => x.CentroCustoOrigemId,
                        principalTable: "CentrosCusto",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TransferenciasInternas_MeiosDePagamento_MeioDePagamentoDestinoId",
                        column: x => x.MeioDePagamentoDestinoId,
                        principalTable: "MeiosDePagamento",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TransferenciasInternas_MeiosDePagamento_MeioDePagamentoOrigemId",
                        column: x => x.MeioDePagamentoOrigemId,
                        principalTable: "MeiosDePagamento",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DetalhesFechamento",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FechamentoPeriodoId = table.Column<int>(type: "int", nullable: false),
                    TipoMovimento = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Valor = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Data = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PlanoContas = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    MeioPagamento = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Membro = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Fornecedor = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Observacoes = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DetalhesFechamento", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DetalhesFechamento_FechamentosPeriodo_FechamentoPeriodoId",
                        column: x => x.FechamentoPeriodoId,
                        principalTable: "FechamentosPeriodo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Entradas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Data = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Valor = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Observacoes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    MeioDePagamentoId = table.Column<int>(type: "int", nullable: false),
                    CentroCustoId = table.Column<int>(type: "int", nullable: false),
                    PlanoDeContasId = table.Column<int>(type: "int", nullable: false),
                    MembroId = table.Column<int>(type: "int", nullable: true),
                    ModeloRateioEntradaId = table.Column<int>(type: "int", nullable: true),
                    ComprovanteUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UsuarioId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DataCriacao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IncluidaEmFechamento = table.Column<bool>(type: "bit", nullable: false),
                    FechamentoQueIncluiuId = table.Column<int>(type: "int", nullable: true),
                    DataInclusaoFechamento = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Entradas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Entradas_AspNetUsers_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Entradas_CentrosCusto_CentroCustoId",
                        column: x => x.CentroCustoId,
                        principalTable: "CentrosCusto",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Entradas_FechamentosPeriodo_FechamentoQueIncluiuId",
                        column: x => x.FechamentoQueIncluiuId,
                        principalTable: "FechamentosPeriodo",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Entradas_MeiosDePagamento_MeioDePagamentoId",
                        column: x => x.MeioDePagamentoId,
                        principalTable: "MeiosDePagamento",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Entradas_Membros_MembroId",
                        column: x => x.MembroId,
                        principalTable: "Membros",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Entradas_ModelosRateioEntrada_ModeloRateioEntradaId",
                        column: x => x.ModeloRateioEntradaId,
                        principalTable: "ModelosRateioEntrada",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Entradas_PlanosDeContas_PlanoDeContasId",
                        column: x => x.PlanoDeContasId,
                        principalTable: "PlanosDeContas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ItensRateioFechamento",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FechamentoPeriodoId = table.Column<int>(type: "int", nullable: false),
                    RegraRateioId = table.Column<int>(type: "int", nullable: false),
                    ValorBase = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Percentual = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    ValorRateio = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Observacoes = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    DataCriacao = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItensRateioFechamento", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItensRateioFechamento_FechamentosPeriodo_FechamentoPeriodoId",
                        column: x => x.FechamentoPeriodoId,
                        principalTable: "FechamentosPeriodo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ItensRateioFechamento_RegrasRateio_RegraRateioId",
                        column: x => x.RegraRateioId,
                        principalTable: "RegrasRateio",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Saidas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Data = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Valor = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Observacoes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    MeioDePagamentoId = table.Column<int>(type: "int", nullable: false),
                    CentroCustoId = table.Column<int>(type: "int", nullable: false),
                    PlanoDeContasId = table.Column<int>(type: "int", nullable: false),
                    FornecedorId = table.Column<int>(type: "int", nullable: true),
                    TipoDespesa = table.Column<int>(type: "int", nullable: false),
                    NumeroDocumento = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DataVencimento = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ComprovanteUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UsuarioId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DataCriacao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IncluidaEmFechamento = table.Column<bool>(type: "bit", nullable: false),
                    FechamentoQueIncluiuId = table.Column<int>(type: "int", nullable: true),
                    DataInclusaoFechamento = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Saidas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Saidas_AspNetUsers_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Saidas_CentrosCusto_CentroCustoId",
                        column: x => x.CentroCustoId,
                        principalTable: "CentrosCusto",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Saidas_FechamentosPeriodo_FechamentoQueIncluiuId",
                        column: x => x.FechamentoQueIncluiuId,
                        principalTable: "FechamentosPeriodo",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Saidas_Fornecedores_FornecedorId",
                        column: x => x.FornecedorId,
                        principalTable: "Fornecedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Saidas_MeiosDePagamento_MeioDePagamentoId",
                        column: x => x.MeioDePagamentoId,
                        principalTable: "MeiosDePagamento",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Saidas_PlanosDeContas_PlanoDeContasId",
                        column: x => x.PlanoDeContasId,
                        principalTable: "PlanosDeContas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PagamentosDespesasRecorrentes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DespesaRecorrenteId = table.Column<int>(type: "int", nullable: false),
                    DataVencimento = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DataPagamento = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ValorPrevisto = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ValorPago = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Pago = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    SaidaGerada = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    SaidaId = table.Column<int>(type: "int", nullable: true),
                    Observacoes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DataRegistro = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PagamentosDespesasRecorrentes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PagamentosDespesasRecorrentes_DespesasRecorrentes_DespesaRecorrenteId",
                        column: x => x.DespesaRecorrenteId,
                        principalTable: "DespesasRecorrentes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PagamentosDespesasRecorrentes_Saidas_SaidaId",
                        column: x => x.SaidaId,
                        principalTable: "Saidas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_CentroCustoId",
                table: "AspNetUsers",
                column: "CentroCustoId");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CentrosCusto_Nome",
                table: "CentrosCusto",
                column: "Nome",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContasBancarias_CentroCustoId",
                table: "ContasBancarias",
                column: "CentroCustoId");

            migrationBuilder.CreateIndex(
                name: "IX_DespesasRecorrentes_Ativa",
                table: "DespesasRecorrentes",
                column: "Ativa");

            migrationBuilder.CreateIndex(
                name: "IX_DespesasRecorrentes_CentroCusto_Ativa",
                table: "DespesasRecorrentes",
                columns: new[] { "CentroCustoId", "Ativa" });

            migrationBuilder.CreateIndex(
                name: "IX_DespesasRecorrentes_FornecedorId",
                table: "DespesasRecorrentes",
                column: "FornecedorId");

            migrationBuilder.CreateIndex(
                name: "IX_DespesasRecorrentes_MeioDePagamentoId",
                table: "DespesasRecorrentes",
                column: "MeioDePagamentoId");

            migrationBuilder.CreateIndex(
                name: "IX_DespesasRecorrentes_PlanoDeContasId",
                table: "DespesasRecorrentes",
                column: "PlanoDeContasId");

            migrationBuilder.CreateIndex(
                name: "IX_DetalhesFechamento_FechamentoPeriodoId",
                table: "DetalhesFechamento",
                column: "FechamentoPeriodoId");

            migrationBuilder.CreateIndex(
                name: "IX_DevolucaoEmprestimos_EmprestimoId",
                table: "DevolucaoEmprestimos",
                column: "EmprestimoId");

            migrationBuilder.CreateIndex(
                name: "IX_Emprestimos_DataEmprestimo",
                table: "Emprestimos",
                column: "DataEmprestimo",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_Emprestimos_Status",
                table: "Emprestimos",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Emprestimos_Status_DataEmprestimo",
                table: "Emprestimos",
                columns: new[] { "Status", "DataEmprestimo" });

            migrationBuilder.CreateIndex(
                name: "IX_Entradas_CentroCustoId",
                table: "Entradas",
                column: "CentroCustoId");

            migrationBuilder.CreateIndex(
                name: "IX_Entradas_FechamentoQueIncluiuId",
                table: "Entradas",
                column: "FechamentoQueIncluiuId");

            migrationBuilder.CreateIndex(
                name: "IX_Entradas_MeioDePagamentoId",
                table: "Entradas",
                column: "MeioDePagamentoId");

            migrationBuilder.CreateIndex(
                name: "IX_Entradas_MembroId",
                table: "Entradas",
                column: "MembroId");

            migrationBuilder.CreateIndex(
                name: "IX_Entradas_ModeloRateioEntradaId",
                table: "Entradas",
                column: "ModeloRateioEntradaId");

            migrationBuilder.CreateIndex(
                name: "IX_Entradas_PlanoDeContasId",
                table: "Entradas",
                column: "PlanoDeContasId");

            migrationBuilder.CreateIndex(
                name: "IX_Entradas_UsuarioId",
                table: "Entradas",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_FechamentosPeriodo_CentroCustoId",
                table: "FechamentosPeriodo",
                column: "CentroCustoId");

            migrationBuilder.CreateIndex(
                name: "IX_FechamentosPeriodo_FechamentoSedeProcessadorId",
                table: "FechamentosPeriodo",
                column: "FechamentoSedeProcessadorId");

            migrationBuilder.CreateIndex(
                name: "IX_FechamentosPeriodo_UsuarioAprovacaoId",
                table: "FechamentosPeriodo",
                column: "UsuarioAprovacaoId");

            migrationBuilder.CreateIndex(
                name: "IX_FechamentosPeriodo_UsuarioSubmissaoId",
                table: "FechamentosPeriodo",
                column: "UsuarioSubmissaoId");

            migrationBuilder.CreateIndex(
                name: "IX_Fornecedores_Nome",
                table: "Fornecedores",
                column: "Nome",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ItensRateioFechamento_FechamentoPeriodoId",
                table: "ItensRateioFechamento",
                column: "FechamentoPeriodoId");

            migrationBuilder.CreateIndex(
                name: "IX_ItensRateioFechamento_RegraRateioId",
                table: "ItensRateioFechamento",
                column: "RegraRateioId");

            migrationBuilder.CreateIndex(
                name: "IX_LogsAuditoria_UsuarioId",
                table: "LogsAuditoria",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_MeiosDePagamento_Nome",
                table: "MeiosDePagamento",
                column: "Nome",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Membros_CentroCustoId",
                table: "Membros",
                column: "CentroCustoId");

            migrationBuilder.CreateIndex(
                name: "IX_Membros_CPF",
                table: "Membros",
                column: "CPF",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ModelosRateioEntrada_Nome",
                table: "ModelosRateioEntrada",
                column: "Nome",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PagamentosDespesas_Despesa_Vencimento",
                table: "PagamentosDespesasRecorrentes",
                columns: new[] { "DespesaRecorrenteId", "DataVencimento" });

            migrationBuilder.CreateIndex(
                name: "IX_PagamentosDespesas_Pago",
                table: "PagamentosDespesasRecorrentes",
                column: "Pago");

            migrationBuilder.CreateIndex(
                name: "IX_PagamentosDespesasRecorrentes_SaidaId",
                table: "PagamentosDespesasRecorrentes",
                column: "SaidaId");

            migrationBuilder.CreateIndex(
                name: "IX_PlanosDeContas_Descricao",
                table: "PlanosDeContas",
                column: "Descricao",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RegrasRateio_CentroCustoDestinoId",
                table: "RegrasRateio",
                column: "CentroCustoDestinoId");

            migrationBuilder.CreateIndex(
                name: "IX_RegrasRateio_CentroCustoOrigemId",
                table: "RegrasRateio",
                column: "CentroCustoOrigemId");

            migrationBuilder.CreateIndex(
                name: "IX_Saidas_CentroCustoId",
                table: "Saidas",
                column: "CentroCustoId");

            migrationBuilder.CreateIndex(
                name: "IX_Saidas_FechamentoQueIncluiuId",
                table: "Saidas",
                column: "FechamentoQueIncluiuId");

            migrationBuilder.CreateIndex(
                name: "IX_Saidas_FornecedorId",
                table: "Saidas",
                column: "FornecedorId");

            migrationBuilder.CreateIndex(
                name: "IX_Saidas_MeioDePagamentoId",
                table: "Saidas",
                column: "MeioDePagamentoId");

            migrationBuilder.CreateIndex(
                name: "IX_Saidas_PlanoDeContasId",
                table: "Saidas",
                column: "PlanoDeContasId");

            migrationBuilder.CreateIndex(
                name: "IX_Saidas_UsuarioId",
                table: "Saidas",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_TransferenciasInternas_CentroCustoDestinoId",
                table: "TransferenciasInternas",
                column: "CentroCustoDestinoId");

            migrationBuilder.CreateIndex(
                name: "IX_TransferenciasInternas_CentroCustoOrigemId",
                table: "TransferenciasInternas",
                column: "CentroCustoOrigemId");

            migrationBuilder.CreateIndex(
                name: "IX_TransferenciasInternas_MeioDePagamentoDestinoId",
                table: "TransferenciasInternas",
                column: "MeioDePagamentoDestinoId");

            migrationBuilder.CreateIndex(
                name: "IX_TransferenciasInternas_MeioDePagamentoOrigemId",
                table: "TransferenciasInternas",
                column: "MeioDePagamentoOrigemId");

            migrationBuilder.CreateIndex(
                name: "IX_TransferenciasInternas_UsuarioId",
                table: "TransferenciasInternas",
                column: "UsuarioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "ContasBancarias");

            migrationBuilder.DropTable(
                name: "DetalhesFechamento");

            migrationBuilder.DropTable(
                name: "DevolucaoEmprestimos");

            migrationBuilder.DropTable(
                name: "Entradas");

            migrationBuilder.DropTable(
                name: "ItensRateioFechamento");

            migrationBuilder.DropTable(
                name: "LogsAuditoria");

            migrationBuilder.DropTable(
                name: "PagamentosDespesasRecorrentes");

            migrationBuilder.DropTable(
                name: "TransferenciasInternas");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "Emprestimos");

            migrationBuilder.DropTable(
                name: "Membros");

            migrationBuilder.DropTable(
                name: "ModelosRateioEntrada");

            migrationBuilder.DropTable(
                name: "RegrasRateio");

            migrationBuilder.DropTable(
                name: "DespesasRecorrentes");

            migrationBuilder.DropTable(
                name: "Saidas");

            migrationBuilder.DropTable(
                name: "FechamentosPeriodo");

            migrationBuilder.DropTable(
                name: "Fornecedores");

            migrationBuilder.DropTable(
                name: "MeiosDePagamento");

            migrationBuilder.DropTable(
                name: "PlanosDeContas");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "CentrosCusto");
        }
    }
}
