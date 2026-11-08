# EduMentor AI Lab Moldova — ediție cu pagină informativă și resurse Puntea Edu

Această ediție unește aplicația EduMentor AI cu informații și resurse publice selectate din proiectul Puntea Edu MD.

## Ce este nou
- Pagina publică informativă este afișată înainte de autentificare.
- Butonul „Intră în cont” deschide formularul existent de autentificare/înregistrare.
- Din formular există un buton de întoarcere la pagina informativă.
- Pagina publică prezintă misiunea EduMentor, traseul profesorului, revizuirea umană și șase direcții: oportunități, mentorat, comunități, trasee de dezvoltare, evidență/cercetare și feedback/guvernanță.
- Am adăugat o selecție de resurse educaționale din Moldova: Curriculumul MEC, Busola Curriculară, EduResurse, Educație Online, ANCE, INEL și eTwinning.
- Linkurile externe se deschid în pagină nouă și indică sursele originale.

## Ce am preluat din Puntea Edu MD
Am integrat informația publică și structura conceptuală relevantă pentru profesori: catalogul de oportunități și categorii, conexiunile spre mentorat și eTwinning, resurse curriculare, documentarea surselor și a limitelor informației, feedback și guvernanță.

Nu am copiat mecanismul tehnic de autentificare Puntea Edu (care este specific mediului lor) și nu am înlocuit autentificarea EduMentor. Utilizatorii intră prin sistemul de conturi EduMentor existent.

Resursele din pagina publică sunt linkuri externe statice. Nu există încă sincronizare automată cu catalogul Puntea Edu sau verificare automată a termenelor; utilizatorii trebuie să confirme condițiile la furnizorul original.

## Funcționalități EduMentor păstrate
- Conturi JWT: Administrator, Profesor, Elev/Student.
- Înregistrare publică pentru profesori; elevii sunt adăugați de profesori în cohorte.
- Cohorte și membri.
- Încărcare și descărcare materiale pentru profesor/cohortă.
- Bibliotecă publică doar cu practici aprobate.
- Moderarea practicilor de către administrator.
- Mentor AI Gemini pentru analiză și adaptare.

## Pornire locală
1. Instalează Docker Desktop.
2. Copiază `.env.example` în `.env`.
3. Completează cheia Gemini și parole/secrete proprii.
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

## Administrator inițial
- Email: `admin@edumentor.local`
- Parolă implicită: `ChangeMe123!`

Schimbă parola în `.env` înainte de folosire. Administratorul este creat automat la pornirea API-ului dacă nu există deja.

## Limitări importante
- Aceasta este o versiune MVP pentru testare locală, nu pentru publicare directă pe internet.
- Pagina informativă este publică și nu cere autentificare.
- Conturile și funcțiile interne folosesc în continuare autentificarea EduMentor cu JWT și baza de date configurată în `docker-compose.yml`.
- Resursele Puntea Edu sunt prezentate ca linkuri; nu am importat baza lor de date, postările dinamice sau conturile utilizatorilor.
- Nu există încă resetare email/parolă, verificare email, antivirus pentru fișiere, audit complet sau rate limiting.
- Nu încărca date personale sensibile despre elevi.
- Revizuirea umană rămâne obligatorie înainte de publicarea unei practici.


## Depanare: NetworkError la autentificare

1. Deschide aplicația la `http://localhost:5173` (nu prin `file://`).
2. Verifică API-ul direct: `http://localhost:8080/api/health`.
3. În PowerShell, din folderul cu `docker-compose.yml`, rulează:
   ```powershell
   docker compose ps
   docker compose logs --tail=100 api
   docker compose logs --tail=100 db
   ```
4. Dacă API-ul nu este pornit:
   ```powershell
   $env:BUILDX_BAKE_ENTITLEMENTS_FS="0"
   docker compose up --build -d
   ```
5. Pentru acces prin `127.0.0.1`, CORS permite acum atât `localhost:5173`, cât și `127.0.0.1:5173`.

Dacă `/api/health` nu răspunde, problema este la pornirea API-ului sau a bazei de date, nu la formularul de autentificare.
