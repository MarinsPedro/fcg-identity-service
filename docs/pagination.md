# Paginação por cursor

Este é o padrão do Identity para endpoints que percorrem coleções mutáveis ou
que podem crescer continuamente. Ele usa keyset pagination e não usa número de
página, `offset` ou contagem total.

## Contrato HTTP

Toda consulta paginada usa os mesmos nomes:

| Campo | Significado |
|---|---|
| `limit` | quantidade máxima de itens; padrão 50, mínimo 1 e máximo 200 |
| `after` | cursor opaco retornado pela página anterior |
| `items` | itens da página atual |
| `nextCursor` | posição para continuar; `null` quando não há próxima página |

```http
GET /v1/audit-events?limit=50&after=<cursor>
```

```json
{
  "items": [],
  "nextCursor": null
}
```

Um cursor inválido, vazio, incompatível ou de versão desconhecida retorna
`400 Bad Request` no formato Problem Details. O token é Base64 URL-safe para
transporte, mas seu conteúdo é um detalhe interno: consumidores devem apenas
reutilizar o valor recebido.

O cursor não é uma credencial nem substitui autorização. Todo endpoint continua
protegido por sua policy e seu scope.

## Regras da consulta

- a ordenação deve ser total, determinística e imutável durante a navegação;
- campos não únicos precisam de um desempate único, normalmente o `Id`;
- o cursor contém todos os campos usados na ordenação;
- a comparação é exclusiva: o item apontado pelo cursor não reaparece;
- o repository busca `limit + 1` itens para descobrir se existe outra página;
- somente `limit` itens são expostos;
- `nextCursor` é criado a partir do último item exposto e apenas quando existe
  o item excedente;
- filtros são combinados com `AND` e devem permanecer iguais entre as páginas;
- não se calcula `COUNT(*)`, pois o contrato não expõe total de itens ou páginas.

O `audit-events` usa:

```text
ORDER BY occurred_at_utc DESC, id DESC
```

e o cursor tipado contém `OccurredAtUtc` e `Id`. No PostgreSQL, a continuação é
uma comparação de tupla equivalente a:

```sql
WHERE (occurred_at_utc, id) < (@occurredAtUtc, @id)
```

## Responsabilidade por camada

### Presentation

- herda `CursorQueryRequest` para receber `limit` e `after`;
- adiciona somente os filtros HTTP específicos da feature;
- declara limites simples com Data Annotations para validação antecipada e
  documentação correta no OpenAPI;
- não informa `FromQuery(Name = ...)`: a convenção global publica nomes de query
  em camelCase e os atributos de validação derivam o nome da propriedade;
- usa o codec compartilhado para converter o token opaco em cursor tipado;
- devolve `CursorPageResponse<T>`;
- nunca envia Base64 ou JSON para a Application.

### Application

- recebe um Input com cursor tipado e filtros sem semântica HTTP;
- usa `QueryValidation` para normalização textual, UTC e intervalo de datas;
- usa `CursorPagination` para validar o limite, calcular `limit + 1` e montar a
  página;
- devolve `CursorPageResult<TItem, TCursor>`.

### Infrastructure

- aplica filtros antes da paginação;
- usa a mesma ordenação representada pelo cursor;
- mantém índice composto compatível com os filtros principais, a ordenação e o
  campo de desempate;
- não expõe `IQueryable` fora da Infrastructure.

## Como adicionar outro endpoint

1. Crie um Request da feature herdando `CursorQueryRequest`.
2. Defina um cursor tipado com todos os campos da ordenação.
3. Receba esse cursor no Input da Application.
4. Retorne `CursorPageResult<TItem, TCursor>` no caso de uso.
5. Converta o resultado em `CursorPageResponse<T>` na Presentation.
6. Crie o índice EF compatível e gere uma migration.
7. Teste validações na UnitTests, envelope e continuidade na IntegrationTests,
   além da tradução SQL e do índice na Database.IntegrationTests.

Não reutilize o cursor de outra feature apenas porque o formato atual coincide.
O tipo do cursor pertence à ordenação do recurso; o codec e o envelope HTTP são
as partes compartilhadas.
