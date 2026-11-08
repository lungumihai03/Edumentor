import React from 'react';
import { ArrowRight, BookOpen, Brain, CalendarDays, GraduationCap, Handshake, LibraryBig, ShieldCheck, ExternalLink, Lightbulb, Users, FileCheck2 } from 'lucide-react';

const resources = [
  { title: 'Curriculumul național', provider: 'Ministerul Educației și Cercetării', description: 'Documente curriculare pentru învățământul primar, gimnazial și liceal.', url: 'https://www.mec.gov.md/ro/content/curriculumul-national' },
  { title: 'Busola Curriculară', provider: 'Lappsus · index independent', description: 'Căutare după disciplină, clasă, unitate de învățare și competență.', url: 'https://curriculum.lappsus.com/' },
  { title: 'EduResurse', provider: 'MEC · UNICEF · PNUD', description: 'Catalog de manuale, lecții și materiale didactice pentru școlile din Republica Moldova.', url: 'https://www.eduresurse.gov.md/catalog' },
  { title: 'Educație Online', provider: 'Biblioteca Digitală', description: 'Lecții video pentru clasele I–XII, inclusiv în română și rusă.', url: 'https://educatieonline.md/' },
  { title: 'Programe și examene', provider: 'ANCE', description: 'Programe de examene, subiecte și bareme pentru gimnaziu și bacalaureat.', url: 'https://www.ance.gov.md/content/programe-de-examene' },
  { title: 'Resurse pentru profesori', provider: 'INEL', description: 'Instrumente despre mentorat, reflecție pedagogică, valori și siguranță online.', url: 'https://inel.md/ro/resurse' },
  { title: 'Parteneriate eTwinning', provider: 'eTwinning Moldova · UTM', description: 'Spațiu pentru colaborare între profesori și proiecte educaționale.', url: 'https://e-twinning.utm.md/' },
  { title: 'Mentorat INEL', provider: 'Institutul Național pentru Educație și Leadership', description: 'Informații despre mentorat și posibilități de sprijin pentru profesori.', url: 'https://mentoratinel.org/' },
];

const modules = [
  ['Oportunități', 'Catalog de cursuri, concursuri, comunități, evenimente, granturi, proiecte și resurse.'],
  ['Mentorat', 'Legătura dintre profesor și expertiză, cu recomandarea de a confirma disponibilitatea direct la furnizor.'],
  ['Comunități', 'Colaborare între profesori, parteneri educaționali și grupuri de învățare.'],
  ['Trasee de dezvoltare', 'Organizarea ofertelor după public, domeniu, condiții și pașii următori.'],
  ['Evidență și cercetare', 'Păstrarea sursei, contextului, datei verificării și limitelor observațiilor despre practici.'],
  ['Feedback și guvernanță', 'Canal pentru corecturi, revizuire umană și răspuns documentat la propuneri.'],
];

export default function PublicInfo({onLogin}:{onLogin:()=>void}) {
  return <div className="public-site">
    <header className="public-header">
      <a className="public-brand" href="#top"><span className="brand-mark">E</span><span><b>EduMentor AI</b><small>LAB MOLDOVA</small></span></a>
      <nav><a href="#despre">Despre</a><a href="#module">Ce oferim</a><a href="#resurse">Resurse</a><button className="public-login" onClick={onLogin}>Intră în cont <ArrowRight size={16}/></button></nav>
    </header>
    <main id="top">
      <section className="public-hero">
        <div className="public-hero-copy">
          <span className="public-kicker">EDUCAȚIE · COMUNITATE · TEHNOLOGIE</span>
          <h1>Experiența din clasă devine <em>cunoaștere împărtășită.</em></h1>
          <p>EduMentor AI Lab Moldova aduce împreună practici pedagogice, mentorat asistat de inteligență artificială, resurse educaționale și oportunități de dezvoltare profesională.</p>
          <div className="public-actions"><button className="public-cta" onClick={onLogin}>Intră în platformă <ArrowRight size={17}/></button><a className="public-secondary" href="#despre">Află cum funcționează</a></div>
          <div className="public-proof"><span><ShieldCheck size={16}/> Revizuire umană</span><span><BookOpen size={16}/> Practici documentate</span><span><Users size={16}/> Comunitate educațională</span></div>
        </div>
        <div className="public-visual" aria-label="EduMentor: practici, mentorat și comunitate">
          <div className="public-orbit orbit-a"></div><div className="public-orbit orbit-b"></div>
          <div className="public-core"><Brain size={43}/><b>AI</b><small>MENTOR</small></div>
          <span className="public-float float-a"><BookOpen size={17}/> Practici</span>
          <span className="public-float float-b"><GraduationCap size={17}/> Formare</span>
          <span className="public-float float-c"><Users size={17}/> Comunitate</span>
        </div>
      </section>

      <section className="public-intro" id="despre">
        <div><span className="public-kicker">DESPRE PLATFORMĂ</span><h2>De la o idee bună la o practică pe care o pot folosi și alți profesori.</h2></div>
        <div><p>Platforma sprijină profesorii să descrie ce au încercat la clasă, să reflecteze asupra rezultatelor și să adapteze ideile la propriul context. Mentorul AI poate ajuta la analiză și adaptare, dar nu înlocuiește decizia profesorului.</p><p>Biblioteca publică este destinată practicilor revizuite. Oportunitățile și resursele externe trimit la sursele lor originale, unde trebuie confirmate condițiile și disponibilitatea.</p></div>
      </section>

      <section className="public-modules" id="module">
        <div className="public-section-heading"><span className="public-kicker">UN SINGUR SPAȚIU, MAI MULTE RUTE</span><h2>Ce găsești în EduMentor</h2><p>Integram direcțiile de informare și colaborare din Puntea Edu MD în fluxul de practici și mentorat EduMentor.</p></div>
        <div className="public-module-grid">
          {modules.map(([title,description],i)=><article className="public-module" key={title}><span className="module-number">0{i+1}</span><h3>{title}</h3><p>{description}</p></article>)}
        </div>
      </section>

      <section className="public-flow">
        <div className="flow-heading"><span className="public-kicker">PARCURSUL PROFESORULUI</span><h2>Învață, aplică, reflectează, împărtășește.</h2><p>Un traseu simplu, care conectează dezvoltarea profesională cu activitatea reală de la clasă.</p></div>
        <div className="flow-steps">
          <article><span><CalendarDays/></span><b>01 · Descoperă</b><p>Explorează resurse, oportunități de formare și exemple de practici.</p></article>
          <article><span><Lightbulb/></span><b>02 · Adaptează</b><p>Folosește mentorul AI pentru a analiza și adapta o idee la contextul clasei.</p></article>
          <article><span><FileCheck2/></span><b>03 · Documentează</b><p>Descrie obiectivul, metoda, ce ai observat și ce ai schimba.</p></article>
          <article><span><Handshake/></span><b>04 · Împărtășește</b><p>Trimite practica spre revizuire și contribuie la comunitatea profesională.</p></article>
        </div>
      </section>

      <section className="public-resources" id="resurse">
        <div className="public-section-heading"><span className="public-kicker">PUNCTE DE PORNIRE</span><h2>Resurse educaționale din Republica Moldova și cadre utile</h2><p>Selecție de surse și instrumente preluată din proiectul Puntea Edu MD. Verifică întotdeauna ediția, condițiile și actualitatea informației pe pagina furnizorului.</p></div>
        <div className="public-resource-grid">{resources.map(item=><article className="public-resource" key={item.title}><span>{item.provider}</span><h3>{item.title}</h3><p>{item.description}</p><a href={item.url} target="_blank" rel="noopener noreferrer">Deschide sursa <ExternalLink size={14}/></a></article>)}</div>
      </section>

      <section className="public-note">
        <div><span className="public-kicker">CALITATE ȘI RESPONSABILITATE</span><h2>Tehnologia ajută. Profesorul decide.</h2><p>Informațiile externe trebuie verificate la sursă. Practicile propuse de utilizatori nu devin automat publice: ele necesită revizuire umană. Nu include date personale ale elevilor în descrieri sau materiale partajate.</p></div>
        <div className="public-note-icon"><ShieldCheck size={54}/><b>Responsabilitate<br/>și încredere</b></div>
      </section>
      <section className="public-bottom-cta"><span className="public-kicker">ALĂTURĂ-TE COMUNITĂȚII</span><h2>Transformă experiența ta într-o resursă pentru alții.</h2><p>Intră în EduMentor pentru a accesa biblioteca, mentorul AI și spațiile de colaborare.</p><button className="public-cta" onClick={onLogin}>Intră în cont <ArrowRight size={17}/></button></section>
    </main>
    <footer className="public-footer"><a className="public-brand" href="#top"><span className="brand-mark">E</span><span><b>EduMentor AI</b><small>LAB MOLDOVA</small></span></a><span>Platformă educațională în dezvoltare · Revizuirea umană rămâne esențială.</span><a href="#top">Înapoi sus ↑</a></footer>
  </div>;
}
