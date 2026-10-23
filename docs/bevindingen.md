# Bevindingen

De zeven punten hieronder komen uit de run met SecureMode false. Daarna heb ik dezelfde checks gedaan met SecureMode true. Screenshots heb ik niet. De CVSS-scores zijn mijn inschatting, geen score uit ZAP.

## Bevinding: client van een ander account op te vragen

- Endpoint: GET /api/clients/{id}
- OWASP-categorie: API1 Broken Object Level Authorization
- CVSS 3.1-vector: CVSS:3.1/AV:N/AC:L/PR:L/UI:N/S:U/C:H/I:N/A:N
- CVSS 3.1-score: 6.5
- Modus: SecureMode false. Bij true dicht.

### Stappen om te reproduceren

1. Log in als account-a.
2. Vraag GET /api/clients/3 op. Die client hoort bij account-b.

### Bewijs

- ZAP History-ID: 3306 bij false (200, ownerId 2). 7328 bij true (404, "Niet gevonden.").
- X-Request-Id: fa1cd39c-7105-4969-9ac4-e668bcde6e0c bij false. 6098a587-81f2-432a-96e9-20d7ef220018 bij true.
- Screenshot: geen.

### Advies

Geef een client alleen terug als ownerId gelijk is aan het ingelogde account. Anders 404.

## Bevinding: role van een andere user te zetten

- Endpoint: PUT /api/users/{id}
- OWASP-categorie: API3 Broken Object Property Level Authorization
- CVSS 3.1-vector: CVSS:3.1/AV:N/AC:L/PR:L/UI:N/S:U/C:N/I:H/A:N
- CVSS 3.1-score: 6.5
- Modus: SecureMode false. Bij true dicht.

### Stappen om te reproduceren

1. Log in als account-a.
2. Stuur PUT /api/users/2 met het veld role op admin.

### Bewijs

- ZAP History-ID: 3307 bij false (200, role admin). 7329 bij true (404).
- X-Request-Id: 6f65c036-8f9c-4d7e-99d9-f58f82f0146c bij false. 8ccbfc7f-7041-4662-a9d1-c0cc9dd50eb1 bij true.
- Screenshot: geen.

### Advies

Alleen je eigen user bijwerken, en role niet uit de body overnemen.

## Bevinding: login verraadt of de gebruiker bestaat

- Endpoint: POST /api/login
- OWASP-categorie: API2 Broken Authentication
- CVSS 3.1-vector: CVSS:3.1/AV:N/AC:L/PR:N/UI:N/S:U/C:L/I:N/A:N
- CVSS 3.1-score: 5.3
- Modus: SecureMode false. Bij true dezelfde fouttekst, en na te veel pogingen 429.

### Stappen om te reproduceren

1. POST /api/login met een gebruiker die niet bestaat.
2. POST /api/login voor account-a met een fout wachtwoord.
3. Bij SecureMode true daarna nog een paar foute pogingen.

### Bewijs

- ZAP History-ID: 3303 "Onbekende gebruiker." en 3304 "Onjuist wachtwoord." Bij true 7325 en 7326, allebei "Ongeldige inloggegevens." History 7340 is de 429.
- X-Request-Id: 2dd564c7-5b5d-47bb-9003-44021c1e9062 en ecb89d84-d406-4fb2-a0ce-4fdb93b828be bij false. 991a525a-0599-4bcf-9252-53c69b5faae1 en 65cf3d7f-acda-4b29-9171-0e8c7fff4dc0 bij true.
- Screenshot: geen.

### Advies

Eén fouttekst voor een mislukte login, en een limiet op het aantal pogingen.

## Bevinding: pageSize heeft geen maximum

- Endpoint: GET /api/bulkdata
- OWASP-categorie: API4 Unrestricted Resource Consumption
- CVSS 3.1-vector: CVSS:3.1/AV:N/AC:L/PR:L/UI:N/S:U/C:N/I:N/A:L
- CVSS 3.1-score: 4.3
- Modus: SecureMode false. Bij true dicht.

### Stappen om te reproduceren

1. Log in.
2. Vraag GET /api/bulkdata?page=1&pageSize=500 op.

### Bewijs

- ZAP History-ID: 3308 bij false (200, pageSize 500, total 250). 7330 bij true (400).
- X-Request-Id: 250eaf9f-1b16-4529-97dd-7e227283fffd bij false. b433a205-7b8e-44a0-b538-a389049501bd bij true.
- Screenshot: geen.

### Advies

Zet een maximum op pageSize, bijvoorbeeld 100, en geef 400 als het verzoek daaroverheen gaat.

## Bevinding: user verwijderen zonder admin-rol

- Endpoint: DELETE /api/admin/users/{id}
- OWASP-categorie: API5 Broken Function Level Authorization
- CVSS 3.1-vector: CVSS:3.1/AV:N/AC:L/PR:L/UI:N/S:U/C:N/I:H/A:H
- CVSS 3.1-score: 8.1
- Modus: SecureMode false. Bij true dicht.

### Stappen om te reproduceren

1. Log in als account-a. Die heeft role user.
2. Stuur DELETE /api/admin/users/2.

### Bewijs

- ZAP History-ID: 3310 bij false (204). 7332 bij true (403, "Geen toegang.").
- X-Request-Id: ec9d7f09-f091-4918-8bc3-4d7d8889b3c1 bij false. f31c288f-e45f-45b7-8656-86dae1924149 bij true.
- Screenshot: geen.

### Advies

Controleer of role admin is. Anders 403.

## Bevinding: search geeft een SQL-fout

- Endpoint: GET /api/clients?search=
- OWASP-categorie: SQL-injectie. ZAP noemt alert 40018.
- CVSS 3.1-vector: CVSS:3.1/AV:N/AC:L/PR:L/UI:N/S:U/C:L/I:N/A:N
- CVSS 3.1-score: 4.3
- Modus: SecureMode false. Bij true geen 500.

De score gaat over de foutmelding die ik zag, een 500 met een SQLite-fout. Ik heb geen data uit de database gehaald. ZAP zette de confidence op Low.

### Stappen om te reproduceren

1. Log in. Zonder token krijg je 401 en loopt de query niet.
2. Stuur GET /api/clients?search= met een quote in de zoekterm.

### Bewijs

- ZAP History-ID: 805 in scan-resultaten/secure-mode-false/active-scan-securemode-false.csv, 14:07:23, status 500, body 1681 bytes. Alert 40018 wijst naar History 814, die niet in die csv staat. Bij SecureMode true gaf dezelfde soort request 200, History 7335, geen SQLite-fout. Die staat in scan-resultaten/secure-mode-true/zap-history-securemode-true.csv.
- X-Request-Id: voor 805 niet genoteerd. Voor 7335 is dat 577b2afa-ed57-4bea-aa71-2ae465ff742d.
- Screenshot: geen. Het rapport van 14:23 heeft het 500-antwoord.

### Advies

Zet de zoekterm als parameter in de query, niet met stringconcatenatie.

## Bevinding: geen security headers, open CORS, stacktrace in een fout

- Endpoint: de responses van de API, en GET / met een Origin-header
- OWASP-categorie: API8 Security Misconfiguration
- CVSS 3.1-vector: CVSS:3.1/AV:N/AC:L/PR:N/UI:N/S:U/C:L/I:N/A:N
- CVSS 3.1-score: 5.3
- Modus: SecureMode false. Bij true dicht voor de headers en CORS. De stacktrace hoort bij de SQL-fout hierboven.

### Stappen om te reproduceren

1. Stuur GET / met header Origin: https://example.test.
2. Kijk of Access-Control-Allow-Origin: * terugkomt, en of X-Content-Type-Options ontbreekt.
3. Kijk bij een 500 of de body een stacktrace bevat.

### Bewijs

- ZAP History-ID: 3309, alert 10098, Medium. Het rapport van 14:23 telt X-Content-Type-Options 4 keer. Bij true is History 7331, zonder Access-Control-Allow-Origin, met nosniff, X-Frame-Options DENY en een Content-Security-Policy. Geen Server-header.
- X-Request-Id: 0a653a78-1d5e-489e-be2f-358f2994fade bij false. cb356ede-46f8-4c1f-8f11-b9ba73a27f02 bij true.
- Screenshot: geen.

### Advies

Zet de security headers, laat CORS niet op * staan, en geef bij een fout geen stacktrace terug.
