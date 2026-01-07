-- =============================================
-- Script de Melhoria de Performance e Consistência
-- Sistema de Tesouraria Eclesiástica
-- =============================================

-- ===== ÍNDICES PARA MELHORIA DE PERFORMANCE =====

-- Índices para queries de fechamento
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Entradas_CentroCustoId_Data_IncluidaEmFechamento')
CREATE NONCLUSTERED INDEX IX_Entradas_CentroCustoId_Data_IncluidaEmFechamento
    ON Entradas (CentroCustoId, Data, IncluidaEmFechamento)
    INCLUDE (Valor, MeioDePagamentoId, FechamentoQueIncluiuId);
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Saidas_CentroCustoId_Data_IncluidaEmFechamento')
CREATE NONCLUSTERED INDEX IX_Saidas_CentroCustoId_Data_IncluidaEmFechamento
    ON Saidas (CentroCustoId, Data, IncluidaEmFechamento)
    INCLUDE (Valor, MeioDePagamentoId, FechamentoQueIncluiuId);
GO

-- Índices para queries de fechamentos por status
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_FechamentosPeriodo_Status_DataInicio_DataFim')
CREATE NONCLUSTERED INDEX IX_FechamentosPeriodo_Status_DataInicio_DataFim
    ON FechamentosPeriodo (Status, DataInicio, DataFim)
    INCLUDE (CentroCustoId, TotalEntradas, TotalSaidas, SaldoFinal);
GO

-- Índice para validação de lançamentos incluídos em fechamentos
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Entradas_FechamentoQueIncluiuId')
CREATE NONCLUSTERED INDEX IX_Entradas_FechamentoQueIncluiuId
    ON Entradas (FechamentoQueIncluiuId)
    WHERE FechamentoQueIncluiuId IS NOT NULL;
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Saidas_FechamentoQueIncluiuId')
CREATE NONCLUSTERED INDEX IX_Saidas_FechamentoQueIncluiuId
    ON Saidas (FechamentoQueIncluiuId)
    WHERE FechamentoQueIncluiuId IS NOT NULL;
GO

-- ===== CONSTRAINTS PARA GARANTIR INTEGRIDADE =====

-- Garantir que valor de entrada seja positivo
IF NOT EXISTS (SELECT * FROM sys.check_constraints WHERE name = 'CK_Entradas_Valor_Positivo')
ALTER TABLE Entradas
    ADD CONSTRAINT CK_Entradas_Valor_Positivo CHECK (Valor > 0);
GO

-- Garantir que valor de saída seja positivo
IF NOT EXISTS (SELECT * FROM sys.check_constraints WHERE name = 'CK_Saidas_Valor_Positivo')
ALTER TABLE Saidas
    ADD CONSTRAINT CK_Saidas_Valor_Positivo CHECK (Valor > 0);
GO

-- Garantir que valor de transferência seja positivo
IF NOT EXISTS (SELECT * FROM sys.check_constraints WHERE name = 'CK_TransferenciasInternas_Valor_Positivo')
ALTER TABLE TransferenciasInternas
    ADD CONSTRAINT CK_TransferenciasInternas_Valor_Positivo CHECK (Valor > 0);
GO

-- Garantir que data de fim seja maior que data de início em fechamentos
IF NOT EXISTS (SELECT * FROM sys.check_constraints WHERE name = 'CK_FechamentosPeriodo_DataFim_Maior_DataInicio')
ALTER TABLE FechamentosPeriodo
    ADD CONSTRAINT CK_FechamentosPeriodo_DataFim_Maior_DataInicio CHECK (DataFim >= DataInicio);
GO

-- Garantir que transferências não tenham origem e destino iguais (Centro de Custo)
IF NOT EXISTS (SELECT * FROM sys.check_constraints WHERE name = 'CK_TransferenciasInternas_CentrosCusto_Diferentes')
ALTER TABLE TransferenciasInternas
    ADD CONSTRAINT CK_TransferenciasInternas_CentrosCusto_Diferentes 
    CHECK (CentroCustoOrigemId != CentroCustoDestinoId);
GO

-- Garantir que transferências não tenham origem e destino iguais (Meio de Pagamento)
IF NOT EXISTS (SELECT * FROM sys.check_constraints WHERE name = 'CK_TransferenciasInternas_MeiosPagamento_Diferentes')
ALTER TABLE TransferenciasInternas
    ADD CONSTRAINT CK_TransferenciasInternas_MeiosPagamento_Diferentes 
    CHECK (MeioDePagamentoOrigemId != MeioDePagamentoDestinoId);
GO

-- Garantir consistência entre flags de fechamento em Entradas
IF NOT EXISTS (SELECT * FROM sys.check_constraints WHERE name = 'CK_Entradas_Consistencia_Fechamento')
ALTER TABLE Entradas
    ADD CONSTRAINT CK_Entradas_Consistencia_Fechamento 
    CHECK ((IncluidaEmFechamento = 0 AND FechamentoQueIncluiuId IS NULL) OR 
           (IncluidaEmFechamento = 1 AND FechamentoQueIncluiuId IS NOT NULL));
GO

-- Garantir consistência entre flags de fechamento em Saídas
IF NOT EXISTS (SELECT * FROM sys.check_constraints WHERE name = 'CK_Saidas_Consistencia_Fechamento')
ALTER TABLE Saidas
    ADD CONSTRAINT CK_Saidas_Consistencia_Fechamento 
    CHECK ((IncluidaEmFechamento = 0 AND FechamentoQueIncluiuId IS NULL) OR 
           (IncluidaEmFechamento = 1 AND FechamentoQueIncluiuId IS NOT NULL));
GO

-- ===== VIEWS PARA FACILITAR CONSULTAS =====

-- View para totais de lançamentos por centro de custo
IF OBJECT_ID('vw_TotaisPorCentroCusto', 'V') IS NOT NULL
    DROP VIEW vw_TotaisPorCentroCusto;
GO

CREATE VIEW vw_TotaisPorCentroCusto
AS
SELECT 
    c.Id AS CentroCustoId,
    c.Nome AS CentroCustoNome,
    ISNULL(e.TotalEntradas, 0) AS TotalEntradas,
    ISNULL(s.TotalSaidas, 0) AS TotalSaidas,
    ISNULL(e.TotalEntradas, 0) - ISNULL(s.TotalSaidas, 0) AS Saldo,
    ISNULL(e.QuantidadeEntradas, 0) AS QuantidadeEntradas,
    ISNULL(s.QuantidadeSaidas, 0) AS QuantidadeSaidas
FROM CentrosCusto c
LEFT JOIN (
    SELECT CentroCustoId, SUM(Valor) AS TotalEntradas, COUNT(*) AS QuantidadeEntradas
    FROM Entradas
    GROUP BY CentroCustoId
) e ON c.Id = e.CentroCustoId
LEFT JOIN (
    SELECT CentroCustoId, SUM(Valor) AS TotalSaidas, COUNT(*) AS QuantidadeSaidas
    FROM Saidas
    GROUP BY CentroCustoId
) s ON c.Id = s.CentroCustoId;
GO

-- View para lançamentos não incluídos em fechamentos
IF OBJECT_ID('vw_LancamentosNaoIncluidos', 'V') IS NOT NULL
    DROP VIEW vw_LancamentosNaoIncluidos;
GO

CREATE VIEW vw_LancamentosNaoIncluidos
AS
SELECT 
    'Entrada' AS TipoLancamento,
    Id AS LancamentoId,
    Data,
    Valor,
    CentroCustoId,
    Descricao
FROM Entradas
WHERE IncluidaEmFechamento = 0

UNION ALL

SELECT 
    'Saida' AS TipoLancamento,
    Id AS LancamentoId,
    Data,
    Valor,
    CentroCustoId,
    Descricao
FROM Saidas
WHERE IncluidaEmFechamento = 0;
GO

-- ===== STORED PROCEDURE PARA RECALCULAR TOTAIS DE FECHAMENTO =====

IF OBJECT_ID('sp_RecalcularTotaisFechamento', 'P') IS NOT NULL
    DROP PROCEDURE sp_RecalcularTotaisFechamento;
GO

CREATE PROCEDURE sp_RecalcularTotaisFechamento
    @FechamentoId INT
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @TotalEntradas DECIMAL(18,2);
    DECLARE @TotalSaidas DECIMAL(18,2);
    DECLARE @TotalEntradasFisicas DECIMAL(18,2);
    DECLARE @TotalEntradasDigitais DECIMAL(18,2);
    DECLARE @TotalSaidasFisicas DECIMAL(18,2);
    DECLARE @TotalSaidasDigitais DECIMAL(18,2);
    DECLARE @BalancoFisico DECIMAL(18,2);
    DECLARE @BalancoDigital DECIMAL(18,2);
    DECLARE @TotalRateios DECIMAL(18,2);
    DECLARE @SaldoFinal DECIMAL(18,2);
    
    -- Calcular totais de entradas
    SELECT 
        @TotalEntradas = ISNULL(SUM(e.Valor), 0),
        @TotalEntradasFisicas = ISNULL(SUM(CASE WHEN m.TipoCaixa = 0 THEN e.Valor ELSE 0 END), 0),
        @TotalEntradasDigitais = ISNULL(SUM(CASE WHEN m.TipoCaixa = 1 THEN e.Valor ELSE 0 END), 0)
    FROM Entradas e
    INNER JOIN MeiosDePagamento m ON e.MeioDePagamentoId = m.Id
    WHERE e.FechamentoQueIncluiuId = @FechamentoId;
    
    -- Calcular totais de saídas
    SELECT 
        @TotalSaidas = ISNULL(SUM(s.Valor), 0),
        @TotalSaidasFisicas = ISNULL(SUM(CASE WHEN m.TipoCaixa = 0 THEN s.Valor ELSE 0 END), 0),
        @TotalSaidasDigitais = ISNULL(SUM(CASE WHEN m.TipoCaixa = 1 THEN s.Valor ELSE 0 END), 0)
    FROM Saidas s
    INNER JOIN MeiosDePagamento m ON s.MeioDePagamentoId = m.Id
    WHERE s.FechamentoQueIncluiuId = @FechamentoId;
    
    -- Calcular balanços
    SET @BalancoFisico = @TotalEntradasFisicas - @TotalSaidasFisicas;
    SET @BalancoDigital = @TotalEntradasDigitais - @TotalSaidasDigitais;
    
    -- Calcular total de rateios
    SELECT @TotalRateios = ISNULL(SUM(ValorRateio), 0)
    FROM ItemRateioFechamento
    WHERE FechamentoPeriodoId = @FechamentoId;
    
    -- Calcular saldo final
    SET @SaldoFinal = (@BalancoFisico + @BalancoDigital) - @TotalRateios;
    
    -- Atualizar fechamento
    UPDATE FechamentosPeriodo
    SET 
        TotalEntradas = @TotalEntradas,
        TotalSaidas = @TotalSaidas,
        TotalEntradasFisicas = @TotalEntradasFisicas,
        TotalEntradasDigitais = @TotalEntradasDigitais,
        TotalSaidasFisicas = @TotalSaidasFisicas,
        TotalSaidasDigitais = @TotalSaidasDigitais,
        BalancoFisico = @BalancoFisico,
        BalancoDigital = @BalancoDigital,
        TotalRateios = @TotalRateios,
        SaldoFinal = @SaldoFinal
    WHERE Id = @FechamentoId;
    
    SELECT 
        @TotalEntradas AS TotalEntradas,
        @TotalSaidas AS TotalSaidas,
        @SaldoFinal AS SaldoFinal,
        'Totais recalculados com sucesso' AS Mensagem;
END;
GO

-- ===== FUNÇÃO PARA VALIDAR CONSISTÊNCIA DE FECHAMENTO =====

IF OBJECT_ID('fn_ValidarConsistenciaFechamento', 'FN') IS NOT NULL
    DROP FUNCTION fn_ValidarConsistenciaFechamento;
GO

CREATE FUNCTION fn_ValidarConsistenciaFechamento
(
    @FechamentoId INT
)
RETURNS TABLE
AS
RETURN
(
    SELECT 
        f.Id AS FechamentoId,
        f.TotalEntradas AS TotalEntradasRegistrado,
        ISNULL(SUM(e.Valor), 0) AS TotalEntradasReal,
        f.TotalSaidas AS TotalSaidasRegistrado,
        ISNULL(SUM(s.Valor), 0) AS TotalSaidasReal,
        CASE 
            WHEN ABS(f.TotalEntradas - ISNULL(SUM(e.Valor), 0)) > 0.01 OR 
                 ABS(f.TotalSaidas - ISNULL(SUM(s.Valor), 0)) > 0.01 THEN 0
            ELSE 1
        END AS EstaConsistente
    FROM FechamentosPeriodo f
    LEFT JOIN Entradas e ON e.FechamentoQueIncluiuId = f.Id
    LEFT JOIN Saidas s ON s.FechamentoQueIncluiuId = f.Id
    WHERE f.Id = @FechamentoId
    GROUP BY f.Id, f.TotalEntradas, f.TotalSaidas
);
GO

PRINT 'Script de melhorias de consistência executado com sucesso!';
PRINT 'Índices criados para otimização de queries.';
PRINT 'Constraints adicionados para garantir integridade.';
PRINT 'Views e stored procedures criados para facilitar análises.';
GO
