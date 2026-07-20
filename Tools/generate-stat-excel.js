const ExcelJS = require('exceljs');
const fs = require('fs');
const path = require('path');

const DIR = path.join(__dirname, '..', 'docs', 'combat');
const XLSX_OUT = path.join(DIR, 'CHARACTER_LEVEL_PROGRESS.xlsx');
const MD_OUT = path.join(DIR, 'CHARACTER_LEVEL_PROGRESS.md');

function calcHP(char, str, ma) {
  if (char === 'ren') return Math.round(str * 2.0 + 30);
  if (char === 'charlotte') return Math.round(str * 6.0 + 50);
  return Math.round(str * 2.0 + ma * 0.35 + 15);
}

function calcW(hb) {
  return Math.max(7, Math.min(10, 7 + Math.floor((hb - 120) / 26)));
}

function calcLatency(hb) {
  return Math.max(0, 2 - Math.floor(hb / 85));
}

const XP_TO_NEXT = [
  0, 60, 90, 130, 180, 240, 310, 390, 480, 580, 690, 810, 940, 1080, 1230, 3600, 4200, 4800,
];

const DUNGEON_XP_BANDS = [
  { floors: '1–3', battle: 120, elite: 200, recLv: '1–4' },
  { floors: '4–6', battle: 220, elite: 380, recLv: '4–7' },
  { floors: '7–9', battle: 350, elite: 600, recLv: '7–10' },
  { floors: '10–12', battle: 500, elite: 850, recLv: '10–13' },
  { floors: '13–15', battle: 700, elite: 1200, recLv: '13–15' },
];

const BOSS_XP_GRANT = 12600;

const chars = {
  ren: {
    name: 'Ren', role: 'DPS', element: 'Melody',
    base: { str: 22, ma: 6, hb: 145, en: 4 },
    growth: { str: 1.0, ma: 0.2, hb: 0.5, en: 0.2 },
    luck: [8, 9, 10, 11, 11, 12, 13, 14, 14, 15, 16, 16, 17, 17, 18, 19, 19, 20],
    critMult: [1.15, 1.16, 1.17, 1.18, 1.20, 1.21, 1.23, 1.24, 1.26, 1.28, 1.29, 1.31, 1.32, 1.34, 1.35, 1.37, 1.38, 1.40],
    dmgType: 'Physical',
    formula: 'HP = STR * 2.0 + 30',
    skills: [
      { lv: 1, name: 'Strike', type: 'Basic' },
      { lv: 4, name: 'Crosscut', type: 'Skill' },
      { lv: 10, name: 'Finale', type: 'Ult' },
    ],
    alloc: {
      str: [0, 1, 1, 0, 1, 0, 0, 1, 0, 0, 1, 1, 0, 0, 0, 1, 0],
      hb: [0, 0, 0, 0, 0, 1, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 1],
      en: [1, 0, 0, 1, 0, 0, 1, 0, 0, 1, 0, 0, 1, 0, 1, 0, 0],
      ma: [],
    },
  },
  charlotte: {
    name: 'Charlotte', role: 'Tank', element: 'Rhythm',
    base: { str: 15, ma: 5, hb: 105, en: 10 },
    growth: { str: 1.0, ma: 0.1, hb: 0.5, en: 0.3 },
    luck: [3, 3, 4, 4, 5, 5, 5, 6, 6, 7, 7, 7, 8, 8, 8, 9, 9, 9],
    critMult: [1.05, 1.05, 1.06, 1.06, 1.08, 1.08, 1.09, 1.09, 1.10, 1.12, 1.12, 1.13, 1.14, 1.14, 1.15, 1.16, 1.17, 1.18],
    dmgType: 'Physical',
    formula: 'HP = STR * 6.0 + 50',
    skills: [
      { lv: 1, name: 'Ram', type: 'Basic' },
      { lv: 3, name: 'Anchor', type: 'Skill' },
      { lv: 9, name: 'Bulwark', type: 'Ult' },
    ],
    alloc: {
      str: [0, 1, 1, 0, 1, 0, 0, 1, 0, 0, 1, 1, 0, 0, 0, 1, 0],
      hb: [0, 0, 0, 0, 0, 1, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 1],
      en: [1, 0, 0, 1, 0, 0, 1, 0, 0, 1, 0, 0, 1, 0, 1, 0, 0],
      ma: [],
    },
  },
  coda: {
    name: 'Coda', role: 'Support', element: 'Harmony',
    base: { str: 6, ma: 30, hb: 125, en: 3 },
    growth: { str: 1.0, ma: 1.0, hb: 0.5, en: 0.2 },
    luck: [7, 8, 8, 9, 10, 10, 11, 12, 12, 13, 14, 14, 15, 15, 16, 17, 17, 18],
    critMult: [1.12, 1.13, 1.14, 1.15, 1.18, 1.19, 1.20, 1.22, 1.23, 1.25, 1.26, 1.27, 1.28, 1.29, 1.30, 1.32, 1.33, 1.35],
    dmgType: 'Magical',
    formula: 'HP = STR * 2.0 + Ma * 0.35 + 15',
    skills: [
      { lv: 1, name: 'Pulse', type: 'Basic' },
      { lv: 5, name: 'Mend', type: 'Skill' },
      { lv: 11, name: 'Encore', type: 'Ult' },
    ],
    alloc: {
      str: [],
      hb: [0, 0, 0, 0, 0, 1, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 1],
      en: [1, 0, 0, 1, 0, 0, 1, 0, 0, 1, 0, 0, 1, 0, 1, 0, 0],
      ma: [0, 1, 1, 0, 1, 0, 0, 1, 0, 0, 1, 1, 0, 0, 0, 1, 0],
    },
  },
};

const skillUnlock = [
  { lv: 1, ren: 'Strike (Basic)', charlotte: 'Ram (Basic)', coda: 'Pulse (Basic)' },
  { lv: 3, ren: '', charlotte: 'Anchor (Skill)', coda: '' },
  { lv: 4, ren: 'Crosscut (Skill)', charlotte: '', coda: '' },
  { lv: 5, ren: '', charlotte: '', coda: 'Mend (Skill)' },
  { lv: 9, ren: '', charlotte: 'Bulwark (Ult)', coda: '' },
  { lv: 10, ren: 'Finale (Ult)', charlotte: '', coda: '' },
  { lv: 11, ren: '', charlotte: '', coda: 'Encore (Ult)' },
];

function spendLabel(a, idx) {
  if (idx < 0) return '—';
  const parts = [];
  if (a.str && a.str[idx]) parts.push(`+${a.str[idx]} STR`);
  if (a.ma && a.ma[idx]) parts.push(`+${a.ma[idx]} Ma`);
  if (a.hb && a.hb[idx]) parts.push(`+${a.hb[idx] * 5} HB`);
  if (a.en && a.en[idx]) parts.push(`+${a.en[idx]} EN`);
  return parts.length ? parts.join(', ') : '—';
}

function cumXpToReach(lv) {
  let sum = 0;
  for (let i = 1; i < lv; i++) sum += XP_TO_NEXT[i];
  return sum;
}

function genData(key) {
  const c = chars[key], a = c.alloc, rows = [];
  for (let lv = 1; lv <= 18; lv++) {
    const i = lv - 1;
    let s = c.base.str + c.growth.str * i;
    let m = c.base.ma + c.growth.ma * i;
    let h = c.base.hb + c.growth.hb * i;
    let e = c.base.en + c.growth.en * i;
    if (lv > 1) for (let l = 0; l < i; l++) {
      if (a.str && a.str[l]) s += a.str[l];
      if (a.hb && a.hb[l]) h += a.hb[l] * 5;
      if (a.en && a.en[l]) e += a.en[l];
      if (a.ma && a.ma[l]) m += a.ma[l];
    }
    s = Math.round(s * 10) / 10; m = Math.round(m * 10) / 10;
    h = Math.round(h * 10) / 10; e = Math.round(e * 10) / 10;
    rows.push({
      lv, str: s, ma: m, hb: h, en: e,
      hp: calcHP(key, s, m),
      w: calcW(h),
      latency: calcLatency(h),
      luck: c.luck[i],
      critMult: c.critMult[i],
      mp: lv > 1 ? i : 0,
      spent: spendLabel(a, lv - 2),
      xpToNext: lv < 18 ? XP_TO_NEXT[lv] : 0,
      cumXp: cumXpToReach(lv),
    });
  }
  return rows;
}

function genMD(ren, ch, co) {
  const M = [1, 3, 4, 5, 9, 10, 11, 15, 18];
  let o = '';
  o += '# Character Level Progression\n\n';
  o += '> Cap arc 1: **Lv 18** · Soft target boss: **Lv 15** · 17 manual points · HB 1pt = +5  \n';
  o += '> XP / soft-cap: [combat-level-xp-progression-design](../superpowers/specs/2026-07-19-combat-level-xp-progression-design.md) · W/Latency: [COMBAT_MECHANICS.md](./COMBAT_MECHANICS.md)\n\n';
  o += '---\n\n## Stat Allocation\n\n';
  o += '| Into | Gain | Impact |\n|------|------|--------|\n';
  o += '| STR | +1 | Ren/Coda +2HP, Charlotte +6HP |\n';
  o += '| Ma | +1 | +Skill dmg (Magical) |\n';
  o += '| EN | +1 | EnduranceFactor + reactive Guard |\n';
  o += '| HB | +5 | +W beat bar · intel · planning latency |\n\n';
  o += 'Party Combat Level: mỗi level-up → **+1 điểm / nhân vật**.  \n';
  o += 'Formula: `Stat(Lv) = Base + Growth*(Lv-1) + Pts*Conversion`  \n';
  o += 'Max/stat: 10 · Total: 17 (Lv15 = 14 · Lv18 = 17)\n\n';
  o += '---\n\n## Auto-Growth\n\n';
  o += '| Stat | Ren | Charlotte | Coda |\n|------|-----|-----------|------|\n';
  o += '| STR | +1.0 | +1.0 | +1.0 |\n';
  o += '| Ma | +0.2 | +0.1 | +1.0 |\n';
  o += '| HB | +0.5 | +0.5 | +0.5 |\n';
  o += '| EN | +0.2 | +0.3 | +0.2 |\n\n';

  [{ k: 'ren', d: ren }, { k: 'charlotte', d: ch }, { k: 'coda', d: co }].forEach(cfg => {
    const c = chars[cfg.k], sm = {};
    skillUnlock.forEach(s => { sm[s.lv] = s[cfg.k]; });
    o += `---\n\n## ${c.name} — ${c.role} · ${c.element}\n\n`;
    o += `HP: ${c.formula} · Dmg: ${c.dmgType}\n\n`;
    o += `| Lv | STR | Ma | HB | EN | HP | W | Latency | Luck | Crit | Pts | Spent | Skill |\n`;
    o += `|----|-----|----|----|----|----|---|---------|------|------|-----|-------|-------|\n`;
    cfg.d.forEach(r => {
      const u = sm[r.lv] || '';
      const cs = c.skills.filter(s => s.lv === r.lv).map(s => `${s.name} (${s.type})`).join(', ');
      const n = u || cs;
      const lvLabel = M.includes(r.lv) ? `**${r.lv}**` : `${r.lv}`;
      o += `| ${lvLabel} | ${r.str} | ${r.ma} | ${r.hb} | ${r.en} | ${r.hp} | ${r.w} | ${r.latency} | ${r.luck}% | x${r.critMult} | ${r.mp} | ${r.spent} | ${n} |\n`;
    });
    o += `\nOptimal: ${cfg.k === 'coda' ? '6 Ma' : '6 STR'} → 3 HB → 5 EN (→Lv15) · rồi +1 EN / +1 ${cfg.k === 'coda' ? 'Ma' : 'STR'} / +1 HB (→Lv18)\n\n`;
  });

  o += '---\n\n## Party HP (optimal build)\n\n';
  o += '| Lv | Ren | Charlotte | Coda | Total |\n|----|-----|-----------|------|-------|\n';
  [0, 4, 9, 14, 17].forEach(i => {
    o += `| ${i + 1} | ${ren[i].hp} | ${ch[i].hp} | ${co[i].hp} | **${ren[i].hp + ch[i].hp + co[i].hp}** |\n`;
  });

  o += '\n---\n\n## Combat XP\n\n';
  o += '| From→To | XP | Cum. to reach To | Note |\n|---------|-----|------------------|------|\n';
  let cum = 0;
  for (let lv = 1; lv < 18; lv++) {
    cum += XP_TO_NEXT[lv];
    let note = '';
    if (lv === 14) note = 'Soft target';
    if (lv === 15) note = 'Soft-cap start';
    if (lv === 17) note = 'Hard cap Arc 1';
    o += `| ${lv}→${lv + 1} | ${XP_TO_NEXT[lv]} | ${cum} | ${note} |\n`;
  }
  o += `\n- **Σ 1→15 = ${cumXpToReach(15)}** · **Σ 15→18 = ${BOSS_XP_GRANT}** = boss first-clear grant\n`;
  o += '- Soft-cap dungeon: `floor(baseXP × 0.12)` when party ≥ Lv15; overlevel band ×0.5 thêm\n\n';
  o += '### Dungeon node XP\n\n';
  o += '| Floor band | Battle | Elite | Recommended Lv |\n|------------|--------|-------|----------------|\n';
  DUNGEON_XP_BANDS.forEach(b => {
    o += `| ${b.floors} | ${b.battle} | ${b.elite} | ${b.recLv} |\n`;
  });

  o += '\n---\n\n## Skill Unlock\n\n';
  o += '| Lv | Ren | Charlotte | Coda |\n|----|-----|-----------|------|\n';
  skillUnlock.forEach(s => {
    o += `| ${s.lv} | ${s.ren || '-'} | ${s.charlotte || '-'} | ${s.coda || '-'} |\n`;
  });

  o += '\n---\n\n## Milestones\n\n';
  o += '| Range | |\n|-------|--|\n';
  o += '| 1-2 | Basic only · Space guard |\n';
  o += '| 3-5 | Counter / Setup unlock |\n';
  o += '| 9-11 | Full 3-skill kit |\n';
  o += '| 12-15 | Stat polish · soft target |\n';
  o += '| 15 | Boss entry (F16) |\n';
  o += '| 15-18 | Soft-cap grind **or** boss XP dump → Lv18 |\n';
  return o;
}

const hf = { type: 'pattern', pattern: 'solid', fgColor: { argb: 'FF1A1A2E' } };
const hfont = { bold: true, color: { argb: 'FFFFFFFF' }, size: 11 };
const sf2 = { type: 'pattern', pattern: 'solid', fgColor: { argb: 'FF16213E' } };
const sfont2 = { bold: true, color: { argb: 'FF00D2FF' }, size: 10 };
const hlF = { type: 'pattern', pattern: 'solid', fgColor: { argb: 'FF1E3A5F' } };
const bd = {
  top: { style: 'thin', color: { argb: 'FF333355' } },
  left: { style: 'thin', color: { argb: 'FF333355' } },
  bottom: { style: 'thin', color: { argb: 'FF333355' } },
  right: { style: 'thin', color: { argb: 'FF333355' } },
};

function sH(row) {
  row.eachCell(c => {
    c.fill = hf; c.font = hfont; c.border = bd;
    c.alignment = { vertical: 'middle', horizontal: 'center' };
  });
  row.height = 22;
}
function sD(cell, hl) {
  cell.border = bd;
  cell.alignment = { vertical: 'middle', horizontal: 'center' };
  if (hl) cell.fill = hlF;
}

async function main() {
  const renD = genData('ren'), charD = genData('charlotte'), codD = genData('coda');
  const wb = new ExcelJS.Workbook();
  wb.creator = 'Fractured Chorus';
  const ms = [3, 4, 5, 9, 10, 11, 15, 18];

  const ws = wb.addWorksheet('Summary', { properties: { tabColor: { argb: 'FF00D2FF' } } });
  ws.columns = Array(13).fill(null).map(() => ({ width: 8 }));
  ws.mergeCells('A1:M1');
  const t = ws.getCell('A1');
  t.value = 'FRACTURED CHORUS - STAT PROGRESS Lv1-18 (Optimal Build)';
  t.font = { bold: true, color: { argb: 'FFFFFFFF' }, size: 13 }; t.fill = hf;
  t.alignment = { horizontal: 'center', vertical: 'middle' }; ws.getRow(1).height = 28;
  const h2 = ws.addRow(['', '- REN (DPS/Melody) -', '', '', '', '- CHARLOTTE (Tank/Rhythm) -', '', '', '', '- CODA (Support/Harmony) -', '', '', '']);
  h2.eachCell(c => { c.fill = sf2; c.font = sfont2; c.border = bd; c.alignment = { vertical: 'middle', horizontal: 'center' }; });
  ws.mergeCells('B2:E2'); ws.mergeCells('F2:I2'); ws.mergeCells('J2:M2');
  const h3 = ws.addRow(['Lv', 'STR', 'Ma', 'HB', 'EN', 'STR', 'Ma', 'HB', 'EN', 'STR', 'Ma', 'HB', 'EN']);
  sH(h3);
  for (let i = 0; i < 18; i++) {
    const r = renD[i], c2 = charD[i], d2 = codD[i];
    const row = ws.addRow([i + 1, r.str, r.ma, r.hb, r.en, c2.str, c2.ma, c2.hb, c2.en, d2.str, d2.ma, d2.hb, d2.en]);
    row.eachCell(c3 => sD(c3, ms.includes(i + 1))); row.height = 18;
  }

  for (const key of ['ren', 'charlotte', 'coda']) {
    const c = chars[key], d = key === 'ren' ? renD : key === 'charlotte' ? charD : codD;
    const cols = { ren: 'FFE94560', charlotte: 'FF0F3460', coda: 'FF00D2FF' };
    const w = wb.addWorksheet(c.name, { properties: { tabColor: { argb: cols[key] } } });
    w.columns = [
      { width: 6 }, { width: 8 }, { width: 8 }, { width: 8 }, { width: 8 }, { width: 8 },
      { width: 6 }, { width: 8 }, { width: 8 }, { width: 8 }, { width: 8 }, { width: 14 }, { width: 28 },
    ];
    w.mergeCells('A1:M1');
    const tt = w.getCell('A1');
    tt.value = `${c.name.toUpperCase()} - ${c.role.toUpperCase()} - ${c.element}`;
    tt.font = { bold: true, color: { argb: 'FFFFFFFF' }, size: 13 }; tt.fill = hf;
    tt.alignment = { horizontal: 'center', vertical: 'middle' }; w.getRow(1).height = 28;
    w.mergeCells('A2:M2');
    const inf = w.getCell('A2');
    inf.value = `HP: ${c.formula}  |  Dmg: ${c.dmgType}`;
    inf.font = { italic: true, color: { argb: 'FFAAAAAA' }, size: 9 };
    inf.fill = { type: 'pattern', pattern: 'solid', fgColor: { argb: 'FF111122' } };
    inf.alignment = { horizontal: 'left', vertical: 'middle' };
    const h = w.addRow(['Lv', 'STR', 'Ma', 'HB', 'EN', 'HP', 'W', 'Latency', 'Luck', 'Crit x', 'Pts', 'Spent', 'Skill Unlock']);
    sH(h);
    const sm = {}; skillUnlock.forEach(s => { sm[s.lv] = s[key]; });
    for (let i = 0; i < 18; i++) {
      const r = d[i], u = sm[r.lv] || '';
      const cs = c.skills.filter(s => s.lv === r.lv).map(s => `${s.name} (${s.type})`).join(', ');
      const n = u || cs;
      const row = w.addRow([r.lv, r.str, r.ma, r.hb, r.en, r.hp, r.w, r.latency, `${r.luck}%`, `x${r.critMult}`, r.mp, r.spent, n]);
      const hl = [1, 3, 4, 5, 9, 10, 11, 15, 18].includes(r.lv);
      const isU = !!n;
      row.eachCell((cell, cn) => { sD(cell, hl); if (isU && cn === 13) cell.font = { bold: true, color: { argb: 'FFE94560' } }; });
      row.height = 18;
    }
  }

  const sk = wb.addWorksheet('Skill Unlock', { properties: { tabColor: { argb: 'FFE94560' } } });
  sk.columns = [{ width: 8 }, { width: 32 }, { width: 32 }, { width: 32 }];
  sk.mergeCells('A1:D1');
  const skt = sk.getCell('A1');
  skt.value = 'SKILL UNLOCK PROGRESSION'; skt.font = { bold: true, color: { argb: 'FFFFFFFF' }, size: 13 }; skt.fill = hf;
  skt.alignment = { horizontal: 'center', vertical: 'middle' }; sk.getRow(1).height = 28;
  sH(sk.addRow(['Lv', 'Ren', 'Charlotte', 'Coda']));
  for (let lv = 1; lv <= 18; lv++) {
    const s = skillUnlock.find(x => x.lv === lv);
    const row = sk.addRow([lv, s ? s.ren || '-' : '-', s ? s.charlotte || '-' : '-', s ? s.coda || '-' : '-']);
    const hl = [1, 3, 4, 5, 9, 10, 11].includes(lv);
    row.eachCell((cell, cn) => { sD(cell, hl); if (hl && cn > 1 && cell.value !== '-') cell.font = { bold: true, color: { argb: 'FFE94560' } }; });
  }

  const hpW = wb.addWorksheet('Party HP', { properties: { tabColor: { argb: 'FF533483' } } });
  hpW.columns = [{ width: 8 }, { width: 12 }, { width: 14 }, { width: 12 }, { width: 12 }];
  hpW.mergeCells('A1:E1');
  const hpt = hpW.getCell('A1');
  hpt.value = 'PARTY HP BY LEVEL (optimal build)';
  hpt.font = { bold: true, color: { argb: 'FFFFFFFF' }, size: 13 }; hpt.fill = hf;
  hpt.alignment = { horizontal: 'center', vertical: 'middle' }; hpW.getRow(1).height = 28;
  sH(hpW.addRow(['Lv', 'Ren', 'Charlotte', 'Coda', 'Total']));
  for (let i = 0; i < 18; i++) {
    const r = renD[i], c2 = charD[i], d2 = codD[i];
    const row = hpW.addRow([i + 1, r.hp, c2.hp, d2.hp, r.hp + c2.hp + d2.hp]);
    row.eachCell(c3 => sD(c3, [4, 9, 14, 17].includes(i)));
  }

  const xpW = wb.addWorksheet('Combat XP', { properties: { tabColor: { argb: 'FF2D6A4F' } } });
  xpW.columns = [{ width: 12 }, { width: 10 }, { width: 16 }, { width: 28 }];
  xpW.mergeCells('A1:D1');
  const xpt = xpW.getCell('A1');
  xpt.value = 'COMBAT XP CURVE · Soft target Lv15 · Soft-cap · Boss grant 12600';
  xpt.font = { bold: true, color: { argb: 'FFFFFFFF' }, size: 13 }; xpt.fill = hf;
  xpt.alignment = { horizontal: 'center', vertical: 'middle' }; xpW.getRow(1).height = 28;
  sH(xpW.addRow(['From→To', 'XP', 'Cum to To', 'Note']));
  let cum = 0;
  for (let lv = 1; lv < 18; lv++) {
    cum += XP_TO_NEXT[lv];
    let note = '';
    if (lv === 14) note = 'Soft target';
    if (lv === 15) note = 'Soft-cap start';
    if (lv === 17) note = 'Hard cap';
    const row = xpW.addRow([`${lv}→${lv + 1}`, XP_TO_NEXT[lv], cum, note]);
    row.eachCell(c => sD(c, [14, 15, 17].includes(lv)));
  }
  xpW.addRow([]);
  sH(xpW.addRow(['Floor band', 'Battle', 'Elite', 'Recommended Lv']));
  DUNGEON_XP_BANDS.forEach(b => {
    const row = xpW.addRow([b.floors, b.battle, b.elite, b.recLv]);
    row.eachCell(c => sD(c, false));
  });
  xpW.addRow([]);
  const bossRow = xpW.addRow(['Boss F16 grant', BOSS_XP_GRANT, 'Σ 15→18', 'First clear only']);
  bossRow.eachCell(c => sD(c, true));

  const hbW = wb.addWorksheet('HB Conversion', { properties: { tabColor: { argb: 'FF0F3460' } } });
  hbW.columns = [{ width: 12 }, { width: 10 }, { width: 12 }, { width: 24 }];
  hbW.mergeCells('A1:D1');
  const hbt = hbW.getCell('A1');
  hbt.value = 'HB CONVERSION - 1 POINT = +5 HB';
  hbt.font = { bold: true, color: { argb: 'FFFFFFFF' }, size: 13 }; hbt.fill = hf;
  hbt.alignment = { horizontal: 'center', vertical: 'middle' }; hbW.getRow(1).height = 28;
  sH(hbW.addRow(['HB', 'W', 'Latency', 'Meaning']));
  [
    [127, 7, 1, 'Charlotte Lv15 optimal'],
    [147, 8, 1, 'Coda Lv15 optimal'],
    [167, 8, 1, 'Ren Lv15 optimal'],
    [172, 9, 0, 'Ren high HB build'],
  ].forEach(d => {
    const row = hbW.addRow(d); row.eachCell(c => sD(c, false));
  });

  await wb.xlsx.writeFile(XLSX_OUT);
  console.log('Excel:', XLSX_OUT);

  fs.writeFileSync(MD_OUT, genMD(renD, charD, codD), 'utf8');
  console.log('Markdown:', MD_OUT);

  const r15 = renD[14], c15 = charD[14], o15 = codD[14];
  console.log('Lv15 check:', {
    ren: { str: r15.str, en: r15.en, hp: r15.hp, hb: r15.hb },
    charlotte: { str: c15.str, en: c15.en, hp: c15.hp, hb: c15.hb },
    coda: { ma: o15.ma, en: o15.en, hp: o15.hp, hb: o15.hb },
    partyHp: r15.hp + c15.hp + o15.hp,
    sum1to15: cumXpToReach(15),
    sum15to18: XP_TO_NEXT[15] + XP_TO_NEXT[16] + XP_TO_NEXT[17],
  });
}

main().catch(console.error);
