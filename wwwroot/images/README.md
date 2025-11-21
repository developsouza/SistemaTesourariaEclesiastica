# ?? Diretório de Imagens

Este diretório contém as imagens utilizadas no sistema.

## ??? Logo da Igreja

### Arquivo Necessário:
- **Nome:** `logo-igreja.png`
- **Formato:** PNG (preferencialmente com fundo transparente)
- **Tamanho Recomendado:** 500x500 pixels
- **Uso:** Cabeçalho do PDF da Escala de Porteiros

### Como Adicionar:

1. Prepare o logo da sua igreja no formato PNG
2. Renomeie o arquivo para: `logo-igreja.png`
3. Coloque neste diretório (`wwwroot/images/`)
4. Reinicie a aplicação (se estiver rodando)
5. Gere um PDF de teste para verificar

### Especificações Técnicas:

```
Nome do Arquivo: logo-igreja.png
Caminho Completo: wwwroot/images/logo-igreja.png
Formato: PNG, JPG ou GIF
Dimensões: 200x200 até 1000x1000 pixels
Tamanho Máximo: 2 MB recomendado
```

### Visualização no PDF:

O logo aparecerá:
- **Posição:** Canto esquerdo do cabeçalho
- **Tamanho:** 60x60 pixels (redimensionado automaticamente)
- **Fundo:** Branco com cantos arredondados
- **Alinhamento:** Ao lado do título "Escala de Porteiros"

### Exemplo de Layout:

```
????????????????????????????????????????
? ??????                               ?
? ?Logo?  ESCALA DE PORTEIROS          ?
? ??????  Período: DD/MM/YYYY          ?
????????????????????????????????????????
```

### Se o Logo Não For Encontrado:

O sistema continuará funcionando normalmente, apenas sem o logo no cabeçalho do PDF.

---

**Documentação Completa:** `/DOCS/LOGO_IGREJA_PDF.md`
