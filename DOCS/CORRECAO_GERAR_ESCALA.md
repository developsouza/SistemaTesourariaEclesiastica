# ?? Correção: Erro ao Gerar Escala de Porteiros

## ? Problema Identificado

Ao clicar no botão **"Gerar Escala"**, o sistema apresentava erro no console e a escala não era gerada.

### Erro no Console:
```
HTTP 400 Bad Request
AntiForgeryToken validation failed
```

---

## ? Correção Implementada

### 1. **Envio Correto do Token Anti-Forgery**

**Arquivo:** `Views/EscalasPorteiros/Gerar.cshtml`

#### ? Antes (Incorreto):
```javascript
$.ajax({
    url: '@Url.Action("GerarEscala", "EscalasPorteiros")',
    type: 'POST',
    contentType: 'application/json',
    data: JSON.stringify(dados),
    headers: {
        'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
    }
});
```

#### ? Depois (Correto):
```javascript
function getAntiForgeryToken() {
    return $('input[name="__RequestVerificationToken"]').val();
}

$.ajax({
    url: '@Url.Action("GerarEscala", "EscalasPorteiros")',
    type: 'POST',
    contentType: 'application/json',
    data: JSON.stringify(dados),
    beforeSend: function(xhr) {
        // Adicionar token anti-forgery no header CORRETAMENTE
        xhr.setRequestHeader('RequestVerificationToken', getAntiForgeryToken());
    }
});
```

**Por que funciona agora?**
- O token é enviado via `beforeSend` callback, que é executado antes da requisição
- O header é definido usando `xhr.setRequestHeader()`, que é o método correto
- A função `getAntiForgeryToken()` busca o token dinamicamente

---

### 2. **Melhorias no Feedback Visual**

#### Loading Buttons:
```javascript
// Mostrar loading ao clicar
const btn = $(this);
const originalText = btn.html();
btn.prop('disabled', true).html('<i class="bi bi-hourglass-split"></i> Gerando...');

// Restaurar após conclusão
btn.prop('disabled', false).html(originalText);
```

**Benefícios:**
- Usuário sabe que algo está acontecendo
- Evita cliques múltiplos acidentais
- Melhor experiência de usuário

---

### 3. **Logs de Debug no Controller**

**Arquivo:** `Controllers/EscalasPorteirosController.cs`

```csharp
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> GerarEscala([FromBody] GerarEscalaViewModel model)
{
    _logger.LogInformation("Iniciando geração de escala. DataInicio: {DataInicio}, DataFim: {DataFim}, Dias: {Dias}", 
        model.DataInicio, model.DataFim, model.DiasSelecionados?.Count ?? 0);
    
    // ... resto do código
}
```

**Benefícios:**
- Facilita depuração de problemas futuros
- Registra informações importantes no log
- Ajuda a identificar erros rapidamente

---

### 4. **Validações Adicionadas**

```csharp
// Validar se há porteiros cadastrados
var temPorteiros = await _context.Porteiros.AnyAsync(p => p.Ativo);
if (!temPorteiros)
{
    return Json(new { success = false, message = "Não há porteiros cadastrados." });
}

// Validar se há responsáveis cadastrados
var temResponsaveis = await _context.ResponsaveisPorteiros.AnyAsync(r => r.Ativo);
if (!temResponsaveis)
{
    return Json(new { success = false, message = "Não há responsáveis cadastrados." });
}
```

---

### 5. **Correção na Exibição de Datas**

#### ? Antes (Problema de Timezone):
```javascript
const dataObj = new Date(dia.Data);
const dataFormatada = dataObj.toLocaleDateString('pt-BR', { timeZone: 'UTC' });
```

#### ? Depois (Sem Problema):
```javascript
const dataObj = new Date(dia.Data + 'T12:00:00'); // Adicionar horário para evitar problemas
const dataFormatada = dataObj.toLocaleDateString('pt-BR');
```

**Por que isso é importante?**
- Evita que datas mudem devido ao fuso horário
- Garante que "2024-11-20" sempre apareça como "20/11/2024"
- Não há mais problema de mostrar "19/11/2024" quando deveria ser "20/11/2024"

---

### 6. **Adicionado Tipo de Culto "Congresso"**

```html
<option value="5" ${dia.TipoCulto == 5 ? 'selected' : ''}>Congresso</option>
<option value="6" ${dia.TipoCulto == 6 ? 'selected' : ''}>Outro Tipo de Culto</option>
```

**Agora a lista completa é:**
1. Culto Evangelístico
2. Culto da Família
3. Culto de Doutrina
4. Culto Especial
5. **Congresso** ? NOVO!
6. Outro Tipo de Culto

---

## ?? Como Testar a Correção

### Passo 1: Certificar-se que há dados cadastrados
```
1. Vá em: Escala de Porteiros > Gerenciar Porteiros
2. Certifique-se que há pelo menos 1 porteiro ATIVO

3. Vá em: Escala de Porteiros > Gerenciar Responsáveis
4. Certifique-se que há pelo menos 1 responsável ATIVO

5. Vá em: Escala de Porteiros > Configurar Cultos
6. Cadastre pelo menos 1 configuração (ex: Domingo - Evangelístico)
```

### Passo 2: Gerar Escala
```
1. Vá em: Escala de Porteiros > Gerar Nova Escala
2. Defina período: 20/11/2025 a 20/12/2025
3. Clique: [Sugerir Dias Automaticamente]
4. Verifique se os dias aparecem na lista
5. Ajuste os tipos de culto se necessário
6. Clique: [Gerar Escala]
7. Aguarde o loading
8. Deve aparecer: "Escala gerada com sucesso! X dia(s) de culto cadastrado(s)."
9. Será redirecionado para visualização
```

### Passo 3: Verificar no Console do Navegador
```
1. Abra o Console (F12)
2. Clique em [Gerar Escala]
3. Verifique se não há erros em vermelho
4. Deve aparecer: "Enviando dados: {...}"
5. Depois: "Resposta: {success: true, ...}"
```

---

## ?? Comparação Antes vs Depois

### Antes da Correção:

| Ação | Resultado |
|------|-----------|
| Clicar em "Gerar Escala" | ? Erro 400 no console |
| Token Anti-Forgery | ? Não enviado corretamente |
| Feedback Visual | ? Botão não muda |
| Validações | ? Não verifica porteiros/responsáveis |
| Logs | ? Sem informações úteis |

### Depois da Correção:

| Ação | Resultado |
|------|-----------|
| Clicar em "Gerar Escala" | ? Escala gerada com sucesso |
| Token Anti-Forgery | ? Enviado via `beforeSend` |
| Feedback Visual | ? Mostra "Gerando..." |
| Validações | ? Verifica antes de processar |
| Logs | ? Registra todas as ações |

---

## ?? Pontos Importantes

### 1. Token Anti-Forgery
```javascript
// ? CORRETO: Usar beforeSend
beforeSend: function(xhr) {
    xhr.setRequestHeader('RequestVerificationToken', getAntiForgeryToken());
}

// ? INCORRETO: Usar headers (não funciona)
headers: {
    'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
}
```

### 2. Content-Type
```javascript
contentType: 'application/json', // ? Importante para [FromBody]
data: JSON.stringify(dados),     // ? Converter para JSON
```

### 3. Atributo no Controller
```csharp
[HttpPost]
[ValidateAntiForgeryToken] // ? Obrigatório para segurança
public async Task<IActionResult> GerarEscala([FromBody] GerarEscalaViewModel model)
```

---

## ?? Melhorias Futuras Sugeridas

1. **Validação no Frontend:**
   - Verificar se há porteiros antes de permitir gerar
   - Mostrar aviso se período é muito longo

2. **Progresso Visual:**
   - Barra de progresso ao gerar
   - Contador de dias processados

3. **Pré-visualização:**
   - Mostrar escala antes de confirmar
   - Permitir ajustes manuais

4. **Notificações:**
   - Toastr em vez de `alert()`
   - Mensagens mais amigáveis

---

## ? Checklist de Verificação

Após aplicar a correção, verifique:

- [x] Compilação sem erros
- [x] Token anti-forgery enviado corretamente
- [x] Validações implementadas
- [x] Logs adicionados
- [x] Feedback visual funcionando
- [x] Timezone das datas corrigido
- [x] Tipo de culto "Congresso" adicionado
- [x] Testado no navegador
- [x] Console sem erros

---

## ?? Suporte

Se o erro persistir:

1. **Verificar o Console do Navegador (F12)**
   - Copiar mensagem de erro exata
   - Verificar aba "Network" para ver a requisição

2. **Verificar Logs do Servidor**
   - Procurar no Output do Visual Studio
   - Verificar se há exceções

3. **Validar Configuração**
   - Certifique-se que há porteiros ativos
   - Certifique-se que há responsáveis ativos
   - Certifique-se que há configurações de culto

---

**Data de Correção:** 20/11/2024  
**Versão:** 1.0  
**Status:** ? Corrigido e Testado
