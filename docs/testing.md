# Estratégia de testes

Esta é a regra de testes do Identity Service. Cada comportamento deve ser
validado na suíte mais interna e rápida capaz de comprová-lo.

## Matriz das suítes

| Projeto | O que comprova | Pode depender de |
|---|---|---|
| `Identity.UnitTests` | invariantes do Domain e decisões dos casos de uso da Application | Domain e Application |
| `Identity.IntegrationTests` | host real, pipeline HTTP, contratos públicos e componentes técnicos | API e camadas externas necessárias |
| `Identity.Database.IntegrationTests` | modelo relacional, metadata Npgsql, migrations e snapshot | Infrastructure e Migrations |
| `Identity.ArchitectureTests` | direção das dependências, localização de responsabilidades e limites das suítes | assemblies e arquivos de projeto inspecionados |

## UnitTests

A suíte unitária contém somente duas áreas de comportamento e uma de suporte:

```text
UnitTests/
  Application/
    Auditing/
    Authentication/
    Users/
    ServiceClients/
  Domain/
    Auditing/
    Users/
    ServiceClients/
    Sessions/
  Support/
```

Regras obrigatórias:

- referenciar somente os projetos `Application` e `Domain`;
- não usar Infrastructure, Presentation, API, Migrations, EF Core ou
  `WebApplicationFactory`;
- testar Domain por invariantes, value objects e transições de estado;
- testar Application por decisões observáveis do caso de uso;
- usar fakes, stubs e spies pequenos em `Support`, sem container de DI;
- usar relógio fixo e dados determinísticos;
- verificar que falhas não persistem nem executam efeitos posteriores;
- evitar testar getters, mapeamentos triviais ou implementação privada.

Todos os casos de uso de `Application/Features` possuem uma suíte unitária
dedicada. Os fluxos HTTP continuam cobrindo integração e segurança do contrato,
mas não substituem os testes das decisões de Application.

Exemplo de nome:

```text
ExecuteAsync_WhenEmailAlreadyExists_ReturnsConflictWithoutPersisting
Rotate_WhenAlreadyRevoked_RejectsSecondRotation
```

O formato é `Metodo_Cenario_ResultadoEsperado`.

## IntegrationTests

```text
IntegrationTests/
  Host/
  Components/
    Authentication/
    Seeding/
  Contracts/
  Support/
```

- `Host`: inicialização, configuração de logging, Problem Details, correlação,
  sanitização de erros, health checks e comportamento global do processo;
- `Components`: integração de componentes técnicos que não exige contrato HTTP
  completo, como assinatura JWT e seeds;
- `Contracts`: status code, headers, Problem Details, JSON, rotas, autorização e
  fluxos HTTP relevantes;
- `Support`: `WebApplicationFactory`, fixtures e utilitários compartilhados.

Testes por endpoint só devem existir quando comprovam comportamento HTTP ou de
segurança que não pode ser coberto adequadamente na Application. Regras de
negócio não devem ser testadas exclusivamente através da API.

O banco em memória é usado apenas pelo host HTTP para manter os testes de
contrato rápidos. Convenções específicas do PostgreSQL pertencem à suíte de
banco.

## Database.IntegrationTests

Essa suíte inspeciona o modelo configurado com o provider Npgsql e o assembly de
migrations. Ela valida:

- schema, tabelas, colunas, chaves e índices;
- localização e descoberta das migrations;
- ausência de divergência entre o modelo atual e o snapshot;
- assembly correto de migrations.

Esses testes não abrem conexão com um PostgreSQL real. Um teste com banco real
deve ser criado separadamente quando houver comportamento que dependa da
execução do servidor, como SQL específico, locking ou transação distribuída.

## ArchitectureTests

As regras automatizadas devem falhar quando:

- uma camada interna passa a referenciar uma camada externa;
- Controllers aparecem fora de Presentation;
- tipos públicos do Domain expõem setters ou construtores públicos;
- actions de Controllers expõem tipos internos de Application ou Domain;
- migrations aparecem fora do projeto Migrations;
- UnitTests ganha referência de projeto além de Application e Domain;
- UnitTests adiciona EF Core ou `Microsoft.AspNetCore.Mvc.Testing`.

## Execução local

```powershell
dotnet build FiapCloudGames.Identity.slnx -c Release
dotnet test FiapCloudGames.Identity.slnx -c Release --no-build
dotnet format FiapCloudGames.Identity.slnx --verify-no-changes --no-restore
```

Durante o desenvolvimento, execute primeiro a suíte diretamente afetada. Antes
de entregar, execute a solução completa.
