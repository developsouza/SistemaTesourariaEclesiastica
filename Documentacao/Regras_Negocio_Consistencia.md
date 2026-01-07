# ?? Documentação de Regras de Negócio e Consistência
## Sistema de Tesouraria Eclesiástica

---

## ?? Fórmulas de Cálculo Financeiro

### 1. Cálculo de Totais de Fechamento

#### Total de Entradas
```
TotalEntradas = ? (Entrada.Valor) 
WHERE Entrada.FechamentoQueIncluiuId = Fechamento.Id
```

#### Total de Saídas
```
TotalSaidas = ? (Saida.Valor) 
WHERE Saida.FechamentoQueIncluiuId = Fechamento.Id
```

#### Totais por Tipo de Caixa

**Entradas Físicas:**
```
TotalEntradasFisicas = ? (Entrada.Valor) 
WHERE Entrada.FechamentoQueIncluiuId = Fechamento.Id 
  AND Entrada.MeioDePagamento.TipoCaixa = Fisico
```

**Entradas Digitais:**
```
TotalEntradasDigitais = ? (Entrada.Valor) 
WHERE Entrada.FechamentoQueIncluiuId = Fechamento.Id 
  AND Entrada.MeioDePagamento.TipoCaixa = Digital
```

**Saídas Físicas:**
```
TotalSaidasFisicas = ? (Saida.Valor) 
WHERE Saida.FechamentoQueIncluiuId = Fechamento.Id 
  AND Saida.MeioDePagamento.TipoCaixa = Fisico
```

**Saídas Digitais:**
```
TotalSaidasDigitais = ? (Saida.Valor) 
WHERE Saida.FechamentoQueIncluiuId = Fechamento.Id 
  AND Saida.MeioDePagamento.TipoCaixa = Digital
```

### 2. Cálculo de Balanços

#### Balanço Físico
```
BalancoFisico = TotalEntradasFisicas - TotalSaidasFisicas
```

#### Balanço Digital
```
BalancoDigital = TotalEntradasDigitais - TotalSaidasDigitais
```

#### Balanço Total
```
BalancoTotal = BalancoFisico + BalancoDigital
             = (TotalEntradasFisicas + TotalEntradasDigitais) - (TotalSaidasFisicas + TotalSaidasDigitais)
             = TotalEntradas - TotalSaidas
```

### 3. Cálculo de Rateios

#### Rateio Individual
```
ValorRateio = ValorBase × (Percentual / 100)
```

Onde:
- **ValorBase**: Geralmente é o total de receitas (entradas) do fechamento
- **Percentual**: Definido na regra de rateio (ex: 10% para FUNDO)

#### Total de Rateios
```
TotalRateios = ? (ItemRateio.ValorRateio) 
WHERE ItemRateio.FechamentoPeriodoId = Fechamento.Id
```

### 4. Saldo Final

```
SaldoFinal = BalancoTotal - TotalRateios
           = (TotalEntradas - TotalSaidas) - TotalRateios
```

**Importante:** O saldo final considera o balanço (diferença entre entradas e saídas) subtraído dos rateios aplicados.

---

## ?? Regras de Integridade de Dados

### 1. Validações de Valores

#### Entradas
- ? Valor deve ser **positivo** (> 0)
- ? Valor máximo: **R$ 999.999,99**
- ? Precisão: **2 casas decimais**

#### Saídas
- ? Valor deve ser **positivo** (> 0)
- ? Valor máximo: **R$ 999.999,99**
- ? Precisão: **2 casas decimais**

### 2. Validações de Data

- ? Data não pode ser **mais de 2 anos no passado**
- ? Data não pode ser **mais de 30 dias no futuro**
- ? Data de fim de fechamento deve ser **>= Data de início**

### 3. Validações de Referências

#### Obrigatórias
- ? Toda entrada deve ter **MeioDePagamento** válido
- ? Toda entrada deve ter **CentroCusto** válido
- ? Toda entrada deve ter **PlanoDeContas** válido (tipo Receita)
- ? Toda saída deve ter **MeioDePagamento** válido
- ? Toda saída deve ter **CentroCusto** válido
- ? Toda saída deve ter **PlanoDeContas** válido (tipo Despesa)

#### Regras Adicionais
- ? MeioDePagamento, CentroCusto e PlanoDeContas devem estar **ativos**
- ? Não permitir **lançamentos duplicados** (mesma data, valor, centro de custo e plano de contas)

### 4. Validações de Transferências Internas

- ? **CentroCustoOrigem ? CentroCustoDestino**
- ? **MeioPagamentoOrigem ? MeioPagamentoDestino**
- ? Valor deve ser positivo
- ? Ambos os centros de custo devem estar ativos

### 5. Consistência de Fechamentos

#### Flags de Inclusão
```
SE IncluidaEmFechamento = true ENTÃO FechamentoQueIncluiuId IS NOT NULL
SE IncluidaEmFechamento = false ENTÃO FechamentoQueIncluiuId IS NULL
```

#### Validações de Totais
```
|TotalEntradas - ?(Entradas.Valor)| < 0.01
|TotalSaidas - ?(Saidas.Valor)| < 0.01
|SaldoFinal - (BalancoTotal - TotalRateios)| < 0.01
```

*(Tolerância de R$ 0,01 para arredondamentos)*

---

## ?? Fluxo de Aprovação de Fechamentos

### Etapas do Fechamento de Congregação

```
1. CRIAÇÃO (Status: PENDENTE)
   ?? Tesoureiro Local cria fechamento
   ?? Sistema calcula totais automaticamente
   ?? Lançamentos são BLOQUEADOS (IncluidaEmFechamento = true)
   ?? Aguarda aprovação da Sede

2. APROVAÇÃO (Status: APROVADO)
   ?? Tesoureiro Geral/Admin revisa
   ?? Se aprovado: Status ? APROVADO
   ?? Disponível para inclusão em Fechamento da Sede

3. PROCESSAMENTO PELA SEDE (FoiProcessadoPelaSede = true)
   ?? Fechamento incluído no fechamento consolidado da Sede
   ?? Rateios são calculados
   ?? Status final: PROCESSADO
```

### Etapas do Fechamento da Sede

```
1. CRIAÇÃO (Status: PENDENTE)
   ?? Tesoureiro Geral cria fechamento consolidado
   ?? Seleciona fechamentos APROVADOS das congregações
   ?? Sistema soma: Entradas/Saídas Congregações + Entradas/Saídas Sede
   ?? Aplica rateios automaticamente
   ?? Calcula saldo final

2. APROVAÇÃO (Status: APROVADO)
   ?? Admin/Tesoureiro Geral aprova
   ?? Fechamentos das congregações ? FoiProcessadoPelaSede = true
   ?? Fechamento consolidado finalizado
```

### Fluxo de Rejeição

```
REJEIÇÃO (Status: REJEITADO)
?? Tesoureiro Geral rejeita com motivo
?? Lançamentos são LIBERADOS (IncluidaEmFechamento = false)
?? Tesoureiro Local pode corrigir
?? Criar novo fechamento após correções
```

---

## ?? Regras de Rateio

### Aplicação de Rateios

1. **Momento de Aplicação:**
   - Rateios são aplicados APENAS em fechamentos da **SEDE**
   - Aplicados automaticamente após cálculo dos totais

2. **Base de Cálculo:**
   ```
   ValorBase = TotalReceitasConsolidadas
             = ?(Entradas da Sede) + ?(Entradas das Congregações Incluídas)
   ```

3. **Cálculo Individual:**
   ```
   ValorRateio = ValorBase × (RegraRateio.Percentual / 100)
   ```

4. **Destinos Comuns:**
   - **FUNDO DE REPASSE**: 10% das receitas totais
   - **FUNDO DE EMPRÉSTIMOS**: 20% das receitas (dízimo dos dízimos)
   - Outros conforme configuração

### Exemplo Prático

```
FECHAMENTO DA SEDE - NOVEMBRO/2024
???????????????????????????????????
Receitas da Sede:           R$ 15.000,00
Receitas Congregação A:     R$ 12.500,00
Receitas Congregação B:     R$  8.000,00
???????????????????????????????????
Total de Receitas:          R$ 35.500,00

RATEIOS:
- FUNDO (10%):              R$  3.550,00

Despesas Totais:            R$ 18.000,00
Balanço Bruto:              R$ 17.500,00
Saldo Final:                R$ 13.950,00
```

---

## ?? Situações de Inconsistência

### Inconsistências Críticas (Requerem Correção Imediata)

1. **Lançamentos com valores ? 0**
   - Causa: Bug na inserção ou edição manual incorreta
   - Correção: Excluir ou corrigir valor

2. **Totais de fechamento não batem com lançamentos**
   - Causa: Exclusão/edição de lançamentos após fechamento
   - Correção: Recalcular totais do fechamento usando stored procedure

3. **Lançamentos órfãos** (vinculados a fechamentos inexistentes)
   - Causa: Exclusão incorreta de fechamento
   - Correção: Limpar flags (IncluidaEmFechamento = false, FechamentoQueIncluiuId = NULL)

4. **Referências inválidas** (FKs para registros inexistentes)
   - Causa: Exclusão cascata incorreta ou corrupção de dados
   - Correção: Corrigir referências ou remover lançamentos

### Inconsistências de Aviso (Investigar)

1. **Lançamentos duplicados**
   - Causa: Inserção dupla acidental
   - Ação: Verificar se são realmente duplicatas e excluir extras

2. **Saldo negativo em centro de custo**
   - Causa: Despesas sem provisão prévia
   - Ação: Verificar se há lançamentos incorretos

3. **Datas futuras excessivas**
   - Causa: Erro de digitação
   - Ação: Verificar e corrigir datas

### Inconsistências Informativas

1. **Tipo de caixa incoerente com nome do meio de pagamento**
   - Exemplo: "PIX" marcado como Físico
   - Ação: Revisar e corrigir tipo de caixa

---

## ??? Ações Corretivas

### Recalcular Totais de Fechamento

```sql
EXEC sp_RecalcularTotaisFechamento @FechamentoId = [ID];
```

### Liberar Lançamentos Órfãos

```sql
UPDATE Entradas 
SET IncluidaEmFechamento = 0, 
    FechamentoQueIncluiuId = NULL,
    DataInclusaoFechamento = NULL
WHERE FechamentoQueIncluiuId NOT IN (SELECT Id FROM FechamentosPeriodo);

UPDATE Saidas 
SET IncluidaEmFechamento = 0, 
    FechamentoQueIncluiuId = NULL,
    DataInclusaoFechamento = NULL
WHERE FechamentoQueIncluiuId NOT IN (SELECT Id FROM FechamentosPeriodo);
```

### Verificar Consistência de um Fechamento

```sql
SELECT * FROM fn_ValidarConsistenciaFechamento([ID]);
```

---

## ?? Métricas de Saúde do Sistema

### Classificação

| Pontuação | Classificação | Descrição |
|-----------|---------------|-----------|
| 90-100%   | Excelente     | Sistema íntegro e consistente |
| 70-89%    | Bom           | Pequenos ajustes necessários |
| 50-69%    | Regular       | Inconsistências moderadas |
| 30-49%    | Ruim          | Problemas graves detectados |
| 0-29%     | Crítico       | Requer atenção imediata |

### Cálculo da Pontuação

```
PontuaçãoNegativa = (Críticas × 10) + (Avisos × 3) + (Informações × 1)
Saúde = MAX(0, 100 - PontuaçãoNegativa)
```

---

## ?? Diagnóstico de Consistência

### Validações Realizadas

1. ? Lançamentos duplicados
2. ? Totais de fechamentos incorretos
3. ? Integridade referencial (FKs)
4. ? Lançamentos órfãos
5. ? Transferências internas inválidas
6. ? Rateios aplicados incorretamente
7. ? Saldos negativos
8. ? Tipos de caixa incoerentes
9. ? Datas futuras excessivas
10. ? Valores inválidos (? 0)

### Frequência Recomendada

- **Manual**: Mensal ou após operações críticas
- **Automática**: Diária (background service)

---

## ?? Notas Importantes

1. **Exclusão de Fechamentos Aprovados:**
   - Apenas administradores podem excluir
   - Libera automaticamente lançamentos incluídos
   - Se for fechamento da Sede, libera também fechamentos de congregações

2. **Edição de Lançamentos Incluídos em Fechamentos:**
   - **PENDENTES**: Permitido (recalcula totais)
   - **APROVADOS**: Bloqueado (exceto admin)
   - **REJEITADOS**: Bloqueado (histórico)

3. **Backup e Auditoria:**
   - Todas as operações críticas são auditadas
   - Recomenda-se backup diário do banco de dados
   - Logs de auditoria são permanentes

---

**Última Atualização:** Janeiro 2025  
**Versão:** 2.0  
**Sistema:** Tesouraria Eclesiástica - .NET 9
