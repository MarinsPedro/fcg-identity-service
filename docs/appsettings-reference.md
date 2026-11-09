# Referência de appsettings

Este guia descreve todas as configurações próprias do Identity lidas pela API e
pelo executável de migrations. Os dois processos possuem arquivos separados e
não carregam o `appsettings.json` um do outro.

## Como a configuração é resolvida

O .NET combina as fontes de configuração e a fonte carregada por último
sobrescreve as anteriores. Para este projeto, use a seguinte ordem mental:

1. `appsettings.json` contém valores padrão e a estrutura completa;
2. `appsettings.{Environment}.json` contém diferenças do ambiente;
3. User Secrets são usados pelo migrador durante `Development`;
4. variáveis de ambiente substituem os arquivos;
5. argumentos de linha de comando têm a maior precedência.

Em variáveis de ambiente, substitua cada `:` por `__`. Por exemplo:

```text
RabbitMq:VirtualHost        -> RabbitMq__VirtualHost
Jwt:UserTokenLifetime      -> Jwt__UserTokenLifetime
Messaging:UseInMemory      -> Messaging__UseInMemory
```

Não versione valores reais de senha, segredo ou chave privada. Os valores
locais conhecidos devem existir apenas em `Development`; em ambientes
compartilhados, injete-os por secret manager, Kubernetes Secret ou mecanismo
equivalente.

### Compose e Kubernetes do workspace

Os `appsettings*.json` deste repositório fornecem a estrutura e os padrões para
execução direta. No ambiente integrado, o repositório irmão
`fgc-orchestrator-service` aplica as substituições de runtime:

| Execução | API | Migrations |
|---|---|---|
| Docker Compose | `config/compose/identity/api.env` | `config/compose/identity/migrations.env` |
| Kubernetes | `config/k8s-identity-api.env` | `config/k8s-identity-migrations.env` |

No Compose acadêmico, as credenciais locais conhecidas estão no arquivo de
migrations. No Kubernetes, os arquivos `.env` contêm somente valores não
secretos; `k8s/migrations-job.yaml` recebe senha do administrador e secrets de
`web-bff` e `orders-api` por `Secret`. Como a seed é executada em todo rollout,
esses dois secrets precisam continuar disponíveis para o Job.

## API

O arquivo da API está em
`src/FiapCloudGames.Identity.Api/appsettings.json`.

### Exemplo completo

```json
{
  "ConnectionStrings": {
    "Database": "Host=postgres;Port=5432;Database=fcg_identity;Username=fcg_identity_app;Password=CHANGE_ME"
  },
  "Messaging": {
    "UseInMemory": false
  },
  "RabbitMq": {
    "Host": "rabbitmq",
    "Port": 5672,
    "VirtualHost": "/",
    "Username": "fcg_identity",
    "Password": "CHANGE_ME"
  },
  "Jwt": {
    "Issuer": "https://identity.example.com",
    "PrivateKeyPem": null,
    "PrivateKeyPath": "/run/secrets/identity-signing-key.pem",
    "KeyId": "identity-key-2026-01",
    "UserTokenLifetime": "00:15:00",
    "ServiceTokenLifetime": "00:05:00",
    "RefreshTokenLifetime": "7.00:00:00",
    "AllowEphemeralDevelopmentKey": false
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore.Database.Command": "Warning"
    }
  },
  "AllowedHosts": "identity.example.com"
}
```

### Banco de dados

| Chave | Obrigatória | Como preencher |
|---|---:|---|
| `ConnectionStrings:Database` | sim | Connection string Npgsql do banco exclusivo do Identity. |

Exemplo como variável de ambiente:

```text
ConnectionStrings__Database=Host=postgres;Port=5432;Database=fcg_identity;Username=fcg_identity_app;Password=CHANGE_ME
```

Use `localhost` ou `127.0.0.1` somente quando a API e o PostgreSQL estiverem
acessíveis pela mesma máquina. Dentro de Docker Compose ou Kubernetes, use o
nome DNS do serviço, como `postgres`.

### Mensageria

| Chave | Obrigatória | Padrão | Como preencher |
|---|---:|---:|---|
| `Messaging:UseInMemory` | sim | `false` | Mantenha `false` fora de testes automatizados. |
| `RabbitMq:Host` | quando não estiver em memória | — | Host ou nome DNS do broker; não é a URL da interface web. |
| `RabbitMq:Port` | quando não estiver em memória | `5672` | Porta AMQP. A porta `15672` é somente da interface administrativa. |
| `RabbitMq:VirtualHost` | quando não estiver em memória | `/` | Virtual host previamente existente e acessível pelo usuário. |
| `RabbitMq:Username` | quando não estiver em memória | — | Usuário com permissão para declarar e publicar na topologia. |
| `RabbitMq:Password` | quando não estiver em memória | — | Segredo do usuário do RabbitMQ. |

Com `Messaging:UseInMemory=true`, a API não conecta ao RabbitMQ, não cria a
exchange e não utiliza o outbox do PostgreSQL. Esse modo é destinado à suíte de
testes. Para execução normal, use:

```text
Messaging__UseInMemory=false
RabbitMq__Host=rabbitmq
RabbitMq__Port=5672
RabbitMq__VirtualHost=/
RabbitMq__Username=fcg_identity
RabbitMq__Password=CHANGE_ME
```

O `UserCreatedEvent` é publicado na exchange durável e `fanout`
`identity.user-created.v1`. O nome faz parte da topologia da aplicação e não é
uma configuração operacional do `appsettings`.

### JWT

| Chave | Obrigatória | Exemplo | Como preencher |
|---|---:|---|---|
| `Jwt:Issuer` | sim | `https://identity.example.com` | Identificador estável do emissor esperado por todas as APIs consumidoras. |
| `Jwt:PrivateKeyPath` | produção | `/run/secrets/identity-signing-key.pem` | Caminho de uma chave privada RSA PEM montada como secret. |
| `Jwt:PrivateKeyPem` | alternativa | `null` | Conteúdo PEM completo; prefira `PrivateKeyPath` para evitar problemas com múltiplas linhas. |
| `Jwt:KeyId` | sim | `identity-key-2026-01` | Identificador da chave publicado no JWT e no JWKS; altere a cada rotação. |
| `Jwt:UserTokenLifetime` | sim | `00:15:00` | Duração do access token de usuário. |
| `Jwt:ServiceTokenLifetime` | sim | `00:05:00` | Duração do token entre serviços. |
| `Jwt:RefreshTokenLifetime` | sim | `7.00:00:00` | Duração do refresh token. |
| `Jwt:AllowEphemeralDevelopmentKey` | sim | `false` | Só use `true` localmente em `Development`. |

Configure apenas uma fonte de chave privada. Em produção, mantenha
`AllowEphemeralDevelopmentKey=false`; a API falhará cedo se a chave persistente
não estiver disponível. A sintaxe de duração é `d.hh:mm:ss`, em que a parte dos
dias pode ser omitida.

Exemplo recomendado em contêiner:

```text
Jwt__Issuer=https://identity.example.com
Jwt__PrivateKeyPath=/run/secrets/identity-signing-key.pem
Jwt__KeyId=identity-key-2026-01
Jwt__UserTokenLifetime=00:15:00
Jwt__ServiceTokenLifetime=00:05:00
Jwt__RefreshTokenLifetime=7.00:00:00
Jwt__AllowEphemeralDevelopmentKey=false
```

### Logging e host

| Chave | Padrão | Como preencher |
|---|---|---|
| `Logging:LogLevel:Default` | `Information` | Nível geral de logs. |
| `Logging:LogLevel:Microsoft.AspNetCore` | `Warning` | Reduz o volume do pipeline HTTP. |
| `Logging:LogLevel:Microsoft.EntityFrameworkCore.Database.Command` | `Warning` | Evita registrar todo SQL em operação normal. |
| `AllowedHosts` | `*` | Hosts HTTP aceitos pelo Host Filtering Middleware; restrinja no deployment quando aplicável. |

O ambiente e a URL de escuta são configurações do host, normalmente definidas
fora do JSON:

```text
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:8080
```

## Executável de migrations

O arquivo do migrador está em
`src/FiapCloudGames.Identity.Migrations/appsettings.json`. Ele atualiza o schema
e executa as seeds do administrador e dos clients técnicos iniciais.

### Conexão

O migrador resolve a conexão nesta ordem:

1. `IDENTITY_MIGRATIONS_CONNECTION`;
2. `ConnectionStrings__Database`;
3. `IDENTITY_DESIGN_CONNECTION`;
4. `ConnectionStrings:Database` do seu próprio `appsettings.json`.

Para um Job de deployment, prefira:

```text
IDENTITY_MIGRATIONS_CONNECTION=Host=postgres;Port=5432;Database=fcg_identity;Username=fcg_identity_migrator;Password=CHANGE_ME
DOTNET_ENVIRONMENT=Production
```

### Administrador inicial

| Chave | Obrigatória | Como preencher |
|---|---:|---|
| `Admin:Name` | não | Nome exibido; usa `Administrador` quando omitido. |
| `Admin:Email` | junto com a senha | E-mail válido e exclusivo do administrador. |
| `Admin:Password` | junto com o e-mail | Mínimo de oito caracteres, com letra, número e caractere especial. |

Se `Email` e `Password` forem `null`, a seed de administrador é ignorada. Se
uma das duas for preenchida, a outra também se torna obrigatória.

```text
Admin__Name=Administrador
Admin__Email=admin@example.com
Admin__Password=CHANGE_ME
```

### Clients técnicos iniciais

| Chave | Obrigatória | Padrão/observação |
|---|---:|---|
| `InitialServiceClients:Clients` | sim | Coleção dos clients gerenciados pelo migrador. |
| `InitialServiceClients:Clients:n:ClientId` | sim | Identificador único do client. |
| `InitialServiceClients:Clients:n:DisplayName` | sim | Nome administrativo do client. |
| `InitialServiceClients:Clients:n:Secret` | sim, em toda execução | Segredo forte; `CHANGE_ME` é rejeitado. O hash é atualizado quando o valor muda. |
| `InitialServiceClients:Clients:n:CanIssueUserTokens` | sim | `true` somente para pontas autorizadas, como o `web-bff`. |
| `InitialServiceClients:Clients:n:AllowedAudiences` | sim | Lista de audiences que o client pode solicitar. |
| `InitialServiceClients:Clients:n:AllowedScopes` | sim | Lista de scopes concedidos ao client. |

O segredo é obrigatório e a configuração é a fonte de verdade. Em cada
execução, o migrador sincroniza nome, secret, estado, capacidade de emitir
tokens, audiences e scopes. Coleções e listas em variáveis de ambiente usam
índices iniciados em zero:

```text
InitialServiceClients__Clients__0__Secret=CHANGE_ME
InitialServiceClients__Clients__0__AllowedAudiences__0=web-bff
InitialServiceClients__Clients__0__AllowedAudiences__1=identity-api
InitialServiceClients__Clients__0__AllowedScopes__0=store.access
InitialServiceClients__Clients__1__Secret=CHANGE_ME
InitialServiceClients__Clients__1__AllowedAudiences__0=catalog-api
InitialServiceClients__Clients__1__AllowedScopes__0=catalog.games.read
```

Ao substituir uma lista por variáveis de ambiente, informe todos os itens que
o ambiente deve conceder. Mantenha o princípio do menor privilégio. A coleção é
autoritativa apenas para os clients nela declarados: cada um é sincronizado e
reativado; clients não declarados permanecem inalterados.

## Checklist antes de iniciar

### API

- PostgreSQL acessível com o usuário da aplicação;
- migrations já executadas;
- `Messaging:UseInMemory=false` fora de testes;
- RabbitMQ acessível pela porta AMQP e virtual host correto;
- chave RSA persistente disponível fora de `Development`;
- `Issuer` e `KeyId` alinhados com as APIs consumidoras;
- nenhum segredo real salvo no Git.

### Migrations

- usuário do banco com permissão para alterar o schema `identity`;
- secrets de todos os clients gerenciados fornecidos em toda execução;
- credenciais de administrador fornecidas juntas ou ambas omitidas;
- `DOTNET_ENVIRONMENT` definido corretamente;
- secrets injetados por Secret/secret manager, sem valores reais no manifesto
  ou na imagem.
