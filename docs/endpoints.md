# Endpoints e ordem de uso do Identity

Este documento explica a finalidade de cada endpoint do Identity, quem deve
chamá-lo e a sequência recomendada para os fluxos de cadastro, login, renovação
e logout.

## Visão rápida

| Endpoint | Quem chama | Credencial exigida | Finalidade |
|---|---|---|---|
| `POST /connect/token` | BFF, Gateway, API ou worker | `client_id` e `client_secret` | emitir token técnico `service` |
| `POST /internal/users` | aplicação interna autorizada | token `service` + `identity.users.create` | cadastrar usuário |
| `GET /internal/users/{id}` | aplicação interna autorizada | token `service` + `identity.users.read` | consultar usuário |
| `PUT /internal/users/{id}` | aplicação interna autorizada | token `service` + `identity.users.write` | alterar nome e e-mail |
| `DELETE /internal/users/{id}` | aplicação interna autorizada | token `service` + `identity.users.write` | desativar usuário |
| `POST /internal/clients` | `web-bff` ou client administrador | token `service` + `identity.clients.create` | criar client e obter o segredo uma vez |
| `GET /internal/clients/{clientId}` | client administrador | token `service` + `identity.clients.read` | consultar dados públicos do client |
| `PUT /internal/clients/{clientId}/permissions` | client administrador | token `service` + `identity.clients.write` | substituir audiences e scopes |
| `POST /internal/clients/{clientId}/rotate-secret` | client administrador | token `service` + `identity.clients.write` | rotacionar o segredo |
| `DELETE /internal/clients/{clientId}` | client administrador | token `service` + `identity.clients.write` | desativar client |
| `GET /internal/audit-events` | client auditor | token `service` + `identity.audit.read` | consultar eventos administrativos |
| `POST /api/auth/login` | BFF ou Gateway | credenciais da aplicação e do usuário | iniciar sessão do usuário |
| `POST /api/auth/refresh` | BFF ou Gateway | credenciais da aplicação + refresh token | renovar a sessão |
| `POST /api/auth/revoke` | BFF ou Gateway | credenciais da aplicação + refresh token | encerrar a sessão |
| `GET /.well-known/openid-configuration` | bibliotecas e infraestrutura | nenhuma | descobrir os metadados do emissor |
| `GET /.well-known/jwks.json` | APIs que validam JWT | nenhuma | obter a chave pública de assinatura |
| `GET /health/live` | infraestrutura | nenhuma | verificar se o processo está vivo |
| `GET /health/ready` | infraestrutura | nenhuma | verificar se API e banco estão prontos |

Os endpoints de autenticação e discovery possuem `AllowAnonymous`. Isso quer
dizer que não recebem um JWT no header `Authorization`; eles validam as
credenciais enviadas em seu próprio contrato. O ícone de cadeado pode aparecer
no Swagger porque o esquema Bearer é registrado globalmente, mas o servidor não
exige Bearer nesses endpoints.

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
1. POST /connect/token
        │ token service para identity-api
        ▼
2. POST /internal/users
        │ usuário criado
        ▼
3. POST /api/auth/login
        │ access token user + refresh token
        ▼
4. usar a sessão na ponta
        │
        ├── POST /api/auth/refresh  quando precisar renovar
        └── POST /api/auth/revoke   no logout
```

Os endpoints de discovery podem ser consultados a qualquer momento. Eles não
precisam ser chamados antes de cada login ou de cada validação.

## Passo a passo no Swagger

### 1. Obter um token de serviço

Abra `POST /connect/token`, clique em **Try it out** e informe os campos como
`application/x-www-form-urlencoded`:

```text
grant_type=client_credentials
client_id=web-bff
client_secret=SEU_SEGREDO
audience=identity-api
scope=identity.users.create identity.users.read identity.users.write
```

Copie o `access_token` da resposta. O Identity só emite os scopes e a audience
que estiverem permitidos para esse client.

### 2. Autorizar os endpoints internos

Clique em **Authorize** e cole somente o JWT, sem escrever `Bearer`. Depois
disso, o Swagger enviará:

```http
Authorization: Bearer <token-service>
```

### 3. Criar o usuário

Execute `POST /internal/users` com nome, e-mail e senha. Guarde o `id` retornado;
ele será usado nas operações de consulta, atualização e desativação.

### 4. Realizar o login

Execute `POST /api/auth/login` com o mesmo e-mail e senha do usuário, além das
credenciais do client autorizado a emitir tokens de usuário.

### 5. Renovar ou encerrar a sessão

Use o `refreshToken` retornado no login em `/api/auth/refresh`. Quando houver
logout, envie o refresh token atual para `/api/auth/revoke`.

## Detalhamento dos endpoints

### `POST /connect/token`

Emite um JWT de aplicação com `token_use=service`. É usado para chamadas entre
BFF, Gateway, APIs e workers.

O body é `application/x-www-form-urlencoded`, não JSON:

```http
POST /connect/token
Content-Type: application/x-www-form-urlencoded

grant_type=client_credentials&client_id=web-bff&client_secret=SEU_SEGREDO&audience=identity-api&scope=identity.users.create
```

Resposta `200 OK`:

```json
{
  "access_token": "eyJ...",
  "token_type": "Bearer",
  "expires_in": 300,
  "scope": "identity.users.create"
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

Para acessar `/internal/users`, a audience obrigatória é `identity-api`.

### Gerenciamento de service clients

Esses endpoints usam audience `identity-api`. Eles devem ser chamados por um
client administrativo com o scope indicado, nunca por navegador ou aplicativo
externo.

#### `POST /internal/clients`

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

#### `GET /internal/clients/{clientId}`

Retorna nome, estado, audiences e scopes. Não retorna `client_secret` nem o hash
persistido. Exige `identity.clients.read`.

#### `PUT /internal/clients/{clientId}/permissions`

Substitui integralmente as audiences e scopes atuais. Valores omitidos deixam
de ser permitidos, portanto envie o conjunto completo desejado. Exige
`identity.clients.write`.

#### `POST /internal/clients/{clientId}/rotate-secret`

Gera um novo segredo, invalida o anterior imediatamente e devolve o novo valor
uma única vez. Exige `identity.clients.write`. Faça a distribuição pelo secret
manager antes de reiniciar o consumidor.

#### `DELETE /internal/clients/{clientId}`

Desativa logicamente o client, bloqueando novas emissões de token. O chamador
não pode desativar a própria credencial. Exige `identity.clients.write`.

### `GET /internal/audit-events`

Retorna os eventos mais recentes de provisionamento e gerenciamento de service
clients. O parâmetro `take` aceita de 1 a 200 e usa 100 quando omitido.

```http
GET /internal/audit-events?take=50
Authorization: Bearer <token-service>
```

Exige `identity.audit.read`. Os eventos não contêm segredo, hash ou JWT.

### `POST /internal/users`

Cadastra um usuário. Não é um endpoint público de auto cadastro: somente uma
aplicação interna com token `service` e scope `identity.users.create` pode
chamá-lo.

```http
POST /internal/users
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

### `GET /internal/users/{id}`

Consulta um usuário pelo `Guid`. Exige token `service`, audience `identity-api`
e scope `identity.users.read`.

```http
GET /internal/users/47de4c37-34ab-4084-a293-e46229c34aad
Authorization: Bearer <token-service>
```

Retorna `200 OK` com `UserResponse` ou `404 Not Found` quando o ID não existe.

### `PUT /internal/users/{id}`

Altera somente nome e e-mail. Não troca senha, role ou estado do usuário. Exige
token `service`, audience `identity-api` e scope `identity.users.write`.

```http
PUT /internal/users/47de4c37-34ab-4084-a293-e46229c34aad
Authorization: Bearer <token-service>
Content-Type: application/json

{
  "name": "Maria Souza",
  "email": "maria.souza@example.com"
}
```

Retorna `200 OK` com o usuário atualizado, `404 Not Found` para ID inexistente
ou `409 Conflict` quando o novo e-mail já está em uso.

### `DELETE /internal/users/{id}`

Desativa logicamente o usuário; nenhum registro é removido do banco. Exige
token `service`, audience `identity-api` e scope `identity.users.write`.

```http
DELETE /internal/users/47de4c37-34ab-4084-a293-e46229c34aad
Authorization: Bearer <token-service>
```

Retorna `204 No Content`. Um usuário desativado não consegue fazer login nem
renovar a sessão. Uma segunda tentativa retorna `409 Conflict`.

### `POST /api/auth/login`

Valida a aplicação, o e-mail, a senha e o estado do usuário. Deve ser chamado
pelo BFF ou Gateway; o cliente externo não deve conhecer o `client_secret` nem
chamar o Identity diretamente.

```http
POST /api/auth/login
Content-Type: application/json

{
  "email": "maria@example.com",
  "password": "Senha123!",
  "clientId": "web-bff",
  "clientSecret": "SEU_SEGREDO"
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
o login e não autoriza `/internal/users`.

### `POST /api/auth/refresh`

Renova a sessão sem pedir a senha novamente.

```http
POST /api/auth/refresh
Content-Type: application/json

{
  "refreshToken": "token-opaco-atual...",
  "clientId": "web-bff",
  "clientSecret": "SEU_SEGREDO"
}
```

Retorna `200 OK` no mesmo formato do login. O refresh token é rotacionado: a
resposta contém um novo valor e o anterior deixa de ser válido. O chamador deve
substituir os dois tokens armazenados de forma atômica.

### `POST /api/auth/revoke`

Revoga o refresh token no logout.

```http
POST /api/auth/revoke
Content-Type: application/json

{
  "refreshToken": "token-opaco-atual...",
  "clientId": "web-bff",
  "clientSecret": "SEU_SEGREDO"
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

É público e normalmente é consultado e mantido em cache pela biblioteca de
autenticação, não pelo código de negócio.

### `GET /.well-known/jwks.json`

Publica a chave pública RSA usada para validar a assinatura dos JWTs. A chave
privada nunca é exposta.

```http
GET /.well-known/jwks.json
```

As APIs consumidoras usam esse documento para validar tokens localmente. Elas
não precisam chamar o Identity a cada requisição e devem respeitar o cache e a
rotação das chaves.

## Fluxos que não devem ser misturados

### Administração de usuários

```text
client credentials → token service → /internal/users
```

### Sessão do usuário

```text
login → access token user + refresh token → refresh ou revoke
```

Um token `user` não substitui o token `service` nos endpoints internos. Da
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
