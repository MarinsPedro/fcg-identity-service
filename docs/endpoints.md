# Endpoints e ordem de uso do Identity

Este documento explica a finalidade de cada endpoint do Identity, quem deve
chamá-lo e a sequência recomendada para os fluxos de cadastro, login, renovação
e logout.

## Visão rápida

| Endpoint | Quem chama | Credencial exigida | Finalidade |
|---|---|---|---|
| `POST /oauth2/token` | BFF, Gateway, API ou worker | `client_id` e `client_secret` | emitir token técnico `service` |
| `POST /v1/users` | aplicação autorizada | token `service` + `identity.users.create` | cadastrar usuário |
| `GET /v1/users/{id}` | aplicação autorizada | token `service` + `identity.users.read` | consultar usuário |
| `PUT /v1/users/{id}` | aplicação autorizada | token `service` + `identity.users.write` | alterar nome e e-mail |
| `DELETE /v1/users/{id}` | aplicação autorizada | token `service` + `identity.users.write` | desativar usuário |
| `POST /v1/service-clients` | `web-bff` ou client administrador | token `service` + `identity.clients.create` | criar client e obter o segredo uma vez |
| `GET /v1/service-clients/{clientId}` | client administrador | token `service` + `identity.clients.read` | consultar dados públicos do client |
| `PUT /v1/service-clients/{clientId}/permissions` | client administrador | token `service` + `identity.clients.write` | substituir audiences e scopes |
| `POST /v1/service-clients/{clientId}/secret/rotate` | client administrador | token `service` + `identity.clients.write` | rotacionar o segredo |
| `DELETE /v1/service-clients/{clientId}` | client administrador | token `service` + `identity.clients.write` | desativar client |
| `GET /v1/audit-events` | client auditor | token `service` + `identity.audit.read` | consultar eventos administrativos |
| `POST /v1/auth/login` | BFF ou Gateway | token `service` + `identity.auth.login` | iniciar sessão do usuário |
| `POST /v1/auth/refresh` | BFF ou Gateway | token `service` + `identity.auth.refresh` | renovar a sessão |
| `POST /v1/auth/revoke` | BFF ou Gateway | token `service` + `identity.auth.revoke` | encerrar a sessão |
| `GET /.well-known/openid-configuration` | bibliotecas e infraestrutura | nenhuma | descobrir os metadados do emissor |
| `GET /.well-known/jwks.json` | APIs que validam JWT | nenhuma | obter a chave pública de assinatura |
| `GET /health` | infraestrutura | nenhuma | executar todos os health checks |
| `GET /health/live` | infraestrutura | nenhuma | verificar se o processo está vivo |
| `GET /health/ready` | infraestrutura | nenhuma | verificar se API e banco estão prontos |

Somente OAuth, discovery, JWKS e health aceitam chamadas anônimas na aplicação.
Em produção, continuam restritos à rede interna. Login, refresh e revoke exigem
JWT de serviço destinado a `identity-api`; o `client_id` é obtido do token e o
segredo não é repetido nesses contratos.

## Convenção das URLs

- `/v1` identifica os recursos e contratos versionados do Identity;
- `/oauth2` contém endpoints do protocolo OAuth;
- `/.well-known` e `/health` são contratos de protocolo e infraestrutura e não
  recebem a versão da API.

O prefixo da URL não é um mecanismo de segurança. As rotas `/v1` continuam
protegidas por JWT de serviço, audience, scopes e controles de rede. As URLs
antigas não são mantidas como aliases; consumidores devem migrar para os
contratos versionados.

## Pré-requisitos

Antes de testar os fluxos:

1. aplique as migrations do Identity;
2. forneça `InitialServiceClient__Secret` na primeira execução do migrador;
3. inicie o Identity;
4. confirme que o client possui as audiences e os scopes necessários.

Ao executar os projetos com seus perfis `Development`, já ficam disponíveis:

```text
Administrador:
  email: admin@fiapcloudgames.com
  senha: Admin@123456

Service client:
  client_id: web-bff
  client_secret: WebBff@123456
```

Essas credenciais são intencionalmente conhecidas e devem ser usadas somente em
desenvolvimento local.

Para executar todo o exemplo deste documento, o client precisa de:

```text
CanIssueUserTokens=true
AllowedAudiences:
  - web-bff
  - identity-api
AllowedScopes:
  - store.access
  - identity.auth.login
  - identity.auth.refresh
  - identity.auth.revoke
  - identity.users.create
  - identity.users.read
  - identity.users.write
  - identity.clients.create
  - identity.clients.read
  - identity.clients.write
  - identity.audit.read
```

`web-bff` é apenas o identificador usado no exemplo. Cada BFF, Gateway, API e
worker deve possuir credenciais próprias. Nunca envie o `client_secret` ao
navegador ou aplicativo do usuário.

## Ordem recomendada para o primeiro uso

Em um banco novo ainda não existe usuário para realizar login. A sequência é:

```text
1. POST /oauth2/token
        │ token service para identity-api
        ▼
2. POST /v1/users
        │ usuário criado
        ▼
3. POST /v1/auth/login
        │ access token user + refresh token
        ▼
4. usar a sessão na ponta
        │
        ├── POST /v1/auth/refresh  quando precisar renovar
        └── POST /v1/auth/revoke   no logout
```

Os endpoints de discovery podem ser consultados a qualquer momento. Eles não
precisam ser chamados antes de cada login ou de cada validação.

## Passo a passo no Swagger

### 1. Obter um token de serviço

Abra `POST /oauth2/token`, clique em **Try it out** e informe os campos como
`application/x-www-form-urlencoded`:

```text
grant_type=client_credentials
client_id=web-bff
client_secret=SEU_SEGREDO
audience=identity-api
scope=identity.users.create identity.users.read identity.users.write identity.auth.login identity.auth.refresh identity.auth.revoke
```

Copie o `access_token` da resposta. O Identity só emite os scopes e a audience
que estiverem permitidos para esse client.

### 2. Autorizar os endpoints protegidos

Clique em **Authorize** e cole somente o JWT, sem escrever `Bearer`. Depois
disso, o Swagger enviará:

```http
Authorization: Bearer <token-service>
```

### 3. Criar o usuário

Execute `POST /v1/users` com nome, e-mail e senha. Guarde o `id` retornado;
ele será usado nas operações de consulta, atualização e desativação.

### 4. Realizar o login

Execute `POST /v1/auth/login` com o mesmo e-mail e senha do usuário. O
token de serviço autorizado no Swagger identifica o client solicitante.

### 5. Renovar ou encerrar a sessão

Use o `refreshToken` retornado no login em `/v1/auth/refresh`. Quando houver
logout, envie o refresh token atual para `/v1/auth/revoke`.

## Detalhamento dos endpoints

### `POST /oauth2/token`

Emite um JWT de aplicação com `token_use=service`. É usado para chamadas entre
BFF, Gateway, APIs e workers.

O body é `application/x-www-form-urlencoded`, não JSON:

```http
POST /oauth2/token
Content-Type: application/x-www-form-urlencoded

grant_type=client_credentials&client_id=web-bff&client_secret=SEU_SEGREDO&audience=identity-api&scope=identity.auth.login
```

Resposta `200 OK`:

```json
{
  "access_token": "eyJ...",
  "token_type": "Bearer",
  "expires_in": 300,
  "scope": "identity.auth.login"
}
```

A resposta também envia `Cache-Control: no-store` e `Pragma: no-cache`, para
impedir que intermediários armazenem o token.

Regras importantes:

- `grant_type` deve ser `client_credentials`;
- `audience` identifica a API destinatária;
- `scope` aceita um ou mais valores separados por espaço;
- o client precisa ter permissão para todas as audiences e scopes pedidos;
- o chamador deve manter o token em cache até próximo do vencimento, em vez de
  solicitar outro a cada requisição.

Para acessar `/v1/users`, a audience obrigatória é `identity-api`.

### Gerenciamento de service clients

Esses endpoints usam audience `identity-api`. Eles devem ser chamados por um
client administrativo com o scope indicado, nunca por navegador ou aplicativo
externo.

#### `POST /v1/service-clients`

Cria as credenciais de uma API, worker, BFF ou Gateway. O segredo é gerado pelo
Identity e retornado somente nesta resposta:

```json
{
  "clientId": "orders-api",
  "displayName": "Orders API",
  "canIssueUserTokens": false,
  "allowedAudiences": ["payments-api"],
  "allowedScopes": ["payments.write"]
}
```

A resposta `201 Created` contém `client` e `clientSecret`. Copie imediatamente
o segredo para o secret manager. O endpoint de consulta nunca o mostrará.

#### `GET /v1/service-clients/{clientId}`

Retorna nome, estado, audiences e scopes. Não retorna `client_secret` nem o hash
persistido. Exige `identity.clients.read`.

#### `PUT /v1/service-clients/{clientId}/permissions`

Substitui integralmente as audiences e scopes atuais. Valores omitidos deixam
de ser permitidos, portanto envie o conjunto completo desejado. Exige
`identity.clients.write`.

#### `POST /v1/service-clients/{clientId}/secret/rotate`

Gera um novo segredo, invalida o anterior imediatamente e devolve o novo valor
uma única vez. Exige `identity.clients.write`. Faça a distribuição pelo secret
manager antes de reiniciar o consumidor.

#### `DELETE /v1/service-clients/{clientId}`

Desativa logicamente o client, bloqueando novas emissões de token. O chamador
não pode desativar a própria credencial. Exige `identity.clients.write`.

### `GET /v1/audit-events`

Retorna os eventos de provisionamento e gerenciamento de service clients em
páginas ordenadas por `occurredAtUtc DESC, id DESC`. Usa o padrão de paginação
por cursor descrito em [pagination.md](pagination.md).

```http
GET /v1/audit-events?subjectType=service_client&subjectId=orders-api&limit=50
Authorization: Bearer <token-service>
```

Parâmetros disponíveis:

| Parâmetro | Regra |
|---|---|
| `eventType` | igualdade exata; máximo de 100 caracteres |
| `actorClientId` | igualdade exata; máximo de 100 caracteres |
| `subjectType` | igualdade exata; máximo de 50 caracteres |
| `subjectId` | igualdade exata; máximo de 254 caracteres |
| `occurredFromUtc` | início inclusivo do período, obrigatoriamente UTC |
| `occurredToUtc` | fim inclusivo do período, obrigatoriamente UTC |
| `limit` | tamanho da página; padrão 50, mínimo 1 e máximo 200 |
| `after` | cursor opaco retornado em `nextCursor` pela página anterior |

Resposta:

```json
{
  "items": [
    {
      "id": "e8c1f16c-d657-44ef-bbab-e4295c82ba50",
      "eventType": "service_client.created",
      "actorClientId": "web-bff",
      "subjectType": "service_client",
      "subjectId": "orders-api",
      "occurredAtUtc": "2026-10-17T12:00:00+00:00"
    }
  ],
  "nextCursor": "eyJ2ZXJzaW9uIjoxLCJ2YWx1ZSI6Li4ufQ"
}
```

Quando `nextCursor` não for `null`, envie o mesmo conjunto de filtros e use o
valor em `after`:

```http
GET /v1/audit-events?subjectType=service_client&subjectId=orders-api&limit=50&after=<nextCursor>
Authorization: Bearer <token-service>
```

Filtros são combinados com `AND`. `occurredFromUtc` não pode ser posterior a
`occurredToUtc`; datas com offset diferente de zero e cursores inválidos
retornam `400 Bad Request` em Problem Details. O cursor é opaco, versionado e
não contém segredo, mas não deve ser interpretado nem fabricado pelo consumidor.

O endpoint exige `identity.audit.read`. Os eventos não contêm segredo, hash ou
JWT.

### `POST /v1/users`

Cadastra um usuário. Não é um endpoint público de auto cadastro: somente uma
aplicação autorizada com token `service` e scope `identity.users.create` pode
chamá-lo.

```http
POST /v1/users
Authorization: Bearer <token-service>
Content-Type: application/json

{
  "name": "Maria Silva",
  "email": "maria@example.com",
  "password": "Senha123!"
}
```

Resposta `201 Created`:

```json
{
  "id": "47de4c37-34ab-4084-a293-e46229c34aad",
  "name": "Maria Silva",
  "email": "maria@example.com",
  "role": "User",
  "isActive": true,
  "createdAtUtc": "2026-10-03T12:00:00+00:00"
}
```

A senha deve ter pelo menos oito caracteres e conter letra, número e caractere
especial. O e-mail deve ser único.

### `GET /v1/users/{id}`

Consulta um usuário pelo `Guid`. Exige token `service`, audience `identity-api`
e scope `identity.users.read`.

```http
GET /v1/users/47de4c37-34ab-4084-a293-e46229c34aad
Authorization: Bearer <token-service>
```

Retorna `200 OK` com `UserResponse` ou `404 Not Found` quando o ID não existe.

### `PUT /v1/users/{id}`

Altera somente nome e e-mail. Não troca senha, role ou estado do usuário. Exige
token `service`, audience `identity-api` e scope `identity.users.write`.

```http
PUT /v1/users/47de4c37-34ab-4084-a293-e46229c34aad
Authorization: Bearer <token-service>
Content-Type: application/json

{
  "name": "Maria Souza",
  "email": "maria.souza@example.com"
}
```

Retorna `200 OK` com o usuário atualizado, `404 Not Found` para ID inexistente
ou `409 Conflict` quando o novo e-mail já está em uso.

### `DELETE /v1/users/{id}`

Desativa logicamente o usuário; nenhum registro é removido do banco. Exige
token `service`, audience `identity-api` e scope `identity.users.write`.

```http
DELETE /v1/users/47de4c37-34ab-4084-a293-e46229c34aad
Authorization: Bearer <token-service>
```

Retorna `204 No Content`. Um usuário desativado não consegue fazer login nem
renovar a sessão. Uma segunda tentativa retorna `409 Conflict`.

### `POST /v1/auth/login`

Valida o client identificado pelo token de serviço, o e-mail, a senha e o estado
do usuário. Deve ser chamado pelo BFF ou Gateway; o cliente externo não chama o
Identity diretamente. Exige audience `identity-api` e scope
`identity.auth.login`.

```http
POST /v1/auth/login
Authorization: Bearer <token-service>
Content-Type: application/json

{
  "email": "maria@example.com",
  "password": "Senha123!"
}
```

Resposta `200 OK`:

```json
{
  "accessToken": "eyJ...",
  "tokenType": "Bearer",
  "expiresIn": 900,
  "refreshToken": "token-opaco...",
  "user": {
    "id": "47de4c37-34ab-4084-a293-e46229c34aad",
    "name": "Maria Silva",
    "email": "maria@example.com",
    "role": "User",
    "isActive": true,
    "createdAtUtc": "2026-10-03T12:00:00+00:00"
  }
}
```

O access token emitido possui `token_use=user`, `sub` igual ao ID do usuário e
audience igual ao `client_id` solicitante. Ele é destinado à ponta que realizou
o login e não autoriza `/v1/users`.

### `POST /v1/auth/refresh`

Renova a sessão sem pedir a senha novamente. Exige token de serviço do mesmo
client vinculado ao refresh token e scope `identity.auth.refresh`.

```http
POST /v1/auth/refresh
Authorization: Bearer <token-service>
Content-Type: application/json

{
  "refreshToken": "token-opaco-atual..."
}
```

Retorna `200 OK` no mesmo formato do login. O refresh token é rotacionado: a
resposta contém um novo valor e o anterior deixa de ser válido. O chamador deve
substituir os dois tokens armazenados de forma atômica.

### `POST /v1/auth/revoke`

Revoga o refresh token no logout. Exige token de serviço do mesmo client
vinculado ao refresh token e scope `identity.auth.revoke`.

```http
POST /v1/auth/revoke
Authorization: Bearer <token-service>
Content-Type: application/json

{
  "refreshToken": "token-opaco-atual..."
}
```

Retorna `204 No Content`. O access token já emitido continua válido até expirar;
o endpoint impede novas renovações com aquele refresh token.

### `GET /.well-known/openid-configuration`

Publica metadados para bibliotecas e componentes de infraestrutura descobrirem
o emissor, o endpoint de token, o endereço do JWKS e os algoritmos suportados.

```http
GET /.well-known/openid-configuration
```

É anônimo para permitir o bootstrap da validação de JWT, mas deve ficar acessível
somente na rede interna. Normalmente é consultado e mantido em cache pela
biblioteca de autenticação, não pelo código de negócio.

### `GET /.well-known/jwks.json`

Publica a chave pública RSA usada para validar a assinatura dos JWTs. A chave
privada nunca é exposta.

```http
GET /.well-known/jwks.json
```

As APIs consumidoras usam esse documento para validar tokens localmente. Elas
não precisam chamar o Identity a cada requisição e devem respeitar o cache e a
rotação das chaves. O endpoint é anônimo para evitar dependência circular na
validação, mas deve ficar restrito à rede interna.

## Fluxos que não devem ser misturados

### Administração de usuários

```text
client credentials → token service → /v1/users
```

### Sessão do usuário

```text
login → access token user + refresh token → refresh ou revoke
```

Um token `user` não substitui o token `service` nos endpoints protegidos. Da
mesma forma, um token `service` não representa um usuário logado.

## Respostas de erro mais comuns

| Status | Significado típico |
|---|---|
| `400 Bad Request` | contrato inválido, senha fora da regra ou `grant_type` inválido |
| `401 Unauthorized` | credencial, JWT ou refresh token inválido/expirado |
| `403 Forbidden` | client, audience ou scope sem permissão; usuário inativo |
| `404 Not Found` | usuário não encontrado |
| `409 Conflict` | e-mail duplicado ou usuário já desativado |

Os erros são retornados como `application/problem+json`. Não registre
`client_secret`, senhas, access tokens ou refresh tokens em logs.
