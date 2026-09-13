# DD-1 — Registrar e consultar reserva

Status: implementação ainda não iniciada.

## Fontes e fluxo de trabalho

- Requisitos funcionais: [PRD-1](../PRDs/PRD-1.md).
- Este documento é o local para ajustar as decisões técnicas destas duas features antes de implementar.
- Novas features devem ter seu Design Doc preparado antes da implementação. Mudanças funcionais também devem ser refletidas no PRD correspondente.
- O projeto de estudos não é referência para este design.

## Escopo

Implementar somente registrar reserva (escrita) e consultar reserva por identificador (leitura).

## Projetos e referências

| Projeto | Responsabilidade | Referências de projeto |
| --- | --- | --- |
| PickupReservations.Domain | Agregado e invariantes | Nenhuma |
| PickupReservations.Application | Casos de uso e abstrações externas | Domain |
| PickupReservations.Infrastructure | EF Core, SQLite e implementação de abstrações | Application e Domain |
| PickupReservations.Contracts | Requests e responses HTTP | Nenhuma |
| PickupReservations.Api | Minimal API e Composition Root | Application, Contracts e Infrastructure apenas para composição |

EF Core e seu provedor SQLite permanecem na Infrastructure. A Application não recebe DbContext, IQueryable ou outros tipos de persistência. Versões de .NET e pacotes serão definidas antes da implementação, conforme o ambiente disponível.

## Domain

```text
Common/
  Entity.cs
  AggregateRoot.cs
  IDomainEvent.cs
  DomainException.cs
ReservationAggregate/
  Reservation.cs
  ReservationStatus.cs
```

- Reservation deriva de AggregateRoot e constitui o único agregado deste recorte.
- IDomainEvent é uma interface de domínio sem vínculo com bibliotecas de mensageria, necessária à base AggregateRoot.
- DomainException recebe uma mensagem no construtor.
- ReservationStatus é um enum: Open, Collected, Cancelled e Expired. Somente Open será atribuído neste recorte.
- Não haverá Domain Services, eventos concretos, tipos Result/DomainError nem pacote ErrorOr.
- Não serão introduzidos Value Objects para encapsular isoladamente cada campo neste recorte. Caso necessários em evolução posterior, deverão derivar da base ValueObject.

### Propriedades de Reservation

| Propriedade | Tipo | Definição |
| --- | --- | --- |
| Id | Guid | Herdado de Entity; gerado na criação |
| CustomerName | string | Nome obrigatório |
| ItemDescription | string | Descrição obrigatória de um único tipo de item |
| Quantity | int | Maior que zero |
| Status | ReservationStatus | Estado inicial Open |
| CreatedAt | DateTimeOffset | Instante de criação em UTC |
| ExpiresAt | DateTimeOffset | CreatedAt acrescido de 15 minutos |

As propriedades próprias da reserva terão setters privados; leituras públicas serão expostas para os casos de uso e projeções necessários. Id será herdado de Entity, com get e init públicos.

### Criação e invariantes

Reservation.Create(customerName, itemDescription, quantity, now) cria uma reserva válida ou lança DomainException antes de qualquer persistência.

1. Rejeitar nome e descrição nulos, vazios ou compostos somente por espaços.
2. Rejeitar quantidade menor ou igual a zero.
3. Gerar o identificador e registrar o instante recebido, normalizado para UTC.
4. Definir Status como Open e ExpiresAt como CreatedAt + 15 minutos.

Nome, descrição, quantidade, estado e prazo não terão operações públicas de edição neste recorte. Não serão inventados limites de caracteres ou quantidade máxima. O Domain não consultará o relógio diretamente.

## Application

```text
Common/Interfaces/
  IUnitOfWork.cs
Reservations/
  Interfaces/
    IReservationRepository.cs
    IReservationQueries.cs
  Models/
    ReservationDetails.cs
  CreateReservation/
    CreateReservationCommand.cs
    CreateReservationCommandHandler.cs
  GetReservationById/
    GetReservationByIdQuery.cs
    GetReservationByIdQueryHandler.cs
DependencyInjectionExtensions.cs
```
Commands, Queries e ReservationDetails serão records com entrada e saída explícitas para cada caso de uso. Os handlers terão HandleAsync e receberão CancellationToken. Inicialmente serão chamados diretamente pelos endpoints via injeção de dependência; não há necessidade de introduzir um mediador.

### Escrita

- CreateReservationCommand: CustomerName, ItemDescription e Quantity.
- Saída do handler: Task<Guid>, contendo o identificador criado, sem um tipo Result intermediário.
- CreateReservationCommandHandler: obter o instante atual, invocar Reservation.Create, adicionar a reserva pelo repositório, confirmar a unidade de trabalho e devolver o Id.
- IReservationRepository: adicionar uma Reservation à unidade de trabalho; não confirmar a transação dentro do repositório.
- IUnitOfWork: SaveChangesAsync(CancellationToken).

Proposta para o relógio: injetar TimeProvider, da biblioteca padrão do .NET, na Application e usar GetUtcNow. O Domain recebe apenas o instante. Isso substitui os tipos IClock/SystemClock do plano anterior e evita criar uma implementação pequena com um único uso. A adoção depende de escolher uma versão de .NET que ofereça TimeProvider.

### Leitura

- GetReservationByIdQuery: Id.
- ReservationDetails: modelo de leitura em Reservations/Models na Application, contendo Id, CustomerName, ItemDescription, Quantity, Status, CreatedAt e ExpiresAt.
- GetReservationByIdQueryHandler: consultar IReservationQueries e retornar Task<ReservationDetails?>; null representa reserva ausente.
- IReservationQueries: GetByIdAsync(Guid id, CancellationToken), retornando Task<ReservationDetails?>.

O estado pode usar ReservationStatus no resultado interno da Application. A borda HTTP o converte para o contrato externo. A leitura projeta os dados diretamente, sem exigir a materialização de Reservation e sem executar SaveChangesAsync.

DependencyInjectionExtensions expõe AddApplicationLayer e registra os handlers. Não haverá registro de DI no Domain, pois o recorte não possui serviços de domínio a registrar e ele deve permanecer livre de frameworks.

## Infrastructure e persistência

```te
Common/Persistence/
  ReservationsDbContext.cs
  Migrations/
Reservations/Persistence/
  ReservationConfiguration.cs
  ReservationRepository.cs
  ReservationQueries.cs
DependencyInjectionExtensions.cs
```

| Tipo | Responsabilidade |
| --- | --- |
| ReservationsDbContext | Contexto EF Core; implementa também IUnitOfWork |
| ReservationConfiguration | Mapeamento do agregado para a tabela Reservations |
| ReservationRepository | Implementar a adição do agregado ao contexto |
| ReservationQueries | Buscar por ID com AsNoTracking e projeção para o resultado da Application |
| DependencyInjectionExtensions | Expor AddInfrastructureLayer e registrar as implementações |

Implementações concretas serão internal por padrão; somente a extensão necessária à composição será pública. DbContext, repositório, consultas e unidade de trabalho terão escopo por requisição e compartilharão o mesmo contexto quando necessário.

### Modelo de armazenamento

- Tabela Reservations com Id como chave primária.
- Nome, descrição, quantidade, estado e datas obrigatórios.
- Status armazenado com representação definida no mapeamento, sem atributos de serialização ou ORM no Domain.
- Datas normalizadas para UTC; o mapeamento deve definir uma representação compatível com SQLite. Este recorte só consulta por ID e não depende de comparação de datas traduzida pelo provedor.
- A conversão e a leitura devem preservar o instante e o prazo de 15 minutos.
- Eventos internos do agregado não serão mapeados como dados persistidos.
- A string de conexão e o caminho do arquivo SQLite virão de configuração. Arquivos locais de banco não devem ser versionados.

A primeira migration criará o esquema e ficará na Infrastructure. O banco será preparado por aplicação explícita das migrations; não substituir migrations por EnsureCreated. A estratégia de execução e os comandos serão definidos na implementação, sem executar build neste planejamento.

A escrita realizará um único SaveChangesAsync. Falhas de validação não adicionarão dados ao contexto; falhas de persistência não retornarão sucesso. Todas as operações assíncronas de banco receberão CancellationToken.

## Contracts e Minimal API

```text
PickupReservations.Contracts/
  Reservations/
    CreateReservationRequest.cs
    CreateReservationResponse.cs
    GetReservationByIdResponse.cs

PickupReservations.Api/
  Endpoints/Reservations/
    ReservationEndpoints.cs
  Common/ExceptionHandling/
    DomainExceptionHandler.cs
  DependencyInjectionExtensions.cs
  Program.cs
  appsettings.json
```

Os contratos serão records independentes das entidades e dos tipos do Domain.

ReservationEndpoints será uma classe estática de extensões com MapReservationEndpoints, agrupando as duas rotas e seus adaptadores HTTP. Métodos estáticos de mapeamento não são Domain Services. Não serão criados Controllers.

### POST /reservations

Request:

```json
{
  "customerName": "Ana",
  "itemDescription": "Caderno",
  "quantity": 2
}
```

- Converter CreateReservationRequest em CreateReservationCommand.
- Chamar CreateReservationCommandHandler.
- Retornar 201 Created com CreateReservationResponse contendo id e Location apontando para GET /reservations/{id}.
- Nome, descrição e quantidade são os únicos campos de entrada do caso de uso.
- JSON ou binding inválido retorna 400; falha de regra de criação lança DomainException, convertida em 400 com ProblemDetails na API.

### GET /reservations/{id}

- Fazer binding de id como Guid; valor malformado retorna 400.
- Converter o identificador em GetReservationByIdQuery.
- Chamar GetReservationByIdQueryHandler.
- Resultado ausente retorna 404 com ProblemDetails.
- Resultado presente retorna 200 com GetReservationByIdResponse.
- A resposta contém id, customerName, itemDescription, quantity, status, createdAt e expiresAt. Status é uma string do contrato HTTP; datas são representadas em UTC.
- O endpoint não acessa repositório, DbContext ou implementação concreta da Infrastructure.

### Composição e erros

Program.cs será o Composition Root: chamará AddApplicationLayer, AddInfrastructureLayer e AddPresentationLayer, configurará o tratamento de exceções e mapeará os endpoints. TimeProvider.System será registrado na composição, se confirmada a proposta de relógio.

DomainExceptionHandler tratará DomainException de forma centralizada. Exceções inesperadas retornarão 500 sem expor detalhes internos; cancelamento da requisição não será convertido em falha de negócio. Tipos HTTP e ProblemDetails permanecerão na API.

## Ordem de implementação

1. Finalizar este design e selecionar versões de .NET e pacotes.
2. Criar os cinco projetos e referências permitidas.
3. Implementar bases obrigatórias, Reservation e criação válida.
4. Implementar os dois casos de uso e abstrações.
5. Implementar EF Core, SQLite, mapeamento e migration inicial.
6. Implementar contratos, Minimal API, composição e tratamento de erros.
7. Verificar os critérios de aceite conforme as autorizações de execução e testes.

Não criar classes auxiliares pequenas com único uso nem aumentar artificialmente sua quantidade de linhas.

## Validação prevista

Quando a criação de testes for autorizada:

- Domain: dados válidos e inválidos; estado inicial e cálculo exato do prazo.
- Application: orquestração da criação, confirmação única, ausência de persistência após falha de domínio; consulta encontrada e ausente.
- API: POST e GET reais via cliente HTTP; status, Location, body, binding inválido e ProblemDetails; persistência com SQLite e migrations.
- Arquitetura: referências de projeto e isolamento de Domain/Application em relação a infraestrutura e transporte.

Os testes terão pastas espelhadas, sem I/O nos testes unitários. Projetos de testes e builds não serão criados ou executados sem a autorização correspondente. O aceite funcional inclui preservar os dados entre reinicializações da aplicação.
