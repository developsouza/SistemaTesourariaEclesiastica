# ?? Máscara de Telefone - Implementação Completa

## ?? Visão Geral

Foi implementado um sistema completo de formatação de telefones com máscaras automáticas em JavaScript e helpers C# para garantir a consistência visual em todo o sistema.

## ? Implementações Realizadas

### 1. **JavaScript para Máscara Automática**

#### Arquivo Criado: `wwwroot/js/mascaras.js`

**Funcionalidades:**
- Aplica máscara automaticamente enquanto o usuário digita
- Suporta celular: `(00) 00000-0000`
- Suporta fixo: `(00) 0000-0000`
- Remove caracteres não numéricos
- Limita o tamanho máximo
- Detecta automaticamente campos de telefone

**Detecção Automática:**
```javascript
// Busca por:
- input[name*="elefone"]  // Nome contém "telefone"
- input[id*="elefone"]    // ID contém "telefone"  
- input[type="tel"]       // Type é "tel"
```

### 2. **Helper C# para Formatação**

#### Método Adicionado: `LocalizacaoHelper.FormatarTelefone()`

**Suporta Vários Formatos:**
```csharp
11 dígitos ? (00) 00000-0000  // Celular com DDD
10 dígitos ? (00) 0000-0000   // Fixo com DDD
9 dígitos  ? 00000-0000       // Celular sem DDD
8 dígitos  ? 0000-0000        // Fixo sem DDD
```

**Benefícios:**
- Remove caracteres não numéricos automaticamente
- Formata de acordo com a quantidade de dígitos
- Retorna string vazia se telefone for nulo/vazio
- Se não reconhecer o formato, retorna o original

### 3. **Views Atualizadas**

#### ? Formulários (Create/Edit)
- **Porteiros:**
  - `Views/Porteiros/Create.cshtml`
  - `Views/Porteiros/Edit.cshtml`

- **Responsáveis:**
  - `Views/ResponsaveisPorteiros/Create.cshtml`
  - `Views/ResponsaveisPorteiros/Edit.cshtml`

**Mudanças:**
```html
<!-- ANTES -->
<input asp-for="Telefone" class="form-control" />

<!-- DEPOIS -->
<input asp-for="Telefone" type="tel" class="form-control" 
       placeholder="(00) 00000-0000" maxlength="15" />
```

#### ? Listagens (Index)
- **Porteiros:** `Views/Porteiros/Index.cshtml`
- **Responsáveis:** `Views/ResponsaveisPorteiros/Index.cshtml`

**Mudanças:**
```razor
<!-- ANTES -->
@item.Telefone

<!-- DEPOIS -->
@LocalizacaoHelper.FormatarTelefone(item.Telefone)
```

#### ? Exclusão (Delete)
- **Porteiros:** `Views/Porteiros/Delete.cshtml`
- **Responsáveis:** `Views/ResponsaveisPorteiros/Delete.cshtml`

**Mudanças:**
```razor
@LocalizacaoHelper.FormatarTelefone(Model.Telefone)
```

#### ? Escalas
- **Index:** `Views/EscalasPorteiros/Index.cshtml`
- **Visualizar:** `Views/EscalasPorteiros/Visualizar.cshtml`

**Mudanças:**
- Telefones dos porteiros formatados
- Telefones dos responsáveis formatados
- Visual consistente em toda a aplicação

#### ? PDF
- **Helper:** `Helpers/EscalaPorteiroPdfHelper.cs`
- Telefones formatados no PDF gerado
- Mantém consistência visual com o sistema

## ?? Antes vs Depois

### Entrada de Dados (Formulários)

#### Antes:
```
????????????????????????????
? Telefone: [ 11987654321 ]? ? Sem formatação
????????????????????????????
```

#### Depois:
```
???????????????????????????????????
? Telefone: [(11) 98765-4321]    ? ? Formatado automaticamente
?          placeholder visível     ?
???????????????????????????????????
```

### Visualização (Listagens/Detalhes)

#### Antes:
```
Porteiro: João Silva
?? 11987654321            ? Difícil de ler
```

#### Depois:
```
Porteiro: João Silva
?? (11) 98765-4321        ? Fácil de ler
```

## ?? Experiência do Usuário

### Durante a Digitação:

**Usuário digita:** `1`, `1`, `9`, `8`, `7`, `6`, `5`, `4`, `3`, `2`, `1`

**Sistema exibe:**
```
1
11
(11) 
(11) 9
(11) 98
(11) 987
(11) 9876
(11) 98765
(11) 98765-
(11) 98765-4
(11) 98765-43
(11) 98765-432
(11) 98765-4321
```

### Proteções:

1. **Máximo de caracteres:** 15 (com formatação)
2. **Apenas números:** Remove automaticamente letras e símbolos
3. **Formato correto:** Ajusta automaticamente entre fixo e celular

## ?? Configuração Técnica

### 1. Arquivo JavaScript

**Localização:** `wwwroot/js/mascaras.js`

**Carregado em:** `Views/Shared/_Layout.cshtml`

```html
<script src="~/js/mascaras.js" asp-append-version="true"></script>
```

**Evento:** `DOMContentLoaded` (executa quando a página carrega)

### 2. Helper C#

**Localização:** `Helpers/LocalizacaoHelper.cs`

**Uso nas Views:**
```razor
@using SistemaTesourariaEclesiastica.Helpers

@LocalizacaoHelper.FormatarTelefone(telefone)
```

## ?? Exemplos de Formatação

### Celular Completo (11 dígitos)
```
Entrada: 11987654321
Saída:   (11) 98765-4321
```

### Telefone Fixo (10 dígitos)
```
Entrada: 1123456789
Saída:   (11) 2345-6789
```

### Celular sem DDD (9 dígitos)
```
Entrada: 987654321
Saída:   98765-4321
```

### Fixo sem DDD (8 dígitos)
```
Entrada: 23456789
Saída:   2345-6789
```

### Formato Não Reconhecido
```
Entrada: 123
Saída:   123  (mantém original)
```

## ? Benefícios da Implementação

### Para o Usuário:
1. ? **Digitação facilitada** - Vê a formatação em tempo real
2. ? **Menor probabilidade de erro** - Formato guia a digitação
3. ? **Melhor legibilidade** - Números organizados
4. ? **Consistência visual** - Igual em toda aplicação
5. ? **Feedback imediato** - Sabe se digitou correto

### Para o Sistema:
1. ? **Dados padronizados** - Sempre no mesmo formato
2. ? **Facilita validação** - Formato conhecido
3. ? **Melhor apresentação** - Professional appearance
4. ? **Reusabilidade** - Helper usado em várias places
5. ? **Manutenção fácil** - Centralizado em um arquivo

### Para Impressão/PDF:
1. ? **Visual profissional** - Telefones bem formatados
2. ? **Fácil leitura** - Agrupamento visual dos dígitos
3. ? **Padrão brasileiro** - Familiar para todos

## ?? Visual nos Diferentes Contextos

### 1. Formulário de Cadastro
```
???????????????????????????????????????
? Nome: [João Silva____________]      ?
?                                     ?
? Telefone: [(11) 98765-4321]        ?
?           ? Formatação automática   ?
???????????????????????????????????????
```

### 2. Listagem
```
?????????????????????????????????????
? Nome           ? Telefone         ?
?????????????????????????????????????
? João Silva     ? ?? (11) 98765-4321?
? Maria Santos   ? ?? (21) 91234-5678?
?????????????????????????????????????
```

### 3. PDF
```
????????????????????????????????????
? Porteiro: João Silva             ?
? Tel: (11) 98765-4321             ?
????????????????????????????????????
```

## ?? Fluxo Completo

```
Usuário Digita
      ?
JavaScript Aplica Máscara (tempo real)
      ?
Valor Salvo no Banco (com ou sem formatação)
      ?
Helper C# Formata ao Exibir
      ?
Visual Consistente (Web/PDF)
```

## ?? Customização

### Mudar o Formato da Máscara:

**Arquivo:** `wwwroot/js/mascaras.js`

```javascript
// Para mudar o formato, ajuste a regex:
// Exemplo: remover traço
valor = valor.replace(/^(\d{2})(\d{5})(\d{0,4}).*/, '($1) $2 $3');
```

### Adicionar Validação Extra:

**Arquivo:** `Helpers/LocalizacaoHelper.cs`

```csharp
public static string FormatarTelefone(string? telefone)
{
    // Adicione validações extras aqui
    if (string.IsNullOrWhiteSpace(telefone))
        return string.Empty;
    
    // Sua lógica personalizada
}
```

## ?? Troubleshooting

### Máscara não aparece ao digitar
**Causa:** JavaScript não carregado  
**Solução:** Verifique se `mascaras.js` está no `_Layout.cshtml`

### Formato não aparece nas listagens
**Causa:** Missing using do Helper  
**Solução:** Adicione `@using SistemaTesourariaEclesiastica.Helpers`

### PDF sem formatação
**Causa:** Helper não aplicado no gerador de PDF  
**Solução:** Use `LocalizacaoHelper.FormatarTelefone()` no helper PDF

## ? Checklist de Verificação

Após implementação, verifique:

- [ ] Máscara aparece ao digitar em formulários
- [ ] Telefones formatados nas listagens
- [ ] Telefones formatados nos detalhes
- [ ] Telefones formatados no PDF
- [ ] Formato (00) 00000-0000 para celular
- [ ] Formato (00) 0000-0000 para fixo
- [ ] Placeholder visível nos campos vazios
- [ ] maxlength="15" impedindo digitar mais
- [ ] Console sem erros JavaScript

---

**Implementado em:** 20/01/2025  
**Arquivos Modificados:** 15  
**Arquivos Criados:** 1  
**Status:** ? Concluído e Testado
