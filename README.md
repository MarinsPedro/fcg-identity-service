# Fiap Cloud Games — Identity Service

Serviço responsável por identidades, autenticação e emissão de tokens da plataforma Fiap Cloud Games.

Este repositório é independente do monólito. Ele possui banco, migrations, pipeline e ciclo de entrega próprios.

## Responsabilidades

- cadastrar, consultar, atualizar e desativar usuários;
- publicar `UserCreatedEvent` de forma transacional após o cadastro;
- autenticar usuários por e-mail e senha;
- autenticar aplicações por `client_id` e `client_secret`;
- cadastrar, consultar, reconfigurar, rotacionar e desativar service clients;
- emitir access tokens JWT dos tipos `user` e `service`;
- emitir e rotacionar refresh tokens de usuário;
- publicar a chave pública de assinatura em JWKS;
- expor metadados de descoberta para os consumidores dos tokens;
- registrar eventos persistentes das operações administrativas de clients;
- expor sinais separados de liveness e readiness.

O Identity **não** decide se o usuário pode comprar um jogo, visualizar um pedido ou executar outra regra de negócio. Essa autorização pertence à ponta (BFF/API Gateway) e, quando necessário, ao serviço que possui o recurso.

## Fluxos de token

### Token de usuário

1. O cliente envia as credenciais ao BFF ou API Gateway.
2. A ponta chama `POST /v1/auth/login` usando seu token de serviço.
3. O Identity valida aplicação, usuário e senha.
4. O Identity devolve um JWT com `token_use=user`, `sub` igual ao ID do usuário e `client_id` igual à ponta solicitante.
5. O token permanece na borda. Ao chamar APIs internas, a ponta usa um token de serviço e envia o ID do usuário no contrato da operação quando a regra exigir esse contexto.

### Token de serviço

1. Uma aplicação ou worker chama `POST /oauth2/token` com `grant_type=client_credentials`.
2. O Identity valida o `client_id`, o segredo, a audiência e os escopos solicitados.
3. O Identity devolve um JWT curto com `token_use=service`, destinado somente à audiência solicitada.
4. A aplicação mantém esse token em cache até pouco antes da expiração. Não se solicita um token novo a cada chamada.

Não existe token `delegated`, cadeia `act` ou `origin_client_id` neste desenho.

## Estrutura

```text
src/
  FiapCloudGames.Identity.Api/             composition root e pipeline
  FiapCloudGames.Identity.Application/     casos de uso e portas técnicas
  FiapCloudGames.Identity.Domain/          agregados e regras do domínio
  FiapCloudGames.Identity.Infrastructure/  EF Core, repositories, hashing e JWT
  FiapCloudGames.Identity.Presentation/    Controllers, Requests, Responses e mappings HTTP
  FiapCloudGames.Identity.Migrations/      migrations, seeds e atualizador do banco
tests/
  FiapCloudGames.Identity.ArchitectureTests/
  FiapCloudGames.Identity.Database.IntegrationTests/
  FiapCloudGames.Identity.IntegrationTests/
  FiapCloudGames.Identity.UnitTests/
docs/
  appsettings-reference.md
  architecture.md
  configuration.md
  endpoints.md
  logging-monitoring.md
  pagination.md
  service-authentication-flows.md
  testing.md
```

As regras e exemplos da estrutura estão em [docs/architecture.md](docs/architecture.md).
O preenchimento completo das configurações da API e do migrador está em
[docs/appsettings-reference.md](docs/appsettings-reference.md).
O uso, os contratos e a ordem recomendada dos endpoints estão em
[docs/endpoints.md](docs/endpoints.md).
A estratégia e as fronteiras das suítes de teste estão em
[docs/testing.md](docs/testing.md).
Os fluxos completos de autenticação entre BFF, APIs e serviços internos estão
em [docs/service-authentication-flows.md](docs/service-authentication-flows.md).
A configuração de logs, correlação, auditoria e health checks está em
[docs/logging-monitoring.md](docs/logging-monitoring.md).
O contrato reutilizável de paginação por cursor está em
[docs/pagination.md](docs/pagination.md).

## Pré-requisitos

- .NET SDK 10;
- PostgreSQL;
- RabbitMQ;
- acesso ao pacote privado `FiapCloudGames.BuildingBlocks` versão `0.3.0` no GitHub Packages.

O `NuGet.Config` referencia as variáveis de ambiente
`GITHUB_PACKAGES_USERNAME` e `GITHUB_PACKAGES_TOKEN`; os valores reais das
credenciais não são gravados no repositório. Defina-os antes do restore:

```powershell
$env:GITHUB_PACKAGES_USERNAME = "MarinsPedro"
$env:GITHUB_PACKAGES_TOKEN = Read-Host "Token do GitHub Packages" -MaskInput

dotnet restore FiapCloudGames.Identity.slnx --configfile NuGet.Config
```

O token precisa de `read:packages` e acesso ao pacote privado. Para o Visual
Studio herdar essas variáveis, configure-as no usuário e reinicie o Visual
Studio. O `appsettings.json` não é usado pelo NuGet, pois o restore ocorre antes
da aplicação existir.

Depois do restore pelo terminal, remova o token da sessão quando ele não for
mais necessário:

```powershell
Remove-Item Env:\GITHUB_PACKAGES_TOKEN
```

Para testar alterações locais do Building Blocks sem publicar um NuGet,
mantenha os repositórios como irmãos e use:

```powershell
dotnet build FiapCloudGames.Identity.slnx -p:UseLocalBuildingBlocks=true
```

O padrão é `UseLocalBuildingBlocks=false`, portanto CI e produção continuam
usando a versão publicada definida em `Directory.Packages.props`.

No GitHub, conceda ao repositório do Identity acesso de leitura ao package Building Blocks em **Package settings > Manage Actions access**. O workflow usa o `GITHUB_TOKEN` efêmero; nenhum PAT deve ser cadastrado no YAML.

## Execução local

1. Copie `.env.example` para o mecanismo local de segredos de sua preferência.
2. Inicie um PostgreSQL e crie o banco configurado em `ConnectionStrings__Database`.
3. Gere uma chave RSA de desenvolvimento:

```powershell
./scripts/New-DevelopmentSigningKey.ps1
```

4. Defina as variáveis conforme [docs/configuration.md](docs/configuration.md).
5. Restaure, aplique as migrations e execute:

```powershell
dotnet tool restore
dotnet restore FiapCloudGames.Identity.slnx
dotnet run --project src/FiapCloudGames.Identity.Migrations
dotnet run --project src/FiapCloudGames.Identity.Api
```

### Provisionar aplicações de desenvolvimento

Depois de executar as migrations e iniciar a API, use o manifesto em
`scripts/service-clients.Development.json` para cadastrar ou sincronizar todos
os clients, audiences e scopes do workspace atual:

```powershell
$env:IDENTITY_BOOTSTRAP_CLIENT_SECRET = "WebBff@123456"
./scripts/Register-ServiceClients.ps1
```

O script é idempotente: cria clients ausentes e substitui as permissões dos já
existentes. Segredos são gerados pela API somente na criação e gravados em
`secrets/service-clients.local.json`, diretório ignorado pelo Git. Para gerar
novos segredos também para clients existentes, execute com
`-RotateExistingSecrets`. Valide o plano sem chamar a API com `-WhatIf`.

O cadastro grava o usuário e a mensagem do `UserCreatedEvent` na mesma
transação do PostgreSQL. O serviço de entrega do MassTransit lê a outbox e
publica na exchange `identity.user-created.v1` do RabbitMQ, inclusive após uma
indisponibilidade temporária do broker.
Antes de subir a API, configure `RabbitMq__Host`, `RabbitMq__VirtualHost`,
`RabbitMq__Username` e `RabbitMq__Password`.

Com os perfis `Development`, o migrador cria de forma idempotente um
administrador local e o único client técnico inicial, `web-bff`:

| Tipo | Identificador | Segredo local |
|---|---|---|
| administrador | `admin@fiapcloudgames.com` | `Admin@123456` |
| service client | `web-bff` | `WebBff@123456` |

Essas credenciais são públicas e exclusivas do ambiente local. Nunca as use em
um ambiente compartilhado ou de produção. As regras e o fluxo de teste estão em
[docs/endpoints.md](docs/endpoints.md).

Sem uma variável de ambiente, o migrador usa
`src/FiapCloudGames.Identity.Migrations/appsettings.json`. Em ambientes reais,
sobrescreva `ConnectionStrings:Database` por secret usando
`ConnectionStrings__Database` ou `IDENTITY_MIGRATIONS_CONNECTION`.

Para criar uma nova migration:

```powershell
dotnet ef migrations add NomeDaMigration `
  --project src/FiapCloudGames.Identity.Migrations `
  --startup-project src/FiapCloudGames.Identity.Migrations `
  --output-dir Migrations
```

O projeto suporta as duas ferramentas do EF Core:

- `dotnet ef`, restaurado pelo tool manifest em `.config/dotnet-tools.json`;
- Package Manager Console do Visual Studio, por meio de
  `Microsoft.EntityFrameworkCore.Tools`.

No Package Manager Console, use explicitamente o projeto de migrations como
projeto de destino e de startup:

```powershell
Add-Migration NomeDaMigration `
  -Project FiapCloudGames.Identity.Migrations `
  -StartupProject FiapCloudGames.Identity.Migrations `
  -OutputDir Migrations

Update-Database `
  -Project FiapCloudGames.Identity.Migrations `
  -StartupProject FiapCloudGames.Identity.Migrations
```

`Microsoft.EntityFrameworkCore.Design` e `Microsoft.EntityFrameworkCore.Tools`
usam `PrivateAssets=all`, portanto permanecem dependências internas de
desenvolvimento e não são propagadas para consumidores.

O processo da API nunca executa migrations automaticamente. Em contêineres,
use a imagem com target `migrations` antes de iniciar ou atualizar a API:

```powershell
docker build `
  --target migrations `
  --secret "id=nuget_config,src=$env:APPDATA\NuGet\NuGet.Config" `
  -t fcg-identity-migrations:local .
```

Em `Development`, a interface Swagger fica disponível em
`http://localhost:5081/swagger`. Use `Authorize` para informar tokens Bearer.
Somente o endpoint de client credentials recebe `client_id` e `client_secret`
no próprio contrato. Login, refresh e revoke exigem token de serviço.

Para criar a imagem sem gravar credenciais do GitHub Packages em camadas Docker, forneça um `NuGet.Config` autenticado como secret do BuildKit:

```powershell
docker build `
  --secret "id=nuget_config,src=$env:APPDATA\NuGet\NuGet.Config" `
  -t fcg-identity:local .
```

## Endpoints principais

| Método | Rota | Autenticação | Uso |
|---|---|---|---|
| `POST` | `/v1/auth/login` | service + `identity.auth.login` | login de usuário |
| `POST` | `/v1/auth/refresh` | service + `identity.auth.refresh` | rotação do token |
| `POST` | `/v1/auth/revoke` | service + `identity.auth.revoke` | logout/revogação |
| `POST` | `/oauth2/token` | client credentials | token de serviço |
| `GET` | `/.well-known/jwks.json` | anônimo | chave pública para validação local |
| `GET` | `/.well-known/openid-configuration` | anônimo | metadados do emissor |
| `POST` | `/v1/users` | service + `identity.users.create` | cria usuário |
| `GET` | `/v1/users/{id}` | service + `identity.users.read` | consulta usuário |
| `PUT` | `/v1/users/{id}` | service + `identity.users.write` | altera usuário |
| `DELETE` | `/v1/users/{id}` | service + `identity.users.write` | desativa usuário |
| `POST` | `/v1/service-clients` | service + `identity.clients.create` | cria client e exibe o segredo uma vez |
| `GET` | `/v1/service-clients/{clientId}` | service + `identity.clients.read` | consulta client sem expor segredo/hash |
| `PUT` | `/v1/service-clients/{clientId}/permissions` | service + `identity.clients.write` | substitui audiences e scopes |
| `POST` | `/v1/service-clients/{clientId}/secret/rotate` | service + `identity.clients.write` | rotaciona e exibe o novo segredo uma vez |
| `DELETE` | `/v1/service-clients/{clientId}` | service + `identity.clients.write` | desativa client |
| `GET` | `/v1/audit-events` | service + `identity.audit.read` | consulta auditoria administrativa |
| `GET` | `/health` | anônimo interno | executa todos os health checks |
| `GET` | `/health/live` | anônimo | confirma que o processo está vivo |
| `GET` | `/health/ready` | anônimo | confirma que a API e o banco estão prontos |

Todos os endpoints são autenticados por padrão. OAuth, discovery, JWKS e health
aceitam chamadas anônimas na aplicação, mas devem ficar restritos à rede interna
em produção.

## Qualidade

```powershell
dotnet build FiapCloudGames.Identity.slnx -c Release
dotnet test FiapCloudGames.Identity.slnx -c Release --no-build
dotnet format FiapCloudGames.Identity.slnx --verify-no-changes --no-restore
```

## Segurança operacional

- use um segredo diferente para cada BFF, Gateway, API e worker;
- substitua as credenciais locais conhecidas fora de `Development`;
- armazene segredos e a chave privada em secret manager/Kubernetes Secret, nunca em Git;
- entregue às APIs apenas a chave pública/JWKS;
- faça rotação dos segredos e da chave de assinatura;
- capture o segredo retornado na criação/rotação: ele não pode ser consultado depois;
- mantenha access tokens curtos e cacheie tokens de serviço até próximo do vencimento;
- não registre senhas, client secrets, refresh tokens ou JWTs em logs;
- revogue imediatamente qualquer token pessoal do GitHub que tenha sido exposto.
