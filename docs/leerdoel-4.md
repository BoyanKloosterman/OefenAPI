# Leerdoel 4

Op 23 oktober 2026 heb ik OefenAPI met ZAP 2.17.0 getest. De app draaide alleen op 127.0.0.1:5080 en SecureMode stond op false. De MCP-server van ZAP luisterde op 127.0.0.1:8282.

Vanuit Cursor, via MCP, heb ik deze tools aangeroepen. zap_version gaf 2.17.0. zap_create_context maakte de context OefenApi, alleen voor http://127.0.0.1:5080. zap_start_spider op die context werd spider-0 en eindigde op 100%. zap_start_active_scan op dezelfde context werd ascan-0 en eindigde ook op 100%. De alerts heb ik daarna gelezen met zap_read_resource op zap://alerts. Die stappen heb ik niet in het ZAP-venster aangeklikt.

De OpenAPI-import ging niet via MCP. Die tool zit er niet bij. Ik heb de ZAP-API aangeroepen via de proxy op 127.0.0.1:8080, pad /JSON/openapi/action/importUrl/. De spec was http://127.0.0.1:5080/openapi/v1.json, de target dezelfde host, context-id 2. In de Sites-lijst stonden daarna de paden, onder andere /api/login en /api/clients/1. Bij /api/admin/users/10 heeft ZAP zelf een voorbeeld-id gekozen.

De gewone requests gingen via de proxy op 127.0.0.1:8080, met de header X-Test-Run: leerdoel-4. Ik logde in als account-a, haalde mijn eigen client op, zocht op Kaas, haalde mijn eigen user op, deed een gewone PUT en vroeg bulkdata met pageSize 20. In zap-history.csv zijn dat de nummers 35 tot en met 40, om 14:07:06. Allemaal status 200.

De spider-export staat in spider.csv. Dat zijn 18 URL's, allemaal Seed. De eerste regel is de kop. ZAP nam robots.txt en sitemap.xml zelf als startpunt. Nieuwe paden kwamen daar niet uit. In deze run gaf de spider dezelfde 18 URL's, dus spider.csv is niet veranderd.

De actieve scan op de context is ascan-0. De export daarvan is active-scan.csv: 3176 requests, id 88 tot 3297, van 14:07:21 tot 14:07:28. ZAP toont de requests van de spider en de scan niet in het History-tabblad. Daarom springen de nummers.

In active-scan.csv geeft GET /api/clients?search= een 500, bijvoorbeeld id 805 om 14:07:23, body 1681 bytes. Alert 40018 (High, confidence Low) wijst naar History 814. Die id staat niet in de csv.

ZAP meldde SQL injection op GET /api/clients, parameter search. Alert 40018, risico High, confidence Low, gezet door de active scanner. In het rapport van 14:23 is het voorbeeld die request: 500, body 1681 bytes, 14:07:22. In active-scan.csv is dat id 805, om 14:07:23. X-Content-Type-Options telt het rapport 4 keer (Low, plugin 10021). Kestrel stond er alleen als informational.

De andere punten zaten niet tussen de alerts van de scan. Die heb ik daarna met scripts/handtests.ps1 gedaan, via de proxy, header X-Test-Run: leerdoel-4-hand, om 14:22. De ids hieronder staan in zap-history.csv.

Twee foute logins gaven allebei 401, maar niet dezelfde tekst. Een onbekende gebruiker kreeg "Onbekende gebruiker." (History 3303). Een fout wachtwoord bij account-a kreeg "Onjuist wachtwoord." (History 3304). Een 429 kwam niet terug.

Als account-a kreeg ik client 3 terug (History 3306, status 200). Die client heeft ownerId 2 en hoort dus bij account-b.

Een PUT op user 2 met alleen role admin gaf 200 en role admin terug (History 3307). Ik was account-a.

pageSize 500 op /api/bulkdata gaf 200. In het antwoord stond pageSize 500 en total 250 (History 3308).

GET / met Origin https://example.test gaf Access-Control-Allow-Origin: * (History 3309). De passieve scanner zette daar alert 10098 op, Cross-Domain Misconfiguration, Medium. Die alert staat in het rapport van 14:23. De active scan stuurde geen Origin-header mee.

DELETE /api/admin/users/2 als account-a gaf 204 (History 3310). Account-b is daarmee weg tot de app opnieuw start.

| Punt | Endpoint | Door ZAP | Zelf nagelopen |
| --- | --- | --- | --- |
| API1 | GET /api/clients/{id} | nee | ja, History 3306, status 200, ownerId 2 |
| API3 | PUT /api/users/{id} | nee | ja, History 3307, role werd admin |
| API2 | POST /api/login | nee | ja, History 3303 en 3304, twee verschillende 401-teksten |
| API4 | GET /api/bulkdata | nee | ja, History 3308, pageSize 500, total 250 |
| API5 | DELETE /api/admin/users/{id} | nee | ja, History 3310, status 204 zonder admin-rol |
| SQL-injectie | GET /api/clients?search= | ja, alert 40018. In active-scan.csv id 805, 500. De alert wijst naar History 814, die niet in de csv staat | de body van 805 is 1681 bytes. Geen aparte stacktrace-alert in de csv |
| API8 | headers, CORS, fouttekst | deels. Het rapport van 14:23 telt X-Content-Type-Options 4 keer. CORS-alert 10098 staat op History 3309 | Server-header Kestrel. De scan stuurde geen Origin-header mee |

De veilige modus heb ik deze keer niet gescand.

De History-export staat in zap-history.csv. Er staan 56 regels in, tot en met id 3331. ZAP toont de requests van de spider en de scan niet in het History-tabblad. Daarom springen de nummers, van 40 naar 86 en van 259 naar 3298. De scanrequests staan in active-scan.csv.

In zap-history.csv staan 33 POSTs naar 127.0.0.1:8282: 15 tot en met 18, 86, 87, 259, 3298 tot en met 3302, en 3311 tot en met 3331. De csv geeft geen toolnaam. 16 heeft status 202, de rest 200. ZAP zet de tijd op 1 januari 1970. Dat is de export, niet het tijdstip van de call.

Het alert-rapport staat in 2026-10-23-ZAP-Report-.html. ZAP maakte het op 14:23:48, versie 2.17.0. Het login-token stond er twee keer voluit in. Dat heb ik zelf vervangen door [weggelaten]. De rest van het rapport is niet aangepast.
