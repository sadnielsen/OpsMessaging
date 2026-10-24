# OpsMessaging

Een kleine, complete backendservice in C#/.NET 10 voor het versturen van operationele
berichten naar medewerkers in het veld (doelgroepbepaling, routing, dispatch, tracking).

Dit is een **leerproject** om de volgende onderwerpen concreet in code te zien:

- Clean / hexagonale architectuur (domein los van infrastructuur)
- Unit-, integratie-, contract- en acceptatietests
- Kwaliteitsgates in CI/CD (GitHub Actions)
- Security-by-design en privacy-by-design
- OAuth2/OIDC met Entra ID-achtige tokens: scopes, app roles en API-beveiliging
  (nagebootst met een lokale tokenprovider, geen Azure-tenant nodig)

## Hoe start je het

Benodigd: .NET 10 SDK (versie vastgelegd in `global.json`).

```bash
dotnet build      # bouwen; elke waarschuwing is een fout
dotnet test       # alle tests, inclusief de architectuurtests
dotnet format     # code formatteren volgens .editorconfig
```

De API en de lokale tokenprovider worden in latere fasen gebouwd. Daarna start je ze met:

```bash
dotnet run --project tools/OpsMessaging.DevTokenIssuer   # lokale "Entra ID" op :5100 (alleen lokaal!)
dotnet run --project src/OpsMessaging.Api                # API op :5000/:5001
```

## Waar begin je met lezen

1. [`DESIGN.md`](DESIGN.md) §1 en §2: het domein en de lagen (Clean Architecture).
2. De projectbestanden in `src/`: de `ProjectReference`s laten zien welke kant de
   afhankelijkheden op wijzen (alleen naar binnen, naar `Domain`).
3. [`tests/OpsMessaging.ArchitectureTests/DependencyRuleTests.cs`](tests/OpsMessaging.ArchitectureTests/DependencyRuleTests.cs):
   hoe die regel automatisch wordt afgedwongen.
4. [`Directory.Build.props`](Directory.Build.props), [`Directory.Packages.props`](Directory.Packages.props)
   en [`.editorconfig`](.editorconfig): de kwaliteitsinstellingen die voor elk project gelden.

Na de laatste fase staat er een volledige leesroute in `docs/WALKTHROUGH.md`.

## Projectstructuur

| Map | Inhoud |
|---|---|
| `src/OpsMessaging.Domain` | Domeinmodel en bedrijfsregels. Geen enkele dependency. |
| `src/OpsMessaging.Application` | Use cases en ports (interfaces naar de buitenwereld). |
| `src/OpsMessaging.Infrastructure` | Adapters die de ports implementeren (in-memory, gesimuleerd). |
| `src/OpsMessaging.Api` | HTTP-endpoints en composition root. |
| `tools/OpsMessaging.DevAuth` | Maakt Entra ID-achtige tokens (alleen lokaal en in tests). |
| `tools/OpsMessaging.DevTokenIssuer` | Mini-OIDC-provider voor lokaal gebruik. |
| `tests/` | Unit-, integratie-, contract-, acceptatie- en architectuurtests. |

## Documenten

| Bestand | Inhoud |
|---|---|
| [`DESIGN.md`](DESIGN.md) | Wat er gebouwd wordt en waarom: domein, architectuur, API, security, teststrategie, CI. |
| [`CLAUDE.md`](CLAUDE.md) | Hoe Claude Code het bouwt: conventies, commentaarstijl en de bouwfasen 0 t/m 8. |

## Bouwen met Claude Code

1. Clone deze repository en open een terminal in de map.
2. Start Claude Code (`claude`).
3. Vraag: *"Lees CLAUDE.md en DESIGN.md en voer fase 0 uit."*
4. Lees na elke fase de genoemde bestanden, stel vragen, en ga dan door met de volgende fase.

Benodigd: .NET 10 SDK.

## Zoeken in de code

```bash
grep -rn "CONCEPT (" src tools tests              # uitleg per onderwerp
grep -rn "REAL-IMPLEMENTATION" src tools .github  # waar een echte implementatie ingeplugd wordt
```
