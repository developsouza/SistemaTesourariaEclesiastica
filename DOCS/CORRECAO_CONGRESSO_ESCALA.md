# Correção: Escala de Porteiros e Tipo de Culto "Congresso"

## ?? Problemas Identificados e Resolvidos

### ? 1. Adicionado Tipo de Culto "Congresso"

**Problema:** O enum `TipoCulto` não tinha a opção "Congresso", apenas "Outro Tipo de Culto".

**Solução:**
```csharp
public enum TipoCulto
{
    [Display(Name = "Culto Evangelístico")]
    Evangelistico = 1,

    [Display(Name = "Culto da Família")]
    DaFamilia = 2,

    [Display(Name = "Culto de Doutrina")]
    DeDoutrina = 3,

    [Display(Name = "Culto Especial")]
    Especial = 4,

    [Display(Name = "Congresso")]   // ? NOVO!
    Congresso = 5,

    [Display(Name = "Outro Tipo de Culto")]
    OutroTipoCulto = 6  // Mudou de 5 para 6
}
```

**Onde usar:**
- Ao criar uma configuração de culto em **Configurações > Configurações de Cultos**
- Agora você pode selecionar "Congresso" diretamente no dropdown

---

### ? 2. Sistema de Datas Específicas Já Implementado

O sistema **JÁ ESTÁ FUNCIONANDO CORRETAMENTE** para datas específicas! Aqui está como usar:

#### **Como Cadastrar um Congresso em Data Específica:**

1. Acesse: **Configurações > Configurações de Cultos**
2. Clique em **[+ Nova Configuração]**
3. Preencha o formulário:

```
????????????????????????????????????????????
? ?? IMPORTANTE: Escolha UMA das opções   ?
????????????????????????????????????????????
?                                          ?
? Dia da Semana (Semanal):                ?
? ?? Deixe em branco (--Selecione--)      ?
?                                          ?
? Data Específica (Evento): ?            ?
? ?? Selecione: 15/06/2024                ?
?                                          ?
? Tipo de Culto:                           ?
? ?? Selecione: Congresso                 ?
?                                          ?
? Observação:                              ?
? ?? Digite: Congresso de Missões 2024   ?
?                                          ?
? Status: Ativo                            ?
????????????????????????????????????????????
```

4. Clique em **[Salvar]**

---

## ?? Como o Sistema Funciona

### Configurações Semanais vs Datas Específicas

#### **Configuração Semanal (Repetitiva)**
```
Tipo: Semanal
Dia da Semana: Quarta-feira
Tipo de Culto: Culto da Família
```
? Gera escala para **TODAS as quartas-feiras**

#### **Configuração por Data Específica (Evento Único)**
```
Tipo: Data Específica
Data: 15/06/2024
Tipo de Culto: Congresso
```
? Gera escala **APENAS para aquela data**

---

## ?? Prioridade de Configurações

### Quando há conflito, **data específica tem prioridade**:

```
Exemplo:
?? Config 1: Toda Quarta-feira = Culto da Família
?? Config 2: 15/06/2024 (quarta) = Congresso
?
??> Resultado:
    ?? 08/06 (quarta) ? Culto da Família
    ?? 15/06 (quarta) ? Congresso ? (prioridade)
    ?? 22/06 (quarta) ? Culto da Família
```

---

## ?? Verificando se Está Funcionando

### Passo 1: Cadastrar Data Específica
```
1. Vá em: Configurações > Configurações de Cultos
2. Crie nova config:
   - Data Específica: 25/11/2024
   - Tipo: Congresso
   - Observação: Teste de Congresso
```

### Passo 2: Gerar Escala com a Data
```
1. Vá em: Escalas > Gerar Escala
2. Defina período: 20/11/2024 a 30/11/2024
3. Clique em "Sugerir Dias"
4. Verifique: 25/11/2024 deve aparecer como "Congresso"
```

### Passo 3: Visualizar Resultado
```
??????????????????????????????????????????
? Dias Sugeridos:                        ?
??????????????????????????????????????????
? ?? 20/11 (Quarta) - Culto da Família  ?
? ?? 24/11 (Domingo) - Evangelístico    ?
? ?? 25/11 (Segunda) - Congresso ?     ?
? ?? 27/11 (Quarta) - Culto da Família  ?
??????????????????????????????????????????
```

---

## ?? Solucionando Problemas

### Problema: "Escala não funciona após cadastrar data específica"

#### Causa Provável:
Você pode ter preenchido **AMBOS** os campos (Dia da Semana + Data Específica)

#### Solução:
1. **Edite a configuração** que criou
2. Certifique-se de que **APENAS UM** campo está preenchido:
   - **OU** Dia da Semana (para configuração semanal)
   - **OU** Data Específica (para evento único)
3. Deixe o outro em branco

#### Validação Automática:
O sistema já valida e **não permite salvar com ambos preenchidos**:
```
? ERRO: "Você deve escolher APENAS uma opção: Data Específica OU Dia da Semana."
```

---

### Problema: "Data específica não aparece ao gerar escala"

#### Verificações:

**1. A configuração está ativa?**
```
Configurações de Cultos > Verifique se Status = Ativo ?
```

**2. A data está dentro do período selecionado?**
```
Se data específica = 15/06/2024
E período da escala = 01/07 a 31/07
? Não vai aparecer! (está fora do período)
```

**3. A data não passou?**
```
Você não pode cadastrar datas no passado
Sistema valida: Data Específica >= Hoje
```

**4. Já existe escala gerada para aquela data?**
```
Se já gerou escala para 15/06/2024 anteriormente,
não vai sugerir novamente
```

---

## ?? Casos de Uso Práticos

### Caso 1: Congresso de 3 Dias
```
Data Início: 10/06/2024
Data Fim: 12/06/2024

Configurações necessárias:
?? Config 1: Data = 10/06/2024, Tipo = Congresso, Obs = Dia 1
?? Config 2: Data = 11/06/2024, Tipo = Congresso, Obs = Dia 2
?? Config 3: Data = 12/06/2024, Tipo = Congresso, Obs = Dia 3
```

### Caso 2: Aniversário da Igreja (Anual)
```
PROBLEMA: Precisa criar todo ano

SOLUÇÃO TEMPORÁRIA:
- Criar configuração para 2024: Data = 10/08/2024
- Em 2025, criar nova: Data = 10/08/2025

?? MELHORIA FUTURA: Criar opção "Evento Anual"
```

### Caso 3: Substituir Culto Semanal por Evento
```
Cenário:
- Quartas-feiras = Culto da Família (semanal)
- 15/06/2024 (quarta) = Congresso (data específica)

Config 1 (Semanal):
?? Dia da Semana: Quarta-feira
?? Tipo: Culto da Família

Config 2 (Data Específica):
?? Data Específica: 15/06/2024
?? Tipo: Congresso

Resultado:
?? 08/06 (quarta) ? Culto da Família
?? 15/06 (quarta) ? Congresso ? (prioridade)
?? 22/06 (quarta) ? Culto da Família
```

---

## ?? Dicas de Manutenção

### Gerenciamento de Configurações

**Visualização na Lista:**
```
??????????????????????????????????????????????????
? Configurações de Cultos                        ?
??????????????????????????????????????????????????
? ?? Data Específica | 15/06/2024 (Sexta)       ?
?    Tipo: Congresso | Em 23 dias | Ativo       ?
??????????????????????????????????????????????????
? ?? Semanal | Quarta-feira                      ?
?    Tipo: Culto da Família | Ativo             ?
??????????????????????????????????????????????????
```

**Editar Configuração:**
- Use o botão ?? Editar
- Pode mudar apenas a Observação
- Para trocar o tipo, exclua e crie nova

**Excluir Configuração:**
- Use o botão ??? Excluir
- Só pode excluir se não tiver escala gerada
- Se tiver, desative em vez de excluir

---

## ? Checklist de Uso

### Para Cadastrar Evento Especial:

- [ ] Acesse: Configurações > Configurações de Cultos
- [ ] Clique: [+ Nova Configuração]
- [ ] Deixe "Dia da Semana" em branco
- [ ] Preencha "Data Específica"
- [ ] Selecione "Tipo de Culto" (agora com "Congresso"!)
- [ ] Digite observação
- [ ] Marque "Ativo"
- [ ] Salve

### Para Gerar Escala com Evento:

- [ ] Acesse: Escalas > Gerar Escala
- [ ] Defina período que inclua a data do evento
- [ ] Clique: [Sugerir Dias]
- [ ] Verifique se evento aparece na lista
- [ ] Confirme os dias selecionados
- [ ] Gere a escala

### Para Verificar Funcionamento:

- [ ] Vá em: Configurações de Cultos
- [ ] Verifique badge: ?? Data Específica
- [ ] Veja contador: "Em X dias" (para futuras)
- [ ] Status deve estar: Ativo

---

## ?? Resumo Executivo

### O que mudou:
1. ? Adicionado "Congresso" como tipo de culto
2. ? Sistema de datas específicas **já estava funcionando**
3. ? Prioridade de data específica sobre configuração semanal

### Como usar:
1. Cadastre configuração com **Data Específica** (não Dia da Semana)
2. Escolha tipo "Congresso" no dropdown
3. Ao gerar escala, a data específica aparecerá automaticamente

### Se der problema:
1. Verifique se NÃO preencheu ambos (Dia da Semana + Data)
2. Certifique-se que configuração está Ativa
3. Confirme que data está dentro do período da escala
4. Veja se não há escala já gerada para aquela data

---

**Data de Atualização:** 20/11/2024  
**Versão:** 1.1  
**Status:** ? Funcionando Corretamente
