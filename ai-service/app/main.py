import os
from functools import lru_cache
from fastapi import FastAPI, HTTPException
from pydantic import BaseModel, Field
from google import genai
from google.genai import types

app = FastAPI(title="EduMentor AI Service - Gemini", version="1.0.0")

@lru_cache(maxsize=1)
def get_client():
    key = os.getenv("GEMINI_API_KEY", "").strip()
    if not key:
        raise HTTPException(status_code=503, detail="GEMINI_API_KEY nu este configurat in .env.")
    return genai.Client(api_key=key)

def chat_model():
    return os.getenv("GEMINI_MODEL", "gemini-2.5-flash")

class AnalyzeRequest(BaseModel):
    text: str = Field(min_length=20, max_length=30000)
    language: str | None = "ro"

class AdaptRequest(BaseModel):
    practiceTitle: str
    practiceDescription: str
    schoolContext: str
    language: str | None = "ro"

class EmbedRequest(BaseModel):
    text: str = Field(min_length=1, max_length=10000)

@app.get("/health")
def health():
    return {"status":"healthy","provider":"Google Gemini","aiConfigured":bool(os.getenv("GEMINI_API_KEY","").strip()),"model":chat_model()}

def generate(prompt: str, system_instruction: str) -> str:
    try:
        response = get_client().models.generate_content(
            model=chat_model(), contents=prompt,
            config=types.GenerateContentConfig(system_instruction=system_instruction, temperature=0.25)
        )
        if not response.text:
            raise HTTPException(status_code=502, detail="Gemini nu a returnat text.")
        return response.text
    except HTTPException:
        raise
    except Exception as exc:
        raise HTTPException(status_code=502, detail=f"Eroare Google Gemini API: {exc}")

@app.post("/analyze")
def analyze(request: AnalyzeRequest):
    language = "romana" if request.language == "ro" else "rusa"
    prompt = f"""Analizeaza textul pedagogic si returneaza JSON valid cu campurile:
title, summary, subject, educationLevel, problem, methods (array), resources (array),
keywords (array), evidenceMentioned (array), limitations (array), requiresHumanReview (boolean).
Scrie valorile in {language}. Nu inventa rezultate, statistici sau surse.
Daca informatia lipseste, foloseste sir gol sau array gol. requiresHumanReview=true.
TEXT:\n{request.text}"""
    return {"analysis":generate(prompt,"Esti un asistent pentru analizarea practicilor pedagogice. Nu inventa dovezi."),"requiresHumanReview":True}

@app.post("/adapt")
def adapt(request: AdaptRequest):
    language = "romana" if request.language == "ro" else "rusa"
    prompt = f"""Adapteaza practica pedagogica STRICT pentru contextul indicat. Raspunde in {language}.
REGULI OBLIGATORII:
1. Pastreaza disciplina, tema, clasa si obiectivele practicii originale. NU schimba tema.
2. Nu introduce subiecte sau continut care nu apar in practica.
3. Nu presupune ca elevii au materiale, dispozitive sau acces la internet daca nu sunt mentionate.
4. Diferentiaza clar faptele furnizate de presupuneri; marcheaza presupunerile.
5. Planul trebuie sa respecte exact 45 de minute (sau durata mentionata in context); suma etapelor trebuie sa fie exacta.
6. Include alternativa complet offline.
7. Include evaluare observabila si limitari.
8. Nu afirma ca practica a fost testata. Nu inventa rezultate.
STRUCTURA:
- Elemente pastrate
- Modificari propuse
- Plan minut cu minut (duratele trebuie sa aiba suma exacta)
- Diferentiere
- Resurse
- Evaluare
- Limitari si presupuneri
PRACTICA ORIGINALA:
Titlu: {request.practiceTitle}
Descriere: {request.practiceDescription}
CONTEXTUL SCOLII:
{request.schoolContext}
"""
    return {"adaptation":generate(prompt,"Esti mentor pedagogic. Pastrezi fidel practica originala si oferi recomandari realiste, prudente si aplicabile."),"isProposal":True}

@app.post("/embed")
def embed(request: EmbedRequest):
    try:
        result=get_client().models.embed_content(model=os.getenv("GEMINI_EMBEDDING_MODEL","gemini-embedding-001"),contents=request.text)
        values=result.embeddings[0].values if result.embeddings else None
        if not values: raise HTTPException(status_code=502,detail="Gemini nu a returnat embedding.")
        return {"embedding":values}
    except HTTPException: raise
    except Exception as exc: raise HTTPException(status_code=502,detail=f"Eroare Gemini embeddings API: {exc}")
