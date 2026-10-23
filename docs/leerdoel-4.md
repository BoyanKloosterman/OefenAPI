# Leerdoel 4

Op 23 oktober 2026 heb ik OefenAPI met ZAP 2.17.0 getest. De app draaide alleen op 127.0.0.1:5080 en SecureMode stond op false. Via MCP in Cursor gaf zap_version 2.17.0 terug, dus die koppeling deed het.

Ik heb in ZAP een context OefenApi gemaakt, alleen voor http://127.0.0.1:5080. De OpenAPI-spec kwam van http://127.0.0.1:5080/openapi/v1.json, met dezelfde host als target. In de Sites-lijst stonden daarna de paden van de API, onder andere /api/login en /api/clients/1. Bij /api/admin/users/10 heeft ZAP zelf een voorbeeld-id gekozen.

De gewone requests gingen via de proxy op 127.0.0.1:8080, met de header X-Test-Run: leerdoel-4. Ik logde in als account-a, haalde mijn eigen client op, zocht op Kaas, haalde mijn eigen user op, deed een gewone PUT en vroeg bulkdata met pageSize 20. In de History zijn dat de nummers 37 tot en met 42. Allemaal status 200.

Daarna een spider (spider-0) en een actieve scan (ascan-0), allebei alleen op die context. Allebei eindigden op 100%. Na de scan stonden er 3668 regels in de History.

ZAP meldde SQL injection op GET /api/clients, parameter search. Alert 40018, risico High, confidence Low, History 896, om 13:12. Het antwoord was een 500. In de body stond een SQLite-fout met een stacktrace. Een aparte melding voor die stacktrace kwam er niet. Verder miste X-Content-Type-Options op 15 responses (Low, plugin 10021). Kestrel stond er alleen als informational.

De andere punten zaten niet tussen de alerts. Die heb ik daarna zelf bekeken, nog steeds via de proxy, nu met X-Test-Run: leerdoel-4-hand.

Als account-a kreeg ik client 3 terug (History 3685, status 200). Die client heeft ownerId 2 en hoort dus bij account-b.

Een PUT op user 2 waarin ik role meegaf, gaf 200 en role admin terug (History 3686). Ik was op dat moment account-a.

Twee foute logins gaven allebei 401, maar niet dezelfde tekst. Bij een onbekende gebruiker stond "Onbekende gebruiker." (History 3682). Bij een fout wachtwoord voor account-a stond "Onjuist wachtwoord." (History 3683). Een 429 kwam niet terug.

pageSize 500 op /api/bulkdata gaf 200. In het antwoord stond pageSize 500 en alle 250 regels (History 3687).

DELETE /api/admin/users/2 als account-a gaf 204 (History 3688). Account-b is daarmee weg tot de app opnieuw start. Bij een herstart wordt de database weer gevuld.

Stuurde ik een Origin-header mee, dan werd Access-Control-Allow-Origin *, History 3690. Zonder die header zet de app de CORS-header niet, en ZAP heeft hem niet gemeld. De Server-header was Kestrel. De missende nosniff-header had ZAP wel gevonden.

| Punt | Endpoint | Door ZAP | Zelf nagelopen |
| --- | --- | --- | --- |
| API1 | GET /api/clients/{id} | nee | ja, History 3685, status 200 voor een client van account-b |
| API3 | PUT /api/users/{id} | nee | ja, History 3686, role werd admin |
| API2 | POST /api/login | nee | ja, History 3682 en 3683, twee verschillende 401-teksten |
| API4 | GET /api/bulkdata | nee | ja, History 3687, pageSize 500 werd geaccepteerd |
| API5 | DELETE /api/admin/users/{id} | nee | ja, History 3688, status 204 zonder admin-rol |
| SQL-injectie | GET /api/clients?search= | ja, alert 40018, History 896 | de 500 bevatte ook een stacktrace, zonder aparte alert |
| API8 | headers, CORS, fouttekst | deels, alleen X-Content-Type-Options | CORS * in History 3690, Server-header Kestrel |

De veilige modus heb ik deze keer niet gescand.
