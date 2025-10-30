# 📘 Manual do Usuário - Sistema de Tesouraria Eclesiástica

## Versão 1.0 - 2024

---

## 📑 Índice

1. [Introdução](#introdução)
2. [Primeiros Passos](#primeiros-passos)
3. [Perfis de Acesso](#perfis-de-acesso)
4. [Funcionalidades por Módulo](#funcionalidades-por-módulo)
5. [Processos Principais](#processos-principais)
6. [Relatórios](#relatórios)
7. [Perguntas Frequentes](#perguntas-frequentes)
8. [Suporte](#suporte)

---

## 📖 Introdução

### O que é o Sistema de Tesouraria Eclesiástica?

O **Sistema de Tesouraria Eclesiástica** é uma plataforma completa desenvolvida para gerenciar as finanças de igrejas, incluindo:

- ✅ Controle de entradas (dízimos, ofertas, doações)
- ✅ Gestão de despesas e pagamentos
- ✅ Fechamentos contábeis mensais e diários
- ✅ Prestação de contas entre congregações e sede
- ✅ Rateios automáticos
- ✅ Relatórios gerenciais e financeiros
- ✅ Auditoria completa de operações

### Para quem é este manual?

Este manual foi criado para **usuários finais** que irão utilizar o sistema no dia a dia, incluindo:

- 👤 Tesoureiros Locais (Congregações)
- 👤 Tesoureiro Geral (Sede)
- 👤 Pastores e Líderes
- 👤 Administradores do Sistema

---

## 🚀 Primeiros Passos

### 1. Acessando o Sistema

1. Abra seu navegador (Chrome, Edge ou Firefox)
2. Digite o endereço: `https://[endereco-do-sistema]`
3. Você verá a tela de login:

```
┌─────────────────────────────────┐
│  SISTEMA DE TESOURARIA         │
│  ECLESIÁSTICA                  │
├─────────────────────────────────┤
│  E-mail: [____________]        │
│  Senha:  [____________]        │
│                                │
│  [ ] Lembrar-me               │
│                                │
│  [   Entrar   ]               │
└─────────────────────────────────┘
```

4. Digite seu e-mail e senha fornecidos pelo administrador
5. Clique em **"Entrar"**

### 2. Primeiro Acesso

No primeiro acesso, você verá o **Dashboard** com:

- 📊 Resumo financeiro do mês
- 📈 Gráficos de entradas e saídas
- 📋 Últimas transações
- 🔔 Alertas e notificações

### 3. Conhecendo a Interface

```
┌──────────────────────────────────────────────────────┐
│ ☰ Menu  |  Dashboard  |  Olá, [Seu Nome]  |  🔔 [3]│
├──────────────────────────────────────────────────────┤
│                                                      │
│  PAINEL DE CONTROLE                                 │
│                                                      │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐         │
│  │Entradas  │  │Saídas    │  │Saldo     │         │
│  │R$ X.XXX  │  │R$ X.XXX  │  │R$ X.XXX  │         │
│  └──────────┘  └──────────┘  └──────────┘         │
│                                                      │
│  [Gráfico de Fluxo de Caixa]                       │
│                                                      │
└──────────────────────────────────────────────────────┘
```

**Elementos principais:**

- **Menu Lateral (☰)**: Acesso a todos os módulos
- **Barra Superior**: Informações do usuário e notificações
- **Dashboard**: Visão geral das finanças
- **Botões de Ação**: Cadastrar, editar, excluir registros

---

## 👥 Perfis de Acesso

O sistema possui **4 perfis** com diferentes permissões:

### 🔴 1. Administrador

**Acesso Total ao Sistema**

- ✅ Gerencia todos os centros de custo
- ✅ Cria e gerencia usuários
- ✅ Aprova fechamentos de congregações
- ✅ Configura regras de rateio
- ✅ Acessa todos os relatórios
- ✅ Visualiza logs de auditoria

**Usuário Padrão:**
- E-mail: `admin@tesouraria.com`
- Senha: `Admin@123` (alterar no primeiro acesso)

---

### 🔵 2. Tesoureiro Geral

**Gerencia a Sede e Aprova Prestações**

- ✅ Acessa dados da Sede
- ✅ Registra entradas e saídas da Sede
- ✅ **Aprova ou rejeita** fechamentos de congregações
- ✅ Cria fechamentos consolidados da Sede
- ✅ Acessa todos os relatórios
- ❌ Não gerencia usuários

**Responsabilidades:**

1. **Receber prestações** das congregações
2. **Aprovar fechamentos** após validação
3. **Criar fechamento mensal da Sede** incluindo as congregações
4. **Gerar relatórios** financeiros gerais

---

### 🟢 3. Tesoureiro Local

**Gerencia Congregação Local**

- ✅ Registra entradas e saídas da sua congregação
- ✅ Cria fechamentos mensais para prestação de contas
- ✅ Visualiza relatórios da sua congregação
- ❌ Não acessa dados de outras congregações
- ❌ Não aprova fechamentos

**Fluxo de Trabalho:**

```
1. Registra Entradas/Saídas → 2. Cria Fechamento → 3. Aguarda Aprovação
```

---

### 🟡 4. Pastor/Liderança

**Acesso de Consulta**

- ✅ Visualiza relatórios financeiros
- ✅ Acompanha o saldo e movimentações
- ❌ Não registra transações
- ❌ Não aprova fechamentos

---

## 📦 Funcionalidades por Módulo

### 💰 1. ENTRADAS (Receitas)

#### O que são Entradas?

**Entradas** são todas as receitas recebidas pela igreja, como:

- 💵 Dízimos
- 🎁 Ofertas
- 🙏 Votos e Promessas
- 💌 Ofertas do Círculo de Oração
- 🏢 Repasses de Congregações

#### Como Registrar uma Entrada

**Passo a Passo:**

1. Acesse: **Menu → Lançamentos → Entradas**
2. Clique em **[+ Nova Entrada]**
3. Preencha o formulário:

```
┌─────────────────────────────────────────┐
│  NOVA ENTRADA                          │
├─────────────────────────────────────────┤
│  Data:        [DD/MM/AAAA] [HH:MM]    │
│  Valor:       R$ [______._,__]        │
│  Fonte:       [Dízimos ▼]             │
│  Centro:      [Sede ▼]                │
│  Membro:      [João Silva ▼] (opc)   │
│  Meio Pgto:   [Dinheiro ▼]            │
│  Descrição:   [___________________]   │
│  Obs:         [___________________]   │
│  Comprovante: [Anexar arquivo]        │
│                                        │
│  [Cancelar]  [Salvar Entrada]         │
└─────────────────────────────────────────┘
```

4. Clique em **[Salvar Entrada]**

**✅ Dica:** Sempre anexe comprovantes (fotos de recibos, extratos) para facilitar a auditoria.

#### Campos Obrigatórios

| Campo | Descrição | Exemplo |
|-------|-----------|---------|
| **Data** | Data e hora do recebimento | 25/12/2024 10:30 |
| **Valor** | Quantia recebida | R$ 150,00 |
| **Fonte de Renda** | Tipo de entrada | Dízimos |
| **Centro de Custo** | Sede ou Congregação | Congregação Centro |
| **Meio de Pagamento** | Como foi recebido | PIX, Dinheiro, etc. |

#### Tipos de Fontes de Renda

- **Dízimos**: Dízimos regulares dos membros
- **Ofertas**: Ofertas voluntárias diversas
- **Votos**: Promessas e votos especiais
- **Ofertas (Círculo de Oração)**: Ofertas de reuniões específicas
- **Repasse de Congregação**: Valores enviados pela congregação para a sede

---

### 📤 2. SAÍDAS (Despesas)

#### O que são Saídas?

**Saídas** são todas as despesas realizadas pela igreja, como:

- 💡 Energia Elétrica
- 💧 Água
- 📝 Material de Expediente
- 🧹 Material de Limpeza
- 🏗️ Manutenção e Reparos
- 📞 Telefone e Internet

#### Como Registrar uma Saída

**Passo a Passo:**

1. Acesse: **Menu → Lançamentos → Saídas**
2. Clique em **[+ Nova Saída]**
3. Preencha o formulário:

```
┌─────────────────────────────────────────┐
│  NOVA SAÍDA                            │
├─────────────────────────────────────────┤
│  Data:        [DD/MM/AAAA] [HH:MM]    │
│  Valor:       R$ [______._,__]        │
│  Categoria:   [Energia Elétrica ▼]    │
│  Centro:      [Sede ▼]                │
│  Fornecedor:  [CELPE ▼] (opcional)    │
│  Meio Pgto:   [Transferência ▼]       │
│  Tipo Desp:   [🔁 Fixa  ○ Variável]  │
│  Nº Doc:      [NF-12345]              │
│  Vencimento:  [DD/MM/AAAA]            │
│  Descrição:   [___________________]   │
│  Obs:         [___________________]   │
│  Comprovante: [Anexar arquivo]        │
│                                        │
│  [Cancelar]  [Registrar Saída]        │
└─────────────────────────────────────────┘
```

4. Clique em **[Registrar Saída]**

#### Tipos de Despesa

- **Fixa**: Despesas recorrentes mensais (aluguel, salários)
- **Variável**: Despesas ocasionais (reparos, eventos)

#### Principais Categorias de Despesa

- 💡 **Energia Elétrica (Luz)**
- 💧 **Água**
- 🏠 **Aluguel**
- 📝 **Material de Expediente**
- 🧹 **Material de Higiene e Limpeza**
- 🔧 **Manutenção**
- 🏗️ **Material de Construção**
- 📞 **Telefone**
- 🚗 **Despesas com Veículo**
- 💊 **Medicamentos**
- ✈️ **Viagens**
- 🏛️ **IPTU e Taxas**

---

### 📊 3. FECHAMENTOS (Prestação de Contas)

#### O que é um Fechamento?

Um **Fechamento** é o processo de consolidar todas as entradas e saídas de um período (mês ou dia) para:

- ✅ Validar lançamentos
- ✅ Calcular saldos
- ✅ Aplicar rateios (no caso da Sede)
- ✅ Gerar prestação de contas

#### Tipos de Fechamento

| Tipo | Frequência | Usado por |
|------|-----------|-----------|
| **Diário** | Todo dia | Controle diário (opcional) |
| **Mensal** | Todo mês | **Congregações e Sede** |

---

#### 🔹 FECHAMENTO DE CONGREGAÇÃO

**Quem faz:** Tesoureiro Local  
**Quando:** Ao final de cada mês (ex: dia 30/11)  
**Para quê:** Prestar contas à Sede

**Passo a Passo:**

1. Acesse: **Menu → Fechamentos → Criar Fechamento**

2. Preencha:

```
┌─────────────────────────────────────────┐
│  CRIAR FECHAMENTO                      │
├─────────────────────────────────────────┤
│  Tipo:        [● Mensal  ○ Diário]    │
│  Mês/Ano:     [Novembro / 2024]       │
│  Centro:      [Congregação Centro]     │
│  Período:     01/11/2024 - 30/11/2024 │
│                                        │
│  TOTAIS CALCULADOS:                    │
│  Entradas Físicas:  R$ 5.000,00       │
│  Entradas Digitais: R$ 3.000,00       │
│  Saídas Físicas:    R$ 2.500,00       │
│  Saídas Digitais:   R$ 1.500,00       │
│                                        │
│  Balanço Físico:    R$ 2.500,00       │
│  Balanço Digital:   R$ 1.500,00       │
│  SALDO FINAL:       R$ 4.000,00       │
│                                        │
│  Observações:                          │
│  [_____________________________]       │
│                                        │
│  [Cancelar]  [Criar Fechamento]       │
└─────────────────────────────────────────┘
```

3. Clique em **[Criar Fechamento]**

4. **Status:** PENDENTE (aguardando aprovação da Sede)

**✅ Importante:**
- ⚠️ Após criar o fechamento, os lançamentos ficam **travados**
- ⚠️ Se o Tesoureiro Geral **REJEITAR**, os lançamentos são **liberados** para correção
- ⚠️ Se o Tesoureiro Geral **APROVAR**, o fechamento é marcado como **PROCESSADO** quando incluído no fechamento da Sede

---

#### 🔸 FECHAMENTO DA SEDE

**Quem faz:** Tesoureiro Geral  
**Quando:** Após aprovar fechamentos das congregações  
**Para quê:** Consolidar prestações e aplicar rateios

**Passo a Passo:**

1. Acesse: **Menu → Fechamentos → Criar Fechamento da Sede**

2. O sistema mostra **prestações aprovadas** disponíveis:

```
┌─────────────────────────────────────────────────┐
│  CRIAR FECHAMENTO DA SEDE                      │
├─────────────────────────────────────────────────┤
│  Mês/Ano: [Novembro / 2024]                    │
│  Período: 01/11/2024 - 30/11/2024              │
│                                                 │
│  PRESTAÇÕES DISPONÍVEIS (APROVADAS):            │
│                                                 │
│  [✓] Congregação Centro                        │
│      Entradas: R$ 8.000,00                     │
│      Saídas:   R$ 4.000,00                     │
│      Saldo:    R$ 4.000,00                     │
│                                                 │
│  [✓] Congregação Bairro Novo                   │
│      Entradas: R$ 6.000,00                     │
│      Saídas:   R$ 3.000,00                     │
│      Saldo:    R$ 3.000,00                     │
│                                                 │
│  LANÇAMENTOS DA SEDE (Novos):                  │
│      Entradas: R$ 10.000,00                    │
│      Saídas:   R$ 5.000,00                     │
│      Saldo:    R$ 5.000,00                     │
│                                                 │
│  TOTAL CONSOLIDADO:                            │
│  Total Entradas:  R$ 24.000,00                 │
│  Total Saídas:    R$ 12.000,00                 │
│  Saldo Antes Rateio: R$ 12.000,00              │
│                                                 │
│  RATEIOS AUTOMÁTICOS:                          │
│  - FUNDO (10%): R$ 2.400,00                    │
│                                                 │
│  SALDO FINAL: R$ 9.600,00                      │
│                                                 │
│  [Cancelar]  [Criar Fechamento]                │
└─────────────────────────────────────────────────┘
```

3. Clique em **[Criar Fechamento]**

**✅ Resultado:**
- As congregações ficam marcadas como **PROCESSADAS**
- Os rateios são calculados **sobre o total de receitas**
- O saldo final considera: `Receitas - Despesas - Rateios`

---

#### 🔍 Aprovar/Rejeitar Fechamento

**Quem pode:** Tesoureiro Geral ou Administrador  

**Como Aprovar:**

1. Acesse: **Menu → Fechamentos**
2. Localize o fechamento com status **PENDENTE**
3. Clique em **[Ver Detalhes]**
4. Revise os valores e lançamentos
5. Clique em **[Aprovar]**

**Como Rejeitar:**

1. Acesse: **Menu → Fechamentos**
2. Localize o fechamento **PENDENTE**
3. Clique em **[Rejeitar]**
4. Informe o motivo da rejeição
5. Confirme

**✅ Efeito da Rejeição:**
- Status muda para **REJEITADO**
- Lançamentos são **liberados**
- Tesoureiro Local pode corrigir e criar novo fechamento

---

### 💸 4. RATEIOS

#### O que são Rateios?

**Rateios** são distribuições automáticas de valores para fundos específicos, como:

- 💰 **FUNDO DE REPASSE**: 10% das receitas totais da Sede

#### Como Funciona?

1. **Regras criadas:** Administrador configura em **Cadastros → Regras de Rateio**
2. **Aplicação automática:** Ao criar fechamento da Sede
3. **Cálculo:** Percentual aplicado sobre **TOTAL DE RECEITAS** (não sobre o saldo)

**Exemplo:**

```
Total de Receitas da Sede: R$ 24.000,00
Rateio FUNDO (10%):        R$  2.400,00

Saldo Final = (Receitas - Despesas) - Rateios
            = (R$ 24.000 - R$ 12.000) - R$ 2.400
            = R$ 9.600,00
```

---

### 📑 5. CADASTROS

#### 📍 Centros de Custo

**O que são:**  
Representam unidades da igreja (Sede, Congregações, Departamentos).

**Como criar:**

1. Menu → Cadastros → Centros de Custo
2. [+ Novo Centro de Custo]
3. Preencha:
   - Nome: ex. "Congregação São João"
   - Tipo: Sede, Congregação, Departamento, etc.
   - Descrição: breve descrição

---

#### 👥 Membros

**Para quê:**  
Vincular entradas (dízimos, ofertas) a membros específicos.

**Como cadastrar:**

1. Menu → Cadastros → Membros
2. [+ Novo Membro]
3. Preencha dados pessoais e centro de custo

---

#### 🏢 Fornecedores

**Para quê:**  
Registrar empresas e prestadores de serviço.

**Como cadastrar:**

1. Menu → Cadastros → Fornecedores
2. [+ Novo Fornecedor]
3. Preencha nome, CNPJ/CPF, contato

---

#### 💳 Meios de Pagamento

**Padrões já cadastrados:**

- 💵 Dinheiro (Físico)
- 📱 PIX (Digital)
- 💳 Débito (Digital)
- 💳 Crédito (Digital)
- 🏦 Transferência Bancária (Digital)
- 📄 Cheque (Físico)
- 📝 Boleto (Digital)

---

#### 📋 Plano de Contas

**Fontes de Renda (Receitas):**

- Dízimos
- Ofertas
- Votos
- Ofertas (Círculo de Oração)
- Repasse de Congregação

**Categorias de Despesa:**

- Material de Expediente
- Material de Higiene e Limpeza
- Energia Elétrica (Luz)
- Água
- Telefone
- Aluguel
- Manutenção
- E muitas outras...

---

## 📊 Relatórios

### 1. Dashboard (Painel de Controle)

Acesse: **Menu → Dashboard**

**Informações exibidas:**

- 💰 Entradas do Mês
- 💸 Saídas do Mês
- 💵 Saldo Atual
- 📈 Gráfico de Fluxo de Caixa (últimos 6 meses)
- 🥧 Despesas por Categoria (top 5)
- 📋 Últimas Transações (10 mais recentes)
- 📜 Atividades Recentes

---

### 2. Fluxo de Caixa

Acesse: **Menu → Relatórios → Fluxo de Caixa**

**Filtros:**
- Data Início
- Data Fim

**Exibe:**

| Data | Entradas | Saídas | Saldo do Dia | Saldo Acumulado |
|------|----------|--------|--------------|-----------------|
| 01/11 | R$ 500 | R$ 200 | R$ 300 | R$ 300 |
| 02/11 | R$ 300 | R$ 150 | R$ 150 | R$ 450 |

---

### 3. Entradas por Período

Acesse: **Menu → Relatórios → Entradas por Período**

**Filtros:**
- Data Início
- Data Fim

**Exibe:**
- Lista de todas as entradas aprovadas
- Total de entradas
- Quantidade de lançamentos

**Botão:** [Exportar para Excel]

---

### 4. Saídas por Período

Acesse: **Menu → Relatórios → Saídas por Período**

**Filtros:**
- Data Início
- Data Fim
- Centro de Custo (Admin/Tesoureiro Geral)
- Fornecedor

**Exibe:**
- Lista de todas as saídas aprovadas
- Total de saídas
- Quantidade de lançamentos

**Botão:** [Exportar para Excel]

---

### 5. Balancete Geral

Acesse: **Menu → Relatórios → Balancete Geral**

**Exibe:**

| Plano de Contas | Tipo | Entradas | Saídas | Saldo |
|----------------|------|----------|--------|-------|
| Dízimos | Receita | R$ 10.000 | R$ 0 | R$ 10.000 |
| Energia | Despesa | R$ 0 | R$ 500 | -R$ 500 |

---

### 6. Balancete Mensal

Acesse: **Menu → Relatórios → Balancete Mensal**

**Filtros:**
- Centro de Custo
- Mês/Ano

**Exibe:**
- Resumo completo do mês
- Separação por Caixa Físico/Digital
- Detalhes de todas as entradas e saídas
- **Botão:** [Gerar PDF]

---

## ❓ Processos Principais

### 🔄 Processo Mensal da Congregação

```
┌─────────────────────────────────────────┐
│  FLUXO MENSAL - TESOUREIRO LOCAL       │
└─────────────────────────────────────────┘

1️⃣  DIA A DIA DO MÊS
   ↓
   • Registra Entradas (dízimos, ofertas)
   • Registra Saídas (despesas)
   • Anexa comprovantes
   ↓
2️⃣  FINAL DO MÊS (ex: dia 30)
   ↓
   • Cria Fechamento Mensal
   • Status: PENDENTE
   ↓
3️⃣  AGUARDA APROVAÇÃO DA SEDE
   ↓
   • Tesoureiro Geral revisa
   • APROVA ou REJEITA
   ↓
4️⃣  SE APROVADO:
   ↓
   • Status: APROVADO
   • Aguarda inclusão no Fechamento da Sede
   ↓
5️⃣  APÓS INCLUSÃO NA SEDE:
   ↓
   • Status: PROCESSADO
   • Fechamento consolidado
```

---

### 🔄 Processo Mensal da Sede

```
┌─────────────────────────────────────────┐
│  FLUXO MENSAL - TESOUREIRO GERAL       │
└─────────────────────────────────────────┘

1️⃣  RECEBE PRESTAÇÕES
   ↓
   • Congregações enviam fechamentos
   • Status: PENDENTE
   ↓
2️⃣  REVISA E APROVA
   ↓
   • Verifica lançamentos
   • Aprova ou Rejeita cada fechamento
   ↓
3️⃣  REGISTRA LANÇAMENTOS DA SEDE
   ↓
   • Entradas próprias da Sede
   • Despesas da Sede
   ↓
4️⃣  CRIA FECHAMENTO CONSOLIDADO
   ↓
   • Inclui fechamentos aprovados
   • Inclui lançamentos novos da Sede
   • Sistema aplica rateios automáticos
   ↓
5️⃣  GERA RELATÓRIOS
   ↓
   • Balancete Mensal PDF
   • Relatórios gerenciais
```

---

### 🔄 Processo de Aprovação

```
┌─────────────────────────────────────────┐
│  APROVAÇÃO DE PRESTAÇÃO               │
└─────────────────────────────────────────┘

TESOUREIRO GERAL:

1. Acessa: Menu → Fechamentos

2. Vê lista com status:
   • 🟡 PENDENTE (aguardando aprovação)
   • 🟢 APROVADO (aprovado, aguarda consolidação)
   • 🔵 PROCESSADO (incluído na Sede)
   • 🔴 REJEITADO (devolvido para correção)

3. Clica em [Ver Detalhes]

4. Revisa:
   • Totais de Entradas/Saídas
   • Lista de lançamentos
   • Observações

5. Decide:
   • [Aprovar] → Status: APROVADO
   • [Rejeitar] → Informa motivo → Status: REJEITADO

6. Se REJEITADO:
   • Lançamentos são liberados
   • Tesoureiro Local corrige
   • Cria novo fechamento
```

---

## 🔐 Segurança e Auditoria

### Logs de Auditoria

**O que é registrado:**

- ✅ Todas as operações de CRUD (Create, Read, Update, Delete)
- ✅ Login/Logout de usuários
- ✅ Criação e aprovação de fechamentos
- ✅ Alterações em configurações

**Como acessar:**

1. Menu → Administração → Logs de Auditoria

**Informações:**

| Data/Hora | Usuário | Ação | Módulo | Detalhes |
|-----------|---------|------|--------|----------|
| 25/11 10:30 | João Silva | Criação | Entrada | Dízimo R$ 150 |
| 25/11 15:45 | Maria Santos | Aprovação | Fechamento | Prestação Nov/2024 |

---

### Controle de Lançamentos

**✅ Regra Importante:**

- Lançamentos **SÓ APARECEM** em relatórios **APÓS APROVAÇÃO** do fechamento
- Isso garante que **apenas dados validados** sejam considerados oficiais
- Evita dupla contagem de valores

---

## ❓ Perguntas Frequentes (FAQ)

### 1. Como altero minha senha?

1. Clique no seu nome (canto superior direito)
2. [Minha Conta]
3. [Alterar Senha]
4. Digite senha atual e nova senha
5. [Salvar]

---

### 2. Registrei uma entrada errada. Como corrigir?

**Se o fechamento NÃO foi criado:**

1. Menu → Lançamentos → Entradas
2. Localize o lançamento
3. [Editar] ou [Excluir]

**Se o fechamento JÁ foi criado e APROVADO:**

- ❌ **Não é possível alterar**
- ✅ **Solução:** Criar lançamento de **estorno** no mês seguinte

---

### 3. Esqueci de registrar uma despesa. E agora?

**Se o fechamento NÃO foi criado:**

- ✅ Registre normalmente

**Se o fechamento JÁ foi criado:**

- ✅ Registre no **mês seguinte** com observação explicativa

---

### 4. O que fazer se o Tesoureiro Geral rejeitou meu fechamento?

1. Menu → Fechamentos
2. Localize o fechamento REJEITADO
3. Leia o **motivo da rejeição** nas observações
4. **Corrija** os lançamentos que estavam errados
5. **Crie um novo fechamento** com os dados corretos

---

### 5. Como sei quais lançamentos estão incluídos em um fechamento?

1. Menu → Fechamentos
2. Clique em [Ver Detalhes]
3. Role até a seção **"Detalhes do Fechamento"**
4. Você verá **TODOS os lançamentos** incluídos

---

### 6. Por que alguns relatórios mostram valores diferentes do que registrei?

**Resposta:** Relatórios só consideram lançamentos **APROVADOS** (incluídos em fechamentos aprovados).

- Se você registrou entradas hoje, mas ainda não criou fechamento → **Não aparecem**
- Após criar e aprovar o fechamento → **Aparecem nos relatórios**

---

### 7. Como faço para exportar dados para Excel?

1. Acesse o relatório desejado
2. Clique em **[Exportar para Excel]**
3. O arquivo será baixado automaticamente

---

### 8. Posso criar mais de um fechamento no mesmo mês?

**Mensal:** Não. Só 1 fechamento mensal por centro de custo.

**Diário:** Sim. Você pode fazer 1 fechamento por dia (opcional).

---

### 9. O que são Caixa Físico e Caixa Digital?

- **Caixa Físico:** Dinheiro em espécie, cheques
- **Caixa Digital:** PIX, transferências, débito, crédito

**Importância:** Ajuda a separar o que está fisicamente no cofre do que está em conta bancária.

---

### 10. Como funciona o rateio automático?

- Configurado pelo Administrador
- Aplicado **automaticamente** ao criar fechamento da Sede
- Calculado sobre **TOTAL DE RECEITAS**, não sobre o saldo
- Exemplo: FUNDO recebe 10% de todas as receitas

---

## 📞 Suporte

### Contato

**Suporte Técnico:**
- 📧 E-mail: suporte@tesouraria.com
- 📱 WhatsApp: (XX) XXXXX-XXXX
- 🕐 Horário: Segunda a Sexta, 9h às 18h

---

### Reportar Problemas

**Se encontrar um erro:**

1. Tire um **print da tela**
2. Anote:
   - O que você estava fazendo
   - Mensagem de erro (se houver)
   - Data e hora
3. Envie para o suporte

---

### Solicitações de Melhorias

Tem uma ideia para melhorar o sistema?

1. Envie e-mail para: `suporte@tesouraria.com`
2. Assunto: **"Sugestão de Melhoria"**
3. Descreva sua ideia

---

## 📚 Glossário

| Termo | Significado |
|-------|-------------|
| **Centro de Custo** | Unidade da igreja (Sede, Congregação, Departamento) |
| **Fechamento** | Consolidação de lançamentos de um período |
| **Prestação de Contas** | Envio de fechamento da congregação para a Sede |
| **Rateio** | Distribuição automática de valores para fundos |
| **Caixa Físico** | Dinheiro em espécie |
| **Caixa Digital** | Valores em conta bancária |
| **PENDENTE** | Aguardando aprovação |
| **APROVADO** | Aprovado pelo Tesoureiro Geral |
| **PROCESSADO** | Incluído no fechamento da Sede |
| **REJEITADO** | Devolvido para correção |

---

## 🎯 Resumo de Ações Rápidas

### Tesoureiro Local (Congregação)

| O que fazer | Como fazer |
|------------|-----------|
| Registrar Dízimo | Menu → Entradas → [+ Nova Entrada] |
| Pagar Conta de Luz | Menu → Saídas → [+ Nova Saída] |
| Prestar Contas (Fim do Mês) | Menu → Fechamentos → [Criar Fechamento] |
| Ver Relatórios | Menu → Relatórios → [Escolher tipo] |

---

### Tesoureiro Geral (Sede)

| O que fazer | Como fazer |
|------------|-----------|
| Aprovar Prestação | Menu → Fechamentos → [Ver Detalhes] → [Aprovar] |
| Rejeitar Prestação | Menu → Fechamentos → [Ver Detalhes] → [Rejeitar] |
| Criar Fechamento Sede | Menu → Fechamentos → [Criar Fechamento da Sede] |
| Ver Consolidado | Menu → Relatórios → Balancete Mensal |

---

### Pastor/Liderança

| O que fazer | Como fazer |
|------------|-----------|
| Ver Saldo Atual | Menu → Dashboard |
| Conferir Entradas | Menu → Relatórios → Entradas por Período |
| Conferir Saídas | Menu → Relatórios → Saídas por Período |
| Balancete | Menu → Relatórios → Balancete Geral |

---

## ✅ Boas Práticas

### 1. Registro de Lançamentos

- ✅ Registre **no mesmo dia** da operação
- ✅ Sempre anexe **comprovantes**
- ✅ Preencha **descrições claras**
- ✅ Informe **observações** quando necessário

---

### 2. Fechamentos

- ✅ Faça o fechamento **todo final de mês**
- ✅ Não deixe acumular meses sem prestação
- ✅ Revise os valores **antes de criar**
- ✅ Adicione **observações relevantes**

---

### 3. Segurança

- ✅ **Não compartilhe** sua senha
- ✅ Faça **logout** ao sair
- ✅ Use senhas **fortes**
- ✅ Altere sua senha **periodicamente**

---

### 4. Backups

- ✅ O sistema faz backup automático
- ✅ Mas **guarde cópias** de comprovantes importantes
- ✅ **Exporte relatórios** para Excel regularmente

---

## 🎓 Conclusão

Este manual foi desenvolvido para facilitar o uso do **Sistema de Tesouraria Eclesiástica**.

**Lembre-se:**

- 📌 Use o sistema **todos os dias**
- 📌 Faça **fechamentos mensais** regularmente
- 📌 **Anexe comprovantes** sempre
- 📌 Em caso de dúvida, **consulte o suporte**

---

**Versão do Manual:** 1.0  
**Última Atualização:** Dezembro de 2024  
**Sistema:** Tesouraria Eclesiástica v1.0  

---

© 2025 - Sistema de Tesouraria Eclesiástica  
Todos os direitos reservados.
