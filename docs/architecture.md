# Arquitetura do Identity Service

O Identity usa Clean Architecture, DDD e organização vertical por caso de uso.
Contratos HTTP, regras de aplicação, modelo de domínio e detalhes técnicos ficam
em projetos distintos.

## Direção das dependências

```text
API ───────────→ Presentation ──→ Application ──→ Domain
API ───────────→ Infrastructure ──→ Application ──→ Domain
Migrations ───────→ Infrastructure
```

| Projeto | Responsabilidade | Referências permitidas |
|---|---|---|
| Domain | agregados, entidades, value objects, invariantes e contratos de repository dos agregados | nenhuma |
| Application | casos de uso, Inputs, Results e portas técnicas | Domain |
| Infrastructure | EF Core, repositories, hashing, JWT e health checks | Application e Domain |
| Presentation | Controllers, Requests, Responses, autorização e mappings HTTP | Application |
| API | composition root, configuração e pipeline | Infrastructure e Presentation |
| Migrations | design-time factory, migrations, execução e seeds iniciais | Infrastructure |

`FiapCloudGames.Identity.ArchitectureTests` fiscaliza essa matriz e as fronteiras
dos testes unitários.

## Domain organizado por agregado

```text
Domain/
  Common/
    DomainRuleViolationException.cs
  Users/
    User.cs
    Email.cs
    Password.cs
    UserRole.cs
    IUserRepository.cs
  ServiceClients/
    ServiceClient.cs
    ServiceClientAudience.cs
    ServiceClientScope.cs
    IServiceClientRepository.cs
  Sessions/
    RefreshToken.cs
    IRefreshTokenRepository.cs
  Auditing/
    SecurityAuditEvent.cs
```

Uma pasta representa um agregado ou um conceito coeso. Entidade, value objects,
enumerações e interface do repository da raiz permanecem próximos. `Common`
contém somente conceitos realmente compartilhados pelo domínio; não é um local
genérico para classes sem destino definido.

`ServiceClient` é a raiz do agregado que controla audiences e scopes. Essas
coleções não são alteradas diretamente por Application ou Infrastructure.
`RefreshToken` pertence ao ciclo de sessões e não ao agregado de usuário.

## Application organizada por caso de uso

```text
Application/
  Abstractions/
    Auditing/
    Persistence/
    Security/
  Features/
    Authentication/
      Common/
      ClientCredentials/
      Login/
      Refresh/
      Revoke/
    Users/
      Create/
      Get/
      Update/
      Deactivate/
    ServiceClients/
      Create/
      Get/
      UpdatePermissions/
      RotateSecret/
      Deactivate/
    Auditing/
      GetSecurityAuditEvents/
```

Os nomes das pastas descrevem o comportamento sem repetir sufixos como
`CreateUser/CreateUserService`. Cada caso de uso expõe `ExecuteAsync`, recebe um
Input sem semântica HTTP, propaga `CancellationToken` e devolve um Result.

As portas técnicas ficam em `Abstractions`:

- `Security`: hashing, proteção de refresh token e emissão de JWT;
- `Persistence`: unidade de trabalho;
- `Auditing`: persistência e consulta de auditoria, pois auditoria não é uma
  raiz de agregado manipulada diretamente pelos consumidores do Domain.

## Presentation organizada por recurso HTTP

```text
Presentation/
  Authorization/
  Features/
    Authentication/
    OAuth/
    Discovery/
    Users/
    ServiceClients/
    Auditing/
```

`Authentication` contém login, refresh e revoke. `OAuth` contém o endpoint
padronizado de client credentials. `Discovery` publica os documentos públicos.
Os Controllers adaptam o protocolo HTTP para os casos de uso e nunca devolvem
entidades de domínio diretamente.

Requests, Responses e mappings permanecem próximos do Controller do recurso.
Policies ficam em `Authorization`, evitando dependências circulares entre
features.

## Infrastructure e EF Core

```text
Infrastructure/
  Authentication/
  Health/
  Persistence/
    Configurations/
      UserConfiguration.cs
      ServiceClientConfiguration.cs
      ServiceClientAudienceConfiguration.cs
      ServiceClientScopeConfiguration.cs
      RefreshTokenConfiguration.cs
      SecurityAuditEventConfiguration.cs
    Repositories/
      UserRepository.cs
      ServiceClientRepository.cs
      RefreshTokenRepository.cs
      SecurityAuditRepository.cs
    IdentityDbContext.cs
  Security/
```

`IdentityDbContext` declara os `DbSet`, define o schema padrão e descobre as
configurações com `ApplyConfigurationsFromAssembly`. Cada tipo possui uma
implementação de `IEntityTypeConfiguration<T>` própria. Isso mantém o contexto
pequeno e permite alterar um mapping sem editar uma classe central extensa.

Repositories concretos ficam somente em `Persistence/Repositories`. Interfaces
dos agregados ficam no Domain; portas técnicas ficam na Application. Não há
repository genérico e `IQueryable` não atravessa a Infrastructure.

As permissões são normalizadas: `service_client_scopes` e
`service_client_audiences` possuem uma linha por associação e chave composta
com `client_id`. As mutações administrativas persistem um
`SecurityAuditEvent` na mesma unidade de trabalho, sem segredo, hash ou JWT.

## Migrations

```text
Migrations/
  Configuration/
  DesignTime/
  Migrations/
  Seeding/
  MigrationRunner.cs
  Program.cs
```

O projeto é um executável independente. `Configuration` resolve a connection
string e configura o `DbContext`; `DesignTime` atende `dotnet ef` e Package
Manager Console; `Migrations` contém somente artefatos gerados pelo EF;
`Seeding` contém o provisionamento inicial idempotente.

A API nunca altera o schema no startup. Em desenvolvimento, execute o migrador
antes da API. Em produção, execute-o como uma etapa ou Job anterior ao rollout.

## Onde implementar uma mudança

| Mudança | Local |
|---|---|
| invariante ou comportamento de agregado | `Domain/<Agregado>` |
| contrato de persistência de uma raiz de agregado | `Domain/<Agregado>` |
| coordenação de uma operação | `Application/Features/<Recurso>/<CasoDeUso>` |
| abstração de recurso técnico | `Application/Abstractions/<Categoria>` |
| mapping EF Core | `Infrastructure/Persistence/Configurations` |
| repository concreto | `Infrastructure/Persistence/Repositories` |
| migration, snapshot ou seed inicial | `Migrations` |
| rota, Request, Response ou mapping HTTP | `Presentation/Features/<Recurso>` |
| configuração final e pipeline | `Api/Program.cs` |
| regra estrutural | `ArchitectureTests` |

## Testes

A estratégia, os limites de cada suíte e as convenções obrigatórias estão em
[testing.md](testing.md).

## Checklist para uma feature

- a regra pertence ao bounded context Identity;
- o agregado permanece válido após toda operação;
- Application depende de abstrações, nunca de EF Core ou HTTP;
- Request e Response permanecem na Presentation;
- nenhum segredo, senha ou token é registrado em log;
- autorização e scopes foram definidos explicitamente;
- erros continuam usando Problem Details do Building Blocks;
- testes unitários cobrem regras e decisões do caso de uso;
- testes de integração cobrem contrato, host ou componente técnico;
- testes de arquitetura continuam aprovados.
