# Configuração do Identity

As configurações seguem a hierarquia padrão do ASP.NET Core. Em produção, use variáveis de ambiente e um gerenciador de segredos.

## Banco de dados

```text
ConnectionStrings__Database=Host=localhost;Port=5432;Database=fcg_identity;Username=fcg_identity;Password=...
```

O Identity é o único proprietário do schema `identity`. Nenhum outro serviço deve acessar diretamente suas tabelas.

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

## Aplicações clientes iniciais

Os clientes iniciais são carregados pela seção `BootstrapClients` somente para preparar ambientes novos. Exemplo em variáveis de ambiente:

```text
Identity__BootstrapClients__0__ClientId=fcg-bff
Identity__BootstrapClients__0__Secret=um-segredo-forte
Identity__BootstrapClients__0__DisplayName=FCG BFF
Identity__BootstrapClients__0__CanIssueUserTokens=true
Identity__BootstrapClients__0__AllowedAudiences__0=fcg-bff
Identity__BootstrapClients__0__AllowedAudiences__1=identity-api
Identity__BootstrapClients__0__AllowedScopes__0=store.access
Identity__BootstrapClients__0__AllowedScopes__1=identity.users.read

Identity__BootstrapClients__1__ClientId=orders-api
Identity__BootstrapClients__1__Secret=outro-segredo-forte
Identity__BootstrapClients__1__DisplayName=Orders API
Identity__BootstrapClients__1__CanIssueUserTokens=false
Identity__BootstrapClients__1__AllowedAudiences__0=payments-api
Identity__BootstrapClients__1__AllowedScopes__0=payments.create
```

Regras:

- `client_id` é único por aplicação/worker;
- o segredo nunca é compartilhado entre clientes;
- `CanIssueUserTokens=true` fica restrito às pontas autorizadas, como BFF e Gateway;
- audiência e escopos seguem privilégio mínimo;
- o valor literal `CHANGE_ME` é rejeitado;
- os segredos são persistidos somente como hash PBKDF2.

Após o bootstrap, remova os segredos da configuração do processo. A administração e rotação de clientes por uma API administrativa auditada pode ser adicionada em uma fase posterior.

## Validação nas APIs consumidoras

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
- exponha o Identity somente pelas rotas previstas na rede;
- habilite TLS;
- limite tentativas de login na borda;
- configure métricas e alertas sem dados sensíveis;
- conceda ao workflow acesso de leitura ao pacote privado Building Blocks.
