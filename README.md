# OefenApi

Lokale REST API om zelf met OWASP ZAP te oefenen. De data is nep. Er zitten geen echte namen, bedrijven of URL's in.

## Waarschuwing

Met `SecureMode` op `false` is deze API expres onveilig. Draai hem alleen op deze computer. Hij luistert alleen op `127.0.0.1:5080` en niet op het netwerk. Zet er geen echte gegevens in en maak hem niet bereikbaar voor anderen.

## Starten

In deze map:

```powershell
dotnet run --project OefenApi.csproj
```

De API staat daarna op http://127.0.0.1:5080.
De OpenAPI-spec staat op http://127.0.0.1:5080/openapi/v1.json.
`GET /` laat zien of de app draait en welke modus aan staat.

Bij elke start wordt `data/oefen.db` opnieuw gevuld. Wijzigingen en tokens zijn daarna weg.

## Modus

In `appsettings.json`:

```json
"SecureMode": false
```

`false` is de oefenstand. `true` is de stand zoals je die in een echte API zou willen. Sla het bestand op en start de app opnieuw. Importeer de OpenAPI-spec daarna opnieuw in ZAP, want die hoort bij de draaiende app.

Een token is 8 uur geldig, tot je de app opnieuw start.

## Testaccounts

| Gebruiker | Wachtwoord | Rol | User-id | Clients |
| --- | --- | --- | --- | --- |
| account-a | OefenAccountA! | user | 1 | 1 en 2 |
| account-b | OefenAccountB! | user | 2 | 3 en 4 |
| admin | OefenAdmin! | admin | 3 | geen |

Login:

```http
POST /api/login
Content-Type: application/json

{"username":"account-a","password":"OefenAccountA!"}
```

Het antwoord bevat `token`. Stuur dat mee als `Authorization: Bearer <token>`.

## Endpoints

- `POST /api/login`
- `GET /api/clients/{id}`
- `GET /api/clients?search=`
- `GET /api/users/{id}`
- `PUT /api/users/{id}`
- `GET /api/bulkdata?page=&pageSize=`
- `DELETE /api/admin/users/{id}`

## Logging

Elk antwoord krijgt de header `X-Request-Id`.

Elk request wordt een JSON-regel in `logs/requests-JJJJ-MM-DD.jsonl`. De datum is je lokale datum. In de regel staan:

- `utc` en `local`
- `method`
- `path` (zonder querystring)
- `statusCode`
- `account` (gebruikersnaam, of `null`)
- `requestId`
- `testRun` (de header `X-Test-Run`, of `null`)

Wachtwoorden, tokens en request-bodies komen niet in dit bestand. Het volledige request blijft in de ZAP History staan. Zo koppel je een History-regel aan een applog-regel via `X-Request-Id`.

## ZAP

1. Start deze API.
2. Start ZAP. De lokale proxy hoort op `127.0.0.1:8080` te staan. Dat is de standaard.
3. Importeer de spec: menu **Import**, daarna **Import an OpenAPI definition**. Kies een URL en plak `http://127.0.0.1:5080/openapi/v1.json`. Target URL: `http://127.0.0.1:5080` als ZAP daar om vraagt.
4. Sla de sessie op via **File > Persist Session**. Daarna blijft de sessie bewaard als je ZAP sluit.
5. Requests die via de proxy lopen, komen in de **History**-tab. De eerste kolom is het History-ID. Die gebruik je in `docs/testlog.md`.
6. Exporteer de History naar CSV met de exportknop in de werkbalk van de History-tab (pictogram met een pijl). Sla het bestand op als CSV. De kolom `Id` is het History-ID.

`scripts/requests.ps1` stuurt een paar gewone requests via die proxy, met de header `X-Test-Run`. ZAP moet dan aan staan, anders komt curl er niet door.

Voor requests die je in ZAP zelf opbouwt: haal eerst een token op via `POST /api/login` en zet de header `Authorization: Bearer <token>` erop.

## Vastleggen van je tests

- `docs/testplan.md` vul je in voordat je test.
- `docs/testlog.md` vul je tijdens het testen.
- `docs/bevindingen.md` is het sjabloon per bevinding.
