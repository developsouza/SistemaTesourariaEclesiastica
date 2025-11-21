# ?? Módulo de Escala de Porteiros

## ?? Visão Geral

O módulo de Escala de Porteiros é uma funcionalidade completa para gerenciar e gerar automaticamente escalas de porteiros para os cultos da igreja.

## ? Funcionalidades

### 1. **Gestão de Porteiros**
- Cadastro completo com nome e telefone
- Status ativo/inativo
- Validação de dados
- Proteção contra exclusão (desativa se houver escalas associadas)

### 2. **Gestão de Responsáveis**
- Cadastro de responsáveis pela coordenação dos porteiros
- Nome e telefone para contato
- Status ativo/inativo

### 3. **Configurações de Cultos**
- Definir dias da semana e tipos de cultos padrão
- Suporte para configurações personalizadas
- Padrões: Domingo (Evangelístico), Quarta (Família), Sexta (Doutrina)
- Permite adicionar cultos especiais em outros dias

### 4. **Geração Automática de Escalas**
- Seleção de período (data início e fim)
- Sugestão automática de dias baseada nas configurações
- Seleção manual de dias adicionais
- Escolha do tipo de culto para cada dia
- **Distribuição justa e automática dos porteiros**
- Prevenção de duplicação de escalas

### 5. **Visualização e Impressão**
- Layout otimizado para impressão
- Tabela com todas as escalas do período
- Rodapé com informações de contato completas
- Lista de todos os porteiros com telefones
- Dados do responsável em destaque

## ?? Como Usar

### Passo 1: Configuração Inicial

#### 1.1 Cadastrar Porteiros
1. Acesse: **Escala de Porteiros ? Gerenciar Porteiros**
2. Clique em **"Novo Porteiro"**
3. Preencha:
   - Nome completo
   - Telefone
   - Status (Ativo)
4. Clique em **"Salvar"**

#### 1.2 Cadastrar Responsáveis
1. Acesse: **Escala de Porteiros ? Gerenciar Responsáveis**
2. Clique em **"Novo Responsável"**
3. Preencha:
   - Nome completo
   - Telefone
   - Status (Ativo)
4. Clique em **"Salvar"**

#### 1.3 Configurar Dias de Cultos (Opcional)
1. Acesse: **Escala de Porteiros ? Configurar Cultos**
2. Clique em **"Nova Configuração"**
3. Selecione:
   - Dia da semana
   - Tipo de culto
   - Observações (opcional)
4. Clique em **"Salvar"**

**Configurações Padrão:**
- **Domingo** - Culto Evangelístico
- **Quarta-feira** - Culto da Família
- **Sexta-feira** - Culto de Doutrina

### Passo 2: Gerar Escala

1. Acesse: **Escala de Porteiros ? Gerar Nova Escala**
2. Defina o período:
   - Data de início (ex: 01/01/2025)
   - Data de fim (ex: 31/01/2025)
3. Clique em **"Sugerir Dias Automaticamente"**
   - O sistema preencherá automaticamente os dias baseado nas configurações
4. **Ajuste conforme necessário:**
   - Adicione dias extras
   - Altere tipos de cultos
   - Remova dias não desejados
5. Clique em **"Gerar Escala"**
6. O sistema distribuirá os porteiros automaticamente de forma justa

### Passo 3: Visualizar e Imprimir

1. Após gerar, você será redirecionado para a visualização
2. Revise a escala gerada
3. Clique em **"Imprimir"** para obter a versão impressa
4. A impressão incluirá:
   - Tabela completa da escala
   - Todos os porteiros com telefones
   - Dados do responsável

### Passo 4: Gerenciar Escalas

#### Visualizar Escalas Existentes
1. Acesse: **Escala de Porteiros ? Visualizar Escalas**
2. Use os filtros de data para encontrar escalas
3. Visualize todas as escalas cadastradas

#### Editar uma Escala
1. Na lista de escalas, clique no ícone de **editar** (??)
2. Altere conforme necessário:
   - Data do culto
   - Tipo de culto
   - Porteiro designado
   - Responsável
   - Observações
3. Clique em **"Salvar Alterações"**

#### Excluir uma Escala
1. Na lista de escalas, clique no ícone de **excluir** (???)
2. Confirme a exclusão

## ?? Recursos Avançados

### Distribuição Justa de Porteiros
O sistema utiliza um algoritmo inteligente que:
- Conta quantas vezes cada porteiro foi escalado recentemente
- Distribui as escalas de forma equilibrada
- Em caso de empate, sorteia aleatoriamente
- Considera apenas porteiros ativos

### Prevenção de Duplicação
- O sistema não permite criar escalas duplicadas para a mesma data
- Ao tentar gerar, dias já escalados serão ignorados automaticamente
- Aviso é exibido quando isso ocorre

### Tipos de Cultos Disponíveis
1. **Culto Evangelístico** - Geralmente aos domingos
2. **Culto da Família** - Geralmente às quartas-feiras
3. **Culto de Doutrina** - Geralmente às sextas-feiras
4. **Culto Especial** - Para eventos especiais
5. **Outro Tipo de Culto** - Personalizável

## ?? Permissões

O módulo está disponível para:
- ? Administrador
- ? Tesoureiro Geral
- ? Tesoureiro Local

## ?? Dicas de Uso

### Para Melhor Organização:
1. **Configure os dias padrão** antes de gerar a primeira escala
2. **Mantenha os dados atualizados** - telefones e status dos porteiros
3. **Gere escalas mensalmente** para melhor controle
4. **Imprima e distribua** - fixe em local visível na igreja
5. **Revise antes de imprimir** - confira se todos os dados estão corretos

### Em Caso de Imprevistos:
1. Use o botão **"Editar"** para trocar um porteiro específico
2. Adicione **observações** para comunicar mudanças
3. Gere uma nova escala se necessário

### Para Cultos Especiais:
1. Use o tipo **"Culto Especial"** 
2. Selecione manualmente a data
3. Adicione observações relevantes (ex: "Culto de Aniversário")

## ?? Impressão

A funcionalidade de impressão foi otimizada para:
- Formato A4
- Layout limpo e profissional
- Tabela legível
- Contatos destacados
- Informações do responsável em destaque

### Como Imprimir:
1. Acesse a visualização da escala
2. Clique no botão **"Imprimir"**
3. Configure sua impressora (ou salve como PDF)
4. Imprima

## ?? Solução de Problemas

### "Não há porteiros cadastrados"
**Solução:** Cadastre pelo menos um porteiro ativo antes de gerar escalas.

### "Não há responsáveis cadastrados"
**Solução:** Cadastre pelo menos um responsável ativo antes de gerar escalas.

### "Já existe escala para este dia"
**Solução:** Verifique a lista de escalas e edite a existente ou exclua-a antes de gerar uma nova.

### "Nenhum dia selecionado"
**Solução:** Use o botão "Sugerir Dias Automaticamente" ou adicione dias manualmente.

## ?? Atualizações Futuras Sugeridas

Possíveis melhorias para versões futuras:
- [ ] Notificações por WhatsApp/SMS para os porteiros
- [ ] Sistema de confirmação de presença
- [ ] Histórico de escalas por porteiro
- [ ] Relatórios estatísticos de participação
- [ ] Importação/Exportação de escalas
- [ ] Múltiplos responsáveis por período

## ?? Suporte

Em caso de dúvidas ou problemas:
1. Verifique este guia primeiro
2. Consulte o administrador do sistema
3. Entre em contato com o suporte técnico

---

**Versão:** 1.0  
**Data:** Janeiro 2025  
**Desenvolvido para:** Sistema de Tesouraria Eclesiástica - AD Jacumã
