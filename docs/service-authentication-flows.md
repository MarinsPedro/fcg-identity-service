# Fluxos de autenticação entre aplicações

Este documento explica como os tokens de serviço são usados em dois cenários:

1. o BFF chama o Catalog em nome do sistema;
2. o Orders chama o Promotions diretamente, sem passar pelo BFF.

Nos dois casos, somente o Identity emite tokens. Catalog, Orders e Promotions
validam os JWTs localmente usando a chave pública publicada pelo Identity.

## Componentes do fluxo

| Recurso do Identity | Finalidade | Consumidor típico |
|---|---|---|
| `POST /oauth2/token` | autenticar uma aplicação e emitir um token de serviço | BFF, APIs e workers |
| `GET /.well-known/openid-configuration` | informar issuer, token endpoint e endereço do JWKS | middleware JWT das aplicações |
| `GET /.well-known/jwks.json` | publicar as chaves públicas usadas para verificar assinaturas | middleware JWT das APIs |

O `client_secret` autentica a aplicação perante o Identity. A chave privada RSA
assina o JWT, e a chave pública do JWKS permite que a API de destino confirme
que o token foi emitido pelo Identity.

Discovery e JWKS não exigem JWT para evitar dependência circular durante o
bootstrap da validação, mas devem ser acessíveis somente na rede interna.

```text
client_id + client_secret  → comprovam quem solicita o token
chave privada do Identity  → assina o token
JWKS                       → permite verificar a assinatura
```

## Fluxo 1: BFF para Catalog

### Contexto

O frontend possui um token de usuário ou uma sessão válida no BFF. O BFF valida
o usuário na borda. Para acessar o Catalog, ele não encaminha o token do usuário:
usa um token de serviço destinado ao `catalog-api`.

Exemplo de permissões do `web-bff` no Identity:

```json
{
  "clientId": "web-bff",
  "allowedAudiences": [
    "identity-api",
    "catalog-api"
  ],
  "allowedScopes": [
    "identity.users.read",
    "catalog.games.read"
  ]
}
```

Essas permissões fazem parte da configuração inicial atual do `web-bff`.

### Primeira chamada

```text
Frontend                   BFF                    Identity              Catalog
   │                        │                        │                      │
   │ GET /api/games         │                        │                      │
   ├───────────────────────►│                        │                      │
   │                        │ valida usuário         │                      │
   │                        │                        │                      │
   │                        │ POST /oauth2/token     │                      │
   │                        ├───────────────────────►│                      │
   │                        │ token de serviço       │                      │
   │                        │◄───────────────────────┤                      │
   │                        │                                               │
   │                        │ GET /internal/v1/games                        │
   │                        │ Bearer <service-token>                        │
   │                        ├──────────────────────────────────────────────►│
   │                        │                                               │
   │                        │       GET openid-configuration                │
   │                        │                        │◄─────────────────────┤
   │                        │                        ├─────────────────────►│
   │                        │                                               │
   │                        │       GET jwks.json                            │
   │                        │                        │◄─────────────────────┤
   │                        │                        ├─────────────────────►│
   │                        │                                               │
   │                        │                  Catalog valida o JWT          │
   │                        │                                               │
   │                        │ lista de jogos                                │
   │                        │◄──────────────────────────────────────────────┤
   │ lista de jogos         │                                               │
   │◄───────────────────────┤                                               │
```

As chamadas a `openid-configuration` e `jwks.json` são feitas pelo middleware
JWT do Catalog, geralmente durante a primeira requisição autenticada. O
Controller recebe somente requisições cujo token já foi validado.

### Solicitação do token pelo BFF

```http
POST http://localhost:5081/oauth2/token
Content-Type: application/x-www-form-urlencoded

grant_type=client_credentials
&client_id=web-bff
&client_secret=SEGREDO_DO_WEB_BFF
&audience=catalog-api
&scope=catalog.games.read
```

Exemplo com `curl`:

```bash
curl --request POST http://localhost:5081/oauth2/token \
  --header "Content-Type: application/x-www-form-urlencoded" \
  --data-urlencode "grant_type=client_credentials" \
  --data-urlencode "client_id=web-bff" \
  --data-urlencode "client_secret=SEGREDO_DO_WEB_BFF" \
  --data-urlencode "audience=catalog-api" \
  --data-urlencode "scope=catalog.games.read"
```

Claims esperadas no token:

```json
{
  "sub": "web-bff",
  "client_id": "web-bff",
  "token_use": "service",
  "scope": [
    "catalog.games.read"
  ],
  "iss": "fiap-cloud-games-identity",
  "aud": "catalog-api"
}
```

### Chamada do BFF para o Catalog

```http
GET http://localhost:5082/internal/v1/games
Authorization: Bearer TOKEN_DE_SERVICO
```

```bash
curl http://localhost:5082/internal/v1/games \
  --header "Authorization: Bearer TOKEN_DE_SERVICO"
```

O Catalog valida:

- assinatura usando a chave pública do JWKS;
- `iss=fiap-cloud-games-identity`;
- `aud=catalog-api`;
- `token_use=service`;
- validade temporal (`nbf` e `exp`);
- presença de `catalog.games.read`.

Para listar jogos, o ID do usuário não é necessário. Quando uma operação tiver
contexto de usuário, o BFF extrai o ID do token de usuário já validado e o envia
explicitamente na rota, query string ou corpo do contrato. O token interno
continua identificando apenas o `web-bff`.

## Fluxo 2: Orders para Promotions

### Contexto

Uma comunicação interna não precisa passar pelo BFF ou API Gateway. O Orders
possui `client_id` e segredo próprios, solicita um token para o Promotions e
chama diretamente a API interna para calcular o desconto de uma compra.

Exemplo de configuração do `orders-api` no Identity:

```json
{
  "clientId": "orders-api",
  "allowedAudiences": [
    "catalog-api",
    "promotions-api",
    "library-api"
  ],
  "allowedScopes": [
    "catalog.games.read",
    "promotions.calculate",
    "promotions.campaigns.read",
    "library.ownership.read"
  ]
}
```

### Primeira chamada

```text
Orders                           Identity                       Promotions
  │                                 │                                │
  │ POST /oauth2/token              │                                │
  ├────────────────────────────────►│                                │
  │ audience=promotions-api         │                                │
  │ scope=promotions.calculate      │                                │
  │                                 │                                │
  │ token de serviço                │                                │
  │◄────────────────────────────────┤                                │
  │                                                                  │
  │ POST /internal/v1/promotions/calculate                           │
  │ Bearer <service-token>                                          │
  ├─────────────────────────────────────────────────────────────────►│
  │                                                                  │
  │                     GET openid-configuration                     │
  │                                 │◄───────────────────────────────┤
  │                                 ├───────────────────────────────►│
  │                                                                  │
  │                     GET jwks.json                                │
  │                                 │◄───────────────────────────────┤
  │                                 ├───────────────────────────────►│
  │                                                                  │
  │                                    Promotions valida o JWT        │
  │                                                                  │
  │ preço final calculado                                            │
  │◄─────────────────────────────────────────────────────────────────┤
```

O Promotions consome Discovery e JWKS pelo mesmo mecanismo usado pelo Catalog.
Não existe troca de segredo entre Orders e Promotions. O segredo do Orders é
apresentado somente ao Identity.

### Solicitação do token pelo Orders

```http
POST http://localhost:5081/oauth2/token
Content-Type: application/x-www-form-urlencoded

grant_type=client_credentials
&client_id=orders-api
&client_secret=SEGREDO_DO_ORDERS
&audience=promotions-api
&scope=promotions.calculate
```

```bash
curl --request POST http://localhost:5081/oauth2/token \
  --header "Content-Type: application/x-www-form-urlencoded" \
  --data-urlencode "grant_type=client_credentials" \
  --data-urlencode "client_id=orders-api" \
  --data-urlencode "client_secret=SEGREDO_DO_ORDERS" \
  --data-urlencode "audience=promotions-api" \
  --data-urlencode "scope=promotions.calculate"
```

Claims esperadas:

```json
{
  "sub": "orders-api",
  "client_id": "orders-api",
  "token_use": "service",
  "scope": [
    "promotions.calculate"
  ],
  "iss": "fiap-cloud-games-identity",
  "aud": "promotions-api"
}
```

### Chamada do Orders para o Promotions

```http
POST http://localhost:5083/internal/v1/promotions/calculate
Authorization: Bearer TOKEN_DE_SERVICO
Content-Type: application/json
```

```json
{
  "gameId": "80fdc742-e7c1-4564-a269-8d52784bec44",
  "basePrice": 99.90,
  "promotionId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

```bash
curl --request POST http://localhost:5083/internal/v1/promotions/calculate \
  --header "Authorization: Bearer TOKEN_DE_SERVICO" \
  --header "Content-Type: application/json" \
  --data '{
    "gameId": "80fdc742-e7c1-4564-a269-8d52784bec44",
    "basePrice": 99.90,
    "promotionId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
  }'
```

O token identifica a aplicação chamadora (`orders-api`). `gameId`, `basePrice`
e `promotionId` são dados do negócio e aparecem explicitamente no corpo. O
Promotions não interpreta o `sub` do token de serviço como ID de usuário.

O Promotions valida:

- assinatura usando o JWKS;
- `iss=fiap-cloud-games-identity`;
- `aud=promotions-api`;
- `token_use=service`;
- validade temporal;
- presença de `promotions.calculate`;
- regras de elegibilidade e vigência da promoção.

## Configuração das APIs consumidoras

Catalog e Promotions podem usar a mesma configuração de autenticação, mudando
somente a audiência:

```csharp
builder.Services
    .AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.Authority = "http://localhost:5081";
        options.Audience = "catalog-api"; // ou promotions-api
        options.RequireHttpsMetadata = false; // somente em Development
    });
```

```csharp
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
```

Com `Authority`, o middleware deduz o endereço:

```text
http://localhost:5081/.well-known/openid-configuration
```

Depois lê `jwks_uri`, baixa as chaves públicas e mantém os metadados em cache.
O Controller não chama Discovery manualmente.

## Cache

Existem dois caches independentes:

```text
Aplicação chamadora
└─ guarda o access token até pouco antes de expirar

API de destino
└─ guarda os metadados de Discovery e as chaves públicas do JWKS
```

O BFF mantém um token para `catalog-api`. O Orders mantém outro token para
`promotions-api`. Um token nunca deve ser reutilizado em uma audiência diferente.

Na segunda chamada, o fluxo normal é:

```text
Orders ── token em cache ──► Promotions
Promotions ── JWKS em cache ──► valida localmente
```

O Identity volta a ser consultado quando:

- o token da aplicação chamadora está próximo da expiração;
- os metadados do middleware precisam ser atualizados;
- um JWT apresenta um `kid` ainda desconhecido;
- ocorreu rotação da chave de assinatura.

## Respostas de segurança

| Situação | Resposta esperada |
|---|---|
| token ausente, expirado, adulterado ou com audience incorreta | `401 Unauthorized` |
| token válido, mas sem o scope exigido | `403 Forbidden` |
| token e scope válidos, mas regra de negócio recusada | erro de negócio apropriado, como `409 Conflict` |

## Regras importantes

- somente o Identity emite tokens;
- cada BFF, API e worker possui `client_id` e segredo próprios;
- o segredo é enviado somente ao Identity, nunca à API de destino;
- tokens de serviço identificam aplicações, não usuários;
- contexto de usuário necessário ao negócio vai na rota, query string ou corpo;
- o token de usuário permanece na borda;
- access tokens e chaves públicas são cacheados;
- senhas, client secrets, JWTs e tokens de pagamento nunca devem ser registrados
  em logs;
- em produção, todos os endpoints usam HTTPS e segredos ficam em um secret
  manager.
