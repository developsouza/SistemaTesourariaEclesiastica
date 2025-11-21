# ?? Troubleshooting: Escala de Porteiros

## Problemas Comuns e Soluções

### 1. ? Botão "Gerar Escala" não funciona

#### Sintomas:
- Clica no botão mas nada acontece
- Console do navegador mostra erro 400
- Mensagem: "AntiForgeryToken validation failed"

#### Solução:
? **JÁ CORRIGIDO na versão atual!**

Se ainda ocorrer:
1. Limpe o cache do navegador (Ctrl+Shift+Delete)
2. Recarregue a página (Ctrl+F5)
3. Verifique se há atualizações pendentes

---

### 2. ? Nenhum dia é sugerido automaticamente

#### Sintomas:
- Clica em "Sugerir Dias Automaticamente"
- Lista aparece vazia

#### Causas Prováveis:

**Causa 1: Sem configurações cadastradas**
```
Solução:
1. Vá em: Escala de Porteiros > Configurar Cultos
2. Cadastre pelo menos 1 configuração
   Exemplo: Domingo - Culto Evangelístico
```

**Causa 2: Configurações inativas**
```
Solução:
1. Vá em: Escala de Porteiros > Configurar Cultos
2. Verifique se Status = Ativo
3. Se estiver Inativo, edite e ative
```

**Causa 3: Datas específicas no passado**
```
Solução:
1. Configurações com data específica no passado não aparecem
2. Crie novas configurações com datas futuras
3. Ou use configurações semanais (Dia da Semana)
```

---

### 3. ? Mensagem: "Não há porteiros cadastrados"

#### Sintomas:
- Ao tentar gerar escala, aparece aviso vermelho

#### Solução:
```
1. Vá em: Escala de Porteiros > Gerenciar Porteiros
2. Clique em: [+ Novo Porteiro]
3. Preencha:
   - Nome Completo: João Silva
   - Telefone: (11) 99999-9999
   - Status: Ativo ?
4. Salve
5. Repita para cadastrar mais porteiros (mínimo 2 recomendado)
```

---

### 4. ? Mensagem: "Não há responsáveis cadastrados"

#### Sintomas:
- Ao tentar gerar escala, aparece aviso vermelho

#### Solução:
```
1. Vá em: Escala de Porteiros > Gerenciar Responsáveis
2. Clique em: [+ Novo Responsável]
3. Preencha:
   - Nome Completo: Pastor José
   - Telefone: (11) 98888-8888
   - E-mail: pastor@igreja.com
   - Status: Ativo ?
4. Salve
```

---

### 5. ? Erro: "Já existem escalas para os dias selecionados"

#### Sintomas:
- Escala não é gerada
- Mensagem diz que dias já têm escala

#### Solução Opção 1: Excluir escalas antigas
```
1. Vá em: Escala de Porteiros > Visualizar Escalas
2. Filtre o período que deseja regerar
3. Clique em [Excluir Período]
4. Confirme
5. Volte e gere novamente
```

#### Solução Opção 2: Gerar para período diferente
```
1. Escolha datas que ainda não têm escala
2. Ou escolha datas mais futuras
```

---

### 6. ? Datas aparecem com dia errado

#### Sintomas:
- Seleciona 20/11/2024 mas aparece 19/11/2024
- Datas ficam com 1 dia a menos

#### Causa:
Problema de timezone (já corrigido!)

#### Se ainda ocorrer:
1. Atualize o sistema
2. Limpe cache do navegador
3. Recarregue a página

---

### 7. ? Tipo de culto "Congresso" não aparece

#### Sintomas:
- Na lista de tipos só aparecem até "Culto Especial"
- Não tem opção "Congresso"

#### Solução:
1. Sistema foi atualizado
2. Reinicie a aplicação
3. Se usar IIS, reinicie o pool
4. Limpe cache do navegador

---

### 8. ? Escala gerada mas sem porteiros designados

#### Sintomas:
- Escala criada com sucesso
- Mas porteiros aparecem vazios

#### Causas:

**Causa 1: Porteiros inativos**
```
Solução:
1. Vá em: Gerenciar Porteiros
2. Verifique se há porteiros ATIVOS
3. Ative pelo menos 2 porteiros
```

**Causa 2: Sistema de rotação**
```
Solução:
O sistema distribui automaticamente.
Se só há 1 porteiro, ele será designado para todos os dias.
Recomendado: cadastrar pelo menos 3 porteiros.
```

---

### 9. ? PDF não é gerado / erro ao baixar

#### Sintomas:
- Clica em "Baixar PDF"
- Nada acontece ou erro aparece

#### Soluções:

**Solução 1: Verificar se há escalas no período**
```
1. Certifique-se que o período tem escalas
2. Verifique se a data de início < data de fim
```

**Solução 2: Verificar logo da igreja**
```
1. Vá em: wwwroot/images/
2. Certifique-se que logo.png existe
3. Se não, adicione uma logo (formato PNG, max 2MB)
```

**Solução 3: Verificar logs**
```
1. Abra Output do Visual Studio
2. Procure por erros relacionados a PDF
3. Pode ser problema de permissões de arquivo
```

---

### 10. ? Configuração de culto não salva

#### Sintomas:
- Preenche formulário
- Clica em Salvar
- Volta para página mas não salvou

#### Validações que impedem salvar:

**Validação 1: Campos obrigatórios**
```
? Deve preencher:
- OU Dia da Semana
- OU Data Específica
- Tipo de Culto
- Status (Ativo/Inativo)
```

**Validação 2: Não pode preencher ambos**
```
? ERRO: Preencher Dia da Semana E Data Específica

? CORRETO:
- Culto semanal: Preencher APENAS Dia da Semana
- Evento único: Preencher APENAS Data Específica
```

**Validação 3: Data no passado**
```
? ERRO: Data Específica no passado

? CORRETO: Data deve ser hoje ou futura
```

**Validação 4: Duplicação**
```
? ERRO: Já existe config ativa para aquele dia/data

? CORRETO:
- Desative a configuração antiga primeiro
- Ou edite a configuração existente
```

---

## ?? Ferramentas de Debug

### 1. Console do Navegador
```
Como abrir:
- Chrome/Edge: F12
- Firefox: F12
- Safari: Option+Command+C

O que verificar:
- Aba "Console": Mensagens de erro em vermelho
- Aba "Network": Requisições HTTP, ver se retornou 200 ou erro
```

### 2. Visual Studio Output
```
Como ver:
1. View > Output (ou Ctrl+Alt+O)
2. Selecionar: "Show output from: Debug"

O que procurar:
- Exceções (Exception)
- Mensagens de erro (ERROR)
- Informações de log (INFO)
```

### 3. Banco de Dados
```
Como verificar:
1. SQL Server Object Explorer
2. Conectar ao banco LocalDB
3. Ver tabelas:
   - Porteiros
   - ResponsaveisPorteiros
   - ConfiguracoesCultos
   - EscalasPorteiros
```

---

## ?? Checklist de Diagnóstico

Use esta lista para identificar o problema:

### Configuração Básica:
- [ ] Há pelo menos 1 porteiro ATIVO?
- [ ] Há pelo menos 1 responsável ATIVO?
- [ ] Há pelo menos 1 configuração de culto ATIVA?

### Geração de Escala:
- [ ] Período selecionado é válido (início < fim)?
- [ ] Há dias selecionados?
- [ ] Console do navegador sem erros?
- [ ] Aplicação reiniciada após últimas mudanças?

### Sugestão de Dias:
- [ ] Configurações estão ativas?
- [ ] Período inclui dias da semana configurados?
- [ ] Datas específicas não estão no passado?

### PDF:
- [ ] Há escalas no período selecionado?
- [ ] Arquivo logo.png existe em wwwroot/images/?
- [ ] Permissões de leitura de arquivo OK?

---

## ?? Quando Entrar em Contato com Suporte

Se nenhuma solução acima resolver, reúna estas informações:

```markdown
**Descrição do Problema:**
[Descreva o que está tentando fazer]

**Passos para Reproduzir:**
1. [Passo 1]
2. [Passo 2]
3. [Erro ocorre aqui]

**Erro Exato:**
[Copie a mensagem de erro do console ou tela]

**Screenshot:**
[Anexe imagem da tela com o erro]

**Verificações Feitas:**
- [ ] Há porteiros ativos
- [ ] Há responsáveis ativos
- [ ] Há configurações ativas
- [ ] Console do navegador verificado
- [ ] Aplicação reiniciada

**Ambiente:**
- Navegador: [Chrome/Edge/Firefox]
- Versão: [92.0.4515.107]
- Sistema: [Windows 10/11]
```

---

## ?? Documentos Relacionados

- [Instalação do Sistema de Escala](INSTALACAO_ESCALA_PORTEIROS.md)
- [Manual de Uso](ESCALA_PORTEIROS.md)
- [Configuração de Cultos](CONFIGURACAO_CULTOS_DATA_ESPECIFICA.md)
- [Correção de Bugs](CORRECAO_GERAR_ESCALA.md)

---

**Última Atualização:** 20/11/2024  
**Versão:** 1.0
