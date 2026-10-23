# Testlog

Modus bij al deze regels: SecureMode false. Datum: 23-10-2026.

| Datum | Tijdstip | ZAP History-ID | Request | Modus | Verwacht | Werkelijk | Statuscode | Bewijs |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 23-10-2026 | 14:07 | 35 | POST /api/login als account-a | false | 200 | 200, token terug | 200 | X-Test-Run leerdoel-4, zap-history.csv |
| 23-10-2026 | 14:07 | 36 | GET /api/clients/1 | false | eigen client | client 1 | 200 | X-Test-Run leerdoel-4, zap-history.csv |
| 23-10-2026 | 14:07 | 37 | GET /api/clients?search=Kaas | false | eigen client | client 1 | 200 | X-Test-Run leerdoel-4, zap-history.csv |
| 23-10-2026 | 14:07 | 38 | GET /api/users/1 | false | eigen user | account-a | 200 | X-Test-Run leerdoel-4, zap-history.csv |
| 23-10-2026 | 14:07 | 39 | PUT /api/users/1 zonder role | false | eigen gegevens blijven | status 200 | 200 | X-Test-Run leerdoel-4, zap-history.csv |
| 23-10-2026 | 14:07 | 40 | GET /api/bulkdata?page=1&pageSize=20 | false | 20 regels | body 1628 bytes | 200 | X-Test-Run leerdoel-4, zap-history.csv |
| 23-10-2026 | 14:07:23 | 805 | GET /api/clients?search= tijdens actieve scan | false | ZAP kijkt naar search | 500, body 1681 bytes. Alert 40018 wijst naar History 814 | 500 | active-scan.csv, alert 40018 |
| 23-10-2026 | 14:22 | 3303 | POST /api/login, onbekende gebruiker | false | zelfde fouttekst als fout wachtwoord | tekst "Onbekende gebruiker." | 401 | X-Test-Run leerdoel-4-hand, scripts/handtests.ps1 |
| 23-10-2026 | 14:22 | 3304 | POST /api/login, fout wachtwoord account-a | false | zelfde fouttekst | tekst "Onjuist wachtwoord." | 401 | X-Test-Run leerdoel-4-hand, scripts/handtests.ps1 |
| 23-10-2026 | 14:22 | 3306 | GET /api/clients/3 als account-a | false | geen client van account-b | client 3, ownerId 2 | 200 | X-Test-Run leerdoel-4-hand, scripts/handtests.ps1 |
| 23-10-2026 | 14:22 | 3307 | PUT /api/users/2 met role, als account-a | false | role niet te wijzigen | role admin | 200 | X-Test-Run leerdoel-4-hand, scripts/handtests.ps1 |
| 23-10-2026 | 14:22 | 3308 | GET /api/bulkdata?page=1&pageSize=500 | false | pageSize begrensd | pageSize 500, total 250 | 200 | X-Test-Run leerdoel-4-hand, scripts/handtests.ps1 |
| 23-10-2026 | 14:22 | 3309 | GET / met Origin https://example.test | false | geen open CORS | Access-Control-Allow-Origin *, alert 10098 | 200 | X-Test-Run leerdoel-4-hand, scripts/handtests.ps1 |
| 23-10-2026 | 14:22 | 3310 | DELETE /api/admin/users/2 als account-a | false | 403 | user verwijderd | 204 | X-Test-Run leerdoel-4-hand, scripts/handtests.ps1 |
| 23-10-2026 | 14:34 | 7325 | POST /api/login, onbekende gebruiker | true | dezelfde fouttekst | "Ongeldige inloggegevens." | 401 | X-Test-Run leerdoel-4-hand, SecureMode true |
| 23-10-2026 | 14:34 | 7326 | POST /api/login, fout wachtwoord account-a | true | dezelfde fouttekst | "Ongeldige inloggegevens." | 401 | X-Test-Run leerdoel-4-hand, SecureMode true |
| 23-10-2026 | 14:34 | 7328 | GET /api/clients/3 als account-a | true | niet de client van account-b | "Niet gevonden." | 404 | X-Test-Run leerdoel-4-hand, SecureMode true |
| 23-10-2026 | 14:34 | 7329 | PUT /api/users/2 met role, als account-a | true | role niet te wijzigen | "Niet gevonden." | 404 | X-Test-Run leerdoel-4-hand, SecureMode true |
| 23-10-2026 | 14:34 | 7330 | GET /api/bulkdata?page=1&pageSize=500 | true | pageSize begrensd | "pageSize mag maximaal 100 zijn." | 400 | X-Test-Run leerdoel-4-hand, SecureMode true |
| 23-10-2026 | 14:34 | 7331 | GET / met Origin https://example.test | true | geen open CORS | geen Access-Control-Allow-Origin, wel nosniff | 200 | X-Test-Run leerdoel-4-hand, SecureMode true |
| 23-10-2026 | 14:34 | 7332 | DELETE /api/admin/users/2 als account-a | true | 403 | "Geen toegang." | 403 | X-Test-Run leerdoel-4-hand, SecureMode true |
| 23-10-2026 | 14:34 | 7335 | GET /api/clients?search= met een quote, account-a | true | geen 500 | 200, geen SQLite-fout | 200 | X-Test-Run leerdoel-4-veilig |
| 23-10-2026 | 14:34 | 7340 | extra foute logins na het script | true | 429 na te veel pogingen | "Te veel inlogpogingen. Wacht een minuut." | 429 | X-Test-Run leerdoel-4-veilig |
