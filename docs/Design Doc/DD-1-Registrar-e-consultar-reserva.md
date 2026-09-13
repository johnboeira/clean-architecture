# DD-1 — Registrar e consultar reserva

Status: código implementado; build da API aprovado durante os comandos do EF Core e snapshot sem alterações pendentes em relação ao modelo. Execução HTTP e aplicação da migration ainda não validadas. Projetos de testes criados sem cenários implementados.

## Fontes e fluxo de trabalho

- Requisitos funcionais: [PRD-1](../PRDs/PRD-1.md).
- Este documento é o local para ajustar as decisões técnicas destas duas features antes de implementar.
- Novas features devem ter seu Design Doc preparado antes da implementação. Mudanças funcionais também devem ser refletidas no PRD correspondente.
- O projeto de estudos não é referência para este design.

## Escopo

Implementar somente registrar reserva (escrita) e consultar reserva por identificador (leitura).

## Projetos e referências

As pastas físicas e as pastas de solução em .sln e .slnx seguem a mesma organização. A numeração não faz parte dos nomes de projeto, assemblies ou namespaces.

```text
src/
  1 - Presentation/
    PickupReservations.Api/
    PickupReservations.Contracts/
  2 - Application/
    PickupReservations.Application/
  3 - Domain/
    PickupReservations.Domain/
  4 - Infrastructure/
    PickupReservations.Infrastructure/
tests/
  1 - Presentation/
    PickupReservations.Api.IntegrationTests/
  2 - Application/
    PickupReservations.Application.UnitTests/
  3 - Domain/
    PickupReservations.Domain.UnitTests/
  5 - Architecture/
    PickupReservations.ArchitectureTests/
```

| Projeto | Responsabilidade | Referências de projeto |
| --- | --- | --- |
| PickupReservations.Domain | Agregado e invariantes | Nenhuma |
| PickupReservations.Application | Casos de uso e abstrações externas | Domain |
| PickupReservations.Infrastructure | EF Core, SQLite e implementação de abstrações | Application e Domain |
| PickupReservations.Contracts | Requests e responses HTTP | Nenhuma |
| PickupReservations.Api | Minimal API e Composition Root | Application, Contracts e Infrastructure apenas para composição |

EF Core e seu provedor SQLite permanecem na Infrastructure. A Application não recebe DbContext, IQueryable ou outros tipos de persistência. A implementação utiliza .NET 10 (SDK 10.0.400), EF Core SQLite 10.0.8 e pacotes centralizados em Directory.Packages.props. Os projetos de testes usam Microsoft.NET.Test.Sdk 17.14.1, xUnit 2.9.2 e runner 2.8.2, sem testes implementados.

### Projetos de testes

Na estruturação da solução, criar também os projetos abaixo na pasta `tests` e adicioná-los à solução. A criação da estrutura está autorizada; a implementação dos testes ficará para uma etapa posterior.

| Projeto | Finalidade futura | Referências de projeto |
| --- | --- | --- |
| PickupReservations.Domain.UnitTests | Regras e invariantes do Domain | Domain |
| PickupReservations.Application.UnitTests | Orquestração dos casos de uso | Application e Domain |
| PickupReservations.Api.IntegrationTests | Endpoints HTTP e integração com persistência | Api e Contracts |
| PickupReservations.ArchitectureTests | Regras de dependência entre camadas | Domain, Application, Infrastructure, Contracts e Api |

Criar somente os arquivos de projeto, referências e configuração básica do framework de testes. Não incluir testes de exemplo gerados por templates, classes de testes, fixtures, mocks ou cenários nesta etapa. As referências do projeto de arquitetura servem para inspecionar as camadas e não alteram as dependências dos projetos de produção.

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
Extensions/
  DependencyInjectionExtensions.cs
```
Commands, Queries e ReservationDetails são records com entrada e saída explícitas para cada caso de uso. A Application usa MediatR 12.5.0, fixado em Directory.Packages.props, última versão anterior à mudança da licença Apache 2.0. CreateReservationCommand implementa IRequest<Guid> e GetReservationByIdQuery implementa IRequest<ReservationDetails?>. Cada handler implementa IRequestHandler com seu método Handle e recebe CancellationToken. Os endpoints injetam ISender e despacham os casos de uso com Send, propagando o CancellationToken. O Domain permanece sem dependência de MediatR.

### Escrita

- CreateReservationCommand: CustomerName, ItemDescription e Quantity.
- Saída do handler: Task<Guid>, contendo o identificador criado, sem um tipo Result intermediário.
- CreateReservationCommandHandler: obter o instante atual, invocar Reservation.Create, adicionar a reserva pelo repositório, chamar repository.SaveChangesAsync e devolver o Id.
- IReservationRepository: expor AddAsync e SaveChangesAsync(CancellationToken). A adição apenas prepara a persistência; SaveChangesAsync retorna Task<int> e confirma as alterações pelo contexto.
- Não haverá uma abstração separada de Unit of Work. Caso um caso de uso envolva vários repositórios, todos compartilharão o mesmo contexto e as alterações serão confirmadas por uma única chamada a SaveChangesAsync.

O relógio usa TimeProvider, da biblioteca padrão do .NET, injetado na Application para obter GetUtcNow. O Domain recebe apenas o instante. TimeProvider.System é registrado no Composition Root.

### Leitura

- GetReservationByIdQuery: Id.
- ReservationDetails: modelo de leitura em Reservations/Models na Application, contendo Id, CustomerName, ItemDescription, Quantity, Status, CreatedAt e ExpiresAt.
- GetReservationByIdQueryHandler: consultar IReservationQueries e retornar Task<ReservationDetails?>; null representa reserva ausente.
- IReservationQueries: GetByIdAsync(Guid id, CancellationToken), retornando Task<ReservationDetails?>.

O estado pode usar ReservationStatus no resultado interno da Application. A borda HTTP o converte para o contrato externo. A leitura projeta os dados diretamente, sem exigir a materialização de Reservation e sem executar SaveChangesAsync.

DependencyInjectionExtensions expõe AddApplicationLayer e usa AddMediatR com RegisterServicesFromAssembly para registrar o mediador e os handlers da Application. Não há registro direto dos handlers concretos nos endpoints. Não haverá registro de DI no Domain, pois o recorte não possui serviços de domínio a registrar e ele deve permanecer livre de frameworks.

## Infrastructure e persistência

```text
Common/Persistence/
  ReservationsDbContext.cs
  Migrations/
Reservations/Persistence/
  ReservationEntityConfiguration.cs
  ReservationRepository.cs
  ReservationQueries.cs
Extensions/
  DependencyInjectionExtensions.cs
```

| Tipo | Responsabilidade |
| --- | --- |
| ReservationsDbContext | Contexto EF Core compartilhado pelos repositórios no mesmo escopo |
| ReservationEntityConfiguration | Mapeamento do agregado para a tabela Reservations |
| ReservationRepository | Adicionar o agregado ao contexto e expor SaveChangesAsync como operação separada |
| ReservationQueries | Buscar por ID com AsNoTracking e projeção para o resultado da Application |
| DependencyInjectionExtensions | Expor AddInfrastructureLayer e registrar as implementações |

Implementações concretas serão internal por padrão; somente a extensão necessária à composição será pública. DbContext, repositório e consultas terão escopo por requisição e compartilharão o mesmo contexto quando necessário.

### Modelo de armazenamento

- Tabela Reservations com Id como chave primária.
- Nome, descrição, quantidade, estado e datas obrigatórios.
- Status armazenado com representação definida no mapeamento, sem atributos de serialização ou ORM no Domain.
- Datas normalizadas para UTC; o mapeamento deve definir uma representação compatível com SQLite. Este recorte só consulta por ID e não depende de comparação de datas traduzida pelo provedor.
- A conversão e a leitura devem preservar o instante e o prazo de 15 minutos.
- Eventos internos do agregado não serão mapeados como dados persistidos.
- A string de conexão e o caminho do arquivo SQLite virão de configuração. Arquivos locais de banco não devem ser versionados.

A migration InitialCreate, seu Designer e o snapshot ficam na Infrastructure e são gerados exclusivamente pelo comando oficial do EF Core. Não criar nem editar esses arquivos manualmente. As datas são armazenadas como ticks UTC em colunas INTEGER, e o estado como texto.

A ferramenta dotnet-ef 10.0.8 está fixada no manifesto local dotnet-tools.json. Microsoft.EntityFrameworkCore.Design 10.0.8 é uma dependência privada do projeto de inicialização Api, necessária à ferramenta.

```powershell
dotnet tool restore
dotnet ef migrations add InitialCreate --project "src/4 - Infrastructure/PickupReservations.Infrastructure" --startup-project "src/1 - Presentation/PickupReservations.Api" --output-dir Common/Persistence/Migrations
dotnet ef migrations has-pending-model-changes --project "src/4 - Infrastructure/PickupReservations.Infrastructure" --startup-project "src/1 - Presentation/PickupReservations.Api"
```

Para desfazer a última migration ainda não aplicada, usar dotnet ef migrations remove com os mesmos argumentos de projeto e inicialização. A geração já foi executada e a verificação confirmou que não há diferenças pendentes no modelo. O restore apontou NU1903 na dependência transitiva SQLitePCLRaw.lib.e_sqlite3 2.1.11; a atualização dessa dependência permanece pendente.

O banco é preparado por aplicação explícita das migrations, usando o argumento --migrate da API. Esse modo aplica as migrations e encerra o processo sem iniciar o servidor. A Infrastructure expõe ApplyInfrastructureMigrationsAsync para a composição, sem expor DbContext à API. Não é utilizado EnsureCreated.

Comandos para execução manual a partir da raiz do repositório, quando a compilação for autorizada (dotnet run compila por padrão):

```powershell
dotnet run --project "src/1 - Presentation/PickupReservations.Api" -- --migrate
dotnet run --project "src/1 - Presentation/PickupReservations.Api" --launch-profile http
```

A API usa http://localhost:5080 no perfil http. A conexão Reservations está em appsettings.json e pode ser substituída por ConnectionStrings__Reservations; seu valor padrão é Data Source=reservations.db. Caminhos relativos são resolvidos pelo diretório de trabalho do processo; ao executar fora do perfil do projeto, usar o mesmo diretório ou configurar um caminho absoluto para reutilizar o banco. Arquivos SQLite locais são ignorados pelo Git.

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
  Middlewares/
    GlobalExceptionHandler.cs
  Extensions/
    DependencyInjectionExtensions.cs
  Program.cs
  appsettings.json
  PickupReservations.Api.http
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
- Despachar o Command por ISender.Send, que resolve CreateReservationCommandHandler.
- Retornar 201 Created com CreateReservationResponse contendo id e Location apontando para GET /reservations/{id}.
- Nome, descrição e quantidade são os únicos campos de entrada do caso de uso.
- JSON ou binding inválido retorna 400; falha de regra de criação lança DomainException, convertida pelo GlobalExceptionHandler em 400 com ProblemDetails na API.

### GET /reservations/{id}

- Fazer binding de id como Guid; valor malformado retorna 400.
- Converter o identificador em GetReservationByIdQuery.
- Despachar a Query por ISender.Send, que resolve GetReservationByIdQueryHandler.
- Resultado ausente retorna 404 com ProblemDetails.
- Resultado presente retorna 200 com GetReservationByIdResponse.
- A resposta contém id, customerName, itemDescription, quantity, status, createdAt e expiresAt. Status é uma string do contrato HTTP; datas são representadas em UTC.
- O endpoint não acessa repositório, DbContext ou implementação concreta da Infrastructure.

### Composição e erros

Program.cs é o Composition Root: chama AddApplicationLayer, AddInfrastructureLayer e AddPresentationLayer, ativa o middleware global com UseExceptionHandler antes da execução dos endpoints e mapeia as rotas. TimeProvider.System é registrado na composição. O modo --migrate aplica as migrations e encerra antes de servir requisições.

AddPresentationLayer registrará GlobalExceptionHandler com AddExceptionHandler<GlobalExceptionHandler>() e os serviços de ProblemDetails com AddProblemDetails().

GlobalExceptionHandler implementará IExceptionHandler e centralizará o tratamento de exceções da API:

- Converter exceções conhecidas em respostas HTTP com ProblemDetails; neste recorte, DomainException de validação da criação retorna 400.
- Registrar exceções inesperadas e retornar 500 com ProblemDetails, sem expor stack traces ou detalhes internos.
- Não converter cancelamento da requisição em falha de negócio.

Não haverá handlers de exceção por feature ou por tipo de exceção, nem tratamento duplicado nos endpoints. O tratamento HTTP de exceções e os tipos ProblemDetails permanecerão exclusivamente na API. A ausência de reserva continuará sendo um resultado de consulta convertido pelo endpoint em 404, sem lançar uma exceção.

### Requisições para teste manual

Criar PickupReservations.Api.http na raiz do projeto de API, com requisições separadas por `###` e variáveis `@baseUrl` e `@reservationId` para configurar o endereço local e o identificador consultado.

O arquivo deverá incluir:

- POST /reservations com dados válidos; resultado esperado: 201, identificador no body e header Location.
- GET /reservations/{id} usando o identificador retornado pela criação; resultado esperado: 200 com os dados registrados.
- POST com nome ou descrição inválidos e POST com quantidade zero ou negativa, em requisições separadas; resultado esperado: 400 com ProblemDetails para as falhas de domínio.
- GET com um Guid sem reserva correspondente; resultado esperado: 404.
- GET com identificador malformado; resultado esperado: 400.

Incluir comentários orientando a copiar o identificador retornado pelo POST para `@reservationId` e a repetir o GET após reiniciar a aplicação para verificar a persistência. O arquivo será um apoio à execução manual, sem constituir uma suíte de testes automatizados.

## Ordem de implementação

1. Finalizar este design e selecionar versões de .NET e pacotes.
2. Criar os cinco projetos de produção e os quatro projetos de testes, com suas referências e registro na solução; deixar a implementação dos testes para depois.
3. Implementar bases obrigatórias, Reservation e criação válida.
4. Implementar os dois casos de uso e abstrações.
5. Implementar EF Core, SQLite, mapeamento e migration inicial.
6. Implementar contratos, Minimal API, composição, tratamento de erros e o arquivo .http para teste manual.
7. Verificar os critérios de aceite conforme as autorizações de execução e testes.

Não criar classes auxiliares pequenas com único uso nem aumentar artificialmente sua quantidade de linhas.

## Validação prevista

Quando a criação de testes for autorizada:

- Domain: dados válidos e inválidos; estado inicial e cálculo exato do prazo.
- Application: orquestração da criação, confirmação única, ausência de persistência após falha de domínio; consulta encontrada e ausente.
- API: POST e GET reais via cliente HTTP; status, Location, body, binding inválido e ProblemDetails; tratamento global de DomainException como 400 e de exceções inesperadas como 500 sem detalhes internos; persistência com SQLite e migrations.
- Arquitetura: referências de projeto e isolamento de Domain/Application em relação a infraestrutura e transporte.

Os testes terão pastas espelhadas, sem I/O nos testes unitários. A estrutura dos quatro projetos de testes será criada junto à solução; os testes serão implementados posteriormente, quando autorizados. Builds e execução de testes não fazem parte desta etapa. O aceite funcional inclui preservar os dados entre reinicializações da aplicação.
