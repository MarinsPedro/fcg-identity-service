# Arquitetura do Identity Service

O serviço mantém as mesmas convenções do monólito: Clean Architecture, DDD,
organização vertical por feature e separação entre contratos HTTP e casos de
uso. O monólito é somente a referência; nenhuma alteração nele é necessária
para evoluir este repositório.

## Direção das dependências

```text
API ───────────→ Presentation ──→ Application ──→ Domain
API ───────────→ Infrastructure ──→ Application ──→ Domain
Migrations ───────→ Infrastructure
```

As setas apontam para o projeto conhecido pela origem. Uma camada externa
conhece uma camada interna; o inverso não ocorre.

| Projeto | Responsabilidade | Dependências de projeto permitidas |
|---|---|---|
| Domain | agregados, entidades, value objects, invariantes e repositories | nenhuma |
| Application | casos de uso, Inputs, Results e portas técnicas | Domain |
| Infrastructure | EF Core, repositories, hashing, JWT e health checks | Application e Domain |
| Presentation | Controllers, Requests, Responses e mappings HTTP | Application |
| API | composition root, middleware e pipeline | Infrastructure e Presentation |
| Migrations | migrations EF Core e atualização explícita do banco | Infrastructure |

`FiapCloudGames.Identity.ArchitectureTests` fiscaliza essa matriz para impedir
dependências acidentais.

## Organização por feature

Application e Presentation são agrupadas pelo comportamento, e não por uma
pasta genérica de services ou DTOs:

```text
Application/Features/
  Authentication/
    Login/       LoginInput, LoginResult, LoginService
    Refresh/     RefreshUserTokenInput, Result e Service
    Revoke/      RevokeRefreshTokenInput e Service
    ClientCredentials/
  Users/
    CreateUser/
    GetUser/
    UpdateUser/
    DeactivateUser/
  Clients/
    CreateClient/
    GetClient/
    UpdatePermissions/
    RotateSecret/
    DeactivateClient/
  Audit/

Presentation/Features/
  Authentication/
  Discovery/
  Users/
  Clients/
  Audit/
```

Cada caso de uso expõe `ExecuteAsync`, recebe um Input sem semântica HTTP,
propaga `CancellationToken` e devolve um Result. Controllers adaptam o contrato
HTTP e nunca devolvem entidades de domínio diretamente.

## Persistência

- interfaces de repository de agregados pertencem ao Domain;
- `IIdentityUnitOfWork` pertence à Application;
- repositories e `IdentityDbContext` pertencem à Infrastructure;
- o `DbContext` implementa somente a unidade de trabalho, não um repository
  genérico com todas as operações;
- escrita usa tracking; novas consultas somente de leitura devem usar
  `AsNoTracking`;
- entidades e `IQueryable` não atravessam a fronteira da Infrastructure.

As permissões dos clients são normalizadas. `service_client_scopes` e
`service_client_audiences` possuem uma linha por associação e chave composta
com `client_id`. `ServiceClient` continua sendo a raiz do agregado e controla
concessão, remoção e verificação dessas coleções. O repository sempre carrega as
duas coleções ao autenticar um client.

As mutações administrativas do agregado `ServiceClient` persistem um
`SecurityAuditEvent` na mesma unidade de trabalho. O evento registra tipo,
client ator, tipo/ID do alvo e instante UTC, mas nunca segredo, hash ou JWT.

Como este é um microserviço com banco próprio, as migrations e o snapshot ficam
em `FiapCloudGames.Identity.Migrations`. Esse projeto é um executável que aplica
somente o banco do Identity. Ele depende de Infrastructure; API e Infrastructure
não dependem dele. A API nunca altera schema durante o startup.

Em desenvolvimento, execute o migrador antes da API. Em Kubernetes, execute a
imagem `migrations` como Job antes do rollout. Uma falha encerra com código `1`;
cancelamento encerra com código `2`, impedindo que o deploy prossiga como se o
banco estivesse atualizado. Depois das migrations, o mesmo executável realiza
as seeds idempotentes do administrador e do único client inicial `web-bff`.
Outros clients são criados pela API administrativa.

O migrador registra o `IdentityDbContext` e o executor no container de DI, usa
`ILogger` para logs estruturados e aplica retry limitado para falhas transitórias
do PostgreSQL. `FiapCloudGames.Identity.MigrationsTests` garante que:

- as migrations são descobertas pelo EF Core;
- o modelo não divergiu do snapshot;
- o assembly de migrations é o projeto correto;
- schema, tabelas e colunas seguem as convenções do Identity.

## Onde implementar uma mudança

| Mudança | Local |
|---|---|
| invariante ou comportamento da entidade | Domain |
| contrato de acesso ao agregado | Domain/Repositories |
| coordenação de uma operação | Application/Features |
| abstração de recurso técnico | Application/Abstractions |
| EF Core, repository, JWT ou hashing | Infrastructure |
| migration, snapshot ou execução de atualização do schema | Migrations |
| rota, Request, Response ou mapping HTTP | Presentation/Features |
| registro final e pipeline | API/Program.cs |
| regra estrutural | ArchitectureTests |

## Contracts entre serviços

Não existe um projeto `Contracts` vazio neste momento. Ele deve ser criado
quando aparecer um contrato real consumido por outro serviço, como eventos de
integração ou um SDK público. Outros serviços não devem referenciar Domain,
Application, Infrastructure ou Presentation do Identity.

## Checklist para uma feature

- a regra pertence ao bounded context Identity;
- o Domain permanece válido após toda operação;
- Application depende de abstrações, nunca de EF Core ou HTTP;
- Request e Response permanecem na Presentation;
- nenhum segredo, senha ou token é registrado em log;
- autorização e escopos foram definidos explicitamente;
- erros continuam usando Problem Details do Building Blocks;
- testes unitários cobrem regras e casos de uso;
- testes de integração cobrem contrato e pipeline;
- testes de arquitetura continuam aprovados.
