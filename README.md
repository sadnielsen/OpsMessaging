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
