# Leerdoel 4

Op 23 oktober 2026 heb ik OefenAPI met ZAP 2.17.0 getest. De app draaide alleen op 127.0.0.1:5080 en SecureMode stond op false. De MCP-server van ZAP luisterde op 127.0.0.1:8282.

Vanuit Cursor, via MCP, heb ik deze tools aangeroepen. zap_version gaf 2.17.0. zap_create_context maakte de context OefenApi, alleen voor http://127.0.0.1:5080. zap_start_spider op die context werd spider-0 en eindigde op 100%. zap_start_active_scan op dezelfde context werd ascan-0 en eindigde ook op 100%. De alerts heb ik daarna gelezen met zap_read_resource op zap://alerts. Die stappen heb ik niet in het ZAP-venster aangeklikt.

De OpenAPI-import ging niet via MCP. Die tool zit er niet bij. Ik heb de ZAP-API aangeroepen via de proxy op 127.0.0.1:8080, pad /JSON/openapi/action/importUrl/. De spec was http://127.0.0.1:5080/openapi/v1.json, de target dezelfde host, context-id 2. In de Sites-lijst stonden daarna de paden, onder andere /api/login en /api/clients/1. Bij /api/admin/users/10 heeft ZAP zelf een voorbeeld-id gekozen.

De gewone requests gingen via de proxy op 127.0.0.1:8080, met de header X-Test-Run: leerdoel-4. Ik logde in als account-a, haalde mijn eigen client op, zocht op Kaas, haalde mijn eigen user op, deed een gewone PUT en vroeg bulkdata met pageSize 20. In zap-history.csv zijn dat de nummers 35 tot en met 40, om 14:07:06. Allemaal status 200.

De spider-export staat in spider.csv. Dat zijn 18 URL's, allemaal Seed. De eerste regel is de kop. ZAP nam robots.txt en sitemap.xml zelf als startpunt. Nieuwe paden kwamen daar niet uit.

De actieve scan op de context is ascan-0. De export daarvan is active-scan.csv: 3176 requests, id 88 tot 3297, van 14:07:21 tot 14:07:28. zap-history.csv slaat die scanrequests over. Daar staan 25 regels in, met gaten, en het hoogste id is 3300.

In active-scan.csv geeft GET /api/clients?search= een 500, bijvoorbeeld id 805 om 14:07:23, body 1681 bytes. Alert 40018 (High, confidence Low) wijst naar History 814. Die id staat niet in de csv. De oude verwijzing naar History 896 van 13:12 hoort bij een eerdere sessie.

ZAP meldde SQL injection op GET /api/clients, parameter search. Alert 40018, risico High, confidence Low, gezet door de active scanner. In het rapport van 14:14 is het voorbeeld die request: 500, body 1681 bytes, 14:07:22. In active-scan.csv is dat id 805, om 14:07:23. X-Content-Type-Options telt het rapport 4 keer (Low, plugin 10021). Kestrel stond er alleen als informational.

De andere punten zaten niet tussen de alerts. Die heb ik in een eerdere sessie zelf bekeken, via de proxy, met X-Test-Run: leerdoel-4-hand. Die history-ids zitten niet in de huidige zap-history.csv.

Als account-a kreeg ik client 3 terug (History 3685, status 200). Die client heeft ownerId 2 en hoort dus bij account-b.

Een PUT op user 2 waarin ik role meegaf, gaf 200 en role admin terug (History 3686). Ik was op dat moment account-a.

Twee foute logins gaven allebei 401, maar niet dezelfde tekst. Bij een onbekende gebruiker stond "Onbekende gebruiker." (History 3682). Bij een fout wachtwoord voor account-a stond "Onjuist wachtwoord." (History 3683). Een 429 kwam niet terug.

pageSize 500 op /api/bulkdata gaf 200. In het antwoord stond pageSize 500 en alle 250 regels (History 3687).

DELETE /api/admin/users/2 als account-a gaf 204 (History 3688). Account-b is daarmee weg tot de app opnieuw start. Bij een herstart wordt de database weer gevuld.

Het rapport van 14:14 heeft geen CORS-alert. Alert 10098 stond in het oude rapport van 13:25, op History 3690. Dat bestand is vervangen. Die id zit niet in de csv van deze run, en active-scan.csv stuurt geen Origin-header mee.

| Punt | Endpoint | Door ZAP | Zelf nagelopen |
| --- | --- | --- | --- |
| API1 | GET /api/clients/{id} | nee | ja, History 3685, status 200 voor een client van account-b |
| API3 | PUT /api/users/{id} | nee | ja, History 3686, role werd admin |
| API2 | POST /api/login | nee | ja, History 3682 en 3683, twee verschillende 401-teksten |
| API4 | GET /api/bulkdata | nee | ja, History 3687, pageSize 500 werd geaccepteerd |
| API5 | DELETE /api/admin/users/{id} | nee | ja, History 3688, status 204 zonder admin-rol |
| SQL-injectie | GET /api/clients?search= | ja, alert 40018. In active-scan.csv id 805, 500. De alert wijst naar History 814, die niet in de csv staat | de body van 805 is 1681 bytes. Geen aparte stacktrace-alert in de csv |
| API8 | headers, CORS, fouttekst | deels. Het rapport van 14:14 telt X-Content-Type-Options 4 keer. Geen CORS-alert | Server-header Kestrel. 10098 zat alleen in het vervangen rapport van 13:25 |

De veilige modus heb ik deze keer niet gescand.

De History-export staat in zap-history.csv. Die heb ik niet gefilterd, maar het is ook niet de hele History. Er staan 25 regels in. De ids springen, onder andere van 40 naar 86 en van 259 naar 3298. De actieve scan zit er niet in. Die staat in active-scan.csv.

In zap-history.csv staan tien POSTs naar 127.0.0.1:8282: 15, 16, 17, 18, 86, 87, 259, 3298, 3299 en 3300. De csv geeft geen toolnaam. 16 heeft status 202, de rest 200. ZAP zet de tijd op 1 januari 1970. Dat is de export, niet het tijdstip van de call. De eerdere zin over alleen id 3691 klopt niet meer voor dit bestand.

Het alert-rapport staat in 2026-10-23-ZAP-Report-.html. ZAP maakte het op 14:14:46, versie 2.17.0. Het login-token stond er weer in. Dat heb ik zelf vervangen door [weggelaten]. De rest van het rapport is niet aangepast.
