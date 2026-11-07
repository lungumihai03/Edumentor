# EduMentor AI Lab Moldova — Platformă locală

Această versiune extinde MVP-ul cu autentificare, roluri, cohorte, materiale și moderarea practicilor.

## Funcționalități incluse
- Conturi și autentificare JWT: Administrator, Profesor, Elev/Student.
- Înregistrare publică pentru profesori; conturile elevilor sunt create de profesori când îi adaugă într-o cohortă.
- Un profesor poate crea mai multe cohorte și poate adăuga elevi/studenți.
- Un student se poate afla în cohorte ale profesorilor diferiți, folosind același cont de email.
- Încărcare fișiere pentru profesor sau pentru o cohortă; descărcarea este autorizată.
- Bibliotecă publică doar cu practici aprobate.
- Practicile trimise de profesori intră în `PendingReview`.
- Administratorul are panou de moderare: aprobă sau respinge, cu notă.
- Profesorul vede starea propriilor propuneri.
- Mentor AI Gemini: analiză și adaptare cu reguli mai stricte pentru păstrarea temei originale.

## Pornire
1. Instalează Docker Desktop.
2. Copiază `.env.example` în `.env`.
3. Completează `GEMINI_API_KEY` și setează parole/secrete proprii.
4. Din PowerShell, în directorul proiectului:

```powershell
$env:BUILDX_BAKE_ENTITLEMENTS_FS="0"
docker compose up --build -d
docker compose ps
```

Deschide:
- Frontend: http://localhost:5173
- API Swagger: http://localhost:8080/swagger
- AI health: http://localhost:8000/health

## Cont administrator inițial
Valorile implicite din `.env.example`:
- Email: `admin@edumentor.local`
- Parolă: `ChangeMe123!`

Schimbă-le în `.env` înainte de folosirea platformei. Administratorul este creat automat la pornirea API-ului dacă nu există deja.

## Flux profesor
1. Profesorul își creează cont.
2. Creează o cohortă.
3. Adaugă elevi folosind nume, email și parolă temporară.
4. Elevul intră folosind emailul și parola primită.
5. Profesorul încarcă fișiere pentru uz personal sau le asociază unei cohorte.
6. Profesorul trimite o practică spre aprobare.
7. Administratorul aprobă practica în Moderare.
8. Practica apare în biblioteca publică.

## Important: upgrade de la versiunea veche
Această versiune păstrează volumul PostgreSQL `pgdata` și adaugă tabele/coloane la pornirea API-ului. Datele vechi din baza de date nu se șterg. Fișierele încărcate se păstrează în volumul Docker `uploads`.

Dacă vrei să începi cu o bază complet goală, numai după ce ai exportat datele, poți rula `docker compose down -v`. ATENȚIE: comanda șterge baza de date și fișierele persistente.

## Limitări / securitate înainte de producție
- MVP pentru testare locală, nu pentru publicare directă pe internet.
- Parolele sunt hash-uite BCrypt, tokenurile expiră după 12 ore.
- Parola temporară a elevului este introdusă de profesor și trebuie transmisă separat; elevul ar trebui să o schimbe într-o etapă ulterioară (funcția de schimbare parolă nu este inclusă încă).
- Nu există încă resetare email/parolă, verificare email, antivirus pentru fișiere, audit complet, rate limiting sau scanare a conținutului.
- Limita implicită de upload este 20 MB; tipurile de fișiere trebuie restricționate mai strict înainte de producție.
- Biblioteca este publică în MVP; datele elevilor și materialele private sunt protejate prin autentificare și verificarea apartenenței la cohortă.
- Administratorul inițial trebuie să schimbe parola implicită.
