# ?? Instruções de Configuração - Sistema de Consistência

## Passos para Ativar o Sistema de Diagnóstico de Consistência

### 1. Registrar o Serviço de Consistência no Program.cs

Adicione as seguintes linhas no arquivo `Program.cs`:

```csharp
// Registrar ConsistenciaService
builder.Services.AddScoped<ConsistenciaService>();

// (OPCIONAL) Registrar serviço de background para diagnóstico automático
// Descomente a linha abaixo para ativar diagnósticos automáticos diários
// builder.Services.AddHostedService<ConsistenciaBackgroundService>();
```

Exemplo completo de onde adicionar:

```csharp
// ... código existente ...

// Registrar serviços customizados
builder.Services.AddScoped<AuditService>();
builder.Services.AddScoped<BusinessRulesService>();
builder.Services.AddScoped<BalanceteService>();
builder.Services.AddScoped<PdfService>();
builder.Services.AddScoped<RateLimitService>();
builder.Services.AddScoped<AuditQueueService>();
builder.Services.AddScoped<EscalaPorteiroService>();
builder.Services.AddScoped<RoleInitializerService>();

// ? ADICIONAR AQUI:
builder.Services.AddScoped<ConsistenciaService>();

// (OPCIONAL) Diagnóstico automático a cada 24 horas
// builder.Services.AddHostedService<ConsistenciaBackgroundService>();

// ... resto do código ...
```

---

### 2. Executar o Script SQL de Melhorias

Execute o script SQL localizado em `Scripts/MelhoriasConsistencia.sql` no seu banco de dados:

**Opção 1: Via SQL Server Management Studio (SSMS)**
1. Abra o SSMS
2. Conecte ao banco de dados do sistema
3. Abra o arquivo `Scripts/MelhoriasConsistencia.sql`
4. Execute o script (F5)

**Opção 2: Via Visual Studio**
1. Abra o SQL Server Object Explorer
2. Conecte ao banco de dados
3. Botão direito no banco ? New Query
4. Cole o conteúdo do script
5. Execute

**O que o script faz:**
- ? Cria índices para otimizar queries de fechamentos
- ? Adiciona constraints para garantir integridade de dados
- ? Cria views para facilitar consultas
- ? Cria stored procedure para recalcular totais de fechamentos
- ? Cria função para validar consistência de fechamentos

---

### 3. Adicionar Link no Menu de Administração

Adicione um link para o dashboard de consistência no menu administrativo.

**Arquivo:** `Views/Shared/_Layout.cshtml` ou `Views/Admin/SystemInfo.cshtml`

```html
<!-- Adicionar na seção de administração -->
<li class="nav-item">
    <a class="nav-link" asp-controller="Consistencia" asp-action="Index">
        <i class="bi bi-shield-check me-2"></i>
        Diagnóstico de Consistência
    </a>
</li>
```

Ou criar um card na página de administração:

```html
<div class="col-md-4">
    <div class="card border-info h-100">
        <div class="card-body text-center">
            <i class="bi bi-shield-check display-1 text-info mb-3"></i>
            <h5 class="card-title">Diagnóstico de Consistência</h5>
            <p class="card-text text-muted small">
                Verifique a integridade dos dados financeiros do sistema
            </p>
            <a asp-controller="Consistencia" asp-action="Index" class="btn btn-info">
                <i class="bi bi-play-circle me-2"></i>
                Executar Diagnóstico
            </a>
        </div>
    </div>
</div>
```

---

### 4. (OPCIONAL) Configurar Notificações de Alerta

Para receber notificações quando inconsistências críticas forem detectadas (requer configuração adicional):

1. Configure SMTP no `appsettings.json`:

```json
{
  "EmailSettings": {
    "SmtpServer": "smtp.gmail.com",
    "SmtpPort": 587,
    "SenderEmail": "noreply@suaigreja.com",
    "SenderName": "Sistema Tesouraria",
    "Username": "seu-email@gmail.com",
    "Password": "sua-senha-app",
    "EnableSsl": true
  },
  "AlertSettings": {
    "AdminEmails": [
      "admin@suaigreja.com",
      "tesoureiro@suaigreja.com"
    ],
    "SendEmailOnCriticalIssues": true
  }
}
```

2. Crie um serviço de e-mail (opcional para implementação futura)

---

### 5. Testar a Instalação

Execute os seguintes testes:

#### Teste 1: Acessar Dashboard
1. Faça login como Administrador
2. Navegue até o menu de Administração
3. Clique em "Diagnóstico de Consistência"
4. Você deve ver a página inicial do diagnóstico

#### Teste 2: Executar Diagnóstico Manual
1. Na página de diagnóstico
2. Marque o checkbox de confirmação
3. Clique em "Executar Diagnóstico Completo"
4. Aguarde a execução (pode levar alguns segundos)
5. Você deve ver o relatório de resultados

#### Teste 3: Verificar Logs
1. Verifique a tabela `LogsAuditoria` no banco de dados
2. Deve haver um registro de "Diagnóstico" executado

#### Teste 4: Validar Índices e Constraints
Execute no SQL Server:

```sql
-- Verificar índices criados
SELECT name, type_desc 
FROM sys.indexes 
WHERE object_id = OBJECT_ID('Entradas') 
  AND name LIKE 'IX_%';

-- Verificar constraints criadas
SELECT name, type_desc 
FROM sys.objects 
WHERE type = 'C' 
  AND parent_object_id IN (
    OBJECT_ID('Entradas'), 
    OBJECT_ID('Saidas'),
    OBJECT_ID('TransferenciasInternas'),
    OBJECT_ID('FechamentosPeriodo')
  );
```

---

### 6. Resolução de Problemas Comuns

#### Problema: "ConsistenciaService not found"

**Solução:** Verifique se adicionou `builder.Services.AddScoped<ConsistenciaService>();` no `Program.cs`

#### Problema: "Stored procedure sp_RecalcularTotaisFechamento not found"

**Solução:** Execute o script SQL `Scripts/MelhoriasConsistencia.sql`

#### Problema: Diagnóstico muito lento

**Soluções:**
1. Verifique se os índices foram criados corretamente
2. Execute diagnóstico fora do horário de pico
3. Considere criar um job SQL para executar diagnóstico noturno

#### Problema: Muitas inconsistências críticas detectadas

**Ações:**
1. Analise o tipo de inconsistência mais frequente
2. Use as ações corretivas sugeridas no relatório
3. Execute o script de correção apropriado
4. Reavalie após correções

---

### 7. Manutenção e Monitoramento

#### Frequência Recomendada

- **Diagnóstico Manual:** Mensal
- **Diagnóstico Automático:** Diário (se ativado o background service)
- **Revisão de Inconsistências:** Semanal
- **Backup do Banco:** Diário

#### Métricas a Monitorar

1. **Saúde do Sistema:** Deve ser ? 90%
2. **Inconsistências Críticas:** Deve ser 0
3. **Tempo de Execução:** Deve ser < 30 segundos
4. **Inconsistências Recorrentes:** Investigar padrões

#### Quando Executar Diagnóstico Manual

- ? Após importação de dados
- ? Após correção de bugs
- ? Após exclusão de fechamentos
- ? Mensalmente (rotina)
- ? Antes de gerar relatórios financeiros importantes

---

### 8. Scripts SQL Úteis

#### Limpar Lançamentos Órfãos

```sql
-- Limpar entradas órfãs
UPDATE Entradas 
SET IncluidaEmFechamento = 0, 
    FechamentoQueIncluiuId = NULL,
    DataInclusaoFechamento = NULL
WHERE FechamentoQueIncluiuId IS NOT NULL
  AND FechamentoQueIncluiuId NOT IN (SELECT Id FROM FechamentosPeriodo);

-- Limpar saídas órfãs
UPDATE Saidas 
SET IncluidaEmFechamento = 0, 
    FechamentoQueIncluiuId = NULL,
    DataInclusaoFechamento = NULL
WHERE FechamentoQueIncluiuId IS NOT NULL
  AND FechamentoQueIncluiuId NOT IN (SELECT Id FROM FechamentosPeriodo);
```

#### Recalcular Todos os Fechamentos Pendentes

```sql
DECLARE @FechamentoId INT;

DECLARE cursor_fechamentos CURSOR FOR
SELECT Id FROM FechamentosPeriodo WHERE Status = 0; -- Status Pendente

OPEN cursor_fechamentos;
FETCH NEXT FROM cursor_fechamentos INTO @FechamentoId;

WHILE @@FETCH_STATUS = 0
BEGIN
    EXEC sp_RecalcularTotaisFechamento @FechamentoId;
    FETCH NEXT FROM cursor_fechamentos INTO @FechamentoId;
END

CLOSE cursor_fechamentos;
DEALLOCATE cursor_fechamentos;
```

#### Verificar Saúde Geral do Sistema

```sql
SELECT 
    'Entradas' AS Tabela,
    COUNT(*) AS Total,
    SUM(CASE WHEN Valor <= 0 THEN 1 ELSE 0 END) AS ValoresInvalidos,
    SUM(CASE WHEN IncluidaEmFechamento = 1 AND FechamentoQueIncluiuId IS NULL THEN 1 ELSE 0 END) AS Orfaos
FROM Entradas

UNION ALL

SELECT 
    'Saidas' AS Tabela,
    COUNT(*) AS Total,
    SUM(CASE WHEN Valor <= 0 THEN 1 ELSE 0 END) AS ValoresInvalidos,
    SUM(CASE WHEN IncluidaEmFechamento = 1 AND FechamentoQueIncluiuId IS NULL THEN 1 ELSE 0 END) AS Orfaos
FROM Saidas;
```

---

### 9. Checklist de Implantação

- [ ] Program.cs atualizado com ConsistenciaService
- [ ] Script SQL executado com sucesso
- [ ] Link adicionado no menu de administração
- [ ] Teste de acesso ao dashboard realizado
- [ ] Diagnóstico manual executado com sucesso
- [ ] Documentação lida e compreendida
- [ ] Backup do banco de dados realizado
- [ ] Equipe treinada sobre o novo sistema

---

### 10. Suporte e Dúvidas

Para dúvidas ou problemas:

1. Consulte a documentação em `Documentacao/Regras_Negocio_Consistencia.md`
2. Verifique os logs de auditoria no banco de dados
3. Execute queries de diagnóstico SQL
4. Contate o desenvolvedor do sistema

---

**Versão:** 1.0  
**Data:** Janeiro 2025  
**Sistema:** Tesouraria Eclesiástica - .NET 9
