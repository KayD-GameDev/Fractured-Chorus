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
function calcAV(hb) { return Math.round(12000 / hb * 10) / 10; }
function calcGuard(k, en, ma) {
  if (k === 'ren') return Math.min(en * 2.5, 65);
  if (k === 'charlotte') return Math.min(en * 3.0, 75);
  return Math.min(en * 2 + ma * 0.5, 55);
}

const chars = {
  ren: {
    name: 'Ren', role: 'DPS', element: 'Melody',
    base: { str: 22, ma: 6, hb: 145, en: 4 },
    growth: { str: 1.0, ma: 0.2, hb: 0.5, en: 0.2 },
    luck: [8,9,10,11,11,12,13,14,14,15,16,16,17,17,18,19,19,20],
    critMult: [1.15,1.16,1.17,1.18,1.20,1.21,1.23,1.24,1.26,1.28,1.29,1.31,1.32,1.34,1.35,1.37,1.38,1.40],
    dmgType: 'Physical', guardType: 'Guard',
    formula: 'HP = STR * 2.0 + 30',
    guardFormula: 'min(EN * 2.5, 65%)',
    skills: [
      { lv: 1, name: 'Strike', type: 'Basic' },
      { lv: 1, name: 'Guard', type: 'Guard' },
      { lv: 4, name: 'Riposte', type: 'Sig A' },
      { lv: 10, name: 'Finale', type: 'Sig B' },
    ],
    alloc: { str: [0,0,1,1,0,1,0,0,1,0,0,1,1,0,0,1,0], hb: [0,0,0,0,1,0,0,0,0,1,0,0,0,1,0,0,1], en: [0,1,0,0,0,0,1,1,0,0,1,0,0,0,1,0,0], ma: [] },
  },
  charlotte: {
    name: 'Charlotte', role: 'Tank', element: 'Rhythm',
    base: { str: 15, ma: 5, hb: 105, en: 10 },
    growth: { str: 1.0, ma: 0.1, hb: 0.5, en: 0.3 },
    luck: [3,3,4,4,5,5,5,6,6,7,7,7,8,8,8,9,9,9],
    critMult: [1.05,1.05,1.06,1.06,1.08,1.08,1.09,1.09,1.10,1.12,1.12,1.13,1.14,1.14,1.15,1.16,1.17,1.18],
    dmgType: 'Physical', guardType: 'Parry',
    formula: 'HP = STR * 6.0 + 50',
    guardFormula: 'min(EN * 3.0, 75%)',
    skills: [
      { lv: 1, name: 'Ram', type: 'Basic' },
      { lv: 1, name: 'Parry', type: 'Guard' },
      { lv: 3, name: 'Bulwark', type: 'Sig A' },
      { lv: 9, name: 'Hold the Line', type: 'Sig B' },
    ],
    alloc: { str: [0,0,1,1,0,1,0,0,1,0,0,1,1,0,0,1,0], hb: [0,0,0,0,1,0,0,0,0,1,0,0,0,1,0,0,1], en: [0,1,0,0,0,0,1,1,0,0,1,0,0,0,1,0,0], ma: [] },
  },
  coda: {
    name: 'Coda', role: 'Support', element: 'Harmony',
    base: { str: 6, ma: 30, hb: 125, en: 3 },
    growth: { str: 1.0, ma: 1.0, hb: 0.5, en: 0.2 },
    luck: [7,8,8,9,10,10,11,12,12,13,14,14,15,15,16,17,17,18],
    critMult: [1.12,1.13,1.14,1.15,1.18,1.19,1.20,1.22,1.23,1.25,1.26,1.27,1.28,1.29,1.30,1.32,1.33,1.35],
    dmgType: 'Magical', guardType: 'Ward',
    formula: 'HP = STR * 2.0 + Ma * 0.35 + 15',
    guardFormula: 'min(EN * 2 + Ma * 0.5, 55%)',
    skills: [
      { lv: 1, name: 'Pulse', type: 'Basic' },
      { lv: 1, name: 'Ward', type: 'Guard' },
      { lv: 5, name: 'Arc', type: 'Sig A' },
      { lv: 11, name: 'Cadence', type: 'Sig B' },
    ],
    alloc: { str: [], hb: [0,0,0,0,1,0,0,0,0,1,0,0,0,1,0,0,1], en: [0,1,0,0,0,0,1,1,0,0,1,0,0,0,1,0,0], ma: [0,1,0,1,0,0,1,0,1,0,1,0,1,0,1,0,1] },
  },
};

const skillUnlock = [
  { lv: 1, ren: 'Strike (Basic) + Guard', charlotte: 'Ram (Basic) + Parry (Guard)', coda: 'Pulse (Basic) + Ward (Guard)' },
  { lv: 3, ren: '', charlotte: 'Bulwark (Sig A)', coda: '' },
  { lv: 4, ren: 'Riposte (Sig A)', charlotte: '', coda: '' },
  { lv: 5, ren: '', charlotte: '', coda: 'Arc (Sig A)' },
  { lv: 9, ren: '', charlotte: 'Hold the Line (Sig B)', coda: '' },
  { lv: 10, ren: 'Finale (Sig B)', charlotte: '', coda: '' },
  { lv: 11, ren: '', charlotte: '', coda: 'Cadence (Sig B)' },
];

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
    rows.push({ lv, str: s, ma: m, hb: h, en: e, hp: calcHP(key, s, m), av: calcAV(h), luck: c.luck[i], critMult: c.critMult[i], guard: calcGuard(key, e, m), mp: lv > 1 ? i : 0 });
  }
  return rows;
}

function genMD(ren, ch, co) {
  const M = [1,3,4,5,9,10,11,15,18];
  let o = '# Character Level Progression\n\n> Cap arc 1: Lv 18 · 17 manual points · HB 1pt = +5\n\n---\n\n## Stat Allocation\n\n| Into | Gain | Impact |\n|------|------|--------|\n| STR | +1 | Ren/Coda +2HP, Charlotte +6HP |\n| Ma | +1 | +Skill dmg |\n| EN | +1 | +Guard% · +0.3-0.6 AV |\n| HB | +5 | +3-4 AV ~0.5 beat/cycle |\n\nFormula: `Stat(Lv) = Base + Growth*(Lv-1) + Pts*Conversion`\nMax/stat: 10 · Total: 17\n\n---\n\n## Auto-Growth\n\n| Stat | Ren | Charlotte | Coda |\n|------|-----|-----------|------|\n| STR | +1.0 | +1.0 | +1.0 |\n| Ma | +0.2 | +0.1 | +1.0 |\n| HB | +0.5 | +0.5 | +0.5 |\n| EN | +0.2 | +0.3 | +0.2 |\n\n';

  [{ k: 'ren', d: ren, gl: 'Guard' }, { k: 'charlotte', d: ch, gl: 'Parry' }, { k: 'coda', d: co, gl: 'Ward' }].forEach(cfg => {
    const c = chars[cfg.k], sm = {};
    skillUnlock.forEach(s => sm[s.lv] = s[cfg.k]);
    o += `---\n\n## ${c.name} — ${c.role} · ${c.element}\n\nHP: ${c.formula} · ${cfg.gl}: ${c.guardFormula} · Dmg: ${c.dmgType}\n\n`;
    o += `| Lv | STR | Ma | HB | EN | HP | ${cfg.gl}% | AV | Luck | Crit | Pts | Skill |\n|----|-----|----|----|----|----|---|-----|------|------|-----|-------|\n`;
    cfg.d.forEach(r => {
      const u = sm[r.lv] || '', cs = c.skills.filter(s => s.lv === r.lv).map(s => `${s.name}(${s.type})`).join(', ');
      const n = [cs, u].filter(Boolean).join(' + ');
      o += `| ${M.includes(r.lv) ? '**'+r.lv+'**' : r.lv} | ${r.str} | ${r.ma} | ${r.hb} | ${r.en} | ${r.hp} | ${r.guard} | ${r.av} | ${r.luck}% | x${r.critMult} | ${r.mp} | ${n} |\n`;
    });
    o += `\nOptimal Lv15: ${cfg.k === 'coda' ? '6 Ma' : '6 STR'} -> 3 HB -> 5 EN\n\n`;
  });

  o += `---\n\n## Party HP (auto-growth)\n\n| Lv | Ren | Charlotte | Coda | Total |\n|----|-----|-----------|------|-------|\n`;
  [0,4,9,14,17].forEach(i => { o += `| ${i+1} | ${ren[i].hp} | ${ch[i].hp} | ${co[i].hp} | **${ren[i].hp+ch[i].hp+co[i].hp}** |\n`; });

  o += `\n---\n\n## Skill Unlock\n\n| Lv | Ren | Charlotte | Coda |\n|----|-----|-----------|------|\n`;
  skillUnlock.forEach(s => { o += `| ${s.lv} | ${s.ren || '-'} | ${s.charlotte || '-'} | ${s.coda || '-'} |\n`; });

  o += `\n---\n\n## Milestones\n\n| Range | |\n|-------|--|\n| 1-3 | Basic + Guard |\n| 3-5 | Sig A unlock |\n| 5-9 | Full counter kit |\n| 9-11 | Sig B. Full kit |\n| 12-15 | Stats ~target |\n| 15-18 | Grind zone |\n`;
  return o;
}

const hf = { type: 'pattern', pattern: 'solid', fgColor: { argb: 'FF1A1A2E' } };
const hfont = { bold: true, color: { argb: 'FFFFFFFF' }, size: 11 };
const sf2 = { type: 'pattern', pattern: 'solid', fgColor: { argb: 'FF16213E' } };
const sfont2 = { bold: true, color: { argb: 'FF00D2FF' }, size: 10 };
const hlF = { type: 'pattern', pattern: 'solid', fgColor: { argb: 'FF1E3A5F' } };
const bd = { top: { style: 'thin', color: { argb: 'FF333355' } }, left: { style: 'thin', color: { argb: 'FF333355' } }, bottom: { style: 'thin', color: { argb: 'FF333355' } }, right: { style: 'thin', color: { argb: 'FF333355' } } };

function sH(row) { row.eachCell(c => { c.fill = hf; c.font = hfont; c.border = bd; c.alignment = { vertical: 'middle', horizontal: 'center' }; }); row.height = 22; }
function sD(cell, hl) { cell.border = bd; cell.alignment = { vertical: 'middle', horizontal: 'center' }; if (hl) cell.fill = hlF; }

async function main() {
  const renD = genData('ren'), charD = genData('charlotte'), codD = genData('coda');
  const wb = new ExcelJS.Workbook();
  wb.creator = 'Fractured Chorus';
  const ms = [3,4,5,9,10,11,15,18];

  // Summary
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
    const row = ws.addRow([i+1, r.str, r.ma, r.hb, r.en, c2.str, c2.ma, c2.hb, c2.en, d2.str, d2.ma, d2.hb, d2.en]);
    row.eachCell(c3 => sD(c3, ms.includes(i+1))); row.height = 18;
  }

  // Character tabs
  for (const key of ['ren', 'charlotte', 'coda']) {
    const c = chars[key], d = key === 'ren' ? renD : key === 'charlotte' ? charD : codD;
    const cols = { ren: 'FFE94560', charlotte: 'FF0F3460', coda: 'FF00D2FF' };
    const w = wb.addWorksheet(c.name, { properties: { tabColor: { argb: cols[key] } } });
    w.columns = [{ width: 6 }, { width: 8 }, { width: 8 }, { width: 8 }, { width: 8 }, { width: 8 }, { width: 12 }, { width: 8 }, { width: 8 }, { width: 8 }, { width: 10 }, { width: 30 }];
    w.mergeCells('A1:L1');
    const tt = w.getCell('A1');
    tt.value = `${c.name.toUpperCase()} - ${c.role.toUpperCase()} - ${c.element}`;
    tt.font = { bold: true, color: { argb: 'FFFFFFFF' }, size: 13 }; tt.fill = hf;
    tt.alignment = { horizontal: 'center', vertical: 'middle' }; w.getRow(1).height = 28;
    w.mergeCells('A2:L2');
    const inf = w.getCell('A2');
    inf.value = `HP: ${c.formula}  |  ${c.guardType}: ${c.guardFormula}  |  Dmg: ${c.dmgType}`;
    inf.font = { italic: true, color: { argb: 'FFAAAAAA' }, size: 9 };
    inf.fill = { type: 'pattern', pattern: 'solid', fgColor: { argb: 'FF111122' } };
    inf.alignment = { horizontal: 'left', vertical: 'middle' };
    const h = w.addRow(['Lv', 'STR', 'Ma', 'HB', 'EN', 'HP', `${c.guardType} %`, 'AV', 'Luck', 'Crit x', 'Pts', 'Skill Unlock']);
    sH(h);
    const sm = {}; skillUnlock.forEach(s => sm[s.lv] = s[key]);
    for (let i = 0; i < 18; i++) {
      const r = d[i], u = sm[r.lv] || '';
      const cs = c.skills.filter(s => s.lv === r.lv).map(s => `${s.name} (${s.type})`).join(', ');
      const n = [cs, u].filter(Boolean).join(' + ');
      const row = w.addRow([r.lv, r.str, r.ma, r.hb, r.en, r.hp, r.guard, r.av, `${r.luck}%`, `x${r.critMult}`, r.mp, n]);
      const hl = [1,3,4,5,9,10,11,15,18].includes(r.lv);
      const isU = c.skills.some(s => s.lv === r.lv) || !!u;
      row.eachCell((cell, cn) => { sD(cell, hl); if (isU && cn === 12) cell.font = { bold: true, color: { argb: 'FFE94560' } }; });
      row.height = 18;
    }
  }

  // Skill Unlock
  const sk = wb.addWorksheet('Skill Unlock', { properties: { tabColor: { argb: 'FFE94560' } } });
  sk.columns = [{ width: 8 }, { width: 32 }, { width: 32 }, { width: 32 }];
  sk.mergeCells('A1:D1');
  const skt = sk.getCell('A1');
  skt.value = 'SKILL UNLOCK PROGRESSION'; skt.font = { bold: true, color: { argb: 'FFFFFFFF' }, size: 13 }; skt.fill = hf;
  skt.alignment = { horizontal: 'center', vertical: 'middle' }; sk.getRow(1).height = 28;
  sH(sk.addRow(['Lv', 'Ren', 'Charlotte', 'Coda']));
  for (let lv = 1; lv <= 18; lv++) {
    const s = skillUnlock.find(x => x.lv === lv);
    const row = sk.addRow([lv, s ? s.ren : '-', s ? s.charlotte : '-', s ? s.coda : '-']);
    const hl = [1,3,4,5,9,10,11].includes(lv);
    row.eachCell((cell, cn) => { sD(cell, hl); if (hl && cn > 1 && cell.value !== '-') cell.font = { bold: true, color: { argb: 'FFE94560' } }; });
  }

  // Party HP
  const hpW = wb.addWorksheet('Party HP', { properties: { tabColor: { argb: 'FF533483' } } });
  hpW.columns = [{ width: 8 }, { width: 12 }, { width: 14 }, { width: 12 }, { width: 12 }];
  hpW.mergeCells('A1:E1');
  const hpt = hpW.getCell('A1');
  hpt.value = 'PARTY HP BY LEVEL (auto-growth only)';
  hpt.font = { bold: true, color: { argb: 'FFFFFFFF' }, size: 13 }; hpt.fill = hf;
  hpt.alignment = { horizontal: 'center', vertical: 'middle' }; hpW.getRow(1).height = 28;
  sH(hpW.addRow(['Lv', 'Ren', 'Charlotte', 'Coda', 'Total']));
  for (let i = 0; i < 18; i++) {
    const r = renD[i], c2 = charD[i], d2 = codD[i];
    const row = hpW.addRow([i+1, r.hp, c2.hp, d2.hp, r.hp + c2.hp + d2.hp]);
    row.eachCell(c3 => sD(c3, [4,9,14].includes(i)));
  }

  // HB Conversion
  const hbW = wb.addWorksheet('HB Conversion', { properties: { tabColor: { argb: 'FF0F3460' } } });
  hbW.columns = [{ width: 12 }, { width: 14 }, { width: 14 }, { width: 12 }, { width: 22 }];
  hbW.mergeCells('A1:E1');
  const hbt = hbW.getCell('A1');
  hbt.value = 'HB CONVERSION - 1 POINT = +5 HB';
  hbt.font = { bold: true, color: { argb: 'FFFFFFFF' }, size: 13 }; hbt.fill = hf;
  hbt.alignment = { horizontal: 'center', vertical: 'middle' }; hbW.getRow(1).height = 28;
  sH(hbW.addRow(['HB Before', 'AV (+1)', 'AV (+5)', 'Shift', 'Meaning']));
  [[100,119.2,114.3,-4.9,'0.8s faster'],[120,100,96,-4,'0.7s faster'],[125,96,92.3,-3.7,'0.7s faster'],[140,85.7,82.8,-2.9,'0.5s faster'],[145,82.8,80,-2.8,'0.5s faster'],[150,80,77.4,-2.6,'0.5s faster'],[170,70.6,68.6,-2,'0.4s faster']].forEach(d => {
    const row = hbW.addRow(d); row.eachCell(c => sD(c, false));
  });

  await wb.xlsx.writeFile(XLSX_OUT);
  console.log('Excel:', XLSX_OUT);

  fs.writeFileSync(MD_OUT, genMD(renD, charD, codD), 'utf8');
  console.log('Markdown:', MD_OUT);
}

main().catch(console.error);
