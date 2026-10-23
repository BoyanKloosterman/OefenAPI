# Testplan

| ID | Endpoint | Wat ik test | Verwachte uitkomst |
| --- | --- | --- | --- |
| 1 | POST /api/login | Inloggen als account-a | 200 en een bearer token |
| 2 | GET /api/clients/1 | Eigen client van account-a | 200, ownerId 1 |
| 3 | GET /api/clients?search=Kaas | Zoeken in eigen clients | 200, client 1 |
| 4 | GET /api/users/1 | Eigen user | 200, account-a |
| 5 | PUT /api/users/1 | Eigen gegevens, zonder role | 200, role blijft user |
| 6 | GET /api/bulkdata?page=1&pageSize=20 | Gewone pagina | 200, 20 regels |
| 7 | GET /api/clients?search= tijdens de actieve scan | Of ZAP een fout in search ziet | Bij SecureMode false een 500. Bij true geen 500 |
| 8 | POST /api/login | Onbekende gebruiker en fout wachtwoord | Bij false twee verschillende 401-teksten. Bij true allebei "Ongeldige inloggegevens." |
| 9 | POST /api/login | Te veel foute pogingen | Bij true een 429. Het script zelf doet daar te weinig pogingen voor |
| 10 | GET /api/clients/3 als account-a | Client van account-b | Bij false 200. Bij true 404 |
| 11 | PUT /api/users/2 met role admin, als account-a | Andermans user en het veld role | Bij false 200 en role admin. Bij true 404 |
| 12 | GET /api/bulkdata?page=1&pageSize=500 | Of pageSize een maximum heeft | Bij false 200 en pageSize 500. Bij true 400 |
| 13 | GET / met Origin https://example.test | CORS en security headers | Bij false Access-Control-Allow-Origin: *. Bij true die header niet, wel nosniff |
| 14 | DELETE /api/admin/users/2 als account-a | Admin-endpoint zonder admin-rol | Bij false 204. Bij true 403 |
