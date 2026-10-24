# DESIGN — OpsMessaging

> Oefenapplicatie: een kleine, complete backendservice in C#/.NET voor het versturen van
> operationele berichten naar medewerkers in het veld. Gebouwd om de volgende onderwerpen
> concreet in code te zien: Clean/hexagonale architectuur, unit-, integratie-, contract- en
> acceptatietests, kwaliteitsgates in CI/CD, security- en privacy-by-design, en
> OAuth2/OIDC met Entra ID-achtige tokens (scopes, app roles, API-beveiliging).
>
> Dit document beschrijft **wat** er gebouwd wordt en **waarom**. `CLAUDE.md` beschrijft
> **hoe** Claude Code het bouwt (conventies en fasering).

---

## 1. Domein in het kort

Een vervoersorganisatie heeft operationeel personeel op voertuigen, op locaties en op
kantoor. Een **dispatcher** in een meldkamer wil die medewerkers snel en gericht informeren:
"Storing op traject X, volg instructie Y", of "Dienstwijziging vanaf 14:00".

OpsMessaging is één **bounded context** uit zo'n multichannel communicatieplatform:

| Stap | Verantwoordelijkheid | In deze app |
|---|---|---|
| Ontvangen | Bericht aannemen en valideren | `POST /api/v1/messages` |
| Doelgroepbepaling | Welke medewerkers vallen onder de doelgroep? | `IRecipientDirectory` (port) |
| Routing | Via welke kanalen gaat het bericht? | Domeinregel op basis van prioriteit |
| Dispatch | Daadwerkelijk versturen | `IChannelDispatcher` (port) |
| Tracking | Is het per ontvanger per kanaal aangekomen? | `Delivery`-records + `GET .../deliveries` |
| Events | Andere contexten informeren | `MessageDispatched` via `IEventPublisher` (port) |

Bewust **buiten scope**: echte database, echte message broker, echte SMS/push-providers,
echte Entra ID-tenant, UI. Op al die plekken staat een duidelijk gemarkeerd
**inplugpunt** (zie §8).

### 1.1 Ubiquitous language (begrippen)

| Begrip (NL) | Code (EN) | Betekenis |
|---|---|---|
| Bericht | `Message` | Aggregate root. Titel, inhoud, prioriteit, doelgroep, geldigheid, status. |
| Prioriteit | `Priority` | `Normal` of `Urgent`. |
| Doelgroep | `Audience` | Eén of meer rolgroepen (bv. `Driver`, `Conductor`, `StationStaff`) plus optioneel een regio. |
| Kanaal | `Channel` | `Push`, `Sms`, `Email`. |
| Ontvanger | `RecipientId` | Pseudoniem ID van een medewerker. **Nooit** naam of telefoonnummer. |
| Aflevering | `Delivery` | Status van één bericht naar één ontvanger via één kanaal. |
| Verzender | `SenderId` | Object-ID (`oid`) uit het token. Geen naam. |

### 1.2 Bedrijfsregels

Deze regels zijn de bron voor de acceptatiecriteria (§6.4). Elke regel krijgt een code,
zodat je hem terugvindt in domeincode, tests en foutmeldingen.

| Code | Regel | Fout bij overtreding |
|---|---|---|
| **BR-01** | Titel is verplicht, max. 100 tekens. Inhoud is verplicht, max. 1000 tekens. | 400 Validation |
| **BR-02** | De doelgroep bevat minimaal één rolgroep. | 400 Validation |
| **BR-03** | Een **urgent** bericht gaat via minimaal **twee** kanalen (redundantie). | 400 Validation |
| **BR-04** | `ValidUntil` ligt in de toekomst en maximaal 24 uur na nu. | 400 Validation |
| **BR-05** | Levert de doelgroepbepaling nul ontvangers op, dan wordt het bericht niet verstuurd. | 422 Unprocessable |
| **BR-06** | Alleen een bericht met status `Dispatched` kan worden ingetrokken (`Cancelled`). | 409 Conflict |
| **BR-07** | Een verlopen bericht (`ValidUntil` < nu) kan niet meer worden ingetrokken. | 409 Conflict |

### 1.3 Statusmodel

```
            Create + Dispatch               Cancel (BR-06/07)
  (nieuw) ─────────────────────▶ Dispatched ─────────────────▶ Cancelled
                                     │
                                     │ tijd verstrijkt (ValidUntil)
                                     ▼
                                  Expired   (afgeleid: berekend met IClock, niet opgeslagen)
```

`Delivery`: `Pending → Sent` of `Pending → Failed`.

---

## 2. Architectuur

### 2.1 Clean / hexagonaal

```
                ┌────────────────────────────────────────────┐
                │                  Api                       │  ← driving adapter (HTTP)
                │  endpoints, auth-policies, ProblemDetails  │     + composition root
                └───────────────┬────────────────────────────┘
                                │ roept use cases aan
                ┌───────────────▼────────────────────────────┐
                │              Application                   │
                │  use cases (commands/queries) + PORTS      │  ← interfaces die de
                │  IMessageRepository, IRecipientDirectory,  │     buitenwereld beschrijven
                │  IChannelDispatcher, IEventPublisher,      │
                │  IClock, ICurrentUser                      │
                └───────────────┬────────────────────────────┘
                                │ gebruikt
                ┌───────────────▼────────────────────────────┐
                │                Domain                      │  ← geen enkele dependency
                │  Message, Delivery, value objects, regels  │
                └────────────────────────────────────────────┘

                ┌────────────────────────────────────────────┐
                │            Infrastructure                  │  ← driven adapters
                │  InMemoryMessageRepository                 │     implementeren de ports
                │  FakeRecipientDirectory                    │     uit Application
                │  SimulatedChannelDispatcher                │
                │  LoggingEventPublisher, SystemClock        │
                └────────────────────────────────────────────┘
```

**Dependency rule:** afhankelijkheden wijzen alleen naar binnen.

- `Domain` → niets (ook geen NuGet-packages, behalve eventueel analyzers).
- `Application` → `Domain`.
- `Infrastructure` → `Application` (en daarmee `Domain`).
- `Api` → `Application` + `Infrastructure` (dat laatste **alleen** in de composition root,
  `Program.cs` / `DependencyInjection`-extensies, om adapters te registreren).

Deze regel wordt **afgedwongen** met architectuurtests (NetArchTest), zodat een overtreding
de build in CI breekt. Dat is zelf een kwaliteitsgate.

### 2.2 Projectstructuur

```
OpsMessaging.sln
Directory.Build.props            # net10.0, Nullable, TreatWarningsAsErrors, analyzers
Directory.Packages.props         # Central Package Management: alle versies op één plek
.editorconfig
global.json                      # vastgezette SDK-versie

src/
  OpsMessaging.Domain/
  OpsMessaging.Application/
  OpsMessaging.Infrastructure/
  OpsMessaging.Api/

tools/
  OpsMessaging.DevAuth/          # class library: DevTokenFactory (maakt Entra-achtige JWT's)
  OpsMessaging.DevTokenIssuer/   # mini OIDC-provider: discovery, JWKS, /token  (alleen lokaal)

tests/
  OpsMessaging.Domain.UnitTests/
  OpsMessaging.Application.UnitTests/
  OpsMessaging.Api.IntegrationTests/
  OpsMessaging.ContractTests/
  OpsMessaging.AcceptanceTests/
  OpsMessaging.ArchitectureTests/

contracts/
  openapi.v1.json                # vastgelegd API-contract (contracttest vergelijkt hiermee)
  events/message-dispatched.v1.schema.json

docs/
  WALKTHROUGH.md                 # leesroute: concept → bestand → regel (wordt in de laatste fase gemaakt)

.github/workflows/ci.yml
```

### 2.3 Technologiekeuzes

| Onderdeel | Keuze | Waarom |
|---|---|---|
| Runtime | .NET 10 (LTS) | Huidige LTS. |
| API-stijl | ASP.NET Core Minimal APIs met `MapGroup` | Weinig ceremonie, endpoints zijn goed leesbaar. |
| OpenAPI | `Microsoft.AspNetCore.OpenApi` (ingebouwd) | Genereert het contract dat de contracttest bewaakt. |
| Auth | `Microsoft.AspNetCore.Authentication.JwtBearer` | Werkt identiek tegen de lokale issuer én tegen Entra ID. |
| Unit tests | xUnit + Shouldly + NSubstitute | Leesbare asserts; mocks alleen waar nodig (voorkeur: handgeschreven fakes). |
| Integratietests | `Microsoft.AspNetCore.Mvc.Testing` (`WebApplicationFactory`) | Echte HTTP-pipeline in-process, inclusief auth. |
| Acceptatietests | Reqnroll (opvolger van SpecFlow) + Gherkin in het **Nederlands** | Acceptatiecriteria leesbaar voor business, uitvoerbaar voor CI. |
| Contracttests | Snapshot van OpenAPI-document + JSON Schema van events | Brekende wijzigingen in API/event worden zichtbaar in een PR. |
| Architectuurtests | NetArchTest.Rules | Dependency rule afdwingen. |
| Coverage | coverlet + drempel in CI | Kwaliteitsgate. |

---

## 3. API

Basispad: `/api/v1`. Alle fouten als **ProblemDetails** (RFC 9457), zonder stacktraces.

| Methode | Pad | Doel | Policy | Succes |
|---|---|---|---|---|
| POST | `/messages` | Bericht aanmaken en direct versturen | `CanSendMessages` | 201 + `Location` |
| GET | `/messages/{id}` | Bericht ophalen | `CanReadMessages` | 200 |
| GET | `/messages/{id}/deliveries` | Afleverstatus per ontvanger/kanaal | `CanReadMessages` | 200 |
| POST | `/messages/{id}/cancel` | Bericht intrekken | `CanCancelMessages` | 204 |
| GET | `/health/live`, `/health/ready` | Health checks voor orchestrator (bv. Kubernetes probes) | anoniem | 200 |

### 3.1 Voorbeeld: aanmaken

```http
POST /api/v1/messages
Authorization: Bearer eyJ...
Content-Type: application/json

{
  "title": "Storing wissel sector 4",
  "body": "Verwacht vertraging. Volg instructie van de verkeersleiding.",
  "priority": "Urgent",
  "channels": ["Push", "Sms"],
  "audience": { "roleGroups": ["Driver", "Conductor"], "regionCode": "REGION-NORTH" },
  "validUntil": "2026-10-26T18:00:00Z"
}
```

```http
HTTP/1.1 201 Created
Location: /api/v1/messages/7b0c...

{ "id": "7b0c...", "status": "Dispatched", "recipientCount": 12, "deliveryCount": 24 }
```

Request- en response-DTO's leven in de **Api**-laag en worden gemapt naar commands van de
Application-laag. Domeinobjecten komen **nooit** rechtstreeks in een HTTP-response
(voorkomt dat je per ongeluk velden lekt: privacy-by-design).

### 3.2 Foutvertaling

| Bron | HTTP |
|---|---|
| Validatiefout (BR-01..04) | 400 met `errors` per veld en `code` (bv. `BR-03`) |
| Geen ontvangers (BR-05) | 422 |
| Niet gevonden | 404 |
| Statusconflict (BR-06/07) | 409 |
| Geen/ongeldig token | 401 (`WWW-Authenticate: Bearer ...`) |
| Geldig token, onvoldoende rechten | 403 |
| Onverwacht | 500, generieke tekst, `traceId` voor correlatie |

Domeinfouten worden gemodelleerd als een `Result`/`Error`-type (geen exceptions voor
verwachte fouten). Een centrale mapper in de Api-laag vertaalt `Error.Code` naar een status.

---

## 4. Identity en API-beveiliging (OAuth2 / OIDC, Entra ID-nabootsing)

### 4.1 Rollen in het OAuth2-verhaal

| OAuth2-rol | In deze app |
|---|---|
| Resource server | `OpsMessaging.Api` — valideert tokens, dwingt autorisatie af. |
| Authorization server / OIDC provider | **Lokaal:** `OpsMessaging.DevTokenIssuer`. **Productie:** Microsoft Entra ID. |
| Client | Dispatcher-UI (gebruiker, *delegated*) of een upstream planningssysteem (*app-only*, client credentials). Lokaal: `curl`/`.http`-bestand met een token van de issuer. |

### 4.2 De lokale tokenprovider

De DevTokenIssuer gedraagt zich als een minimale OIDC-provider, zodat de API exact dezelfde
validatieroute volgt als bij Entra ID:

| Endpoint | Doel |
|---|---|
| `GET /.well-known/openid-configuration` | Discovery-document (`issuer`, `jwks_uri`). |
| `GET /discovery/keys` | JWKS met de publieke RSA-sleutel. |
| `POST /token` | **Alleen voor dev.** Geeft een token terug voor een gekozen persona. |

De API krijgt alleen `Authority` en `Audience` geconfigureerd; JwtBearer haalt zelf
discovery en sleutels op. Wisselen naar Entra ID = **alleen configuratie** aanpassen
(zie §8). Dit laat goed zien waarom OIDC-discovery zo handig is.

Tokens bevatten dezelfde claims als een Entra ID v2-access token:

| Claim | Voorbeeld | Betekenis |
|---|---|---|
| `iss` | `http://localhost:5100/{tenantId}/v2.0` | Wie het token uitgaf. Wordt gevalideerd. |
| `aud` | `api://opsmessaging` | Voor welke API het token bedoeld is. Wordt gevalideerd. |
| `tid` | GUID | Tenant. API accepteert alleen de geconfigureerde tenant. |
| `oid` | GUID | Stabiele object-ID van gebruiker/app. Wordt `SenderId`. |
| `azp` | GUID | Client-ID van de aanroepende applicatie. |
| `scp` | `Messages.Read Messages.Send` | **Delegated scopes** (spatiegescheiden). Alleen bij gebruiker-tokens. |
| `roles` | `["Dispatcher"]` | **App roles**. Bij gebruikers: toegekende rol. Bij app-only: toegekende applicatiepermissie. |
| `exp`, `nbf`, `iat` | | Levensduur; korte geldigheid (60 min). |

Bewust **geen** naam, e-mail of telefoonnummer in het access token (dataminimalisatie).

### 4.3 Scopes vs. app roles

- **Scope (`scp`)**: wat de *client-applicatie* namens de gebruiker mag doen. De gebruiker
  (of admin) geeft daar toestemming (consent) voor. Antwoord op: "mag deze app dit namens
  jou?"
- **App role (`roles`)**: wat de *gebruiker of applicatie zelf* is of mag binnen deze API.
  Toegekend door een beheerder. Antwoord op: "wie ben jij binnen dit systeem?"
- **Delegated** token = gebruiker + app → heeft `scp` (en eventueel `roles` van de gebruiker).
- **App-only** token (client credentials, daemon) → heeft **geen** `scp`, alleen `roles`.

Een veilige API controleert daarom de **combinatie**: de app moet de juiste scope hebben
**én** de gebruiker de juiste rol. Anders kan een app met `Messages.Send`-scope namens een
willekeurige medewerker berichten versturen.

Gedefinieerd in deze API:

| Type | Waarde | Gebruik |
|---|---|---|
| Delegated scope | `Messages.Read` | Berichten lezen namens gebruiker. |
| Delegated scope | `Messages.Send` | Berichten versturen/intrekken namens gebruiker. |
| App role (user) | `Dispatcher` | Gebruiker mag berichten versturen. |
| App role (user) | `Supervisor` | Gebruiker mag berichten intrekken (en versturen). |
| App role (application) | `Messages.Read.All` | Daemon mag alle berichten lezen. |
| App role (application) | `Messages.Send.All` | Daemon (bv. planningssysteem) mag berichten versturen. |

### 4.4 Autorisatiepolicies

| Policy | Regel |
|---|---|
| *FallbackPolicy* | Elke endpoint vereist een geauthenticeerde gebruiker, tenzij expliciet `AllowAnonymous` (**deny by default**). |
| `CanReadMessages` | (`scp` ∋ `Messages.Read`) **of** (`roles` ∋ `Messages.Read.All` **en** geen `scp`). |
| `CanSendMessages` | (`scp` ∋ `Messages.Send` **en** `roles` ∋ `Dispatcher` of `Supervisor`) **of** (`roles` ∋ `Messages.Send.All` **en** geen `scp`). |
| `CanCancelMessages` | `scp` ∋ `Messages.Send` **en** `roles` ∋ `Supervisor`. |

De "**en** geen `scp`"-check voorkomt dat een gebruiker-token met toevallig een
applicatierol als daemon wordt behandeld. Policies worden in één plek gedefinieerd
(`Api/Security/AuthorizationPolicies.cs`) en per endpoint toegepast.

### 4.5 Persona's in de DevTokenIssuer

| Persona | Soort | `scp` | `roles` | Verwacht |
|---|---|---|---|---|
| `dispatcher` | delegated | `Messages.Read Messages.Send` | `Dispatcher` | Lezen + versturen, niet intrekken. |
| `supervisor` | delegated | `Messages.Read Messages.Send` | `Supervisor` | Alles. |
| `reader` | delegated | `Messages.Read` | — | Alleen lezen. |
| `planning-system` | app-only | — | `Messages.Send.All` | Alleen versturen. |
| `wrong-audience` | delegated | alles | `Supervisor` | `aud` klopt niet → 401. |
| `other-tenant` | delegated | alles | `Supervisor` | `tid`/`iss` klopt niet → 401. |
| `expired` | delegated | alles | `Supervisor` | Verlopen → 401. |

Deze persona's vormen samen de **autorisatiematrix** die in de integratietests volledig
wordt getest (elke endpoint × elke persona).

---

## 5. Security-by-design en privacy-by-design

### 5.1 Security-by-design: maatregelen en waar ze zitten

| Principe | Maatregel | Plek |
|---|---|---|
| Deny by default | FallbackPolicy vereist authenticatie | `Program.cs` |
| Least privilege | Fijnmazige scopes/rollen, aparte policy per actie | `AuthorizationPolicies.cs` |
| Token-validatie | `iss`, `aud`, `exp`, `nbf`, handtekening, `tid` | `AuthenticationSetup.cs` |
| Inputvalidatie | Lengtes, enums, verplichte velden; aan de rand (DTO) **en** in het domein | DTO-validatie + `Message.Create` |
| Geen informatielek | ProblemDetails zonder stacktrace; generieke 500 | Exception handler |
| Misbruik beperken | Rate limiting op `POST /messages` | ASP.NET Core rate limiter |
| Transport | HTTPS-redirect + HSTS (buiten Development) | `Program.cs` |
| Secrets | Geen secrets in repo; dev-sleutel wordt runtime gegenereerd | DevTokenIssuer; inplugpunt Key Vault |
| Supply chain | Vulnerable-package-check in CI; Dependabot | `ci.yml`, `.github/dependabot.yml` |
| Security headers | `X-Content-Type-Options`, `Cache-Control: no-store` op API-responses | Middleware |

### 5.2 Mini-dreigingsmodel (STRIDE)

| Dreiging | Voorbeeld | Mitigatie |
|---|---|---|
| **S**poofing | Vervalst token | Handtekening + `iss`/`aud`/`tid`-validatie |
| **T**ampering | Bericht wijzigen na verzending | Geen update-endpoint; alleen intrekken |
| **R**epudiation | "Ik heb dat niet verstuurd" | `SenderId` (oid) + auditlog-regel bij verzenden/intrekken |
| **I**nformation disclosure | Telefoonnummers in logs | Alleen pseudoniem `RecipientId`; log-redactie |
| **D**enial of service | Massaal berichten versturen | Rate limiting; max. lengtes |
| **E**levation of privilege | App met scope stuurt namens elke gebruiker | Combinatie scope + app role vereist |

### 5.3 Privacy-by-design

- **Dataminimalisatie:** het domein kent alleen `RecipientId` (pseudoniem). Contactgegevens
  (telefoon, push-token) worden pas in de **Infrastructure-adapter** opgehaald, op het moment
  van versturen, en nooit opgeslagen in `Message` of `Delivery`.
- **Geen persoonsgegevens in logs:** logregels bevatten alleen technische ID's
  (`MessageId`, aantallen, statussen). **Nooit** de berichtinhoud, ontvangerlijst of
  contactgegevens. Structured logging met vaste templates; een unit test controleert dat
  gevoelige velden niet in logs terechtkomen (via een test-logger).
- **Geen echte testdata:** fake directory gebruikt verzonnen ID's (`EMP-0001`).
- **Doelbinding & bewaartermijn:** `IRetentionPolicy` als inplugpunt; documenteer dat
  berichten na X dagen worden geanonimiseerd/verwijderd.
- **Auditability zonder PII:** auditregel = wie (`oid`), wat (actie), welk bericht (ID),
  wanneer. Geen inhoud.
- **Response-minimalisatie:** `GET /deliveries` toont pseudoniem ID + kanaal + status,
  geen contactgegevens.

---

## 6. Teststrategie

### 6.1 Testpiramide

```
              ▲  Acceptatie (Reqnroll, Gherkin NL)   — weinig, op businessregels
             ▲▲  Contract (OpenAPI + event schema)   — bewaakt de grenzen
            ▲▲▲  Integratie (WebApplicationFactory)  — HTTP + auth + DI echt
          ▲▲▲▲▲  Unit (Domain + Application)          — veel, snel, geen I/O
                 + Architectuur (NetArchTest)         — dependency rule
```

### 6.2 Unit tests

- **Domain:** elke bedrijfsregel BR-01..07, positief én negatief. Testnaam verwijst naar
  de regel: `Create_UrgentWithOneChannel_FailsWithBR03`.
- **Application:** use cases met fakes voor ports (`FakeClock`, `InMemoryRepository`,
  `FakeRecipientDirectory`, `RecordingEventPublisher`). Controleer bijvoorbeeld dat bij
  BR-05 **geen** dispatch en **geen** event plaatsvindt.
- Geen I/O, geen netwerk, deterministisch (tijd via `IClock`).

### 6.3 Integratietests

- `WebApplicationFactory<Program>` start de echte API in-process.
- Auth: de factory vervangt de JwtBearer-configuratie zodat tokens van `DevTokenFactory`
  (zelfde library als de issuer) worden geaccepteerd **zonder** netwerk. Er wordt dus
  geen auth-bypass gebruikt: tokens worden echt gevalideerd.
- **Autorisatiematrix:** `[Theory]` over endpoint × persona → verwachte status
  (200/201/204/401/403).
- Happy flow: aanmaken → ophalen → deliveries → intrekken.
- Foutvertaling: 400 (met `BR-xx`-code), 404, 409, 422.

### 6.4 Acceptatietests

Gherkin in het Nederlands (`# language: nl`), één feature per onderwerp. Voorbeeld:

```gherkin
# language: nl
Functionaliteit: Urgente berichten versturen
  Als dispatcher
  wil ik urgente berichten via meerdere kanalen versturen
  zodat medewerkers het bericht zeker ontvangen

  Scenario: Urgent bericht via twee kanalen wordt verstuurd
    Gegeven ik ben ingelogd als "dispatcher"
    En er zijn 3 medewerkers met rolgroep "Driver" in regio "REGION-NORTH"
    Als ik een urgent bericht verstuur via de kanalen "Push,Sms" aan rolgroep "Driver" in regio "REGION-NORTH"
    Dan is het bericht verstuurd
    En zijn er 6 afleveringen aangemaakt

  Scenario: Urgent bericht via één kanaal wordt geweigerd (BR-03)
    Gegeven ik ben ingelogd als "dispatcher"
    Als ik een urgent bericht verstuur via de kanalen "Push" aan rolgroep "Driver" in regio "REGION-NORTH"
    Dan wordt het verzoek geweigerd met regel "BR-03"
```

De step definitions praten via HTTP met de API (`WebApplicationFactory`), zodat de
acceptatietest het gedrag van buitenaf bewijst. Elke regel uit §1.2 heeft minimaal één
positief en één negatief scenario: dat is de **traceerbaarheid** requirement → test.

### 6.5 Contracttests

1. **API-contract:** test haalt `/openapi/v1.json` op uit de draaiende testserver en
   vergelijkt met `contracts/openapi.v1.json`. Wijkt het af, dan faalt de test met een
   duidelijke diff en instructie: "bewuste wijziging? werk het contract bij en benoem het
   in de PR". Zo is elke API-wijziging zichtbaar in review.
2. **Event-contract:** een `MessageDispatched`-event wordt geserialiseerd en gevalideerd
   tegen `contracts/events/message-dispatched.v1.schema.json`.
3. In de README wordt uitgelegd hoe **consumer-driven** contracttesten (bv. Pact) dit verder
   zou brengen: de consument legt vast wat hij verwacht en de provider verifieert dat.

### 6.6 Architectuurtests

- `Domain` heeft geen dependency op `Application`, `Infrastructure`, `Api` of ASP.NET.
- `Application` heeft geen dependency op `Infrastructure` of `Api`.
- Klassen die een port implementeren staan in `Infrastructure`.

---

## 7. CI/CD en kwaliteitsgates

GitHub Actions workflow `.github/workflows/ci.yml`, triggers: `pull_request` en `push`
naar `main`.

| # | Gate | Breekt de build als... |
|---|---|---|
| 1 | Restore + build (`TreatWarningsAsErrors`) | er een compiler- of analyzerwaarschuwing is |
| 2 | `dotnet format --verify-no-changes` | code niet volgens `.editorconfig` is |
| 3 | Architectuurtests | de dependency rule wordt geschonden |
| 4 | Unit tests + **coverage-drempel** (Domain + Application ≥ 80% line) | een test faalt of coverage te laag is |
| 5 | Integratietests | een test faalt (incl. autorisatiematrix) |
| 6 | Contracttests | API- of eventcontract onbedoeld wijzigt |
| 7 | Acceptatietests | een businessregel niet meer klopt |
| 8 | `dotnet list package --vulnerable --include-transitive` | er een bekende kwetsbaarheid in een package zit |
| 9 | (optioneel) CodeQL | statische security-analyse bevindingen geeft |

Resultaten (test-rapporten, coverage) worden als artifact geüpload. In de README staat
hoe je in GitHub **branch protection** instelt, zodat `main` alleen via een PR met groene
checks kan worden bijgewerkt: dát maakt de gates echt verplicht.

**CD (alleen beschreven, niet gebouwd):** na groene CI → container image bouwen → push naar
registry → deploy naar test-omgeving (Kubernetes) → smoke test op `/health/ready` →
handmatige approval → productie. Rollback = vorige image-tag terugzetten. Dit staat als
uitgecommentarieerde job met `REAL-IMPLEMENTATION`-markers in `ci.yml`.

---

## 8. Inplugpunten voor echte implementaties

Elke plek waar een echte implementatie hoort, is in de code gemarkeerd met:

```csharp
// REAL-IMPLEMENTATION: <wat> — <hoe / welke technologie>
```

Zoek ze allemaal met: `grep -rn "REAL-IMPLEMENTATION" src tools .github`

| Port / plek | Nu (oefen) | Echte implementatie |
|---|---|---|
| `IMessageRepository` | `InMemoryMessageRepository` (ConcurrentDictionary) | EF Core + Azure SQL / PostgreSQL; migraties; optimistic concurrency |
| `IRecipientDirectory` | `FakeRecipientDirectory` (vaste lijst) | HTTP-client naar personeels-/roosterdienst, met `Microsoft.Extensions.Http.Resilience` (retry, timeout, circuit breaker) |
| `IChannelDispatcher` | `SimulatedChannelDispatcher` (logt, configureerbare faalkans) | Per kanaal een adapter: push (Notification Hubs), SMS-provider, e-mail; retry + fallback naar ander kanaal |
| `IEventPublisher` | `LoggingEventPublisher` | Azure Service Bus topic, met **outbox-pattern** zodat opslaan en publiceren atomair zijn |
| `IClock` | `SystemClock` | (blijft) — bestaat voor testbaarheid |
| `ICurrentUser` | `HttpContextCurrentUser` (leest `oid` uit claims) | (blijft) |
| `IRetentionPolicy` | niet geïmplementeerd | Achtergrondjob die oude berichten anonimiseert |
| Authenticatie | `Authority` = DevTokenIssuer | `Authority` = `https://login.microsoftonline.com/{tenantId}/v2.0`, `Audience` = `api://{clientId}`; of `Microsoft.Identity.Web` (`AddMicrosoftIdentityWebApi`) |
| Secrets | runtime gegenereerde RSA-sleutel | Azure Key Vault + Managed Identity |
| Observability | console logging | OpenTelemetry → Azure Monitor / Application Insights (traces, metrics, logs) |
| Hosting | `dotnet run` | Container + Kubernetes (liveness/readiness probes op `/health/*`) |

Omdat alle infrastructuur achter interfaces uit de Application-laag zit, betekent een
echte implementatie: **nieuwe adapterklasse in Infrastructure + één regel in de
DI-registratie**. Domein, use cases en de meeste tests blijven ongewijzigd. Dat is het hele
punt van Clean Architecture.

---

## 9. Configuratie

`appsettings.Development.json` van de API:

```json
{
  "Authentication": {
    // REAL-IMPLEMENTATION: Entra ID → "https://login.microsoftonline.com/<tenantId>/v2.0"
    "Authority": "http://localhost:5100/00000000-0000-0000-0000-000000000001/v2.0",
    "Audience": "api://opsmessaging",
    "TenantId": "00000000-0000-0000-0000-000000000001",
    "RequireHttpsMetadata": false
  }
}
```

Poorten: API `https://localhost:5001` / `http://localhost:5000`, DevTokenIssuer
`http://localhost:5100`.

---

## 10. Wat je hiermee in een gesprek kunt laten zien

| Onderwerp | Waar te vinden |
|---|---|
| Domein los van infra | `src/OpsMessaging.Domain` heeft nul references; architectuurtest bewijst het |
| Bedrijfsregel → code → test | BR-03 in `Message.Create` → unit test → Gherkin-scenario |
| Scopes vs. app roles | `AuthorizationPolicies.cs` + autorisatiematrix-test |
| Entra ID zonder tenant | DevTokenIssuer + alleen-configuratie-wissel |
| Privacy-by-design | `RecipientId`, log-redactietest, DTO-mapping |
| Kwaliteitsgates | `ci.yml`, elke stap een gate |
| Event-driven | `MessageDispatched` + event-contract + outbox-inplugpunt |
