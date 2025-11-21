# ??? Logo da Igreja no PDF - Guia de Implementação

## ?? Visão Geral

Foi implementado suporte para incluir o logo da igreja no cabeçalho do PDF da Escala de Porteiros. O logo aparece à esquerda do título, criando um visual mais profissional e institucional.

## ? Características

### Design do Cabeçalho

```
????????????????????????????????????????????????
? ??????                                       ?
? ?LOGO?  ESCALA DE PORTEIROS                  ? <- Gradiente Azul
? ??????  Período: DD/MM/YYYY a DD/MM/YYYY     ?
????????????????????????????????????????????????
```

### Especificações do Logo

- **Tamanho:** 60x60 pixels
- **Fundo:** Branco com padding de 5px
- **Borda:** Arredondada (6px)
- **Formato:** PNG recomendado (suporta transparência)
- **Posicionamento:** Canto esquerdo do cabeçalho
- **Alinhamento:** Centralizado verticalmente

## ?? Onde Colocar o Logo

### Caminho do Arquivo:
```
C:\Projetos\G3T\SistemaTesourariaEclesiastica\wwwroot\images\logo-igreja.png
```

### Estrutura de Diretórios:
```
SistemaTesourariaEclesiastica/
??? wwwroot/
?   ??? images/
?   ?   ??? logo-igreja.png  ? Coloque o logo aqui
?   ??? css/
?   ??? js/
?   ??? ...
??? Controllers/
??? Models/
??? ...
```

## ?? Recomendações para o Logo

### Formato Ideal

**Formato:** PNG com fundo transparente

**Dimensões Recomendadas:**
- Mínimo: 200x200 pixels
- Ideal: 500x500 pixels
- Máximo: 1000x1000 pixels

**Observação:** O sistema redimensiona automaticamente para 60x60 no PDF.

### Alternativas de Formato

| Formato | Suporte | Recomendado |
|---------|---------|-------------|
| PNG     | ? Sim  | ????? |
| JPG     | ? Sim  | ??? |
| SVG     | ? Não  | - |
| GIF     | ? Sim  | ?? |

### Design do Logo

**Cores:**
- Prefira logos com cores sólidas
- Evite gradientes muito complexos
- Contraste bom com fundo branco

**Formato:**
- Quadrado ou circular funciona melhor
- Se retangular, será ajustado proporcionalmente

**Fundo:**
- Transparente (PNG) é ideal
- Se opaco, prefira fundo branco

## ?? Como Adicionar o Logo

### Passo 1: Preparar a Imagem

1. **Escolha** o logo da igreja
2. **Edite** se necessário:
   - Remova fundos indesejados
   - Ajuste o tamanho (500x500 recomendado)
   - Salve como PNG
3. **Nomeie** o arquivo como: `logo-igreja.png`

### Passo 2: Copiar para o Projeto

1. Navegue até: `C:\Projetos\G3T\SistemaTesourariaEclesiastica\wwwroot\images\`
2. Cole o arquivo `logo-igreja.png`
3. Confirme que o nome está correto (exatamente `logo-igreja.png`)

### Passo 3: Testar

1. **Execute** o projeto (F5)
2. **Acesse:** Escala de Porteiros ? Visualizar Escalas
3. **Gere** uma escala de teste
4. **Clique** em "Baixar PDF"
5. **Abra** o PDF e verifique se o logo aparece

## ?? Comportamento do Sistema

### Se o Logo Existir:
```
? Logo carregado e exibido no cabeçalho
? Layout com logo à esquerda + título ao centro
? Visual profissional e institucional
```

### Se o Logo NÃO Existir:
```
?? Sistema continua funcionando normalmente
?? Cabeçalho aparece sem o logo
?? Título centralizado no espaço todo
?? Nenhum erro é gerado
```

**Vantagem:** O sistema é resiliente. Se o logo não for encontrado, o PDF é gerado normalmente sem ele.

## ?? Como Funciona Tecnicamente

### Conversão para Base64

O sistema converte a imagem PNG para Base64 para incorporá-la diretamente no HTML:

```csharp
// 1. Lê o arquivo de imagem
var imageBytes = File.ReadAllBytes(logoPath);

// 2. Converte para Base64
var base64String = Convert.ToBase64String(imageBytes);

// 3. Cria data URI
return $"data:image/png;base64,{base64String}";
```

### Vantagens do Base64

1. ? **Portabilidade:** Imagem incorporada no HTML
2. ? **Sem dependências:** Não precisa de arquivo externo
3. ? **Funciona offline:** Não requer servidor de imagens
4. ? **Compatível:** iText converte perfeitamente

## ?? Antes vs Depois

### Sem Logo (Padrão)
```
????????????????????????????????????????
?                                      ?
?     ESCALA DE PORTEIROS              ?
?     Período: DD/MM/YYYY              ?
?                                      ?
????????????????????????????????????????
```

### Com Logo ?
```
????????????????????????????????????????
? ??????                               ?
? ? ??? ?   ESCALA DE PORTEIROS         ?
? ??????   Período: DD/MM/YYYY         ?
?                                      ?
????????????????????????????????????????
```

## ?? Customização do Logo

### Alterar Tamanho

**Arquivo:** `Helpers/EscalaPorteiroPdfHelper.cs`

Procure por:
```css
.header-logo img {
    width: 60px;   /* Altere aqui */
    height: 60px;  /* Altere aqui */
    object-fit: contain;
    background: white;
    border-radius: 6px;
    padding: 5px;
}
```

**Sugestões:**
- Pequeno: `40px x 40px`
- Médio: `60px x 60px` (padrão)
- Grande: `80px x 80px`

### Alterar Estilo

**Remover Fundo Branco:**
```css
.header-logo img {
    background: transparent;  /* Ou remova esta linha */
}
```

**Alterar Borda:**
```css
.header-logo img {
    border-radius: 50%;  /* Circular */
    /* ou */
    border-radius: 0px;  /* Quadrado */
}
```

**Adicionar Sombra:**
```css
.header-logo img {
    box-shadow: 0 2px 4px rgba(0,0,0,0.2);
}
```

## ??? Ferramentas Recomendadas

### Para Editar o Logo:

**Gratuitas:**
- [GIMP](https://www.gimp.org/) - Editor completo
- [Paint.NET](https://www.getpaint.net/) - Simples e eficaz
- [Photopea](https://www.photopea.com/) - Online, similar ao Photoshop
- [Remove.bg](https://www.remove.bg/) - Remover fundo online

**Pagas:**
- Adobe Photoshop
- CorelDRAW
- Affinity Designer

### Para Otimizar PNG:

- [TinyPNG](https://tinypng.com/) - Compressor online
- [PNGGauntlet](https://pnggauntlet.com/) - Compressor desktop
- [ImageOptim](https://imageoptim.com/) - Para Mac

## ?? Troubleshooting

### Logo Não Aparece

**Problema:** PDF gerado sem o logo

**Causas Possíveis:**
1. Nome do arquivo incorreto
2. Arquivo em diretório errado
3. Permissões de leitura

**Soluções:**
```
? Verifique o nome: logo-igreja.png (exatamente)
? Verifique o caminho: wwwroot/images/
? Verifique permissões de leitura do arquivo
? Reinicie a aplicação após adicionar o arquivo
```

### Logo Aparece Distorcido

**Problema:** Imagem esticada ou achatada

**Causa:** Proporção não quadrada

**Solução:**
- Use imagem quadrada (ex: 500x500)
- Ou ajuste `object-fit: contain` para `cover`

### Logo Muito Grande no Arquivo

**Problema:** PDF muito grande (MB)

**Causa:** Imagem PNG muito grande

**Solução:**
1. Redimensione a imagem para 500x500
2. Comprima com TinyPNG
3. Salve com compressão moderada

### Logo com Bordas Brancas

**Problema:** Espaço branco ao redor do logo

**Causa:** Imagem tem margens

**Solução:**
1. Abra no editor de imagens
2. Corte as bordas (crop)
3. Salve novamente

## ?? Checklist de Verificação

Antes de gerar o PDF final:

- [ ] Logo preparado em PNG
- [ ] Dimensões adequadas (500x500)
- [ ] Arquivo nomeado como `logo-igreja.png`
- [ ] Arquivo copiado para `wwwroot/images/`
- [ ] Aplicação reiniciada (se necessário)
- [ ] PDF teste gerado
- [ ] Logo aparece corretamente
- [ ] Tamanho do logo adequado
- [ ] Alinhamento correto
- [ ] Qualidade da imagem boa

## ?? Exemplo de Nome Correto

```
? CORRETO:
   - logo-igreja.png
   - Logo-Igreja.png (Windows não diferencia)
   
? ERRADO:
   - logo.png
   - logotipo.png
   - logo-igreja.jpg
   - logo_igreja.png
   - logoigreja.png
```

## ?? Screenshots Esperados

### 1. No Explorador de Arquivos
```
?? wwwroot
  ??? ?? images
       ??? ?? logo-igreja.png (XX KB)
```

### 2. No PDF Gerado
```
??????????????????????????????????
? [LOGO]  ESCALA DE PORTEIROS    ?
?         Período: ...           ?
??????????????????????????????????
? [Tabela da Escala]            ?
?                               ?
??????????????????????????????????
```

## ?? Usos Alternativos

O mesmo logo pode ser usado em:

- ? Relatórios em PDF
- ? Emails automáticos
- ? Recibos de entradas/saídas
- ? Balancetes
- ? Outros documentos do sistema

**Basta copiar o mesmo código de conversão Base64!**

## ?? Atualizações Futuras

Possíveis melhorias:

- [ ] Configurar logo via painel admin
- [ ] Upload de logo pela interface
- [ ] Múltiplos tamanhos automáticos
- [ ] Logo diferente para cada centro de custo
- [ ] Marca d'água no fundo do PDF

---

**Implementado em:** 20/01/2025  
**Arquivo Modificado:** `Helpers/EscalaPorteiroPdfHelper.cs`  
**Método Adicionado:** `ObterLogoBase64()`  
**Status:** ? Pronto para Uso

**Próximo Passo:** Coloque o arquivo `logo-igreja.png` na pasta `wwwroot/images/` e teste!
