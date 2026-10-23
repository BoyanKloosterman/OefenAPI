# Antwoorden

Dit bestand zegt waar de zeven punten in de code zitten. Eerst zelf zoeken, daarna hier kijken.

`SecureMode` staat in `appsettings.json`. `false` betekent dat de punten hieronder open staan. `true` betekent dat ze dicht zijn. Na een wijziging de app opnieuw starten.

## 1. API1 Broken Object Level Authorization

- Endpoint: `GET /api/clients/{id}`
- Code: `Endpoints/ClientEndpoints.cs`, methode `GetById`
- Open: de client wordt teruggegeven zonder te kijken of `OwnerUserId` van het ingelogde account is.
- Dicht: als de client niet van dit account is, komt status 404 terug.

## 2. API3 Broken Object Property Level Authorization

- Endpoint: `PUT /api/users/{id}`
- Code: `Endpoints/UserEndpoints.cs`, methodes `UpdateAny` en `UpdateOwn`
- Open: `UpdateAny` werkt voor elk bestaand id. Het body-type `UserUpdate` heeft een veld `role` en dat wordt opgeslagen.
- Dicht: `UpdateOwn` werkt alleen voor je eigen id. Het body-type `UserProfileUpdate` heeft geen `role`. Extra JSON-velden worden genegeerd.

## 3. API2 Broken Authentication

- Endpoint: `POST /api/login`
- Code: `Endpoints/AuthEndpoints.cs`, methodes `Login` en `LoginError`. De limiet zit in `Program.cs`.
- Open: een onbekende gebruiker geeft `Onbekende gebruiker.` Een fout wachtwoord geeft `Onjuist wachtwoord.` Er is geen limiet.
- Dicht: beide geven `Ongeldige inloggegevens.` Meer dan 5 pogingen per minuut geeft status 429.

## 4. API4 Unrestricted Resource Consumption

- Endpoint: `GET /api/bulkdata`
- Code: `Endpoints/BulkEndpoints.cs`, methode `GetPage`. Het maximum in de spec zit in `OpenApi/OpenApiSetup.cs`.
- Open: `pageSize` heeft geen bovengrens.
- Dicht: `pageSize` boven 100 geeft status 400. In de OpenAPI-spec krijgt die parameter dan maximum 100.

## 5. API5 Broken Function Level Authorization

- Endpoint: `DELETE /api/admin/users/{id}`
- Code: `Endpoints/AdminEndpoints.cs`, methode `Delete`
- Open: er wordt alleen gekeken of het token geldig is.
- Dicht: `role` moet `admin` zijn, anders status 403.

Jezelf verwijderen geeft in beide modi status 409. Dat hoort niet bij dit punt.

## 6. SQL-injectie

- Endpoint: `GET /api/clients?search=`
- Code: `Data/AppDatabase.cs`, methode `SearchClients`, de tak waarin `secureMode` false is
- Open: de zoekterm wordt met stringconcatenatie in de SQL-tekst gezet.
- Dicht: de zoekterm gaat als parameter de query in.

De zoekopdracht blijft in beide modi beperkt tot de clients van het ingelogde account, zolang de query zelf heel blijft.

## 7. API8 Security Misconfiguration

- Code: `Program.cs` (headers, CORS en de Server-header) en `Errors/JsonErrorHandler.cs`
- Open: geen security headers, CORS staat open voor elke origin, een foutantwoord bevat de stacktrace en de Server-header blijft aan.
- Dicht: wel security headers, geen open CORS, een algemene fouttekst en geen Server-header.

`GET /api/users/{id}` mag in beide modi alleen je eigen user opvragen. Dat is geen van deze zeven punten.
