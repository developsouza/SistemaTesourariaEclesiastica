# 📚 Casos de Uso Práticos - Sistema de Tesouraria

## Exemplos Reais do Dia a Dia

---

## 📖 Índice de Casos

1. [Caso 1: Domingo de Culto](#caso-1-domingo-de-culto)
2. [Caso 2: Pagamento de Conta de Luz](#caso-2-pagamento-de-conta-de-luz)
3. [Caso 3: Fim do Mês - Fechamento](#caso-3-fim-do-mês---fechamento)
4. [Caso 4: Aprovação de Prestação](#caso-4-aprovação-de-prestação)
5. [Caso 5: Fechamento Consolidado da Sede](#caso-5-fechamento-consolidado-da-sede)
6. [Caso 6: Correção de Erro](#caso-6-correção-de-erro)
7. [Caso 7: Compra de Materiais](#caso-7-compra-de-materiais)
8. [Caso 8: Evento Especial](#caso-8-evento-especial)
9. [Caso 9: Transferência Entre Contas](#caso-9-transferência-entre-contas)
10. [Caso 10: Consulta de Relatórios](#caso-10-consulta-de-relatórios)

---

## 📋 CASO 1: Domingo de Culto

### Contexto
**Personagem:** Maria Santos - Tesoureira da Congregação Centro  
**Situação:** Domingo após o culto, precisa registrar dízimos e ofertas  
**Data:** 05/11/2024

### Passo a Passo

#### 1. Contar o Dinheiro

```
CONTAGEM DO CAIXA FÍSICO
├─ Dízimos: R$ 1.850,00
├─ Ofertas: R$ 420,00
└─ Ofertas do Círculo de Oração: R$ 150,00
───────────────────────────────────
TOTAL: R$ 2.420,00
```

#### 2. Conferir Transferências PIX

```
EXTRATOS BANCÁRIOS
├─ PIX - João Silva - Dízimo: R$ 200,00
├─ PIX - Ana Costa - Dízimo: R$ 150,00
└─ Transferência - Pedro Oliveira: R$ 100,00
───────────────────────────────────
TOTAL DIGITAL: R$ 450,00
```

#### 3. Registrar no Sistema

**3.1. Registrar Dízimos em Dinheiro**

```
Login → Menu → Entradas → [+ Nova Entrada]

┌─────────────────────────────────────────┐
│ NOVA ENTRADA #001                      │
├─────────────────────────────────────────┤
│ Data:        05/11/2024 10:30          │
│ Valor:       R$ 1.850,00               │
│ Fonte:       Dízimos                   │
│ Centro:      Congregação Centro        │
│ Membro:      [Não especificar]         │
│ Meio Pgto:   Dinheiro                  │
│ Descrição:   Dízimos Culto 05/11       │
│ Obs:         Oferta recolhida no culto │
└─────────────────────────────────────────┘

[Salvar Entrada]
```

**3.2. Registrar Ofertas em Dinheiro**

```
┌─────────────────────────────────────────┐
│ NOVA ENTRADA #002                      │
├─────────────────────────────────────────┤
│ Data:        05/11/2024 10:30          │
│ Valor:       R$ 420,00                 │
│ Fonte:       Ofertas                   │
│ Centro:      Congregação Centro        │
│ Meio Pgto:   Dinheiro                  │
│ Descrição:   Ofertas Culto 05/11       │
└─────────────────────────────────────────┘

[Salvar Entrada]
```

**3.3. Registrar Ofertas do Círculo de Oração**

```
┌─────────────────────────────────────────┐
│ NOVA ENTRADA #003                      │
├─────────────────────────────────────────┤
│ Data:        05/11/2024 10:30          │
│ Valor:       R$ 150,00                 │
│ Fonte:       Ofertas (Círculo Oração)  │
│ Centro:      Congregação Centro        │
│ Meio Pgto:   Dinheiro                  │
│ Descrição:   Ofertas CO - 05/11        │
└─────────────────────────────────────────┘

[Salvar Entrada]
```

**3.4. Registrar PIX - João Silva**

```
┌─────────────────────────────────────────┐
│ NOVA ENTRADA #004                      │
├─────────────────────────────────────────┤
│ Data:        05/11/2024 09:45          │
│ Valor:       R$ 200,00                 │
│ Fonte:       Dízimos                   │
│ Centro:      Congregação Centro        │
│ Membro:      João Silva                │
│ Meio Pgto:   PIX                       │
│ Descrição:   Dízimo João Silva         │
│ Obs:         Chave PIX: joao@email.com │
└─────────────────────────────────────────┘

[Salvar Entrada]
```

**3.5. Registrar PIX - Ana Costa**

```
┌─────────────────────────────────────────┐
│ NOVA ENTRADA #005                      │
├─────────────────────────────────────────┤
│ Data:        05/11/2024 08:30          │
│ Valor:       R$ 150,00                 │
│ Fonte:       Dízimos                   │
│ Centro:      Congregação Centro        │
│ Membro:      Ana Costa                 │
│ Meio Pgto:   PIX                       │
│ Descrição:   Dízimo Ana Costa          │
└─────────────────────────────────────────┘

[Salvar Entrada]
```

**3.6. Registrar Transferência - Pedro Oliveira**

```
┌─────────────────────────────────────────┐
│ NOVA ENTRADA #006                      │
├─────────────────────────────────────────┤
│ Data:        04/11/2024 20:15          │
│ Valor:       R$ 100,00                 │
│ Fonte:       Dízimos                   │
│ Centro:      Congregação Centro        │
│ Membro:      Pedro Oliveira            │
│ Meio Pgto:   Transferência Bancária    │
│ Descrição:   Dízimo Pedro Oliveira     │
└─────────────────────────────────────────┘

[Salvar Entrada]
```

#### 4. Conferir Dashboard

```
DASHBOARD ATUALIZADO
┌─────────────────────────────────────────┐
│ ENTRADAS HOJE: R$ 2.870,00             │
│ ├─ Físico:  R$ 2.420,00                │
│ └─ Digital: R$ 450,00                  │
└─────────────────────────────────────────┘
```

#### 5. Organizar Comprovantes

```
✅ Guardar dinheiro no cofre
✅ Fazer print dos PIX
✅ Anotar na planilha de controle
✅ Registrar no livro de tesouraria (se houver)
```

### ✅ Resultado Final

- ✅ 6 entradas registradas
- ✅ Total de R$ 2.870,00
- ✅ Caixa físico e digital separados
- ✅ Membros identificados (quando possível)

---

## 💡 CASO 2: Pagamento de Conta de Luz

### Contexto
**Personagem:** Carlos Mendes - Tesoureiro da Congregação São João  
**Situação:** Recebeu conta de luz e precisa pagar  
**Data:** 10/11/2024  
**Valor:** R$ 380,50

### Passo a Passo

#### 1. Receber a Conta

```
CONTA DE ENERGIA CELPE
├─ Competência: Outubro/2024
├─ Vencimento: 15/11/2024
├─ Valor: R$ 380,50
└─ Código de Barras: [código]
```

#### 2. Verificar Saldo Disponível

```
Login → Dashboard

SALDO DISPONÍVEL
├─ Caixa Físico: R$ 850,00
└─ Caixa Digital: R$ 1.200,00
```

#### 3. Pagar a Conta

```
Opção escolhida: Pagar via PIX (banco)
✅ Pagamento realizado: 10/11/2024 15:30
✅ Comprovante salvo
```

#### 4. Registrar no Sistema

```
Login → Menu → Saídas → [+ Nova Saída]

┌─────────────────────────────────────────┐
│ NOVA SAÍDA #001                        │
├─────────────────────────────────────────┤
│ Data:        10/11/2024 15:30          │
│ Valor:       R$ 380,50                 │
│ Categoria:   Energia Elétrica (Luz)    │
│ Centro:      Congregação São João      │
│ Fornecedor:  CELPE                     │
│ Meio Pgto:   Transferência Bancária    │
│ Tipo Desp:   🔁 Fixa                   │
│ Nº Doc:      12345678901               │
│ Vencimento:  15/11/2024                │
│ Descrição:   Conta de luz Out/2024     │
│ Obs:         Pago via PIX - em dia     │
│ Comprovante: [anexar_comprovante.pdf]  │
└─────────────────────────────────────────┘

[Registrar Saída]
```

#### 5. Conferir Registro

```
SAÍDA REGISTRADA COM SUCESSO!

Dashboard atualizado:
├─ Saídas do Mês: R$ 380,50
└─ Saldo Digital: R$ 819,50
```

### ✅ Resultado

- ✅ Conta paga dentro do prazo
- ✅ Despesa registrada corretamente
- ✅ Comprovante anexado
- ✅ Saldo atualizado

---

## 📊 CASO 3: Fim do Mês - Fechamento

### Contexto
**Personagem:** Maria Santos - Tesoureira da Congregação Centro  
**Situação:** Dia 30/11, precisa fechar o mês  
**Período:** Novembro/2024

### Passo a Passo

#### 1. Revisar Lançamentos do Mês

```
Login → Menu → Entradas

CONFERIR:
├─ Total de Entradas: R$ 12.500,00
├─ Quantidade: 45 lançamentos
└─ ✅ Todos com comprovantes anexados
```

```
Login → Menu → Saídas

CONFERIR:
├─ Total de Saídas: R$ 6.800,00
├─ Quantidade: 28 lançamentos
└─ ✅ Todos com comprovantes anexados
```

#### 2. Conferir Caixa Físico

```
CONTAGEM FÍSICA
├─ Dinheiro no cofre: R$ 2.350,00
├─ Cheques a compensar: R$ 0,00
└─ TOTAL FÍSICO: R$ 2.350,00

ESPERADO PELO SISTEMA:
└─ Caixa Físico: R$ 2.350,00

✅ BATEU! Pode continuar.
```

#### 3. Conferir Saldo Bancário

```
EXTRATO BANCO
└─ Saldo em 30/11: R$ 3.350,00

ESPERADO PELO SISTEMA:
└─ Caixa Digital: R$ 3.350,00

✅ BATEU! Pode continuar.
```

#### 4. Criar Fechamento

```
Login → Menu → Fechamentos → [Criar Fechamento]

┌─────────────────────────────────────────┐
│ CRIAR FECHAMENTO                       │
├─────────────────────────────────────────┤
│ Tipo:        ● Mensal  ○ Diário        │
│ Mês/Ano:     Novembro / 2024           │
│ Centro:      Congregação Centro        │
│ Período:     01/11/2024 - 30/11/2024   │
│                                        │
│ TOTAIS CALCULADOS:                     │
│ ┌────────────────────────────────────┐ │
│ │ ENTRADAS                           │ │
│ │ Físicas:   R$  8.200,00           │ │
│ │ Digitais:  R$  4.300,00           │ │
│ │ TOTAL:     R$ 12.500,00           │ │
│ └────────────────────────────────────┘ │
│                                        │
│ ┌────────────────────────────────────┐ │
│ │ SAÍDAS                             │ │
│ │ Físicas:   R$  4.100,00           │ │
│ │ Digitais:  R$  2.700,00           │ │
│ │ TOTAL:     R$  6.800,00           │ │
│ └────────────────────────────────────┘ │
│                                        │
│ ┌────────────────────────────────────┐ │
│ │ BALANÇOS                           │ │
│ │ Físico:    R$  4.100,00           │ │
│ │ Digital:   R$  1.600,00           │ │
│ │ TOTAL:     R$  5.700,00           │ │
│ └────────────────────────────────────┘ │
│                                        │
│ Observações:                           │
│ ┌────────────────────────────────────┐ │
│ │ Mês normal de atividades.          │ │
│ │ Todos os comprovantes anexados.    │ │
│ │ Caixa físico e digital conferidos. │ │
│ └────────────────────────────────────┘ │
│                                        │
│ [Cancelar]  [Criar Fechamento]         │
└─────────────────────────────────────────┘
```

#### 5. Confirmar Criação

```
✅ FECHAMENTO CRIADO COM SUCESSO!

Status: 🟡 PENDENTE
Aguardando aprovação da Sede

O que acontece agora:
├─ Lançamentos ficam BLOQUEADOS
├─ Não é possível editar entradas/saídas de Nov/2024
└─ Tesoureiro Geral receberá notificação
```

#### 6. Enviar Comunicação

```
E-MAIL PARA A SEDE
──────────────────
Para: tesoureiro.geral@igreja.com
Assunto: Prestação de Contas - Nov/2024 - Congregação Centro

Prezado Tesoureiro Geral,

Segue prestação de contas de Novembro/2024:

📊 RESUMO:
├─ Entradas:  R$ 12.500,00
├─ Saídas:    R$  6.800,00
└─ Saldo:     R$  5.700,00

O fechamento está disponível no sistema para sua análise.

Atenciosamente,
Maria Santos
Tesoureira - Congregação Centro
```

### ✅ Resultado

- ✅ Fechamento criado
- ✅ Status: PENDENTE
- ✅ Sede notificada
- ✅ Aguardando aprovação

---

## ✅ CASO 4: Aprovação de Prestação

### Contexto
**Personagem:** Paulo Ferreira - Tesoureiro Geral  
**Situação:** Recebeu prestação da Congregação Centro  
**Data:** 02/12/2024

### Passo a Passo

#### 1. Receber Notificação

```
🔔 NOVA PRESTAÇÃO DE CONTAS
────────────────────────────
Congregação Centro
Mês: Novembro/2024
Status: PENDENTE

[Ver Detalhes]
```

#### 2. Acessar Fechamento

```
Login → Menu → Fechamentos

LISTA DE FECHAMENTOS
┌─────────────────────────────────────────┐
│ 🟡 PENDENTE - Congregação Centro       │
│    Nov/2024 - R$ 5.700,00              │
│    [Ver Detalhes]                      │
└─────────────────────────────────────────┘
```

#### 3. Revisar Detalhes

```
DETALHES DO FECHAMENTO
─────────────────────────

📍 CONGREGAÇÃO CENTRO
📅 NOVEMBRO/2024
🟡 STATUS: PENDENTE

┌─────────────────────────────────────────┐
│ TOTAIS                                 │
├─────────────────────────────────────────┤
│ Entradas Físicas:   R$  8.200,00      │
│ Entradas Digitais:  R$  4.300,00      │
│ Saídas Físicas:     R$  4.100,00      │
│ Saídas Digitais:    R$  2.700,00      │
│ ────────────────────────────────────── │
│ Balanço Físico:     R$  4.100,00      │
│ Balanço Digital:    R$  1.600,00      │
│ SALDO FINAL:        R$  5.700,00      │
└─────────────────────────────────────────┘

┌─────────────────────────────────────────┐
│ DETALHES DOS LANÇAMENTOS               │
├─────────────────────────────────────────┤
│ ENTRADAS (45 lançamentos)              │
│ ├─ Dízimos:   R$ 10.000,00            │
│ ├─ Ofertas:   R$  2.000,00            │
│ └─ CO:        R$    500,00            │
│                                        │
│ SAÍDAS (28 lançamentos)                │
│ ├─ Energia:   R$    380,50            │
│ ├─ Água:      R$    120,00            │
│ ├─ Limpeza:   R$    250,00            │
│ └─ Outras:    R$  6.049,50            │
└─────────────────────────────────────────┘
```

#### 4. Verificar Comprovantes

```
✅ CONFERIR:
├─ Entradas com comprovantes? SIM
├─ Saídas com comprovantes? SIM
├─ Valores conferem? SIM
└─ Observações? OK
```

#### 5. Aprovar

```
[Aprovar Fechamento]

┌─────────────────────────────────────────┐
│ CONFIRMAR APROVAÇÃO                    │
├─────────────────────────────────────────┤
│ Você está aprovando:                   │
│ Congregação Centro - Nov/2024          │
│                                        │
│ Após aprovação:                        │
│ • Status → APROVADO                    │
│ • Disponível para inclusão na Sede     │
│ • Congregação será notificada          │
│                                        │
│ [Cancelar]  [Confirmar Aprovação]      │
└─────────────────────────────────────────┘

[Confirmar Aprovação]
```

#### 6. Confirmação

```
✅ PRESTAÇÃO APROVADA COM SUCESSO!

Status: 🟢 APROVADO
Data Aprovação: 02/12/2024 14:30
Aprovado por: Paulo Ferreira

A congregação foi notificada.
```

### ✅ Resultado

- ✅ Prestação aprovada
- ✅ Status: APROVADO
- ✅ Disponível para consolidação
- ✅ Congregação notificada

---

## 🏛️ CASO 5: Fechamento Consolidado da Sede

### Contexto
**Personagem:** Paulo Ferreira - Tesoureiro Geral  
**Situação:** Criar fechamento da Sede incluindo congregações  
**Data:** 05/12/2024  
**Mês:** Novembro/2024

### Passo a Passo

#### 1. Verificar Prestações Aprovadas

```
Login → Menu → Fechamentos → Filtrar por APROVADO

PRESTAÇÕES APROVADAS:
┌─────────────────────────────────────────┐
│ 🟢 APROVADO - Congregação Centro       │
│    Nov/2024 - Saldo: R$ 5.700,00       │
├─────────────────────────────────────────┤
│ 🟢 APROVADO - Congregação São João     │
│    Nov/2024 - Saldo: R$ 3.200,00       │
├─────────────────────────────────────────┤
│ 🟢 APROVADO - Congregação Bairro Novo  │
│    Nov/2024 - Saldo: R$ 2.800,00       │
└─────────────────────────────────────────┘

TOTAL CONGREGAÇÕES: R$ 11.700,00
```

#### 2. Revisar Lançamentos da Sede

```
LANÇAMENTOS DA SEDE (Novembro/2024)
├─ Entradas:  R$ 15.000,00
└─ Saídas:    R$  8.000,00
────────────────────────────────
Saldo Sede:   R$  7.000,00
```

#### 3. Criar Fechamento Consolidado

```
Login → Menu → Fechamentos → [Criar Fechamento da Sede]

┌──────────────────────────────────────────────┐
│ CRIAR FECHAMENTO DA SEDE                    │
├──────────────────────────────────────────────┤
│ Mês/Ano: Novembro / 2024                    │
│ Período: 01/11/2024 - 30/11/2024            │
│                                              │
│ ┌──────────────────────────────────────────┐│
│ │ PRESTAÇÕES DISPONÍVEIS (APROVADAS)       ││
│ ├──────────────────────────────────────────┤│
│ │ [✓] Congregação Centro                   ││
│ │     Entradas: R$ 12.500,00               ││
│ │     Saídas:   R$  6.800,00               ││
│ │     Saldo:    R$  5.700,00               ││
│ ├──────────────────────────────────────────┤│
│ │ [✓] Congregação São João                 ││
│ │     Entradas: R$  8.000,00               ││
│ │     Saídas:   R$  4.800,00               ││
│ │     Saldo:    R$  3.200,00               ││
│ ├──────────────────────────────────────────┤│
│ │ [✓] Congregação Bairro Novo              ││
│ │     Entradas: R$  7.000,00               ││
│ │     Saídas:   R$  4.200,00               ││
│ │     Saldo:    R$  2.800,00               ││
│ └──────────────────────────────────────────┘│
│                                              │
│ ┌──────────────────────────────────────────┐│
│ │ LANÇAMENTOS DA SEDE (Novos)              ││
│ │     Entradas: R$ 15.000,00               ││
│ │     Saídas:   R$  8.000,00               ││
│ │     Saldo:    R$  7.000,00               ││
│ └──────────────────────────────────────────┘│
│                                              │
│ ┌──────────────────────────────────────────┐│
│ │ TOTAL CONSOLIDADO                        ││
│ │ ────────────────────────────────────────││
│ │ Total Entradas:  R$ 42.500,00           ││
│ │ Total Saídas:    R$ 23.800,00           ││
│ │ Saldo Bruto:     R$ 18.700,00           ││
│ │                                          ││
│ │ RATEIOS AUTOMÁTICOS:                    ││
│ │ - FUNDO (10% das receitas):             ││
│ │   R$ 42.500 × 10% = R$ 4.250,00        ││
│ │                                          ││
│ │ SALDO FINAL: R$ 14.450,00               ││
│ └──────────────────────────────────────────┘│
│                                              │
│ Observações:                                 │
│ ┌──────────────────────────────────────────┐│
│ │ Fechamento consolidado incluindo 3       ││
│ │ congregações. Rateio de 10% aplicado.    ││
│ └──────────────────────────────────────────┘│
│                                              │
│ [Cancelar]  [Criar Fechamento]               │
└──────────────────────────────────────────────┘
```

#### 4. Confirmar Criação

```
[Criar Fechamento]

✅ FECHAMENTO DA SEDE CRIADO COM SUCESSO!

Detalhes:
├─ 3 congregações incluídas
├─ Rateio FUNDO: R$ 4.250,00
├─ Saldo Final: R$ 14.450,00
└─ Status: 🟡 PENDENTE

As congregações foram marcadas como PROCESSADAS.
```

#### 5. Aprovar Fechamento

```
(Como Administrador ou outro Tesoureiro Geral)

[Aprovar]

✅ FECHAMENTO DA SEDE APROVADO!

Status: 🟢 APROVADO
Mês encerrado oficialmente.
```

### ✅ Resultado Final

```
SITUAÇÃO FINAL - NOVEMBRO/2024
──────────────────────────────────

CONGREGAÇÕES:
├─ Centro:       🔵 PROCESSADO
├─ São João:     🔵 PROCESSADO
└─ Bairro Novo:  🔵 PROCESSADO

SEDE:
└─ Consolidado:  🟢 APROVADO

VALORES:
├─ Total Receitas:  R$ 42.500,00
├─ Total Despesas:  R$ 23.800,00
├─ Rateio FUNDO:    R$  4.250,00
└─ SALDO FINAL:     R$ 14.450,00
```

---

## 🔧 CASO 6: Correção de Erro

### Contexto
**Personagem:** Maria Santos - Tesoureira  
**Situação:** Registrou entrada com valor errado  
**Data:** 15/11/2024

### Situação Problema

```
ENTRADA REGISTRADA ERRADA:
├─ Data: 10/11/2024
├─ Valor REGISTRADO: R$ 500,00
├─ Valor CORRETO: R$ 150,00
└─ Diferença: R$ 350,00 a mais
```

### Cenário 1: Fechamento NÃO Foi Criado

```
✅ SOLUÇÃO SIMPLES:

1. Login → Menu → Entradas

2. Localizar entrada #ID123
   Data: 10/11/2024
   Valor: R$ 500,00

3. [Editar]

4. Corrigir valor:
   De: R$ 500,00
   Para: R$ 150,00

5. [Salvar]

✅ PRONTO! Valor corrigido.
```

### Cenário 2: Fechamento JÁ Foi Criado e PENDENTE

```
❌ NÃO É POSSÍVEL EDITAR DIRETAMENTE

✅ SOLUÇÃO:

1. Solicitar REJEIÇÃO do fechamento

2. Após rejeição, lançamentos são liberados

3. Editar entrada como no Cenário 1

4. Criar novo fechamento
```

### Cenário 3: Fechamento JÁ Foi APROVADO

```
❌ IMPOSSÍVEL ALTERAR LANÇAMENTOS PASSADOS

✅ SOLUÇÃO: ESTORNO NO MÊS SEGUINTE

DEZEMBRO/2024:

1. Criar SAÍDA de estorno:
   ┌───────────────────────────────┐
   │ NOVA SAÍDA                    │
   ├───────────────────────────────┤
   │ Data: 15/12/2024              │
   │ Valor: R$ 350,00              │
   │ Categoria: Despesas Diversas  │
   │ Descrição: ESTORNO - Erro no  │
   │ registro entrada 10/11 -      │
   │ valor excedente R$ 350,00     │
   └───────────────────────────────┘

✅ Saldo de Dezembro será ajustado automaticamente
```

---

## 🛒 CASO 7: Compra de Materiais

### Contexto
**Personagem:** Carlos Mendes - Tesoureiro  
**Situação:** Compra de material de limpeza  
**Data:** 20/11/2024

### Passo a Passo

#### 1. Fazer a Compra

```
COMPRA NO SUPERMERCADO
├─ Material de limpeza
├─ Valor: R$ 185,90
├─ Pagamento: Cartão de Débito
└─ Nota Fiscal: NF-987654
```

#### 2. Cadastrar Fornecedor (se necessário)

```
Login → Menu → Cadastros → Fornecedores → [+ Novo]

┌─────────────────────────────────────────┐
│ NOVO FORNECEDOR                        │
├─────────────────────────────────────────┤
│ Nome:      Supermercado Bom Preço      │
│ CNPJ:      12.345.678/0001-90          │
│ Telefone:  (81) 3333-4444              │
│ E-mail:    contato@bompreco.com        │
│ Endereço:  Rua das Compras, 123        │
│ Ativo:     ✅ Sim                      │
└─────────────────────────────────────────┘

[Salvar Fornecedor]
```

#### 3. Registrar Despesa

```
Login → Menu → Saídas → [+ Nova Saída]

┌─────────────────────────────────────────┐
│ NOVA SAÍDA                             │
├─────────────────────────────────────────┤
│ Data:        20/11/2024 14:30          │
│ Valor:       R$ 185,90                 │
│ Categoria:   Mat. Higiene e Limpeza    │
│ Centro:      Congregação São João      │
│ Fornecedor:  Supermercado Bom Preço    │
│ Meio Pgto:   Débito                    │
│ Tipo Desp:   ○ Fixa  🔁 Variável      │
│ Nº Doc:      NF-987654                 │
│ Descrição:   Material de limpeza       │
│ Obs:         Detergente, água sanit,   │
│              sabão, panos, vassouras   │
│ Comprovante: [anexar_nota_fiscal.pdf]  │
└─────────────────────────────────────────┘

[Registrar Saída]
```

#### 4. Conferir

```
✅ SAÍDA REGISTRADA!

Dashboard atualizado:
├─ Saídas do Mês: +R$ 185,90
└─ Saldo Digital: atualizado
```

---

## 🎉 CASO 8: Evento Especial

### Contexto
**Personagem:** Ana Costa - Tesoureira  
**Situação:** Realização de evento beneficente  
**Data:** 25/11/2024

### Receitas do Evento

```
EVENTO BENEFICENTE - 25/11/2024
────────────────────────────────

ENTRADAS:
├─ Venda de lanches:    R$ 850,00 (Dinheiro)
├─ Venda de artesanato: R$ 320,00 (Dinheiro)
├─ Ofertas:             R$ 180,00 (Dinheiro)
└─ PIX durante evento:  R$ 250,00 (Digital)
──────────────────────────────────────────
TOTAL ARRECADADO:       R$ 1.600,00
```

### Despesas do Evento

```
DESPESAS:
├─ Compra ingredientes: R$ 420,00
├─ Material descartável: R$ 85,00
├─ Decoração:           R$ 120,00
└─ Aluguel equipamento: R$ 150,00
──────────────────────────────────────────
TOTAL DESPESAS:         R$ 775,00
```

### Registrar no Sistema

#### 1. Registrar Entradas

```
ENTRADA #1: Venda de Lanches
┌─────────────────────────────────────────┐
│ Data:        25/11/2024 18:00          │
│ Valor:       R$ 850,00                 │
│ Fonte:       Ofertas (evento)          │
│ Meio Pgto:   Dinheiro                  │
│ Descrição:   Evento Beneficente -      │
│              Venda Lanches             │
└─────────────────────────────────────────┘

ENTRADA #2: Venda de Artesanato
┌─────────────────────────────────────────┐
│ Data:        25/11/2024 18:00          │
│ Valor:       R$ 320,00                 │
│ Fonte:       Ofertas (evento)          │
│ Meio Pgto:   Dinheiro                  │
│ Descrição:   Evento Beneficente -      │
│              Venda Artesanato          │
└─────────────────────────────────────────┘

ENTRADA #3: Ofertas
┌─────────────────────────────────────────┐
│ Data:        25/11/2024 18:00          │
│ Valor:       R$ 180,00                 │
│ Fonte:       Ofertas                   │
│ Meio Pgto:   Dinheiro                  │
│ Descrição:   Ofertas Evento Beneficente│
└─────────────────────────────────────────┘

ENTRADA #4: PIX
┌─────────────────────────────────────────┐
│ Data:        25/11/2024 17:30          │
│ Valor:       R$ 250,00                 │
│ Fonte:       Ofertas                   │
│ Meio Pgto:   PIX                       │
│ Descrição:   Ofertas PIX - Evento      │
└─────────────────────────────────────────┘
```

#### 2. Registrar Despesas

```
SAÍDA #1: Ingredientes
┌─────────────────────────────────────────┐
│ Data:        24/11/2024 10:00          │
│ Valor:       R$ 420,00                 │
│ Categoria:   Despesas Diversas         │
│ Descrição:   Ingredientes p/ Evento    │
│ Tipo Desp:   Variável                  │
└─────────────────────────────────────────┘

SAÍDA #2: Descartáveis
┌─────────────────────────────────────────┐
│ Data:        24/11/2024 10:00          │
│ Valor:       R$ 85,00                  │
│ Categoria:   Despesas Diversas         │
│ Descrição:   Material descartável      │
│              p/ Evento                 │
└─────────────────────────────────────────┘

SAÍDA #3: Decoração
┌─────────────────────────────────────────┐
│ Data:        25/11/2024 08:00          │
│ Valor:       R$ 120,00                 │
│ Categoria:   Despesas Diversas         │
│ Descrição:   Decoração Evento          │
└─────────────────────────────────────────┘

SAÍDA #4: Aluguel
┌─────────────────────────────────────────┐
│ Data:        25/11/2024 08:00          │
│ Valor:       R$ 150,00                 │
│ Categoria:   Despesas Diversas         │
│ Descrição:   Aluguel equipamento som   │
│              p/ Evento                 │
└─────────────────────────────────────────┘
```

### Balanço do Evento

```
RESULTADO DO EVENTO
──────────────────────────────

Entradas:  R$ 1.600,00
Despesas:  R$   775,00
──────────────────────────────
LUCRO:     R$   825,00

✅ Evento bem-sucedido!
```

---

## 💸 CASO 9: Transferência Entre Contas

### Contexto
**Personagem:** Paulo Ferreira - Tesoureiro  
**Situação:** Transferir dinheiro do cofre para o banco  
**Data:** 18/11/2024  
**Valor:** R$ 2.000,00

### Passo a Passo

```
Login → Menu → Transferências → [+ Nova Transferência]

┌─────────────────────────────────────────┐
│ NOVA TRANSFERÊNCIA INTERNA             │
├─────────────────────────────────────────┤
│ Data:             18/11/2024 10:30     │
│ Valor:            R$ 2.000,00          │
│                                        │
│ ORIGEM:                                │
│ ├─ Meio Pgto:     Dinheiro             │
│ └─ Centro Custo:  Sede                 │
│                                        │
│ DESTINO:                               │
│ ├─ Meio Pgto:     Conta Bancária       │
│ └─ Centro Custo:  Sede                 │
│                                        │
│ Descrição: Depósito de caixa físico    │
│            na conta bancária da igreja │
│                                        │
│ Comprovante: [anexar_comprovante.pdf]  │
│                                        │
│ [Cancelar]  [Registrar Transferência]  │
└─────────────────────────────────────────┘
```

### Resultado

```
✅ TRANSFERÊNCIA REGISTRADA!

ANTES:
├─ Caixa Físico:  R$ 3.500,00
└─ Caixa Digital: R$ 1.200,00

DEPOIS:
├─ Caixa Físico:  R$ 1.500,00
└─ Caixa Digital: R$ 3.200,00

✅ Saldo Total Mantido: R$ 4.700,00
```

---

## 📊 CASO 10: Consulta de Relatórios

### Contexto
**Personagem:** Pastor José - Liderança  
**Situação:** Consultar situação financeira  
**Data:** 28/11/2024

### Relatórios Disponíveis

#### 1. Dashboard

```
Login → Dashboard

VISÃO GERAL - NOVEMBRO/2024
───────────────────────────────

┌────────────────────────────────┐
│ 💰 ENTRADAS DO MÊS            │
│    R$ 12.500,00                │
│    ▲ +8% vs mês anterior       │
└────────────────────────────────┘

┌────────────────────────────────┐
│ 💸 SAÍDAS DO MÊS              │
│    R$ 6.800,00                 │
│    ▼ -3% vs mês anterior       │
└────────────────────────────────┘

┌────────────────────────────────┐
│ 💵 SALDO ATUAL                │
│    R$ 5.700,00                 │
└────────────────────────────────┘
```

#### 2. Fluxo de Caixa

```
Login → Relatórios → Fluxo de Caixa

Período: 01/11/2024 a 30/11/2024

| Data  | Entradas | Saídas   | Saldo Dia | Acumulado |
|-------|----------|----------|-----------|-----------|
| 01/11 | R$ 800   | R$ 0     | R$ 800    | R$ 800    |
| 02/11 | R$ 0     | R$ 380   | -R$ 380   | R$ 420    |
| 05/11 | R$ 2.870 | R$ 0     | R$ 2.870  | R$ 3.290  |
| ...   | ...      | ...      | ...       | ...       |
| 30/11 | R$ 500   | R$ 250   | R$ 250    | R$ 5.700  |
```

#### 3. Balancete Mensal

```
Login → Relatórios → Balancete Mensal

CONGREGAÇÃO CENTRO - NOVEMBRO/2024
───────────────────────────────────

RECEITAS:
├─ Dízimos:      R$ 10.000,00
├─ Ofertas:      R$  2.000,00
└─ CO:           R$    500,00
────────────────────────────────
TOTAL RECEITAS:  R$ 12.500,00

DESPESAS:
├─ Energia:      R$    380,50
├─ Água:         R$    120,00
├─ Limpeza:      R$    250,00
├─ Diversas:     R$  6.049,50
────────────────────────────────
TOTAL DESPESAS:  R$  6.800,00

SALDO FINAL:     R$  5.700,00

[Gerar PDF]
```

---

## ✅ Conclusão dos Casos de Uso

Estes 10 casos cobrem as **situações mais comuns** do dia a dia:

1. ✅ Registro de entradas (domingo)
2. ✅ Pagamento de despesas
3. ✅ Fechamento mensal
4. ✅ Aprovação de prestações
5. ✅ Fechamento consolidado da Sede
6. ✅ Correção de erros
7. ✅ Compra de materiais
8. ✅ Eventos especiais
9. ✅ Transferências internas
10. ✅ Consulta de relatórios

---

**💡 Dica Final:**  
**Use estes exemplos como guia para suas próprias operações!**

---

© 2025 - Sistema de Tesouraria Eclesiástica  
Casos de Uso Práticos v1.0
