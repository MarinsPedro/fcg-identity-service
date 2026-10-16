# Configuração do Identity

As configurações seguem a hierarquia padrão do ASP.NET Core. Em produção, use variáveis de ambiente e um gerenciador de segredos.

## Banco de dados

```text
ConnectionStrings__Database=Host=localhost;Port=5432;Database=fcg_identity;Username=fcg_identity;Password=...
```

O Identity é o único proprietário do schema `identity`. Nenhum outro serviço deve acessar diretamente suas tabelas.

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

## Client inicial e provisionamento de produção

O projeto de migrations provisiona somente o `web-bff`. A API não cria clients
durante o startup. Em um banco novo, injete o segredo antes de executar o
migrador:

```text
InitialServiceClient__Secret=um-segredo-forte-e-exclusivo
```

O valor literal `CHANGE_ME` é rejeitado. O segredo é usado apenas como entrada,
armazenado como hash PBKDF2 e nunca gravado em auditoria ou devolvido por uma
consulta. Depois que o `web-bff` existe, novas execuções do migrador são
idempotentes e não exigem mais essa variável; removê-la do Job reduz a exposição
do segredo.

O `web-bff` inicial recebe audience `web-bff` e `identity-api`, permissão para
emitir tokens de usuário e os scopes administrativos necessários para cadastrar
usuários e gerenciar os demais clients. O migrador rejeita a tentativa de usar
essa seed para outro `client_id`.

Regras:

- `client_id` é único por aplicação/worker;
- o segredo nunca é compartilhado entre clientes;
- `CanIssueUserTokens=true` fica restrito às pontas autorizadas, como BFF e Gateway;
- audiência e escopos seguem privilégio mínimo;
- os segredos são persistidos somente como hash PBKDF2.

Cada audience e scope é persistido como uma associação individual em
`service_client_audiences` e `service_client_scopes`. As listas da configuração
do client inicial são usadas somente como entrada da seed; não são armazenadas em uma coluna
concatenada.

Depois do provisionamento, crie os clients de APIs e workers por
`POST /v1/service-clients`. O Identity gera o segredo com aleatoriedade
criptográfica e o mostra uma única vez. Use
`POST /v1/service-clients/{clientId}/secret/rotate` para rotação. A rotação
invalida imediatamente o segredo anterior; distribua o novo segredo de forma
coordenada pelo secret manager.

Em `Development`, a API possui o seguinte client exclusivamente para testes:

```text
client_id=web-bff
client_secret=WebBff@123456
audiences=web-bff identity-api
scopes=store.access identity.users.create identity.users.read identity.users.write
       identity.auth.login identity.auth.refresh identity.auth.revoke
```

A seed não sobrescreve um client existente. Portanto, alterar
`InitialServiceClient__Secret` não rotaciona uma credencial já persistida.
Da mesma forma, clients já provisionados precisam receber explicitamente os
scopes `identity.auth.login`, `identity.auth.refresh` e `identity.auth.revoke`
antes da adoção dos endpoints protegidos de autenticação. Em desenvolvimento,
também é possível recriar o banco.

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
