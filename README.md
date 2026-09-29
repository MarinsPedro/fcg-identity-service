# Fiap Cloud Games — Identity Service

Serviço responsável por identidades, autenticação e emissão de tokens da plataforma Fiap Cloud Games.

Este repositório é independente do monólito. Ele possui banco, migrations, pipeline e ciclo de entrega próprios.

## Responsabilidades

- cadastrar, consultar, atualizar e desativar usuários;
- autenticar usuários por e-mail e senha;
- autenticar aplicações por `client_id` e `client_secret`;
- emitir access tokens JWT dos tipos `user` e `service`;
- emitir e rotacionar refresh tokens de usuário;
- publicar a chave pública de assinatura em JWKS;
- expor metadados de descoberta para os consumidores dos tokens.

O Identity **não** decide se o usuário pode comprar um jogo, visualizar um pedido ou executar outra regra de negócio. Essa autorização pertence à ponta (BFF/API Gateway) e, quando necessário, ao serviço que possui o recurso.

## Fluxos de token

### Token de usuário

1. O cliente envia as credenciais ao BFF ou API Gateway.
2. A ponta chama `POST /api/auth/login` usando seu próprio `client_id` e segredo.
3. O Identity valida aplicação, usuário e senha.
4. O Identity devolve um JWT com `token_use=user`, `sub` igual ao ID do usuário e `client_id` igual à ponta solicitante.
5. O token permanece na borda. Ao chamar APIs internas, a ponta usa um token de serviço e envia o ID do usuário no contrato da operação quando a regra exigir esse contexto.

### Token de serviço

1. Uma aplicação ou worker chama `POST /connect/token` com `grant_type=client_credentials`.
2. O Identity valida o `client_id`, o segredo, a audiência e os escopos solicitados.
3. O Identity devolve um JWT curto com `token_use=service`, destinado somente à audiência solicitada.
4. A aplicação mantém esse token em cache até pouco antes da expiração. Não se solicita um token novo a cada chamada.

Não existe token `delegated`, cadeia `act` ou `origin_client_id` neste desenho.

## Estrutura

```text
src/
  FiapCloudGames.Identity.Domain/          entidades e regras do domínio
  FiapCloudGames.Identity.Application/     casos de uso, Inputs, Results e portas
  FiapCloudGames.Identity.Infrastructure/  PostgreSQL, hashing e emissão JWT
  FiapCloudGames.Identity.Presentation/    Controllers, Requests, Responses e mappings HTTP
  FiapCloudGames.Identity.Api/             composition root e pipeline
tests/
  FiapCloudGames.Identity.ArchitectureTests/
  FiapCloudGames.Identity.UnitTests/
  FiapCloudGames.Identity.IntegrationTests/
docs/
  architecture.md
  configuration.md
```

As regras e exemplos da estrutura estão em [docs/architecture.md](docs/architecture.md).

## Pré-requisitos

- .NET SDK 10;
- PostgreSQL;
- acesso ao pacote privado `FiapCloudGames.BuildingBlocks` versão `0.2.0` no GitHub Packages.

O `NuGet.Config` versionado contém somente os endereços dos feeds. Credenciais nunca devem ser gravadas no repositório. Para restaurar localmente, configure a fonte no perfil do desenvolvedor ou use um arquivo temporário fora do Git:

```powershell
dotnet nuget update source github `
  --username SEU_USUARIO `
  --password $env:GITHUB_PACKAGES_TOKEN `
  --store-password-in-clear-text `
  --configfile "$env:APPDATA\NuGet\NuGet.Config"
```

O token precisa somente da permissão necessária para ler pacotes privados (`read:packages`) e acesso ao repositório/pacote.

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
dotnet ef database update --project src/FiapCloudGames.Identity.Infrastructure --startup-project src/FiapCloudGames.Identity.Api
dotnet run --project src/FiapCloudGames.Identity.Api
```

Para criar a imagem sem gravar credenciais do GitHub Packages em camadas Docker, forneça um `NuGet.Config` autenticado como secret do BuildKit:

```powershell
docker build `
  --secret "id=nuget_config,src=$env:APPDATA\NuGet\NuGet.Config" `
  -t fcg-identity:local .
```

## Endpoints principais

| Método | Rota | Autenticação | Uso |
|---|---|---|---|
| `POST` | `/api/auth/login` | credenciais da aplicação e do usuário | login de usuário |
| `POST` | `/api/auth/refresh` | credenciais da aplicação + refresh token | rotação do token |
| `POST` | `/api/auth/revoke` | credenciais da aplicação + refresh token | logout/revogação |
| `POST` | `/connect/token` | client credentials | token de serviço |
| `GET` | `/.well-known/jwks.json` | anônimo | chave pública para validação local |
| `GET` | `/.well-known/openid-configuration` | anônimo | metadados do emissor |
| `POST` | `/internal/users` | service + `identity.users.create` | cria usuário |
| `GET` | `/internal/users/{id}` | service + `identity.users.read` | consulta usuário |
| `PUT` | `/internal/users/{id}` | service + `identity.users.write` | altera usuário |
| `DELETE` | `/internal/users/{id}` | service + `identity.users.write` | desativa usuário |

Todos os endpoints são autenticados por padrão. Somente os endpoints públicos marcados explicitamente aceitam chamadas anônimas.

## Qualidade

```powershell
dotnet build FiapCloudGames.Identity.slnx -c Release
dotnet test FiapCloudGames.Identity.slnx -c Release --no-build
dotnet format FiapCloudGames.Identity.slnx --verify-no-changes --no-restore
```

## Segurança operacional

- use um segredo diferente para cada BFF, Gateway, API e worker;
- armazene segredos e a chave privada em secret manager/Kubernetes Secret, nunca em Git;
- entregue às APIs apenas a chave pública/JWKS;
- faça rotação dos segredos e da chave de assinatura;
- mantenha access tokens curtos e cacheie tokens de serviço até próximo do vencimento;
- não registre senhas, client secrets, refresh tokens ou JWTs em logs;
- revogue imediatamente qualquer token pessoal do GitHub que tenha sido exposto.
