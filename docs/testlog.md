# Testlog

Modus bij al deze regels: SecureMode false. Datum: 23-10-2026.

| Datum | Tijdstip | ZAP History-ID | Request | Modus | Verwacht | Werkelijk | Statuscode | Bewijs |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 23-10-2026 | 13:11 | 37 | POST /api/login als account-a | false | 200 | 200, token terug | 200 | X-Test-Run leerdoel-4 |
| 23-10-2026 | 13:11 | 38 | GET /api/clients/1 | false | eigen client | client 1, ownerId 1 | 200 | X-Test-Run leerdoel-4 |
| 23-10-2026 | 13:11 | 39 | GET /api/clients?search=Kaas | false | eigen client | client 1 | 200 | X-Test-Run leerdoel-4 |
| 23-10-2026 | 13:11 | 40 | GET /api/users/1 | false | eigen user | account-a | 200 | X-Test-Run leerdoel-4 |
| 23-10-2026 | 13:11 | 41 | PUT /api/users/1 zonder role | false | eigen gegevens blijven | displayName Account A, role user | 200 | X-Test-Run leerdoel-4 |
| 23-10-2026 | 13:11 | 42 | GET /api/bulkdata?page=1&pageSize=20 | false | 20 regels | pageSize 20, total 250 | 200 | X-Test-Run leerdoel-4 |
| 23-10-2026 | 13:12 | 896 | GET /api/clients?search= tijdens actieve scan | false | ZAP kijkt naar search | alert 40018, 500 met SQLite-fout | 500 | alert 40018, confidence Low |
| 23-10-2026 | 14:22 | 3303 | POST /api/login, onbekende gebruiker | false | zelfde fouttekst als fout wachtwoord | tekst "Onbekende gebruiker." | 401 | X-Test-Run leerdoel-4-hand, scripts/handtests.ps1 |
| 23-10-2026 | 14:22 | 3304 | POST /api/login, fout wachtwoord account-a | false | zelfde fouttekst | tekst "Onjuist wachtwoord." | 401 | X-Test-Run leerdoel-4-hand, scripts/handtests.ps1 |
| 23-10-2026 | 14:22 | 3306 | GET /api/clients/3 als account-a | false | geen client van account-b | client 3, ownerId 2 | 200 | X-Test-Run leerdoel-4-hand, scripts/handtests.ps1 |
| 23-10-2026 | 14:22 | 3307 | PUT /api/users/2 met role, als account-a | false | role niet te wijzigen | role admin | 200 | X-Test-Run leerdoel-4-hand, scripts/handtests.ps1 |
| 23-10-2026 | 14:22 | 3308 | GET /api/bulkdata?page=1&pageSize=500 | false | pageSize begrensd | pageSize 500, total 250 | 200 | X-Test-Run leerdoel-4-hand, scripts/handtests.ps1 |
| 23-10-2026 | 14:22 | 3309 | GET / met Origin https://example.test | false | geen open CORS | Access-Control-Allow-Origin *, alert 10098 | 200 | X-Test-Run leerdoel-4-hand, scripts/handtests.ps1 |
| 23-10-2026 | 14:22 | 3310 | DELETE /api/admin/users/2 als account-a | false | 403 | user verwijderd | 204 | X-Test-Run leerdoel-4-hand, scripts/handtests.ps1 |
