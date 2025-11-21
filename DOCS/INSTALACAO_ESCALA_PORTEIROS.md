# ?? Instruções para Aplicar o Módulo de Escala de Porteiros

## ?? IMPORTANTE - Leia antes de começar!

Este módulo adiciona novas tabelas ao banco de dados. Siga os passos abaixo **na ordem correta**.

## ?? Passo a Passo

### 1?? Verificar se o Projeto Compila

Antes de criar a migration, certifique-se que o projeto compila sem erros:

```bash
dotnet build
```

? Se compilar com sucesso, prossiga para o próximo passo.

### 2?? Criar a Migration

No terminal do Visual Studio (ou PowerShell na pasta do projeto), execute:

```bash
dotnet ef migrations add AdicionarModuloEscalaPorteiros
```

Isso criará os arquivos de migration com as mudanças no banco de dados.

### 3?? Revisar a Migration (Opcional)

Você pode revisar o que será criado no banco de dados:

```bash
dotnet ef migrations script
```

### 4?? Aplicar a Migration ao Banco de Dados

**?? ATENÇÃO:** Este comando modificará o banco de dados. Faça backup antes se necessário!

```bash
dotnet ef database update
```

### 5?? Verificar se Funcionou

Execute o projeto:

```bash
dotnet run
```

Ou pressione **F5** no Visual Studio.

## ?? Tabelas que Serão Criadas

A migration criará as seguintes tabelas:

1. **Porteiros** - Cadastro dos porteiros
2. **ResponsaveisPorteiros** - Cadastro dos responsáveis
3. **ConfiguracoesCultos** - Configurações de dias e tipos de cultos
4. **EscalasPorteiros** - Escalas geradas

## ?? Como Verificar se Deu Certo

1. Faça login no sistema
2. No menu lateral, procure por **"Escala de Porteiros"**
3. Se o menu aparecer, está tudo certo!
4. Tente acessar **"Gerenciar Porteiros"**
5. Tente cadastrar um porteiro de teste

## ?? Problemas Comuns

### Erro: "Build failed"
**Causa:** Projeto tem erros de compilação  
**Solução:** Execute `dotnet build` e corrija os erros antes de criar a migration

### Erro: "Unable to create an object of type 'ApplicationDbContext'"
**Causa:** String de conexão não configurada  
**Solução:** Verifique o `appsettings.json` e configure a connection string corretamente

### Erro: "A connection was successfully established... but an error occurred"
**Causa:** Banco de dados não está acessível  
**Solução:** 
- Verifique se o SQL Server está rodando
- Teste a connection string
- Verifique as credenciais

### Erro: "Cannot insert explicit value for identity column"
**Causa:** Tentando inserir dados com IDs específicos  
**Solução:** Remova os campos de ID ao inserir dados manualmente

## ?? Rollback (Desfazer)

Se algo der errado e você quiser desfazer as mudanças:

```bash
dotnet ef database update NomeDaMigrationAnterior
```

Para ver a lista de migrations:

```bash
dotnet ef migrations list
```

Para remover a última migration (se ainda não aplicou):

```bash
dotnet ef migrations remove
```

## ? Checklist Pós-Instalação

Após aplicar a migration com sucesso:

- [ ] O projeto compila sem erros
- [ ] O menu "Escala de Porteiros" aparece no sidebar
- [ ] É possível acessar "Gerenciar Porteiros"
- [ ] É possível acessar "Gerenciar Responsáveis"
- [ ] É possível acessar "Configurar Cultos"
- [ ] É possível acessar "Gerar Nova Escala"
- [ ] É possível cadastrar um porteiro de teste
- [ ] É possível cadastrar um responsável de teste

## ?? Próximos Passos

Após a instalação bem-sucedida:

1. Leia o guia completo em: `DOCS/ESCALA_PORTEIROS.md`
2. Cadastre os porteiros da sua igreja
3. Cadastre os responsáveis
4. Configure os dias de cultos (opcional)
5. Gere sua primeira escala!

## ?? Comandos Úteis

```bash
# Ver status das migrations
dotnet ef migrations list

# Ver script SQL que será executado
dotnet ef migrations script

# Atualizar para migration específica
dotnet ef database update NomeDaMigration

# Ver última migration aplicada
dotnet ef migrations list | Select-Object -Last 1

# Limpar banco e reaplicar tudo (?? CUIDADO - Apaga dados!)
dotnet ef database drop
dotnet ef database update
```

## ?? Backup Recomendado

Antes de aplicar migrations em produção:

```sql
-- Execute no SQL Server Management Studio
BACKUP DATABASE [NomeDoBancoDeDados] 
TO DISK = N'C:\Backups\Backup_AntesEscalaPorteiros.bak' 
WITH FORMAT, INIT, NAME = N'Backup antes de adicionar Escala de Porteiros';
```

## ?? Suporte

Em caso de problemas durante a instalação:
1. Verifique os logs do Entity Framework
2. Consulte a documentação do EF Core
3. Revise os erros no terminal
4. Entre em contato com o suporte técnico

---

**Desenvolvido para:** Sistema de Tesouraria Eclesiástica - AD Jacumã  
**Versão:** 1.0  
**Data:** Janeiro 2025
