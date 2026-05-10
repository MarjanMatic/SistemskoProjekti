# Sistemsko Projekti
Marjan Matić 19731
Miljan Milivojević 19745

Zadatak 23:
Kreirati Web server koji klijentu omogućava pribavljanje podataka o ukupnom broju commitova
od strane svih contributore-a na datom projektu uz pomoć Github API-a. Pretraga se može vršiti
pomoću filtera koji se definišu u okviru query-a. Podaci o ukupnom broju commit-ova se vraćaju
kao odgovor (pretragu vršiti po željenom repozitorijumu). Svi zahtevi serveru se šalju preko
browser-a korišćenjem GET metode. Ukoliko navedeni podaci za repozitorijum ne postoje,
prikazati grešku klijentu.
Način funkcionisanja GitHub API-a je moguće proučiti na sledećem linku:
https://docs.github.com/en/rest?apiVersion=2022-11-28
Primer poziva serveru: https://api.github.com/repos/OWNER/REPO/stats/contributors
