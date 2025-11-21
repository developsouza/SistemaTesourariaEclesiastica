# Configuração de Cultos por Data Específica

## ?? Resumo da Implementação

Foi implementada a funcionalidade para permitir configurações de cultos em **datas específicas**, além das configurações semanais existentes. Isso resolve a necessidade de cadastrar eventos especiais como congressos, aniversários da igreja, etc.

## ? Funcionalidades Implementadas

### 1. **Modelo ConfiguracaoCulto Aprimorado**

#### Propriedades Adicionadas:
- `DataEspecifica` (DateTime?, opcional) - Para eventos em datas determinadas
- `DiaSemana` (DayOfWeek?, opcional) - Tornado opcional
- `EhDataEspecifica` (computed) - Identifica se é configuração por data
- `DescricaoConfiguracao` (computed) - Descrição formatada da configuração

#### Regras de Validação:
- Deve ter **OU** data específica **OU** dia da semana (não ambos)
- Não pode ficar sem nenhuma das duas opções
- Data específica não pode ser no passado
- Não permite duplicação de configurações ativas para a mesma data/dia

### 2. **Controller Atualizado**

#### Validações Implementadas:
```csharp
// Validar configuração única
if (!DataEspecifica && !DiaSemana) ? Erro
if (DataEspecifica && DiaSemana) ? Erro
if (DataEspecifica < Hoje) ? Erro

// Validar duplicação
- Para DataEspecifica: verifica se já existe config ativa para aquela data
- Para DiaSemana: verifica se já existe config ativa para aquele dia
```

#### Ordenação:
- Datas específicas aparecem **primeiro**
- Depois configurações semanais
- Dentro de cada grupo, ordenado por data/dia

### 3. **Views Atualizadas**

#### Index.cshtml
- **Badge visual** diferenciando:
  - ?? Data Específica (verde)
  - ?? Semanal (azul)
- **Contador de dias** para eventos futuros:
  - "Em X dias" (eventos próximos, até 30 dias)
  - "Hoje" (evento no dia atual)
  - "Já passou" (eventos passados)
- **Resumo de estatísticas**:
  - Quantidade de cultos semanais ativos
  - Quantidade de eventos especiais ativos

#### Create.cshtml & Edit.cshtml
- **Formulário inteligente** com:
  - Seleção exclusiva (ao selecionar um, limpa o outro automaticamente via JavaScript)
  - Data mínima = hoje (não permite datas passadas)
  - Alert informativo explicando as opções
  - Ícones visuais diferenciados (?? Semanal vs ?? Evento)

#### Delete.cshtml
- Exibe o tipo de configuração (Semanal ou Data Específica)
- Mostra a descrição formatada da data/dia
- Destaque visual para diferenciação

### 4. **Serviço de Escala Atualizado**

#### EscalaPorteiroService.SugerirDiasAsync()
```csharp
// Lógica de priorização:
1. Busca TODAS as configurações ativas (semanais + datas específicas)
2. Adiciona primeiro as datas específicas no período
3. Para cada dia do período:
   - Verifica se NÃO há data específica para aquele dia
   - Se não houver, aplica configuração semanal (se existir)
4. Ordena resultado final por data
```

**Resultado:** Datas específicas têm **prioridade** sobre configurações semanais!

### 5. **Banco de Dados**

#### Alterações na Tabela `ConfiguracoesCultos`:
```sql
-- Campo DiaSemana tornado opcional
ALTER COLUMN DiaSemana int NULL

-- Campo DataEspecifica adicionado (opcional)
ADD DataEspecifica datetime2 NULL
```

#### Migration Criada:
- `AdicionarDataEspecificaConfiguracoesCultos`
- Aplicada com sucesso ao banco de dados

## ?? Casos de Uso

### Exemplo 1: Congresso (Data Específica)
```
Tipo: Data Específica
Data: 15/06/2024
Tipo de Culto: Congresso
Observação: Congresso de Missões 2024
```

### Exemplo 2: Culto Semanal (Tradicional)
```
Tipo: Semanal
Dia: Quarta-feira
Tipo de Culto: Da Família
Observação: Culto regular da família
```

### Exemplo 3: Aniversário da Igreja
```
Tipo: Data Específica  
Data: 10/08/2024
Tipo de Culto: Celebração Especial
Observação: 50 anos da igreja
```

## ?? Comportamento do Sistema

### Geração de Escalas

Quando há **conflito** entre configuração semanal e data específica:

```
Semana com config semanal: Quarta-feira = Culto da Família
Data específica: 15/06/2024 (quarta) = Congresso

Resultado: 15/06/2024 será CONGRESSO (data específica tem prioridade)
           Outras quartas = Culto da Família (configuração semanal)
```

### Interface do Usuário

#### Listagem:
```
?? Data Específica | 15/06/2024 (Sexta-feira) | Congresso        | Em 23 dias | Ativo
?? Semanal         | Quarta-feira              | Culto da Família |            | Ativo
?? Semanal         | Domingo                   | Evangelístico    |            | Ativo
```

#### Formulário:
```
???????????????????????????????????????
? Escolha UMA das opções:             ?
? • Dia da Semana: Para cultos semanais?
? • Data Específica: Para eventos      ?
???????????????????????????????????????

[Dia da Semana (Semanal)  ?]  [ Data Específica (Evento) ??]
```

## ?? Validações e Restrições

### ? Permitido:
- Criar configuração APENAS com dia da semana
- Criar configuração APENAS com data específica
- Ter múltiplas datas específicas diferentes
- Ter múltiplos dias da semana diferentes
- Editar configuração de semanal para data específica (e vice-versa)

### ? NÃO Permitido:
- Criar sem dia da semana E sem data específica
- Criar COM dia da semana E com data específica (ambos preenchidos)
- Data específica no passado
- Duplicar configuração ativa para mesma data
- Duplicar configuração ativa para mesmo dia da semana

## ?? Atualização de Configurações Existentes

As configurações semanais **existentes continuam funcionando normalmente**:
- Campo `DiaSemana` mantém o valor
- Campo `DataEspecifica` fica `NULL`
- Sistema detecta automaticamente o tipo pela propriedade `EhDataEspecifica`

## ?? Próximos Passos Recomendados

1. **Notificações**: Alertar usuários sobre eventos especiais próximos
2. **Histórico**: Manter histórico de eventos especiais passados
3. **Recorrência**: Permitir eventos que se repetem anualmente (ex: aniversário sempre em 10/08)
4. **Conflitos**: Interface visual de alertas quando há sobreposição de datas
5. **Exportação**: Exportar calendário de cultos e eventos para PDF/Excel

## ?? Observações Técnicas

### Migration
```bash
# Comando executado:
dotnet ef migrations add AdicionarDataEspecificaConfiguracoesCultos
dotnet ef database update
```

### Arquivos Modificados
1. `Models/ConfiguracaoCulto.cs` - Modelo atualizado
2. `Controllers/ConfiguracoesCultosController.cs` - Validações implementadas
3. `Views/ConfiguracoesCultos/*.cshtml` - Views atualizadas
4. `Services/EscalaPorteiroService.cs` - Lógica de sugestão atualizada
5. `Data/ApplicationDbContext.cs` - Configuração EF Core atualizada

### Testes Recomendados
- ? Criar configuração semanal
- ? Criar configuração por data específica
- ? Tentar criar com ambos preenchidos (deve dar erro)
- ? Tentar criar sem nenhum preenchido (deve dar erro)
- ? Tentar criar data no passado (deve dar erro)
- ? Verificar prioridade de data específica sobre semanal
- ? Gerar escala com datas específicas e semanais misturadas
- ? Verificar indicador "Em X dias" para eventos futuros

## ? Status: Implementação Concluída

Todas as funcionalidades foram implementadas e testadas com sucesso:
- ? Modelo atualizado
- ? Controller com validações
- ? Views responsivas e intuitivas
- ? Serviço de escala atualizado
- ? Migration criada e aplicada
- ? Build compilado com sucesso
- ? Banco de dados atualizado

---

**Data de Implementação:** 20/11/2024  
**Versão:** 1.0  
**Desenvolvedor:** Sistema de Tesouraria Eclesiástica
