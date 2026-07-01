/**
 * Boss run simulation — doc stats only (BOSS_ENCOUNTER §2 + SKILL_KIT + COMBAT_MECHANICS)
 * Usage: node Tools/simulate-boss-run.js [runs]
 */

const fs = require('fs');
const path = require('path');

function loadSongBeatCount() {
  const csvPath = path.join(__dirname, '../Assets/FracturedChorus/Audio/Music/EternalSpark_CadenceRemix_beats.csv');
  const csv = fs.readFileSync(csvPath, 'utf8');
  const times = csv
    .trim()
    .split(/\r?\n/)
    .slice(1)
    .filter(Boolean)
    .map((line) => parseFloat(line.split(',')[1]))
    .filter((t) => !Number.isNaN(t))
    .sort((a, b) => a - b);
  if (times.length && times[0] > 0.001) times.unshift(0);
  return times.length;
}

const RUNS = parseInt(process.argv[2] || '200', 10);
const TOTAL_BEATS = loadSongBeatCount();

const party = {
  ren: { name: 'Ren', hp: 114, maxHp: 114, str: 42, ma: 8.8, en: 10.8, luck: 18, crit: 1.35, element: 'Melody', power: 42, type: 'Physical' },
  charlotte: { name: 'Charlotte', hp: 260, maxHp: 260, str: 35, ma: 6.4, en: 18.2, luck: 8, crit: 1.15, element: 'Rhythm', power: 35, type: 'Physical' },
  coda: { name: 'Coda', hp: 73, maxHp: 73, str: 20, ma: 50, en: 9.8, luck: 16, crit: 1.3, element: 'Harmony', power: 50, type: 'Magical' },
};

const boss = { hp: 2160, maxHp: 2160, str: 58, en: 20, element: 'Rhythm', pulse: 130 };

const noteLeakMult = { purple: 1.35, blue: 1.15, red: 1.0 };

const skills = {
  ren: [
    { name: 'Strike', s1: 1, s: 1, s2: 1, tier: 1, latency: 1 },
    { name: 'Crosscut', s1: 2, s: 2, s2: 2, tier: 2, latency: 1 },
    { name: 'Finale', s1: 2, s: 3, s2: 3, tier: 3, latency: 1 },
  ],
  charlotte: [
    { name: 'Ram', s1: 1, s: 1, s2: 1, tier: 1, latency: 1 },
    { name: 'Anchor', s1: 2, s: 2, s2: 2, tier: 2, latency: 1, utility: 'delay' },
    { name: 'Bulwark', s1: 2, s: 2, s2: 3, tier: 2, latency: 1, utility: 'shield', shield: 65 },
  ],
  coda: [
    { name: 'Pulse', s1: 1, s: 1, s2: 1, tier: 1, latency: 1 },
    { name: 'Mend', s1: 2, s: 1, s2: 2, tier: 2, latency: 1, utility: 'heal', healBase: 25 },
    { name: 'Encore', s1: 1, s: 1, s2: 1, tier: 2, latency: 1, utility: 'encore' },
  ],
};

const tierRand = { 1: [0.80, 1.05], 2: [0.90, 1.10], 3: [1.10, 1.50] };
const phases = {
  opening: { hpAbove: 0.7, scale: 0.75, weights: { purple: 0, blue: 34, red: 66 } },
  mid: { hpAbove: 0.3, scale: 1.0, weights: { purple: 10, blue: 40, red: 50 } },
  enrage: { hpAbove: 0.0, scale: 1.15, weights: { purple: 38, blue: 33, red: 33 } },
};

function rand(min, max) { return min + Math.random() * (max - min); }
function randTier(tier) { const [a, b] = tierRand[tier]; return rand(a, b); }

function enduranceFactor(en) {
  return 100 / (100 * 4 * Math.sqrt(Math.max(en, 1)));
}

function harmony(attackerEl, defenderEl) {
  const beats = (a, d) =>
    (a === 'Rhythm' && d === 'Melody') ||
    (a === 'Melody' && d === 'Harmony') ||
    (a === 'Harmony' && d === 'Rhythm');
  if (beats(attackerEl, defenderEl)) return 1.5;
  if (beats(defenderEl, attackerEl)) return 0.5;
  return 1.0;
}

function calcDamage(attacker, tier, timing = 1.0) {
  const roll = randTier(tier);
  const raw = roll * attacker.power * 10;
  let dmg = raw * enduranceFactor(boss.en) * harmony(attacker.element, boss.element) * timing;
  if (Math.random() * 100 < attacker.luck) dmg *= attacker.crit;
  return dmg;
}

function calcBossHit(defender, guardReduction = 0) {
  const roll = randTier(1);
  const raw = roll * boss.str * 10;
  const harmonyMult = harmony(boss.element, defender.element);
  return raw * harmonyMult * (1 - guardReduction) * enduranceFactor(defender.en);
}

function effectivePulse(phaseKey) {
  return Math.max(1, Math.round(boss.pulse * phases[phaseKey].scale));
}

function minGap(phaseKey) {
  const p = effectivePulse(phaseKey);
  return Math.max(3, Math.min(5, 5 - Math.floor((p - 80) / 25)));
}

function bossPhase(hpRatio) {
  if (hpRatio > 0.7) return 'opening';
  if (hpRatio > 0.3) return 'mid';
  return 'enrage';
}

function pickNoteColor(phaseKey) {
  const w = phases[phaseKey].weights;
  const r = Math.random() * (w.purple + w.blue + w.red);
  if (r < w.purple) return 'purple';
  if (r < w.purple + w.blue) return 'blue';
  return 'red';
}

function noteHits(color) {
  return color === 'purple' ? 3 : color === 'blue' ? 2 : 1;
}

function skillCycle(skill) {
  return skill.s1 + skill.s + skill.s2 + skill.latency;
}

function pickSkill(charKey, noteColor) {
  const kit = skills[charKey];
  if (charKey === 'ren') {
    if (noteColor === 'purple') return kit[2];
    if (noteColor === 'blue') return kit[1];
    return kit[0];
  }
  if (charKey === 'charlotte') {
    if (noteColor === 'purple') return kit[2];
    if (Math.random() < 0.35) return kit[1];
    return kit[0];
  }
  if (charKey === 'coda') {
    const lowest = ['ren', 'charlotte', 'coda'].reduce((a, k) => (party[k].hp / party[k].maxHp < party[a].hp / party[a].maxHp ? k : a), 'ren');
    if (party[lowest].hp / party[lowest].maxHp < 0.55 && Math.random() < 0.4) return kit[1];
    return kit[0];
  }
  return kit[0];
}

function simulateRun(skillLevel = 'competent') {
  Object.values(party).forEach(u => { u.hp = u.maxHp; });
  boss.hp = boss.maxHp;

  const counters = { notesSpawned: 0, notesCancelled: 0, notesLeaked: 0, guardPerfect: 0, guardLate: 0, noGuard: 0 };
  let beat = 0;
  let nextSpawn = 8;
  const notes = [];
  const charKeys = ['ren', 'charlotte', 'coda'];
  const charNextPlan = { ren: 0, charlotte: 0, coda: 0 };
  const charBusyUntil = { ren: -1, charlotte: -1, coda: -1 };
  let charlotteShield = 0;

  const playerSkill = skillLevel === 'good' ? 0.82 : skillLevel === 'competent' ? 0.70 : 0.55;
  const guardSkill = skillLevel === 'good' ? 0.65 : skillLevel === 'competent' ? 0.50 : 0.35;

  while (beat < TOTAL_BEATS && boss.hp > 0 && Object.values(party).some(u => u.hp > 0)) {
    const phaseKey = bossPhase(boss.hp / boss.maxHp);

    if (beat >= nextSpawn) {
      const color = pickNoteColor(phaseKey);
      notes.push({ beat, color, hits: noteHits(color) });
      counters.notesSpawned++;
      nextSpawn = beat + minGap(phaseKey) + Math.floor(rand(0, 2));
    }

    for (const key of charKeys) {
      if (beat < charNextPlan[key] || beat <= charBusyUntil[key]) continue;
      const upcoming = notes.find(n => n.hits > 0 && n.beat >= beat && n.beat <= beat + 8);
      const color = upcoming?.color || 'red';
      const skill = pickSkill(key, color);
      const cycle = skillCycle(skill);

      for (let b = 0; b < skill.s; b++) {
        const activeBeat = beat + skill.s1 + b;
        const note = notes.find(n => n.beat === activeBeat && n.hits > 0);
        if (note && Math.random() < playerSkill) {
          note.hits--;
          if (note.hits <= 0) counters.notesCancelled++;
        }
        if (skill.utility !== 'heal' && skill.utility !== 'delay' && skill.utility !== 'encore') {
          const timing = note && Math.random() < playerSkill ? 1.0 : 0.5;
          boss.hp -= calcDamage(party[key], skill.tier, timing);
        }
      }

      if (skill.utility === 'heal') {
        const target = ['ren', 'charlotte', 'coda'].reduce((a, k) => (party[k].hp / party[k].maxHp < party[a].hp / party[a].maxHp ? k : a), 'ren');
        party[target].hp = Math.min(party[target].maxHp, party[target].hp + skill.healBase + party.coda.ma * 0.5);
      }
      if (skill.utility === 'shield' && party.charlotte.hp > 0) {
        charlotteShield += skill.shield;
      }

      charBusyUntil[key] = beat + skill.s1 + skill.s + skill.s2;
      charNextPlan[key] = charBusyUntil[key] + skill.latency;
    }

    for (const note of notes) {
      if (note.beat !== beat || note.hits > 0) continue;
    }

    const impactNotes = notes.filter(n => n.beat === beat && n.hits > 0);
    for (const note of impactNotes) {
      if (Math.random() < playerSkill && note.hits <= 1) {
        note.hits = 0;
        counters.notesCancelled++;
        continue;
      }
      counters.notesLeaked++;
      const target = note.color === 'red' ? 'coda' : note.color === 'blue' ? 'ren' : 'charlotte';
      const defender = party[target].hp > 0 ? party[target] : party.charlotte;
      let guard = 0;
      const g = Math.random();
      if (g < guardSkill) { guard = 0.5; counters.guardPerfect++; }
      else if (g < guardSkill + 0.25) { guard = 0.15; counters.guardLate++; }
      else counters.noGuard++;
      let dmg = calcBossHit(defender, guard) * noteLeakMult[note.color];
      if (charlotteShield > 0 && defender === party.charlotte) {
        const absorbed = Math.min(charlotteShield, dmg);
        charlotteShield -= absorbed;
        dmg -= absorbed;
      }
      defender.hp -= dmg;
    }

    notes.forEach(n => { if (n.beat === beat && n.hits <= 0) n.dead = true; });
    beat++;
  }

  const winner = boss.hp <= 0 ? 'party' : Object.values(party).every(u => u.hp <= 0) ? 'boss' : 'timeout';
  return { winner, beat, bossHp: boss.hp, party, counters };
}

function percentile(arr, p) {
  const s = [...arr].sort((a, b) => a - b);
  return s[Math.floor(s.length * p)] ?? s[s.length - 1];
}

function analyzeStatic() {
  console.log('=== STATIC CHECK (avg roll, Perfect timing) ===\n');
  const efBoss = enduranceFactor(boss.en);
  console.log(`Boss EN ${boss.en} → EnduranceFactor ${efBoss.toFixed(4)}`);

  for (const [key, u] of Object.entries(party)) {
    const h = harmony(u.element, boss.element);
    for (const sk of skills[key]) {
      const avg = ((tierRand[sk.tier][0] + tierRand[sk.tier][1]) / 2) * u.power * 10 * efBoss * h;
      console.log(`  ${u.name} ${sk.name} (${sk.s1}-${sk.s}-${sk.s2}) tier${sk.tier} → ${avg.toFixed(1)} dmg/active beat (harmony×${h})`);
    }
  }

  console.log('\nBoss hit player (tier1 avg, no guard):');
  for (const u of Object.values(party)) {
    const h = harmony(boss.element, u.element);
    const raw = 0.925 * boss.str * 10 * h * enduranceFactor(u.en);
    console.log(`  → ${u.name} (EN ${u.en}): ${raw.toFixed(1)} raw leak / note`);
  }

  console.log('\nNote spawn rate @ Pulse 130:');
  for (const p of ['opening', 'mid', 'enrage']) {
    console.log(`  ${p}: gap ${minGap(p)} beat, effPulse ${effectivePulse(p)}`);
  }
  console.log('');
}

function main() {
  analyzeStatic();

  const levels = ['learning', 'competent', 'good'];
  for (const level of levels) {
    const results = Array.from({ length: RUNS }, () => simulateRun(level));
    const wins = results.filter(r => r.winner === 'party').length;
    const timeouts = results.filter(r => r.winner === 'timeout').length;
    const beats = results.filter(r => r.winner === 'party').map(r => r.beat);
    const hpLeft = results.filter(r => r.winner === 'party').map(r =>
      Object.values(r.party).reduce((s, u) => s + u.hp, 0));
    const leakRate = results.map(r => r.counters.notesLeaked / Math.max(1, r.counters.notesSpawned));

    console.log(`=== MONTE CARLO ×${RUNS} · skill ${level} ===`);
    console.log(`Win rate: ${(100 * wins / RUNS).toFixed(1)}% · Timeout: ${timeouts} · Boss wins: ${RUNS - wins - timeouts}`);
    if (beats.length) {
      console.log(`Win beat: p50=${percentile(beats, 0.5)} p90=${percentile(beats, 0.9)} (song=${TOTAL_BEATS})`);
      console.log(`Party HP left on win: p50=${percentile(hpLeft, 0.5).toFixed(0)} / 447 max`);
    }
    console.log(`Leak rate p50: ${(100 * percentile(leakRate, 0.5)).toFixed(1)}% of spawned notes`);
    console.log('');
  }

  console.log('=== RED FLAGS TO REVIEW ===');
  const oneHit = 0.925 * boss.str * 10 * enduranceFactor(party.coda.en) * harmony(boss.element, party.coda.element);
  const renDps = 0.925 * party.ren.power * 10 * enduranceFactor(boss.en) * 0.5;
  console.log(`• Coda 1 leak (no guard): ~${oneHit.toFixed(0)} dmg → ${(oneHit / party.coda.maxHp * 100).toFixed(0)}% Coda HP`);
  console.log(`• Ren perfect basic vs boss: ~${renDps.toFixed(1)} dmg (disadvantage ×0.5)`);
  console.log(`• Boss HP ${boss.maxHp} vs party total ${Object.values(party).reduce((s,u)=>s+u.maxHp,0)} HP`);
  console.log(`• Doc §13 auto HP Lv15 = 536 vs §2 optimal = 447 (−17%)`);
}

main();
