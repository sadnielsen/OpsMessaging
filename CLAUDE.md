# CLAUDE.md — instructies voor Claude Code

Dit bestand vertelt Claude Code **hoe** deze repository gebouwd wordt. Het **wat** en
**waarom** staat in [`DESIGN.md`](DESIGN.md). Lees `DESIGN.md` volledig voordat je code schrijft.

## Doel van dit project

Dit is een **leerproject**. De eigenaar wil de code lezen en begrijpen om technische
onderwerpen (Clean Architecture, teststrategie, CI-kwaliteitsgates, security/privacy-by-design,
OAuth2/OIDC met scopes en app roles) concreet te kunnen uitleggen.

Daarom geldt boven alles: **leesbaarheid en uitleg gaan voor slimheid en volledigheid.**
Kies de eenvoudigste oplossing die het concept goed laat zien. Geen extra features,
frameworks of abstracties die niet in `DESIGN.md` staan.

## Werkwijze

1. Werk **fase voor fase** (zie "Bouwfasen" onderaan). Begin pas aan een fase als de eigenaar
   daarom vraagt ("voer fase 2 uit").
2. Aan het eind van elke fase:
   - `dotnet build` en `dotnet test` zijn groen (zonder waarschuwingen).
   - `dotnet format --verify-no-changes` slaagt.
   - Geef een korte samenvatting in het Nederlands: wat is er gebouwd, welke bestanden zijn
     het lezen waard (met pad), en welk concept laten ze zien.
   - Stel een commitbericht voor (Engels, conventional commits, bv. `feat(domain): add Message aggregate`).
     Commit alleen als de eigenaar dat vraagt.
3. Wijk je af van `DESIGN.md`, meld dat dan expliciet met reden. Pas `DESIGN.md` niet stilletjes aan.
4. Twijfel je over een keuze die het ontwerp verandert: vraag het, gok niet.

## Taal

- **Code, identifiers, commitberichten:** Engels.
- **Commentaar in code:** Engels, kort en uitleggend (zie hieronder).
- **Gherkin-features:** Nederlands (`# language: nl`).
- **README, samenvattingen aan de eigenaar, `docs/WALKTHROUGH.md`:** Nederlands.
- Gebruik geen namen van bestaande organisaties in code, data of documentatie. Het domein is
  een generieke vervoersorganisatie.

## Commentaarstijl (belangrijk)

De eigenaar wil de code **begrijpen**, dus commentaar is ruimer dan in een normaal project.
Gebruik drie soorten commentaar:

1. **XML-doc (`///`) op elke publieke klasse en methode:** wat het is en waarom het bestaat.
2. **Concept-commentaar** bij elke plek die een interview-onderwerp illustreert. Vast formaat,
   zodat het doorzoekbaar is:

   ```csharp
   // CONCEPT (Clean Architecture): This interface is a "port". The Application layer
   // defines WHAT it needs; Infrastructure decides HOW. The domain never knows about SQL.
   ```

   Gebruik deze labels (en alleen deze):
   `Clean Architecture`, `DDD`, `Unit test`, `Integration test`, `Contract test`,
   `Acceptance test`, `Architecture test`, `Quality gate`, `Security-by-design`,
   `Privacy-by-design`, `OAuth2/OIDC`, `Scopes vs app roles`, `Event-driven`, `Resilience`,
   `Observability`.

3. **Inplugpunt-commentaar** op elke plek waar een echte implementatie hoort:

   ```csharp
   // REAL-IMPLEMENTATION: Persist to a database — replace with an EF Core repository
   // (Azure SQL/PostgreSQL). Register it in Infrastructure/DependencyInjection.cs instead
   // of InMemoryMessageRepository. Nothing in Domain or Application has to change.
   ```

   Zie `DESIGN.md` §8 voor de volledige lijst. Elke rij in die tabel moet minimaal één
   `REAL-IMPLEMENTATION`-marker in de code hebben.

Daarnaast: verwijs in code naar bedrijfsregels met hun code, bv. `// BR-03: urgent messages need ≥ 2 channels`.

Vermijd commentaar dat alleen herhaalt wat de code al zegt (`i++ // increment i`).

## Architectuurregels (niet onderhandelbaar)

- `Domain` heeft **geen** project- of package-references (alleen BCL).
- `Application` refereert alleen aan `Domain`. Ports (interfaces voor de buitenwereld) staan in
  `Application/Abstractions`.
- `Infrastructure` implementeert ports; refereert aan `Application`.
- `Api` gebruikt `Infrastructure` **alleen** in de composition root (`Program.cs` en
  `AddInfrastructure()`-extensie). Endpoints praten alleen met Application.
- Domeinobjecten verlaten de Api nooit; map naar response-DTO's.
- Verwachte fouten = `Result<T>` / `Error` (met `Code` zoals `BR-03`), **geen** exceptions.
  Exceptions alleen voor echt onverwachte situaties.
- Tijd altijd via `IClock`, nooit `DateTime.UtcNow` in Domain/Application.
- Geen `static` mutable state.

## Code-conventies

- .NET 10, C# nieuwste taalversie, `Nullable` enabled, `ImplicitUsings` enabled,
  `TreatWarningsAsErrors` true, `AnalysisLevel` latest-recommended. Zet dit in `Directory.Build.props`.
- Central Package Management (`Directory.Packages.props`). Gebruik de nieuwste **stabiele**
  versies; geen previews.
- File-scoped namespaces, `sealed` classes tenzij overerving bedoeld is, `record`s voor DTO's,
  commands, events en value objects.
- Value objects met private constructor + `static Result<T> Create(...)`.
- Async methodes eindigen op `Async` en accepteren een `CancellationToken`.
- Minimal API endpoints gegroepeerd per feature in `Api/Endpoints/MessageEndpoints.cs` via `MapGroup`.
- Logging: `ILogger<T>` met **source-generated** `LoggerMessage`-methodes en vaste templates.
  Log **nooit** berichtinhoud, ontvangerlijsten of contactgegevens (privacy-by-design).
- Testnamen: `Method_Scenario_ExpectedResult`. Asserts met Shouldly. Mocks met NSubstitute
  alleen waar een handgeschreven fake onhandig is.
- Geen FluentAssertions (licentie), geen MediatR, geen AutoMapper, geen Moq. Handmatig mappen
  is hier juist leerzaam.

## Commando's

```bash
dotnet build                                   # bouwen
dotnet test                                    # alle tests
dotnet test tests/OpsMessaging.Domain.UnitTests
dotnet format                                  # code formatteren
dotnet run --project tools/OpsMessaging.DevTokenIssuer   # lokale "Entra ID" op :5100
dotnet run --project src/OpsMessaging.Api                # API op :5000/:5001
grep -rn "REAL-IMPLEMENTATION" src tools .github         # alle inplugpunten
grep -rn "CONCEPT (" src tools tests                     # alle concept-uitleg
```

Token ophalen (na fase 4):

```bash
curl -s -X POST http://localhost:5100/token -d "persona=dispatcher"
```

---

## Bouwfasen

Elke fase eindigt met groene build en tests, plus een samenvatting. Voer een fase pas uit
als de eigenaar erom vraagt.

### Fase 0 — Skelet en architectuurbewaking

- `global.json`, `Directory.Build.props`, `Directory.Packages.props`, `.editorconfig`, solution.
- Alle projecten uit `DESIGN.md` §2.2 aanmaken (leeg), met de juiste project-references.
- `OpsMessaging.ArchitectureTests` met NetArchTest: de dependency rules uit `DESIGN.md` §6.6.
- Lege `README.md`-secties invullen: wat is dit, hoe start je het, waar begin je met lezen.
- **Klaar als:** `dotnet build` en `dotnet test` groen; architectuurtests bestaan en slagen.

### Fase 1 — Domein + unit tests

- `Result`/`Error`-type, value objects (`MessageTitle`, `MessageBody`, `Audience`,
  `RecipientId`, `SenderId`), enums (`Priority`, `Channel`, `MessageStatus`, `DeliveryStatus`).
- `Message`-aggregate met `Create(...)`, `Dispatch(recipients)`, `Cancel(now)`, en
  `Delivery`-entiteit. Domain event `MessageDispatched`.
- Alle regels BR-01..BR-07 uit `DESIGN.md` §1.2, met `// BR-xx`-commentaar.
- Unit tests: per regel minimaal één positieve en één negatieve test.
- **Klaar als:** elke BR-code komt voor in zowel domeincode als een testnaam.

### Fase 2 — Application: use cases en ports

- Ports in `Application/Abstractions`: `IMessageRepository`, `IRecipientDirectory`,
  `IChannelDispatcher`, `IEventPublisher`, `IClock`, `ICurrentUser`.
- Use cases als eenvoudige handler-klassen (geen MediatR): `SendMessageHandler`,
  `GetMessageHandler`, `GetDeliveriesHandler`, `CancelMessageHandler`.
- `SendMessageHandler`: valideren → doelgroep bepalen → BR-05 → dispatch per ontvanger/kanaal →
  opslaan → event publiceren. Leg in commentaar uit waar in productie een outbox nodig is.
- Unit tests met fakes (in testproject, of een gedeelde `TestDoubles`-map).
- **Klaar als:** bij BR-05 bewijst een test dat er geen dispatch en geen event plaatsvindt.

### Fase 3 — Infrastructure + API (nog zonder auth) + integratietests

- Adapters: `InMemoryMessageRepository`, `FakeRecipientDirectory` (verzonnen medewerkers
  `EMP-0001`…, met rolgroep en regio), `SimulatedChannelDispatcher`, `LoggingEventPublisher`,
  `SystemClock`. Elk met `REAL-IMPLEMENTATION`-commentaar.
- `Infrastructure/DependencyInjection.cs` met `AddInfrastructure()`.
- API: endpoints uit `DESIGN.md` §3, DTO's, `Error → ProblemDetails`-mapper, exception
  handler, health checks, OpenAPI-document op `/openapi/v1.json`.
- Een `OpsMessaging.Api.http`-bestand met voorbeeldrequests.
- Integratietests met `WebApplicationFactory`: happy flow + 400/404/409/422.
- Een test die met een test-logger aantoont dat berichtinhoud **niet** gelogd wordt.
- **Klaar als:** de API lokaal draait en de `.http`-requests werken.

### Fase 4 — OAuth2/OIDC: lokale tokenprovider + API-beveiliging

- `tools/OpsMessaging.DevAuth`: `DevTokenFactory` die Entra ID v2-achtige tokens maakt
  (claims uit `DESIGN.md` §4.2) en de persona's uit §4.5 kent.
- `tools/OpsMessaging.DevTokenIssuer`: minimal API met discovery, JWKS en `POST /token`.
  RSA-sleutel wordt bij opstarten gegenereerd. Grote waarschuwing in commentaar en README:
  **alleen voor lokaal gebruik**.
- API: JwtBearer met `Authority`/`Audience`/tenant-validatie uit configuratie,
  FallbackPolicy, policies uit §4.4 in `Api/Security/AuthorizationPolicies.cs`, rate limiting,
  security headers. Leg in commentaar scopes vs. app roles uit.
- `ICurrentUser`-implementatie die `oid` leest → `SenderId`.
- Duidelijk `REAL-IMPLEMENTATION`-blok dat laat zien wat er voor echte Entra ID verandert
  (alleen config, of `Microsoft.Identity.Web`).
- Integratietests: de **volledige autorisatiematrix** (endpoint × persona → status) als
  `[Theory]`. De testfactory valideert tokens echt (signing key van `DevTokenFactory`), geen bypass.
- **Klaar als:** de matrix groen is en `.http`-bestand een token-stap heeft.

### Fase 5 — Contracttests

- `OpsMessaging.ContractTests`: OpenAPI-snapshot vs. `contracts/openapi.v1.json`, met een
  duidelijke foutmelding en een gedocumenteerde manier om het contract bewust bij te werken
  (bv. environment variable `UPDATE_CONTRACTS=1`).
- JSON Schema voor `MessageDispatched` in `contracts/events/` + validatietest.
- Commentaar dat uitlegt wat consumer-driven contract testing (Pact) zou toevoegen.
- **Klaar als:** een bewuste wijziging aan een DTO de contracttest laat falen.

### Fase 6 — Acceptatietests

- `OpsMessaging.AcceptanceTests` met Reqnroll + xUnit, features in het Nederlands.
- Minimaal één positief en één negatief scenario per BR-regel, plus een autorisatiescenario
  ("Als lezer mag ik geen bericht versturen").
- Step definitions praten via HTTP met de API (`WebApplicationFactory`) en gebruiken
  persona-tokens.
- Tag scenario's met de regelcode (`@BR-03`) voor traceerbaarheid.
- **Klaar als:** `docs/traceability.md` (tabel BR-code → unit test → scenario) klopt.

### Fase 7 — CI/CD met kwaliteitsgates

- `.github/workflows/ci.yml` met alle gates uit `DESIGN.md` §7, elke stap met een naam die de
  gate benoemt en een `# CONCEPT (Quality gate):`-commentaar.
- Coverage-drempel op Domain + Application.
- `.github/dependabot.yml` voor NuGet en GitHub Actions.
- Uitgecommentarieerde CD-job (build image → deploy test → smoke test → approval → prod) met
  `REAL-IMPLEMENTATION`-markers.
- README: hoe je branch protection instelt zodat de gates verplicht zijn.
- **Klaar als:** de workflow op GitHub groen draait.

### Fase 8 — Leesroute

- `docs/WALKTHROUGH.md` (Nederlands): een leesroute van ±30 minuten door de code. Per onderwerp
  uit `DESIGN.md` §10: 2–3 zinnen uitleg, de bestanden (met pad en regel) om te lezen, en één
  vraag die een interviewer erover zou kunnen stellen met een kort antwoord.
- Controleer dat elke rij uit `DESIGN.md` §8 een `REAL-IMPLEMENTATION`-marker heeft en meld
  ontbrekende.
- **Klaar als:** de eigenaar de walkthrough kan volgen zonder andere uitleg.
