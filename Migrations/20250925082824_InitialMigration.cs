using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaTesourariaEclesiastica.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CentrosCusto",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nome = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Tipo = table.Column<int>(type: "INTEGER", nullable: false),
                    Descricao = table.Column<string>(type: "TEXT", maxLength: 250, nullable: true),
                    Ativo = table.Column<bool>(type: "INTEGER", nullable: false),
                    DataCriacao = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CentrosCusto", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Fornecedores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nome = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CNPJ = table.Column<string>(type: "TEXT", maxLength: 18, nullable: true),
                    CPF = table.Column<string>(type: "TEXT", maxLength: 14, nullable: true),
                    Telefone = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Endereco = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Cidade = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Estado = table.Column<string>(type: "TEXT", maxLength: 2, nullable: true),
                    CEP = table.Column<string>(type: "TEXT", maxLength: 9, nullable: true),
                    Observacoes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Ativo = table.Column<bool>(type: "INTEGER", nullable: false),
                    DataCadastro = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Fornecedores", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MeiosDePagamento",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nome = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Descricao = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Ativo = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeiosDePagamento", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ModelosRateioEntrada",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nome = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
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
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Codigo = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Nome = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Descricao = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Tipo = table.Column<int>(type: "INTEGER", nullable: false),
                    Ativo = table.Column<bool>(type: "INTEGER", nullable: false),
                    DataCriacao = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanosDeContas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RoleId = table.Column<string>(type: "TEXT", nullable: false),
                    ClaimType = table.Column<string>(type: "TEXT", nullable: true),
                    ClaimValue = table.Column<string>(type: "TEXT", nullable: true)
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
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    NomeCompleto = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CentroCustoId = table.Column<int>(type: "INTEGER", nullable: true),
                    Ativo = table.Column<bool>(type: "INTEGER", nullable: false),
                    DataCriacao = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "INTEGER", nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", nullable: true),
                    SecurityStamp = table.Column<string>(type: "TEXT", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "TEXT", nullable: true),
                    PhoneNumber = table.Column<string>(type: "TEXT", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "INTEGER", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "INTEGER", nullable: false)
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
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Banco = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Agencia = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Conta = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    CentroCustoId = table.Column<int>(type: "INTEGER", nullable: false)
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
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NomeCompleto = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Apelido = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    CPF = table.Column<string>(type: "TEXT", nullable: false),
                    Telefone = table.Column<string>(type: "TEXT", maxLength: 15, nullable: true),
                    Email = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    DataNascimento = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Endereco = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Ativo = table.Column<bool>(type: "INTEGER", nullable: false),
                    RG = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    CEP = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                    Numero = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                    Complemento = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Bairro = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Cidade = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    UF = table.Column<string>(type: "TEXT", maxLength: 2, nullable: true),
                    DataBatismo = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DataCadastro = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CentroCustoId = table.Column<int>(type: "INTEGER", nullable: false)
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
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nome = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Descricao = table.Column<string>(type: "TEXT", maxLength: 250, nullable: true),
                    CentroCustoOrigemId = table.Column<int>(type: "INTEGER", nullable: false),
                    CentroCustoDestinoId = table.Column<int>(type: "INTEGER", nullable: false),
                    Percentual = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    Ativo = table.Column<bool>(type: "INTEGER", nullable: false),
                    DataCriacao = table.Column<DateTime>(type: "TEXT", nullable: false)
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
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    ClaimType = table.Column<string>(type: "TEXT", nullable: true),
                    ClaimValue = table.Column<string>(type: "TEXT", nullable: true)
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
                    LoginProvider = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    ProviderKey = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "TEXT", nullable: true),
                    UserId = table.Column<string>(type: "TEXT", nullable: false)
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
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    RoleId = table.Column<string>(type: "TEXT", nullable: false)
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
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    LoginProvider = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: true)
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
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CentroCustoId = table.Column<int>(type: "INTEGER", nullable: false),
                    Ano = table.Column<int>(type: "INTEGER", nullable: false),
                    Mes = table.Column<int>(type: "INTEGER", nullable: false),
                    DataInicio = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DataFim = table.Column<DateTime>(type: "TEXT", nullable: false),
                    BalancoDigital = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BalancoFisico = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalEntradas = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalSaidas = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalRateios = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SaldoFinal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Observacoes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    DataSubmissao = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DataAprovacao = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UsuarioSubmissaoId = table.Column<string>(type: "TEXT", nullable: false),
                    UsuarioAprovacaoId = table.Column<string>(type: "TEXT", nullable: true)
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
                });

            migrationBuilder.CreateTable(
                name: "LogsAuditoria",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UsuarioId = table.Column<string>(type: "TEXT", nullable: false),
                    Acao = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Entidade = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    EntidadeId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    DataHora = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Detalhes = table.Column<string>(type: "TEXT", nullable: true),
                    EnderecoIP = table.Column<string>(type: "TEXT", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
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
                name: "Saidas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Data = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Valor = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Descricao = table.Column<string>(type: "TEXT", maxLength: 250, nullable: false),
                    Observacoes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    MeioDePagamentoId = table.Column<int>(type: "INTEGER", nullable: false),
                    CentroCustoId = table.Column<int>(type: "INTEGER", nullable: false),
                    PlanoDeContasId = table.Column<int>(type: "INTEGER", nullable: false),
                    FornecedorId = table.Column<int>(type: "INTEGER", nullable: true),
                    TipoDespesa = table.Column<int>(type: "INTEGER", nullable: false),
                    NumeroDocumento = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    DataVencimento = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ComprovanteUrl = table.Column<string>(type: "TEXT", nullable: true),
                    UsuarioId = table.Column<string>(type: "TEXT", nullable: false),
                    DataCriacao = table.Column<DateTime>(type: "TEXT", nullable: false)
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
                name: "TransferenciasInternas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Data = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Valor = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Descricao = table.Column<string>(type: "TEXT", maxLength: 250, nullable: true),
                    MeioDePagamentoOrigemId = table.Column<int>(type: "INTEGER", nullable: false),
                    MeioDePagamentoDestinoId = table.Column<int>(type: "INTEGER", nullable: false),
                    CentroCustoOrigemId = table.Column<int>(type: "INTEGER", nullable: false),
                    CentroCustoDestinoId = table.Column<int>(type: "INTEGER", nullable: false),
                    Quitada = table.Column<bool>(type: "INTEGER", nullable: false),
                    UsuarioId = table.Column<string>(type: "TEXT", nullable: false),
                    DataCriacao = table.Column<DateTime>(type: "TEXT", nullable: false)
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
                name: "Entradas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Data = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Valor = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Descricao = table.Column<string>(type: "TEXT", maxLength: 250, nullable: true),
                    Observacoes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    MeioDePagamentoId = table.Column<int>(type: "INTEGER", nullable: false),
                    CentroCustoId = table.Column<int>(type: "INTEGER", nullable: false),
                    PlanoDeContasId = table.Column<int>(type: "INTEGER", nullable: false),
                    MembroId = table.Column<int>(type: "INTEGER", nullable: true),
                    ModeloRateioEntradaId = table.Column<int>(type: "INTEGER", nullable: true),
                    ComprovanteUrl = table.Column<string>(type: "TEXT", nullable: true),
                    UsuarioId = table.Column<string>(type: "TEXT", nullable: false),
                    DataCriacao = table.Column<DateTime>(type: "TEXT", nullable: false)
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
                name: "DetalhesFechamento",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FechamentoPeriodoId = table.Column<int>(type: "INTEGER", nullable: false),
                    TipoMovimento = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Descricao = table.Column<string>(type: "TEXT", maxLength: 250, nullable: false),
                    Valor = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Data = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PlanoContas = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    MeioPagamento = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Membro = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Fornecedor = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Observacoes = table.Column<string>(type: "TEXT", maxLength: 250, nullable: true)
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
                name: "ItensRateioFechamento",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FechamentoPeriodoId = table.Column<int>(type: "INTEGER", nullable: false),
                    RegraRateioId = table.Column<int>(type: "INTEGER", nullable: false),
                    ValorBase = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Percentual = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    ValorRateio = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Observacoes = table.Column<string>(type: "TEXT", maxLength: 250, nullable: true),
                    DataCriacao = table.Column<DateTime>(type: "TEXT", nullable: false)
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

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true);

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
                unique: true);

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
                name: "IX_DetalhesFechamento_FechamentoPeriodoId",
                table: "DetalhesFechamento",
                column: "FechamentoPeriodoId");

            migrationBuilder.CreateIndex(
                name: "IX_Entradas_CentroCustoId",
                table: "Entradas",
                column: "CentroCustoId");

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
                name: "Entradas");

            migrationBuilder.DropTable(
                name: "ItensRateioFechamento");

            migrationBuilder.DropTable(
                name: "LogsAuditoria");

            migrationBuilder.DropTable(
                name: "Saidas");

            migrationBuilder.DropTable(
                name: "TransferenciasInternas");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "Membros");

            migrationBuilder.DropTable(
                name: "ModelosRateioEntrada");

            migrationBuilder.DropTable(
                name: "FechamentosPeriodo");

            migrationBuilder.DropTable(
                name: "RegrasRateio");

            migrationBuilder.DropTable(
                name: "Fornecedores");

            migrationBuilder.DropTable(
                name: "PlanosDeContas");

            migrationBuilder.DropTable(
                name: "MeiosDePagamento");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "CentrosCusto");
        }
    }
}
