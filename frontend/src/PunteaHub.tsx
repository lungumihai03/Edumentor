import React, { useMemo, useState } from 'react';
import { catalog } from './puntea/catalog.mjs';
import { Search, ExternalLink, CalendarDays, Users, BookOpen, Filter, ArrowUpRight } from 'lucide-react';

const categories = [
  ['toate','Toate'],
  ['formari','Cursuri și formări'],
  ['concursuri','Concursuri'],
  ['comunitati','Comunități'],
  ['evenimente','Evenimente'],
  ['finantari','Granturi și finanțări'],
  ['proiecte','Proiecte'],
  ['resurse','Resurse'],
  ['mentorat','Mentorat'],
  ['activitati','Activități'],
  ['cercetari','Cercetări'],
];
const audienceOptions = ['Toți','Elevi','Profesori','Școli','Companii'];
const fold = (s='') => String(s).normalize('NFD').replace(/[\u0300-\u036f]/g,'').toLocaleLowerCase('ro-MD');

export default function PunteaHub() {
  const [query,setQuery] = useState('');
  const [category,setCategory] = useState('toate');
  const [audience,setAudience] = useState('Toți');
  const [expanded,setExpanded] = useState('');
  const items = useMemo(() => {
    const q = fold(query).split(/\s+/).filter(Boolean);
    return catalog.filter((item:any) => {
      const text = fold([item.title,item.provider,item.summary,item.eligible,item.next,item.keywords,item.kind].join(' '));
      const matchesText = q.every(term => text.includes(term));
      const matchesCategory = category === 'toate' || item.category === category;
      const people = item.audience || [];
      const matchesAudience = audience === 'Toți' || people.includes(audience) || people.length === 0;
      return matchesText && matchesCategory && matchesAudience;
    });
  },[query,category,audience]);
  const featured = [
    ['Găsești oportunitatea','Catalog de cursuri, concursuri, comunități, evenimente, granturi, proiecte și resurse.'],
    ['Decizi dacă te înscrii','Verifică publicul, eligibilitatea, costurile, termenul și sursa oficială înainte de a aplica.'],
    ['Publici și corectezi informația','Contribuțiile pot fi corectate și revizuite; informațiile trebuie urmărite până la sursa organizatorului.'],
    ['Continui și verifici rezultatul','Leagă formarea de mentorat, aplicare la clasă și reflecție asupra practicii.'],
  ];
  return <div className="puntea-page">
    <section className="puntea-hero">
      <div><span className="eyebrow">PUNTEA EDU MD · INTEGRAT ÎN EDUMENTOR</span><h2>Oportunități, resurse și comunități pentru educație.</h2><p>Explorează catalogul Puntea Edu MD direct din contul EduMentor. Fiecare fișă păstrează organizatorul, descrierea, eligibilitatea, următorul pas și legătura către sursa originală.</p></div>
      <div className="puntea-count"><BookOpen size={25}/><b>{catalog.length}</b><span>fișe în catalog</span></div>
    </section>
    <div className="puntea-path">{featured.map(([title,desc],i)=><article key={title}><span>0{i+1}</span><h3>{title}</h3><p>{desc}</p></article>)}</div>
    <section className="puntea-catalog">
      <div className="section-head"><div><span className="eyebrow">CATALOG PUNTEA EDU MD</span><h3>Găsește ce îți trebuie</h3><p>Caută după domeniu, organizator, public sau cuvinte-cheie.</p></div><span className="puntea-result-count">{items.length} rezultate</span></div>
      <div className="puntea-controls">
        <label className="puntea-search"><Search size={18}/><input value={query} onChange={e=>setQuery(e.target.value)} placeholder="Ex. matematică, mentorat, concurs, formare..."/></label>
        <label className="puntea-select"><Users size={17}/><select value={audience} onChange={e=>setAudience(e.target.value)}>{audienceOptions.map(a=><option key={a}>{a}</option>)}</select></label>
      </div>
      <div className="puntea-filters"><Filter size={16}/>{categories.map(([id,label])=><button key={id} className={category===id?'selected':''} onClick={()=>setCategory(id)}>{label}</button>)}</div>
      <div className="puntea-grid">
        {items.map((item:any)=> <article className="puntea-card" key={item.id}>
          <div className="puntea-card-top"><span>{item.kind || 'Resursă'}</span>{item.status==='conflict'&&<em>Termen de verificat</em>}{item.status==='deadline'&&item.deadline&&<small><CalendarDays size={13}/> Termen: {item.deadline}</small>}</div>
          <h4>{item.title}</h4><p className="puntea-provider">{item.provider}</p><p>{item.summary}</p>
          {item.eligible&&<div className="puntea-detail"><b>Cui se adresează</b><span>{item.eligible}</span></div>}
          {item.when&&<div className="puntea-detail"><b>Perioadă / dată</b><span>{item.when}</span></div>}
          {item.cost&&<div className="puntea-detail"><b>Cost</b><span>{item.cost}</span></div>}
          {item.next&&<div className="puntea-next"><b>Următorul pas</b><span>{item.next}</span></div>}
          <div className="puntea-card-actions"><a href={item.source} target="_blank" rel="noopener noreferrer">Sursa oficială <ExternalLink size={14}/></a>{item.reference?.length>0&&<button onClick={()=>setExpanded(expanded===item.id?'':item.id)}>Referințe <ArrowUpRight size={14}/></button>}</div>
          {expanded===item.id&&<div className="puntea-references">{item.reference.map((r:any,i:number)=><a key={i} href={r[1]} target="_blank" rel="noopener noreferrer">{r[0]} ↗</a>)}</div>}
          <div className="puntea-tags">{(item.audience||[]).map((a:string)=><span key={a}>{a}</span>)}</div>
        </article>)}
      </div>
      {!items.length&&<div className="empty"><BookOpen size={28}/><p>Nu am găsit rezultate. Încearcă alt termen sau schimbă filtrele.</p><button className="secondary" onClick={()=>{setQuery('');setCategory('toate');setAudience('Toți')}}>Resetează filtrele</button></div>}
      <p className="puntea-disclaimer">Informațiile și termenele provin din fișele Puntea Edu MD. Verifică întotdeauna condițiile actuale și eventualele modificări direct la organizator, prin sursa indicată.</p>
    </section>
  </div>;
}
