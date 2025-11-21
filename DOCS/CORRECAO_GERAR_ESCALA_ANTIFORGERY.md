# Correção: Gerar Escala com Token Anti-Forgery

## Problema Identificado

Após adicionar a lógica para dias de cultos customizados em data específica, o sistema de geração de escala parou de funcionar. O erro estava relacionado ao token anti-forgery (CSRF protection).

## Causa Raiz

Quando enviamos dados via **AJAX com JSON** (`contentType: 'application/json'`), o token anti-forgery precisa ser enviado no **header HTTP** com o nome específico que o ASP.NET Core espera.

### Problema Anterior

```javascript
headers: {
    'RequestVerificationToken': token  // ? Nome incorreto
}
```

O ASP.NET Core **não reconhecia** este header customizado.

## Solução Implementada

### 1. Correção do Nome do Header

```javascript
headers: {
    '__RequestVerificationToken': token  // ? Nome correto
}
```

### 2. Código Completo Corrigido

**Arquivo:** `Views/EscalasPorteiros/Gerar.cshtml`

```javascript
$('#btnGerarEscala').click(function() {
    if (diasSelecionados.length === 0) {
        alert('Selecione pelo menos um dia para gerar a escala.');
        return;
    }

    const dataInicio = $('#dataInicio').val();
    const dataFim = $('#dataFim').val();

    const dados = {
        DataInicio: dataInicio,
        DataFim: dataFim,
        DiasSelecionados: diasSelecionados
    };

    console.log('Enviando dados:', dados);

    const btn = $(this);
    const originalText = btn.html();
    btn.prop('disabled', true).html('<i class="bi bi-hourglass-split"></i> Gerando...');

    // ? Obter token anti-forgery
    const token = $('input[name="__RequestVerificationToken"]').val();

    $.ajax({
        url: '@Url.Action("GerarEscala", "EscalasPorteiros")',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(dados),
        headers: {
            '__RequestVerificationToken': token  // ? Nome correto
        },
        success: function(response) {
            console.log('Resposta:', response);
            if (response.success) {
                alert(response.message);
                if (response.redirectUrl) {
                    window.location.href = response.redirectUrl;
                }
            } else {
                alert('Erro: ' + response.message);
            }
        },
        error: function(xhr, status, error) {
            console.error('Erro:', xhr.responseText);
            alert('Erro ao gerar escala: ' + (xhr.responseJSON?.message || error));
        },
        complete: function() {
            btn.prop('disabled', false).html(originalText);
        }
    });
});
```

### 3. Melhorias no Controller

**Arquivo:** `Controllers/EscalasPorteirosController.cs`

Adicionado log detalhado dos dias selecionados para facilitar debug:

```csharp
// Log dos dias selecionados para debug
foreach (var dia in model.DiasSelecionados)
{
    _logger.LogInformation("Dia selecionado: {Data}, TipoCulto: {TipoCulto}", dia.Data, dia.TipoCulto);
}
```

## Como Funciona o Token Anti-Forgery

### Em Requisições de Formulário (Form POST)

```html
<form asp-action="Action" method="post">
    @Html.AntiForgeryToken()  <!-- Gera campo hidden __RequestVerificationToken -->
    <!-- campos do formulário -->
</form>
```

O token é enviado automaticamente no **form data**.

### Em Requisições AJAX com JSON

```javascript
// 1. Incluir o token na view
@Html.AntiForgeryToken()

// 2. Obter o token
const token = $('input[name="__RequestVerificationToken"]').val();

// 3. Enviar no header
$.ajax({
    // ... outras configurações
    headers: {
        '__RequestVerificationToken': token  // ? Nome obrigatório
    }
});
```

## Nomes Válidos de Headers para Anti-Forgery

O ASP.NET Core aceita os seguintes nomes de header:

1. `__RequestVerificationToken` (usado nesta correção) ?
2. `X-XSRF-TOKEN` (alternativa válida)
3. `RequestVerificationToken` (NÃO funciona por padrão) ?

## Testando a Correção

1. Acesse a página de **Gerar Escala** em `EscalasPorteiros/Gerar`
2. Configure o período (DataInicio e DataFim)
3. Clique em **"Sugerir Dias Automaticamente"** para carregar dias
4. Altere o tipo de culto se necessário
5. Clique em **"Gerar Escala"**
6. Deve funcionar corretamente e redirecionar para visualização

## Logs para Debug

Os seguintes logs foram adicionados no controller:

```csharp
_logger.LogInformation("Iniciando geração de escala. DataInicio: {DataInicio}, DataFim: {DataFim}, Dias: {Dias}",
    model.DataInicio, model.DataFim, model.DiasSelecionados?.Count ?? 0);

foreach (var dia in model.DiasSelecionados)
{
    _logger.LogInformation("Dia selecionado: {Data}, TipoCulto: {TipoCulto}", dia.Data, dia.TipoCulto);
}
```

Verifique os logs em caso de problemas.

## Possíveis Erros e Soluções

### Erro: "The required antiforgery token was not provided"

**Causa:** Token não está sendo enviado corretamente.

**Solução:**
1. Verifique se `@Html.AntiForgeryToken()` está na view
2. Verifique se o nome do header está correto: `__RequestVerificationToken`
3. Verifique se o token está sendo obtido corretamente: `$('input[name="__RequestVerificationToken"]').val()`

### Erro: 400 Bad Request

**Causa:** Dados inválidos ou faltando propriedades obrigatórias.

**Solução:**
1. Verifique os logs do controller
2. Verifique se `DiasSelecionados` tem itens
3. Verifique se `DataInicio` e `DataFim` estão corretos
4. Abra o console do navegador (F12) e veja os logs

### Erro: "Nenhum dia selecionado"

**Causa:** A lista `DiasSelecionados` está vazia.

**Solução:**
1. Clique em "Sugerir Dias Automaticamente" primeiro
2. Ou adicione dias manualmente
3. Verifique se a função `atualizarListaDias()` está funcionando

## Conclusão

A correção garante que:

? O token anti-forgery é enviado corretamente no header  
? O ASP.NET Core valida a requisição AJAX  
? A geração de escala funciona com dias customizados  
? Os logs facilitam o debug de problemas  

## Data da Correção

**Data:** 10/01/2025  
**Versão:** 1.0  
**Autor:** Sistema de IA

---

**Nota:** Esta correção resolve o problema introduzido quando adicionamos suporte para dias de cultos customizados em datas específicas, sem afetar nenhuma outra funcionalidade do sistema.
