# Configuração do Identity

> **FIAP Fase 2 (Compose / Kubernetes):** estes exemplos de configuracao descrevem opcoes do Identity para uso individual e referencia. No pacote integrado atual, os arquivos `appsettings*.json` estao vazios e os valores de runtime sao fornecidos por `fcg-orchestration/config/compose-identity-*.env`, `config/k8s-identity-*.env` e Secrets do OneClick. Nao use os exemplos abaixo para voltar a preencher `appsettings` no projeto integrado.


As configurações seguem a hierarquia padrão do ASP.NET Core. Em produção, use variáveis de ambiente e um gerenciador de segredos.

O catálogo completo de chaves, exemplos por ambiente e instruções de
preenchimento está em [appsettings-reference.md](appsettings-reference.md).

## Banco de dados

```text
ConnectionStrings__Database=Host=localhost;Port=5432;Database=fcg_identity;Username=fcg_identity;Password=...
```

O Identity é o único proprietário do schema `identity`. Nenhum outro serviço deve acessar diretamente suas tabelas.

## RabbitMQ e outbox

```text
RabbitMq__Host=localhost
RabbitMq__Port=5672
RabbitMq__VirtualHost=/
RabbitMq__Username=fcg_identity
RabbitMq__Password=...
```

Ao cadastrar um usuário, a API publica o contrato versionado
`FiapCloudGames.Identity.Contracts.Events.V1.UserCreatedEvent`. O Bus Outbox do
MassTransit persiste a mensagem em `identity.outbox_messages` no mesmo
`SaveChangesAsync` que cria o usuário. A entrega ao RabbitMQ é assíncrona e não
exige que o broker esteja disponível no instante do cadastro.

O evento é publicado na exchange `identity.user-created.v1`, do tipo `fanout`.
O nome está centralizado em `UserCreatedEvent.ExchangeName`; consumidores devem
usar essa constante ao configurar a topologia do MassTransit.

As tabelas `inbox_states`, `outbox_messages` e `outbox_states` pertencem ao
schema do Identity e são criadas pelo executável de migrations. O
`EventId` também é usado como `MessageId` do envelope do MassTransit para
permitir deduplicação pelo consumidor.
O `X-Correlation-ID` recebido pela API é preservado no header homônimo do
envelope do MassTransit para manter o rastreio técnico entre HTTP, outbox,
RabbitMQ e consumidor, sem acoplar esse dado de observabilidade ao contrato do
evento.

Assim como o migrador do monólito, o executável
`FiapCloudGames.Identity.Migrations` usa o Generic Host. Ele carrega o
`appsettings.json` copiado para o diretório de saída, o arquivo específico do
ambiente, User Secrets em Development, variáveis de ambiente e argumentos de
linha de comando.

Os aliases explícitos de connection string são avaliados primeiro, nesta ordem:

1. `IDENTITY_MIGRATIONS_CONNECTION`;
2. `ConnectionStrings__Database`;
3. `IDENTITY_DESIGN_CONNECTION`, mantida para comandos `dotnet ef`;
4. `ConnectionStrings:Database` da configuração montada pelo Generic Host.

Exemplo:

```powershell
$env:IDENTITY_MIGRATIONS_CONNECTION = "Host=localhost;Port=5432;Database=fcg_identity;Username=fcg_identity_app;Password=..."
dotnet run --project src/FiapCloudGames.Identity.Migrations
```

Se nenhuma dessas variáveis existir, basta configurar
`ConnectionStrings:Database` no `appsettings.json` do próprio projeto de
migrations. O migrador não lê arquivos da API, evitando acoplamento entre os
dois executáveis.

A API não executa migrations no startup. O migrador deve terminar com sucesso
antes do processo da API ou do rollout do deployment. A execução usa timeout de
120 segundos por comando e até cinco tentativas para falhas transitórias, com
intervalo máximo de dez segundos.

## Seed do administrador

Depois de atualizar o banco, o projeto de migrations executa uma seed opcional
e idempotente. Ela usa:

```text
Admin__Name=Administrador
Admin__Email=admin@example.com
Admin__Password=uma-senha-forte
```

`Admin:Email` e `Admin:Password` devem ser fornecidos juntos. A senha segue a
mesma regra do cadastro: ao menos oito caracteres, com letra, número e caractere
especial. Se o administrador já existir, a senha não é alterada. Se o mesmo
e-mail pertencer a um usuário sem papel `Administrator`, o migrador falha para
não ocultar uma configuração incorreta.

O seed também é executado quando o schema já está atualizado. Isso permite
preparar um banco existente sem criar uma migration vazia.

O arquivo `appsettings.Development.json` do projeto de migrations contém apenas
credenciais locais conhecidas:

```text
admin@fiapcloudgames.com / Admin@123456
```

O perfil de execução do projeto define `DOTNET_ENVIRONMENT=Development`. Fora
desse ambiente, injete as configurações por secret. Nunca reutilize a senha
local em ambiente compartilhado ou produção.

## Emissão JWT

```text
Jwt__Issuer=https://identity.fcg.local
Jwt__KeyId=fcg-identity-2026-01
Jwt__PrivateKeyPath=/run/secrets/identity-signing-key.pem
Jwt__UserTokenLifetime=00:15:00
Jwt__ServiceTokenLifetime=00:05:00
Jwt__RefreshTokenLifetime=7.00:00:00
```

`Jwt__PrivateKeyPem` também é aceito, mas uma montagem de secret como arquivo costuma ser mais simples em contêineres. Fora de `Development`, a aplicação deve falhar na inicialização se nenhuma chave persistente for fornecida. Uma chave efêmera de desenvolvimento não pode ser usada em produção.

Cada rotação deve usar um novo `KeyId`. Durante uma rotação sem interrupção, a publicação simultânea da chave anterior e da nova será uma evolução necessária; a primeira versão publica a chave ativa.

## Clients iniciais e provisionamento de produção

O projeto de migrations provisiona os clients declarados em
`InitialServiceClients:Clients`. A API não cria clients durante o startup. Em
um banco novo, injete os segredos antes de executar o migrador:

```text
InitialServiceClients__Clients__0__Secret=segredo-exclusivo-do-web-bff
InitialServiceClients__Clients__1__Secret=segredo-exclusivo-do-orders-api
```

O valor literal `CHANGE_ME` é rejeitado. Cada segredo é usado apenas como
entrada, armazenado como hash PBKDF2 e nunca gravado em auditoria ou devolvido
por uma consulta. Depois que os clients existem, novas execuções são
idempotentes e não exigem mais os secrets.

O `web-bff` recebe as audiences e os scopes usados pela borda. O `orders-api`
recebe somente as permissões necessárias para consultar Catalog, Promotions e
Library. Apenas o `web-bff` pode emitir tokens de usuário.

Regras:

- `client_id` é único por aplicação/worker;
- o segredo nunca é compartilhado entre clientes;
- `CanIssueUserTokens=true` fica restrito às pontas autorizadas, como BFF e Gateway;
- audiência e escopos seguem privilégio mínimo;
- os segredos são persistidos somente como hash PBKDF2.

Cada audience e scope é persistido como uma associação individual em
`service_client_audiences` e `service_client_scopes`. As listas da configuração
são usadas somente como entrada da seed; não são armazenadas em uma coluna
concatenada.

Depois do provisionamento, clients não gerenciados pelo migrador podem ser
criados por `POST /v1/service-clients`. Use
`POST /v1/service-clients/{clientId}/secret/rotate` para rotação explícita.

Em `Development`, o migrador provisiona:

```text
client_id=web-bff      client_secret=WebBff@123456
client_id=orders-api   client_secret=Fiap@123456
```

A seed trata a configuração como fonte de verdade e sincroniza nome, secret,
estado ativo, capacidade de emitir tokens, audiences e scopes. Permissões
obsoletas são removidas, clients configurados são reativados e todas as mudanças
são registradas na auditoria.

## Health checks e auditoria

A operação detalhada de logging, correlação, níveis, auditoria persistente e
probes está em [logging-monitoring.md](logging-monitoring.md).

- `/health/live` verifica somente se o processo HTTP está respondendo;
- `/health/ready` verifica se o PostgreSQL pode ser acessado;
- `/health` mantém o endpoint agregado fornecido pelos Building Blocks.

As operações de criação, alteração de permissões, rotação e desativação de
service clients geram registros em `identity.security_audit_events`. O
provisionamento inicial também é registrado com ator `identity-migrations`.
Consulte os eventos por `GET /v1/audit-events`, usando o scope
`identity.audit.read`. Segredos e hashes não fazem parte do evento.

## Validação nas APIs consumidoras

Os consumidores devem validar os tokens emitidos localmente e respeitar os
claims do contrato abaixo.

Cada API configura localmente:

- `Authority`: endereço do Identity;
- `Audience`: seu próprio identificador, por exemplo `catalog-api`;
- tipos de token aceitos: para APIs internas, normalmente apenas `service`;
- políticas por escopo.

A validação do JWT é local por assinatura, emissor, audiência, expiração e claims. Isso evita consultar o Identity em toda requisição. O Identity só é chamado para login, refresh, emissão/renovação de token de serviço e obtenção eventual de metadados/chaves públicas.

## Produção

Antes de publicar:

- substitua todos os placeholders;
- injete a chave privada e segredos por mecanismo seguro;
- mantenha o Identity na rede interna; OAuth, discovery, JWKS e health são
  anônimos na aplicação, não públicos na internet;
- habilite TLS;
- limite tentativas de login na borda;
- configure métricas e alertas sem dados sensíveis;
- conceda ao workflow acesso de leitura ao pacote privado Building Blocks.
