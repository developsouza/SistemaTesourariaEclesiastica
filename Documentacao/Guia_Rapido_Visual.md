# 🎯 Guia Rápido Visual - Sistema de Tesouraria

## 📱 Acesso Rápido por Perfil

---

## 👤 TESOUREIRO LOCAL (Congregação)

### ⚡ Tarefas Diárias

```
┌──────────────────────────────┐
│  ROTINA DIÁRIA              │
└──────────────────────────────┘

🌅 MANHÃ
├─ Verificar Dashboard
├─ Conferir saldo disponível
└─ Checar notificações

📊 DURANTE O DIA
├─ Registrar Entradas
│  └─ Menu → Entradas → [+ Nova]
│
├─ Registrar Saídas
│  └─ Menu → Saídas → [+ Nova]
│
└─ Anexar Comprovantes

🌙 FIM DO DIA
├─ Conferir lançamentos
└─ Verificar pendências
```

---

### 📅 Tarefas Mensais

```
┌──────────────────────────────┐
│  ROTINA MENSAL              │
└──────────────────────────────┘

📆 DIA 28-30 (Fim do Mês)
│
├─ 1️⃣  Revisar Lançamentos
│    └─ Conferir se tudo está registrado
│
├─ 2️⃣  Criar Fechamento
│    ├─ Menu → Fechamentos
│    ├─ [Criar Fechamento]
│    └─ Preencher dados
│
├─ 3️⃣  Aguardar Aprovação
│    └─ Status: PENDENTE
│
└─ 4️⃣  Se Aprovado
     └─ Status: APROVADO → PROCESSADO
```

---

## 👔 TESOUREIRO GERAL (Sede)

### 📋 Processo de Aprovação

```
┌──────────────────────────────────────┐
│  APROVAR PRESTAÇÕES               │
└──────────────────────────────────────┘

1️⃣  ACESSAR PENDENTES
    └─ Menu → Fechamentos
       └─ Filtrar: Status = PENDENTE

2️⃣  REVISAR CADA PRESTAÇÃO
    ├─ [Ver Detalhes]
    ├─ Conferir totais
    ├─ Analisar lançamentos
    └─ Verificar comprovantes

3️⃣  DECISÃO
    ├─ ✅ APROVAR
    │  └─ Status → APROVADO
    │
    └─ ❌ REJEITAR
       ├─ Informar motivo
       └─ Status → REJEITADO
```

---

### 📊 Fechamento Consolidado

```
┌──────────────────────────────────────┐
│  CRIAR FECHAMENTO DA SEDE         │
└──────────────────────────────────────┘

1️⃣  APROVAR CONGREGAÇÕES
    └─ Todas as prestações APROVADAS

2️⃣  REGISTRAR LANÇAMENTOS DA SEDE
    ├─ Entradas próprias
    └─ Despesas próprias

3️⃣  CRIAR FECHAMENTO CONSOLIDADO
    ├─ Menu → Fechamentos
    ├─ [Criar Fechamento da Sede]
    ├─ Selecionar prestações
    └─ Sistema aplica rateios

4️⃣  RESULTADO
    ├─ Congregações → PROCESSADAS
    ├─ Rateios aplicados
    └─ Saldo final calculado
```

---

## 📈 Fluxograma do Sistema

### 🔄 Ciclo Completo Mensal

```
CONGREGAÇÃO                    SEDE
──────────                    ─────

DIA A DIA
├─ Registra Entradas
├─ Registra Saídas
└─ Anexa Comprovantes
         │
         ▼
FIM DO MÊS
├─ Cria Fechamento
└─ Status: PENDENTE ────────────────┐
                                    │
                                    ▼
                          REVISA PRESTAÇÃO
                                    │
                          ┌─────────┴─────────┐
                          ▼                   ▼
                      APROVA              REJEITA
                          │                   │
                          ▼                   ▼
                   Status: APROVADO    Status: REJEITADO
                          │                   │
                          │                   └──┐
                          ▼                      ▼
                  AGUARDA INCLUSÃO         CORRIGE E
                  NA SEDE                  REENVIA
                          │
                          ▼
                  FECHAMENTO DA SEDE
                  ├─ Inclui Aprovadas
                  ├─ Lançamentos Sede
                  ├─ Aplica Rateios
                  └─ Status: PROCESSADO
```

---

## 🎨 Legenda de Cores e Ícones

### Status de Fechamento

| Ícone | Status | Cor | Significado |
|-------|--------|-----|-------------|
| 🟡 | PENDENTE | Amarelo | Aguardando aprovação |
| 🟢 | APROVADO | Verde | Aprovado pela Sede |
| 🔵 | PROCESSADO | Azul | Incluído no consolidado |
| 🔴 | REJEITADO | Vermelho | Devolvido para correção |

---

### Tipos de Lançamento

| Ícone | Tipo | Descrição |
|-------|------|-----------|
| ⬇️ | Entrada | Receitas (dízimos, ofertas) |
| ⬆️ | Saída | Despesas e pagamentos |
| 🔄 | Transferência | Movimentações internas |
| 💰 | Rateio | Distribuição automática |

---

### Tipos de Caixa

| Ícone | Tipo | Meios de Pagamento |
|-------|------|-------------------|
| 💵 | Físico | Dinheiro, Cheque |
| 📱 | Digital | PIX, Transferência, Cartão |

---

## 📊 Cards do Dashboard

### Visualização Principal

```
┌─────────────────────────────────────────────────────┐
│                    DASHBOARD                        │
├─────────────────────────────────────────────────────┤
│                                                     │
│  ┌──────────────┐  ┌──────────────┐  ┌───────────┐│
│  │💰 ENTRADAS   │  │💸 SAÍDAS     │  │💵 SALDO   ││
│  │R$ 15.000,00  │  │R$ 8.000,00   │  │R$ 7.000   ││
│  │▲ +12% mês    │  │▼ -5% mês     │  │          ││
│  └──────────────┘  └──────────────┘  └───────────┘│
│                                                     │
│  ┌──────────────────────────────────────────────┐  │
│  │      📈 FLUXO DE CAIXA (6 MESES)           │  │
│  │  [Gráfico de barras: Entradas vs Saídas]   │  │
│  └──────────────────────────────────────────────┘  │
│                                                     │
│  ┌──────────────────────────────────────────────┐  │
│  │      🥧 DESPESAS POR CATEGORIA             │  │
│  │  [Gráfico de pizza: Top 5 categorias]     │  │
│  └──────────────────────────────────────────────┘  │
│                                                     │
└─────────────────────────────────────────────────────┘
```

---

## 🗓️ Calendário de Atividades

### Cronograma Mensal Sugerido

```
SEMANA 1 (Dias 1-7)
├─ Registrar lançamentos diários
└─ Conferir comprovantes

SEMANA 2 (Dias 8-14)
├─ Registrar lançamentos diários
├─ Exportar relatórios parciais
└─ Verificar saldos

SEMANA 3 (Dias 15-21)
├─ Registrar lançamentos diários
├─ Revisar lançamentos anteriores
└─ Corrigir erros (se houver)

SEMANA 4 (Dias 22-28)
├─ Registrar lançamentos diários
├─ Preparar fechamento
└─ Conferir todos os lançamentos

DIA 29-30
├─ 🚨 CRIAR FECHAMENTO MENSAL
└─ Enviar prestação de contas
```

---

## 📝 Checklist Pré-Fechamento

### ✅ Antes de Criar o Fechamento

```
☑️  CONFERIR LANÇAMENTOS
    ├─ Todas as entradas registradas?
    ├─ Todas as saídas registradas?
    └─ Comprovantes anexados?

☑️  VERIFICAR VALORES
    ├─ Valores corretos?
    ├─ Centros de custo corretos?
    └─ Categorias corretas?

☑️  REVISAR SALDOS
    ├─ Caixa Físico bate?
    ├─ Caixa Digital bate?
    └─ Conta bancária conferida?

☑️  DOCUMENTAÇÃO
    ├─ Recibos organizados?
    ├─ Notas fiscais guardadas?
    └─ Comprovantes digitalizados?

☑️  PRONTO PARA FECHAR
    └─ Se tudo OK → [Criar Fechamento]
```

---

## 🎯 Atalhos e Dicas Rápidas

### ⌨️ Navegação Rápida

| Atalho | Ação |
|--------|------|
| `Alt + H` | Ir para Home/Dashboard |
| `Alt + E` | Nova Entrada |
| `Alt + S` | Nova Saída |
| `Alt + F` | Fechamentos |
| `Alt + R` | Relatórios |

---

### 💡 Dicas de Produtividade

```
✅ USE DESCRIÇÕES PADRÃO
   Exemplo: "Dízimo - João Silva - Nov/2024"

✅ CRIE MODELOS DE RATEIO
   Para entradas recorrentes

✅ CADASTRE FORNECEDORES
   Antes de registrar despesas

✅ ANEXE COMPROVANTES
   Sempre que possível

✅ REVISE DIARIAMENTE
   Não deixe acumular
```

---

## 📞 Suporte Rápido

### 🆘 Problemas Comuns

```
❓ PROBLEMA: Não consigo criar fechamento
   ✅ SOLUÇÃO:
      ├─ Verificar se há lançamentos no período
      ├─ Conferir se não há fechamento pendente
      └─ Verificar permissões

❓ PROBLEMA: Valores não batem
   ✅ SOLUÇÃO:
      ├─ Revisar todos os lançamentos
      ├─ Verificar se algo foi esquecido
      └─ Conferir extratos bancários

❓ PROBLEMA: Fechamento foi rejeitado
   ✅ SOLUÇÃO:
      ├─ Ler o motivo da rejeição
      ├─ Corrigir os lançamentos
      └─ Criar novo fechamento

❓ PROBLEMA: Esqueci minha senha
   ✅ SOLUÇÃO:
      ├─ Clicar em "Esqueci minha senha"
      ├─ Verificar e-mail
      └─ Ou contatar administrador
```

---

## 🎓 Vídeos Tutoriais (Links)

```
📺 TUTORIAIS EM VÍDEO

├─ 01. Primeiro Acesso e Navegação
├─ 02. Como Registrar Entradas
├─ 03. Como Registrar Saídas
├─ 04. Criar Fechamento Mensal
├─ 05. Aprovar Prestações (Sede)
├─ 06. Fechamento Consolidado
├─ 07. Gerar Relatórios
└─ 08. Exportar para Excel

[Playlist Completa: youtube.com/tesouraria-tutoriais]
```

---

## 📱 App Mobile (Futuro)

```
🚀 EM BREVE: APLICATIVO MOBILE

├─ Registrar lançamentos pelo celular
├─ Tirar foto de comprovantes
├─ Notificações push
└─ Consulta de saldos em tempo real

[Cadastre-se para ser notificado]
```

---

## 🏆 Melhores Práticas Visuais

### ✨ Organização de Comprovantes

```
PASTA FÍSICA
└─ [Ano]/[Mês]
   ├─ Entradas/
   │  ├─ 001_Dizimo_JoaoSilva.pdf
   │  └─ 002_Oferta_CirculoOracao.pdf
   │
   └─ Saídas/
      ├─ 001_NF_EnergiaEletrica.pdf
      └─ 002_Recibo_MaterialLimpeza.pdf

PASTA DIGITAL (Google Drive/OneDrive)
└─ [Igreja]/[Ano]/[Mês]
   ├─ Entradas/
   └─ Saídas/
```

---

### 📊 Modelo de Planilha Auxiliar

```
CONFERÊNCIA MENSAL
┌──────────┬──────────┬──────────┬──────────┐
│   Data   │ Entrada  │  Saída   │  Saldo   │
├──────────┼──────────┼──────────┼──────────┤
│ 01/11    │  500,00  │    0,00  │  500,00  │
│ 02/11    │  300,00  │  150,00  │  650,00  │
│ 03/11    │    0,00  │  200,00  │  450,00  │
│   ...    │   ...    │   ...    │   ...    │
├──────────┼──────────┼──────────┼──────────┤
│ TOTAL    │ 15.000   │ 8.000    │ 7.000    │
└──────────┴──────────┴──────────┴──────────┘
```

---

## 🎨 Cores por Tipo de Transação

```
LEGENDA DE CORES NOS RELATÓRIOS

🟢 VERDE   → Entradas (Receitas)
🔴 VERMELHO → Saídas (Despesas)
🔵 AZUL    → Transferências
🟣 ROXO    → Rateios
🟡 AMARELO → Pendentes
⚫ CINZA   → Cancelados
```

---

## 📖 Dicionário Ilustrado

### 💰 Entrada
```
┌─────────────────────┐
│ ⬇️  ENTRADA         │
├─────────────────────┤
│ Dinheiro que ENTRA  │
│ na igreja           │
│                     │
│ Exemplos:           │
│ • Dízimos           │
│ • Ofertas           │
│ • Doações           │
└─────────────────────┘
```

### 💸 Saída
```
┌─────────────────────┐
│ ⬆️  SAÍDA           │
├─────────────────────┤
│ Dinheiro que SAI    │
│ da igreja           │
│                     │
│ Exemplos:           │
│ • Contas (luz/água) │
│ • Materiais         │
│ • Manutenção        │
└─────────────────────┘
```

### 📊 Fechamento
```
┌─────────────────────┐
│ 📊 FECHAMENTO       │
├─────────────────────┤
│ Consolidação do mês │
│                     │
│ Contém:             │
│ • Todas entradas    │
│ • Todas saídas      │
│ • Saldo final       │
└─────────────────────┘
```

---

## ✅ Aprovação/Rejeição Visual

```
FLUXO DE APROVAÇÃO
─────────────────────

Congregação                  Sede
────────────                ────

[Criar Fechamento]
       │
       ▼
  🟡 PENDENTE ──────────────→ [Revisar]
                                  │
                    ┌─────────────┴─────────────┐
                    ▼                           ▼
              ✅ APROVAR                  ❌ REJEITAR
                    │                           │
                    ▼                           ▼
             🟢 APROVADO                  🔴 REJEITADO
                    │                           │
                    ▼                           ▼
            Aguarda Inclusão              Corrigir Erros
                    │                           │
                    ▼                           │
             🔵 PROCESSADO                     │
                    │                           │
                    └───────── FIM ←────────────┘
```

---

## 🎯 Resumo Ultra-Rápido

### TESOUREIRO LOCAL
```
1. Registre entradas/saídas DIARIAMENTE
2. Anexe comprovantes
3. Crie fechamento TODO FIM DE MÊS
4. Aguarde aprovação
```

### TESOUREIRO GERAL
```
1. Aprove/Rejeite prestações
2. Registre lançamentos da Sede
3. Crie fechamento consolidado
4. Gere relatórios mensais
```

### PASTOR/LIDERANÇA
```
1. Acompanhe Dashboard
2. Consulte Relatórios
3. Monitore Saldos
4. Valide Prestações
```

---

**🎓 Dica Final:**  
**Imprima este guia e mantenha próximo ao computador!**

---

© 2025 - Sistema de Tesouraria Eclesiástica  
Guia Rápido Visual v1.0
