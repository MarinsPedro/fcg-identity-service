# Logging estruturado e monitoramento

> **FIAP Fase 2 (Compose / Kubernetes):** estes exemplos de configuracao descrevem opcoes do Identity para uso individual e referencia. No pacote integrado atual, os arquivos `appsettings*.json` estao vazios e os valores de runtime sao fornecidos por `fcg-orchestration/config/compose-identity-*.env`, `config/k8s-identity-*.env` e Secrets do OneClick. Nao use os exemplos abaixo para voltar a preencher `appsettings` no projeto integrado.


## Objetivo

O Identity usa `ILogger<T>` e os Building Blocks da plataforma para produzir
logs estruturados, correlacionáveis e seguros. O monitoramento operacional é
complementado por health checks e por uma trilha persistente de auditoria para
as alterações administrativas de service clients.

Esta implementação permite:

- acompanhar autenticação, emissão e renovação de tokens sem registrar os
  próprios tokens ou credenciais;
- investigar operações de usuários e service clients por identificadores de
  domínio;
- correlacionar os eventos da mesma requisição por `CorrelationId`, `TraceId`,
  `SpanId` e `ParentId`;
- observar rejeições HTTP e exceções inesperadas de forma uniforme;
- distinguir disponibilidade do processo de prontidão do PostgreSQL;
- consultar uma trilha durável das mudanças administrativas de clients;
- configurar a verbosidade por ambiente.

Métricas, criação manual de traces, exportadores OpenTelemetry e plataformas
externas de observabilidade ainda não fazem parte do serviço. Esses recursos
podem ser adicionados depois sem alterar os casos de uso, que dependem apenas de
`ILogger<T>`.

## Visão geral

```mermaid
flowchart LR
    HTTP[Requisição HTTP] --> Activity[Activity do ASP.NET Core]
    Activity --> Correlation[CorrelationIdMiddleware]
    Correlation --> ClientErrors[ClientErrorLoggingMiddleware]
    ClientErrors --> Exceptions[FiapExceptionHandler]
    Exceptions --> Application[Casos de uso]
    Application --> Logger[ILogger do tipo]
    Logger --> Console[Console estruturado]
    Activity -. TraceId, SpanId e ParentId .-> Logger
    Correlation -. CorrelationId .-> Logger
    Application --> Audit[(security_audit_events)]
    Probe[Health probe] --> Health[/health, /live e /ready]
    Health --> Database[(PostgreSQL)]
```

Logging e auditoria têm finalidades diferentes. Logs são sinais operacionais
emitidos no console e podem ser coletados pela plataforma. A auditoria é um
registro de domínio persistido na mesma transação da alteração administrativa.

## Configuração da API

O `Program.cs` aplica os defaults dos Building Blocks e configura o console:

```csharp
builder.AddFiapServiceDefaults();
builder.Logging.AddSimpleConsole(options =>
{
    options.IncludeScopes = true;
    options.SingleLine = true;
    options.TimestampFormat = "yyyy-MM-dd HH:mm:ss.fff zzz ";
});
```

`AddFiapServiceDefaults` adiciona `TraceId`, `SpanId` e `ParentId` aos escopos
do logger quando existe uma `Activity` ativa. O ASP.NET Core cria essa atividade
para requisições HTTP. Execuções fora desse contexto, como migrations, continuam
gerando logs, mas não possuem correlação HTTP.

O console inclui os escopos, usa uma linha por evento e registra o timestamp com
fuso horário. Isso facilita a leitura local e a ingestão por coletores baseados
em `stdout`.

## Correlation ID e trace distribuído

`UseFiapApiDefaults` habilita a correlação antes de autenticação, autorização e
endpoints. O middleware usa o header `X-Correlation-ID` quando seu valor:

- não é vazio;
- possui no máximo 128 caracteres;
- contém somente letras, números, `-`, `_` ou `.`.

Valores ausentes ou inseguros são substituídos pelo `TraceId` atual ou por um
identificador gerado. O valor final:

- é devolvido no header `X-Correlation-ID` da resposta;
- entra no scope dos logs como `CorrelationId`;
- aparece no Problem Details quando a requisição falha.

`CorrelationId` representa um identificador funcional propagável pela borda.
`TraceId`, `SpanId` e `ParentId` descrevem a atividade técnica do ASP.NET Core.
Eles não devem ser tratados como sinônimos nem repetidos manualmente nas
mensagens de log.

Ao publicar um evento de integração, o Identity preserva esse mesmo valor no
header `X-Correlation-ID` do envelope do MassTransit. Se o valor recebido também
for um GUID válido, ele preenche o campo nativo `CorrelationId` do MassTransit;
identificadores textuais continuam inalterados no header. O contrato do evento
não recebe esse dado, pois ele pertence à observabilidade e não ao domínio.

## Logging estruturado

Valores variáveis são passados separadamente da mensagem:

```csharp
logger.LogInformation(
    "Usuário {UserId} autenticado com sucesso pelo client {ClientId}.",
    user.Id,
    client.ClientId);
```

`UserId` e `ClientId` tornam-se propriedades pesquisáveis do evento. Não use
interpolação em chamadas de log, pois ela transforma a mensagem e os valores em
uma única string e elimina essa estrutura.

### Convenção de propriedades

As propriedades usam `PascalCase` e nomes relacionados ao domínio:

| Propriedade | Conteúdo |
|---|---|
| `UserId` | identificador interno do usuário |
| `ClientId` | service client alvo ou autenticado |
| `ActorClientId` | service client que executou uma mudança administrativa |
| `Audience` | audiência solicitada para um token de serviço |
| `ScopeCount` | quantidade de scopes solicitados, sem registrar credenciais |
| `AuditEventLimit` | limite solicitado na consulta de auditoria |
| `AuditFilterCount` | quantidade de filtros aplicados à consulta |
| `AuditHasCursor` | indica se a consulta continuou a partir de um cursor |
| `AuditEventCount` | quantidade de eventos retornados |
| `AuditHasNextPage` | indica se a consulta retornou cursor para outra página |
| `MigrationCount` | quantidade de migrations pendentes |
| `Method` | método HTTP |
| `Path` | caminho da requisição, sem query string |
| `Status` | status HTTP produzido |
| `Type` | tipo estável do Problem Details |
| `DurationMs` | duração da requisição em milissegundos |
| `ValidationFields` | nomes dos campos inválidos, sem valores |
| `ValidationErrorCount` | quantidade de erros de validação |

Não crie variações como `ServiceClientId`, `ClientID` ou `userId` quando
`ClientId` e `UserId` já representarem o conceito.

## Níveis de log

| Nível | Uso no Identity |
|---|---|
| `Debug` | início de autenticação, consultas e operações sem mudança de estado |
| `Information` | operações concluídas, credenciais rejeitadas de forma genérica e respostas esperadas 400, 401, 404, 409 e 422 |
| `Warning` | respostas 403 e 429, tentativa sem permissão, ação de segurança recusada e cancelamento do migrador |
| `Error` | exceção inesperada que produz resposta 500 |
| `Critical` | falha que impede a execução das migrations |

Os níveis são configurados em `appsettings.json`. Produção mantém logs gerais
em `Information`, reduz o ruído do ASP.NET Core e mantém comandos do EF Core em
`Warning`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore.Database.Command": "Warning"
    }
  }
}
```

Em `Development`, o namespace `FiapCloudGames.Identity` usa `Debug` e o
ASP.NET Core usa `Information`. Níveis também podem ser alterados por variável
de ambiente, por exemplo:

```text
Logging__LogLevel__FiapCloudGames.Identity=Debug
Logging__LogLevel__Microsoft.AspNetCore=Warning
```

Habilite comandos do Entity Framework em `Information` somente de forma
temporária e em ambiente controlado. Nunca habilite `EnableSensitiveDataLogging`
em produção.

## Respostas HTTP e exceções

O pipeline comum é instalado por `UseFiapApiDefaults` antes da autenticação e
da autorização.

### Respostas 4xx

O `ClientErrorLoggingMiddleware` registra um evento HTTP depois que o Problem
Details é produzido. Respostas 400, 401, 404, 409 e 422 usam `Information`;
403 e 429 usam `Warning`. O evento inclui `Type`, `Status`, `Method`, `Path`,
`DurationMs`, `ValidationFields` e `ValidationErrorCount`.

Em erros de validação, somente a quantidade e os nomes dos campos são
registrados. Valores rejeitados e mensagens funcionais não entram nesse evento.

### Exceções conhecidas

`AppException` é convertida em Problem Details sem stack trace. O evento HTTP
4xx é responsabilidade do middleware comum, evitando que a camada de
apresentação replique lógica de logging.

### Exceções inesperadas

Falhas não tratadas são registradas em `Error` pelo `FiapExceptionHandler`, com
a exceção original, método e caminho. A resposta continua expondo somente um
Problem Details genérico; mensagem interna e stack trace não são enviados ao
cliente.

## Autenticação e dados sensíveis

Falhas de credenciais de usuário e de service client usam mensagens genéricas.
O log não informa qual e-mail foi usado nem se a conta existe. Depois que uma
identidade é validada, `UserId` e `ClientId` podem ser usados para investigação
operacional.

Nunca registre:

- senha ou hash de senha;
- e-mail, nome ou outro dado pessoal sem necessidade operacional;
- `client_secret` ou seu hash;
- access token, refresh token ou header `Authorization`;
- chave privada JWT ou seu conteúdo;
- connection string;
- corpo de requisições de autenticação;
- mensagens e valores rejeitados durante validação;
- segredo devolvido na criação ou rotação de um service client.

O migrador também segue essa regra: a seed do administrador registra somente o
`UserId`; a seed de service client registra somente o `ClientId`.

## Auditoria persistente

As seguintes operações criam um registro em
`identity.security_audit_events` na mesma unidade de trabalho da alteração:

| Evento | Operação |
|---|---|
| `service_client.provisioned` | provisionamento inicial do `web-bff` pelo migrador |
| `service_client.created` | criação administrativa de client |
| `service_client.permissions_changed` | substituição de audiences e scopes |
| `service_client.secret_rotated` | rotação do segredo |
| `service_client.deactivated` | desativação do client |

Cada registro contém `EventType`, `ActorClientId`, `SubjectType`, `SubjectId` e
`OccurredAtUtc`. Ele não contém segredo, hash ou JWT. A consulta é feita por
`GET /v1/audit-events`, exige token de serviço com o scope
`identity.audit.read` e usa paginação por cursor com `limit` entre 1 e 200.

Logs não substituem essa auditoria, e a auditoria não substitui logs de falhas,
latência e diagnóstico técnico.

## Health checks

| Endpoint | Verificação | Uso esperado |
|---|---|---|
| `GET /health/live` | nenhuma dependência; confirma que o processo HTTP responde | liveness probe |
| `GET /health/ready` | health checks com tag `ready`, atualmente PostgreSQL | readiness probe |
| `GET /health` | todos os health checks registrados | diagnóstico agregado interno |

O check `identity-database` usa `CanConnectAsync` e devolve estado não saudável
quando o PostgreSQL está indisponível. Os endpoints são anônimos na aplicação
para permitir probes, mas devem permanecer acessíveis somente pela rede interna
em produção.

Liveness não deve depender do banco. Caso contrário, uma indisponibilidade do
PostgreSQL provocaria reinícios desnecessários da API em vez de apenas retirá-la
temporariamente do tráfego.

## Validação automatizada

`LoggingConfigurationTests` inicia o host real e confirma:

- rastreamento de `TraceId`, `SpanId` e `ParentId`;
- inclusão dos scopes no console;
- formato de uma linha por evento;
- formato do timestamp.

`HealthEndpointTests` valida os contratos de liveness e readiness. Os próprios
Building Blocks testam o middleware de correlação, o logging de respostas 4xx e
a conversão para Problem Details.

Execute a validação completa com:

```powershell
dotnet build FiapCloudGames.Identity.slnx -c Release
dotnet test FiapCloudGames.Identity.slnx -c Release --no-build
dotnet format FiapCloudGames.Identity.slnx --verify-no-changes --no-restore
```

## Operação local

Acompanhe o terminal que executa a API ou o migrador. Durante uma requisição,
os scopes do console incluem a correlação HTTP. Para seguir um fluxo entre a
borda e o Identity, envie o mesmo `X-Correlation-ID` e pesquise esse valor nos
logs dos dois processos.

Ao investigar uma falha:

1. localize a requisição por `CorrelationId` ou `TraceId`;
2. filtre por `UserId`, `ClientId` ou `ActorClientId`, quando disponíveis;
3. confira o evento HTTP final por `Status` e `Type`;
4. para mudanças de client, confirme a operação na auditoria persistente;
5. use `/health/ready` para separar falha da aplicação de indisponibilidade do
   PostgreSQL.
