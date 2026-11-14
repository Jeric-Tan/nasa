// Outpost Gale — NASA data fetcher.
// Run before each build:  node tools/fetch_data/index.js
// Writes Assets/StreamingAssets/data/*.json. Every record keeps "source" and "retrieved_on".
// Needs Node 18+ (global fetch). No API keys.

const fs = require('fs');
const path = require('path');

const ROOT = path.resolve(__dirname, '..', '..');
const OUT = path.join(ROOT, 'Assets', 'StreamingAssets', 'data');
const CACHE = path.join(__dirname, 'cache');
const TODAY = new Date().toISOString().slice(0, 10);
const DONKI = 'https://ccmc.gsfc.nasa.gov/DONKI/WS/get/';
const YEARS = [];
for (let y = 2012; y <= 2025; y++) YEARS.push(y);

fs.mkdirSync(OUT, { recursive: true });
fs.mkdirSync(CACHE, { recursive: true });

const sleep = ms => new Promise(r => setTimeout(r, ms));

async function getJson(url, cacheName) {
  const cacheFile = cacheName && path.join(CACHE, cacheName);
  if (cacheFile && fs.existsSync(cacheFile)) return JSON.parse(fs.readFileSync(cacheFile, 'utf8'));
  for (let attempt = 1; attempt <= 4; attempt++) {
    try {
      const res = await fetch(url, { headers: { 'User-Agent': 'outpost-gale-data-fetch' } });
      if (!res.ok) throw new Error(res.status + ' ' + (await res.text()).slice(0, 120));
      const text = await res.text();
      const data = text.trim() ? JSON.parse(text) : [];
      if (cacheFile) fs.writeFileSync(cacheFile, JSON.stringify(data));
      return data;
    } catch (e) {
      console.warn(`  retry ${attempt} ${url}: ${e.message}`);
      await sleep(1500 * attempt);
    }
  }
  throw new Error('Failed: ' + url);
}

// DONKI caps a request at 60 days, so walk each year in 59-day chunks.
function chunks(year) {
  const out = [];
  let start = new Date(Date.UTC(year, 0, 1));
  const end = new Date(Date.UTC(year, 11, 31));
  while (start <= end) {
    const stop = new Date(Math.min(end, new Date(start.getTime() + 58 * 86400000)));
    out.push([start.toISOString().slice(0, 10), stop.toISOString().slice(0, 10)]);
    start = new Date(stop.getTime() + 86400000);
  }
  return out;
}

async function donkiYear(type, year) {
  let all = [];
  for (const [a, b] of chunks(year)) {
    const data = await getJson(`${DONKI}${type}?startDate=${a}&endDate=${b}`, `${type}_${a}_${b}.json`);
    all = all.concat(Array.isArray(data) ? data : []);
    await sleep(150);
  }
  return all;
}

function flareRank(cls) {
  if (!cls) return 0;
  const m = /^([ABCMX])(\d+(\.\d+)?)/.exec(cls);
  if (!m) return 0;
  return { A: 0, B: 1, C: 2, M: 3, X: 4 }[m[1]] * 100 + parseFloat(m[2]);
}

async function buildSeasons() {
  const seasons = [];
  for (const year of YEARS) {
    console.log(`DONKI ${year}…`);
    const seps = await donkiYear('SEP', year);
    const flrs = await donkiYear('FLR', year);
    const flrById = new Map(flrs.map(f => [f.flrID, f]));
    const storms = seps
      .filter(s => s.sepID && s.eventTime)
      .map(s => {
        const linked = (s.linkedEvents || []).map(l => l.activityID);
        const flr = linked.map(id => flrById.get(id)).find(Boolean);
        const cme = linked.find(id => /-CME-/.test(id)) || '';
        const instruments = (s.instruments || []).map(i => i.displayName);
        return {
          id: s.sepID,
          date: s.eventTime.slice(0, 10),
          flareClass: flr ? flr.classType : '',
          flareId: flr ? flr.flrID : '',
          cmeId: cme,
          // DONKI is mostly measured near Earth; flag any instrument that is not Earth-side.
          marsRelated: instruments.some(n => /MSL|MAVEN|Mars/i.test(n)),
          instruments: instruments.join(', '),
          link: s.link || '',
        };
      })
      .sort((a, b) => a.date.localeCompare(b.date));
    // Big flares (M5+) as the "flare warning" pool for the year.
    const bigFlares = flrs
      .filter(f => flareRank(f.classType) >= 305)
      .map(f => ({ id: f.flrID, date: (f.peakTime || f.beginTime || '').slice(0, 10), flareClass: f.classType, link: f.link || '' }))
      .sort((a, b) => flareRank(b.flareClass) - flareRank(a.flareClass))
      .slice(0, 12)
      .sort((a, b) => a.date.localeCompare(b.date));
    const strongest = bigFlares.reduce((m, f) => (flareRank(f.flareClass) > flareRank(m) ? f.flareClass : m), '');
    seasons.push({
      year,
      stormCount: storms.length,
      strongestFlare: strongest,
      storms,
      flares: bigFlares,
      source: 'NASA CCMC DONKI (SEP, FLR) ccmc.gsfc.nasa.gov/DONKI/WS/get',
      retrieved_on: TODAY,
    });
  }
  // Label each year by how stormy it was.
  const counts = seasons.map(s => s.stormCount).sort((a, b) => a - b);
  const q1 = counts[Math.floor(counts.length / 3)];
  const q2 = counts[Math.floor((2 * counts.length) / 3)];
  for (const s of seasons) s.kind = s.stormCount <= q1 ? 'calm' : s.stormCount <= q2 ? 'moderate' : 'stormy';
  return seasons;
}

// Hand-picked OSDR studies. Kid text is ours; title/description are fetched so each card can be checked.
const OSDR_PICKS = [
  { id: 'OSD-489', title: 'Bones in space', text: 'Scientists flew mice to the International Space Station to see how bones change without gravity. Bones that carry weight on Earth changed the most. That is why astronauts exercise every day.', meter: 'health' },
  { id: 'OSD-104', title: 'Muscles in space', text: 'Mice on the space station helped scientists study how leg muscles change in low gravity. On Mars, gravity is only about a third of Earth\'s, so crews need a gym.', meter: 'health' },
  { id: 'OSD-7', title: 'Plants in orbit', text: 'Scientists grew a small plant called Arabidopsis in space and checked which genes switched on in its roots and leaves. Learning how plants cope helps us grow food far from Earth.', meter: 'life' },
  { id: 'OSD-678', title: 'Plants need light', text: 'A space station experiment found that light plays a big part in how plants respond to spaceflight. A greenhouse on Mars needs power for its lamps.', meter: 'power' },
  { id: 'OSD-334', title: 'Space radiation', text: 'On Earth, scientists use particle beams to copy space radiation and study its effect on the heart in mice. It helps NASA plan shielding for long missions.', meter: 'health' },
];

async function buildFacts() {
  const facts = [];
  for (const pick of OSDR_PICKS) {
    const num = pick.id.replace('OSD-', '');
    let studyTitle = '';
    try {
      const meta = await getJson(`https://osdr.nasa.gov/osdr/data/osd/meta/${num}`, `osdr_${num}.json`);
      const study = meta.study[pick.id].studies[0];
      studyTitle = study.title;
    } catch (e) {
      console.warn('OSDR lookup failed for ' + pick.id);
    }
    facts.push({
      id: pick.id,
      title: pick.title,
      text: pick.text,
      meter: pick.meter,
      studyTitle,
      url: `https://osdr.nasa.gov/bio/repo/data/studies/${pick.id}`,
      source: 'NASA Open Science Data Repository (OSDR)',
      retrieved_on: TODAY,
    });
  }
  return facts;
}

// RadLab: the API answers (query=timestamp>… returns MSL-RAD-Surface rows) but no dose column name
// we tried returns values. Record the probe, and use the published Curiosity RAD numbers instead,
// labelled as published values (the plan's fallback).
async function buildRadiation() {
  let radlabRows = 0;
  try {
    const res = await fetch('https://visualization.osdr.nasa.gov/radlab/api/?query=' + encodeURIComponent('timestamp>2017-09-10 AND timestamp<2017-09-11'));
    const text = await res.text();
    radlabRows = text.split('\n').filter(l => l.includes('MSL-RAD-Surface')).length;
  } catch (e) { /* offline */ }
  return {
    site: 'Gale crater, Mars (Curiosity rover)',
    baselineDoseEquivalent_mSvPerSol: 0.64,
    baselineAbsorbedDose_mGyPerSol: 0.21,
    chestXray_mSv: 0.1,
    citation: 'Hassler, D. M. et al. (2014). Mars\' Surface Radiation Environment Measured with the Mars Science Laboratory\'s Curiosity Rover. Science 343, 1244797.',
    note: 'Published Curiosity RAD averages. The RadLab API returned MSL-RAD-Surface rows (' + radlabRows + ' on 2017-09-10) but no dose column, so these are published values, not a live query.',
    source: 'Curiosity RAD, published values (RadLab API probed)',
    retrieved_on: TODAY,
  };
}

(async () => {
  const seasons = await buildSeasons();
  fs.writeFileSync(path.join(OUT, 'donki_seasons.json'), JSON.stringify({ seasons }, null, 1));
  console.log('Seasons:', seasons.map(s => `${s.year}:${s.stormCount}(${s.kind})`).join(' '));
  const facts = await buildFacts();
  fs.writeFileSync(path.join(OUT, 'osdr_facts.json'), JSON.stringify({ facts }, null, 1));
  const rad = await buildRadiation();
  fs.writeFileSync(path.join(OUT, 'radiation.json'), JSON.stringify(rad, null, 1));
  console.log('Done. Wrote', OUT);
})().catch(e => { console.error(e); process.exit(1); });
