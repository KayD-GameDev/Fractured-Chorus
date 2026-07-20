const fs = require('fs');
const path = require('path');
const {
  Document, Packer, Paragraph, TextRun, Table, TableRow, TableCell,
  HeadingLevel, WidthType, BorderStyle, ShadingType, AlignmentType,
  PageBreak, Header, Footer, PageNumber,
} = require('docx');

const ROOT = path.join(__dirname, '..');
const OUT = path.join(ROOT, 'docs', 'combat', 'Arc1_Combat_Level_XP_Progression.docx');

const thin = { style: BorderStyle.SINGLE, size: 4, color: 'CCCCCC' };
const borders = { top: thin, bottom: thin, left: thin, right: thin };

function cell(text, opts = {}) {
  const {
    bold = false, header = false, width = 1200, align = AlignmentType.CENTER,
  } = opts;
  return new TableCell({
    borders,
    width: { size: width, type: WidthType.DXA },
    shading: header ? { type: ShadingType.CLEAR, fill: '1A1A2E' } : undefined,
    children: [new Paragraph({
      alignment: align,
      children: [new TextRun({
        text: String(text ?? ''),
        bold: bold || header,
        color: header ? 'FFFFFF' : '222222',
        size: header ? 16 : 15,
        font: 'Calibri',
      })],
    })],
  });
}

function table(headers, rows, colWidths) {
  const widths = colWidths || headers.map(() => Math.floor(9000 / headers.length));
  return new Table({
    width: { size: 9000, type: WidthType.DXA },
    columnWidths: widths,
    rows: [
      new TableRow({
        children: headers.map((h, i) => cell(h, { header: true, width: widths[i] })),
      }),
      ...rows.map(r => new TableRow({
        children: r.map((v, i) => cell(v, { width: widths[i] })),
      })),
    ],
  });
}

function h1(t) {
  return new Paragraph({ heading: HeadingLevel.HEADING_1, spacing: { before: 240, after: 120 }, children: [new TextRun({ text: t, bold: true, font: 'Calibri', size: 28 })] });
}
function h2(t) {
  return new Paragraph({ heading: HeadingLevel.HEADING_2, spacing: { before: 200, after: 100 }, children: [new TextRun({ text: t, bold: true, font: 'Calibri', size: 24 })] });
}
function h3(t) {
  return new Paragraph({ heading: HeadingLevel.HEADING_3, spacing: { before: 160, after: 80 }, children: [new TextRun({ text: t, bold: true, font: 'Calibri', size: 20 })] });
}
function p(t, opts = {}) {
  return new Paragraph({
    spacing: { after: 80 },
    children: [new TextRun({ text: t, font: 'Calibri', size: 20, italics: !!opts.italics, color: opts.color || '222222' })],
  });
}
function bullet(t) {
  return new Paragraph({
    spacing: { after: 40 },
    indent: { left: 360 },
    children: [new TextRun({ text: `• ${t}`, font: 'Calibri', size: 20 })],
  });
}
function codeBlock(lines) {
  return lines.map(line => new Paragraph({
    spacing: { after: 0 },
    shading: { type: ShadingType.CLEAR, fill: 'F4F4F8' },
    children: [new TextRun({ text: line || ' ', font: 'Consolas', size: 16, color: '333333' })],
  }));
}

function parseMdTable(block) {
  const lines = block.trim().split('\n').filter(l => l.trim().startsWith('|'));
  if (lines.length < 2) return null;
  const split = line => line.replace(/^\|/, '').replace(/\|$/, '').split('|').map(c => c.trim().replace(/\*\*/g, ''));
  const headers = split(lines[0]);
  const rows = lines.slice(2).map(split);
  return { headers, rows };
}

function extractTables(md) {
  const re = /((?:^\|.+\|[ \t]*$\r?\n)+)/gm;
  const out = [];
  let m;
  while ((m = re.exec(md))) {
    const t = parseMdTable(m[1]);
    if (t) out.push(t);
  }
  return out;
}

async function main() {
  const spec = fs.readFileSync(
    path.join(ROOT, 'docs', 'superpowers', 'specs', '2026-07-19-combat-level-xp-progression-design.md'),
    'utf8',
  );
  const progress = fs.readFileSync(
    path.join(ROOT, 'docs', 'combat', 'CHARACTER_LEVEL_PROGRESS.md'),
    'utf8',
  );

  const children = [];

  children.push(new Paragraph({
    alignment: AlignmentType.CENTER,
    spacing: { after: 120 },
    children: [new TextRun({ text: 'FRACTURED CHORUS', bold: true, size: 36, font: 'Calibri', color: '1A1A2E' })],
  }));
  children.push(new Paragraph({
    alignment: AlignmentType.CENTER,
    spacing: { after: 200 },
    children: [new TextRun({ text: 'Arc 1 — Combat Level + XP Progression', bold: true, size: 28, font: 'Calibri' })],
  }));
  children.push(p('Design lock · 2026-07-19 · Soft target Lv15 · Hard cap Lv18 · Boss grant 12600 XP', { italics: true, color: '555555' }));
  children.push(p('SoT: docs/superpowers/specs/2026-07-19-combat-level-xp-progression-design.md + docs/combat/CHARACTER_LEVEL_PROGRESS.md'));

  children.push(h1('1. Goals'));
  children.push(table(
    ['Constraint', 'Lock'],
    [
      ['Soft target', 'Dungeon F1→F15 → Party Lv15 before boss F16'],
      ['Boss scene', 'Tuned for Lv15, 14 stat pts (optimal)'],
      ['Soft-cap', 'Lv16–18 grindable but intentionally slow'],
      ['Boss first clear', '+12600 Combat XP (= Σ Lv15→18)'],
      ['Hard cap Arc 1', 'Lv18'],
      ['Lv15 combat', 'STR/Ma/HB/HP/W kept; EN optimal +5 pts'],
    ],
    [2800, 6200],
  ));

  children.push(h1('2. Model'));
  children.push(...codeBlock([
    'Dungeon F1–F15 ──XP──► Party Lv15 ──Boss F16──► +12600 XP ──► Party Lv18',
    '                         │',
    '                         └── grind post-15 (×0.12 XP) ──► slow Lv16–18',
  ]));
  children.push(bullet('1 Party Combat Level — shared XP bar'));
  children.push(bullet('On level-up: each of Ren / Charlotte / Coda gains +1 stat point'));
  children.push(bullet('Points: Lv1 = 0 · Lv15 = 14 · Lv18 = 17'));
  children.push(bullet('No separate skill-point currency — skills unlock by level'));
  children.push(bullet('HB: 1 pt → +5 · max 10 pts/stat'));

  children.push(h1('3. Stat formulas'));
  children.push(...codeBlock([
    'Stat(Lv) = Base + Growth×(Lv−1) + ManualPts×Conversion',
    'W        = clamp(7 + floor((HB − 120) / 26), 7, 10)',
    'Latency  = max(0, 2 − floor(HB / 85))',
    'Ren HP       = STR × 2.0 + 30',
    'Charlotte HP = STR × 6.0 + 50',
    'Coda HP      = STR × 2.0 + Ma × 0.35 + 15',
  ]));

  children.push(h2('Base Lv1'));
  children.push(table(
    ['', 'STR', 'Ma', 'HB', 'EN', 'HP', 'W'],
    [
      ['Ren', '22', '6', '145', '4', '74', '7'],
      ['Charlotte', '15', '5', '105', '10', '140', '7'],
      ['Coda', '6', '30', '125', '3', '38', '7'],
    ],
    [1600, 1200, 1200, 1200, 1200, 1200, 1200],
  ));

  children.push(h2('Auto-growth / level'));
  children.push(table(
    ['', 'Ren', 'Charlotte', 'Coda'],
    [
      ['STR', '+1.0', '+1.0', '+1.0'],
      ['Ma', '+0.2', '+0.1', '+1.0'],
      ['HB', '+0.5', '+0.5', '+0.5'],
      ['EN', '+0.2', '+0.3', '+0.2'],
    ],
    [2000, 2200, 2400, 2200],
  ));

  children.push(h1('4. Optimal point spend'));
  children.push(table(
    ['Char', '14 pts → Lv15', '+3 → Lv18'],
    [
      ['Ren', '6 STR / 3 HB / 5 EN', '+1 EN → +1 STR → +1 HB'],
      ['Charlotte', '6 STR / 3 HB / 5 EN', '+1 EN → +1 STR → +1 HB'],
      ['Coda', '6 Ma / 3 HB / 5 EN', '+1 EN → +1 Ma → +1 HB'],
    ],
    [2200, 3400, 3400],
  ));

  children.push(h2('Spend order (→Lv)'));
  children.push(table(
    ['→Lv', 'Point', '→Lv', 'Point', '→Lv', 'Point'],
    [
      ['2', 'EN', '7', 'HB', '12', 'STR/Ma'],
      ['3', 'STR/Ma', '8', 'EN', '13', 'STR/Ma'],
      ['4', 'STR/Ma', '9', 'STR/Ma', '14', 'EN'],
      ['5', 'EN', '10', 'HB', '15', 'HB'],
      ['6', 'STR/Ma', '11', 'EN', '16', 'EN'],
      ['', '', '', '', '17', 'STR/Ma'],
      ['', '', '', '', '18', 'HB'],
    ],
    [1200, 1600, 1200, 1600, 1200, 1600],
  ));

  children.push(h2('Lv15 reference (boss tune)'));
  children.push(table(
    ['', 'STR', 'Ma', 'HB', 'EN', 'HP', 'W', 'Lat'],
    [
      ['Ren', '42', '8.8', '167', '11.8', '114', '8', '1'],
      ['Charlotte', '35', '6.4', '127', '19.2', '260', '7', '1'],
      ['Coda', '20', '50', '147', '10.8', '73', '8', '1'],
    ],
    [1400, 1000, 1000, 1000, 1000, 1000, 800, 800],
  ));
  children.push(p('Party HP = 447'));

  children.push(h1('5. Skill unlock'));
  children.push(table(
    ['Lv', 'Ren', 'Charlotte', 'Coda'],
    [
      ['1', 'Strike', 'Ram', 'Pulse'],
      ['3', '—', 'Anchor', '—'],
      ['4', 'Crosscut', '—', '—'],
      ['5', '—', '—', 'Mend'],
      ['9', '—', 'Bulwark', '—'],
      ['10', 'Finale', '—', '—'],
      ['11', '—', '—', 'Encore'],
    ],
    [1200, 2600, 2600, 2600],
  ));

  children.push(h1('6. Combat XP curve'));
  children.push(table(
    ['From→To', 'XP', 'Cum. to To', 'Note'],
    [
      ['1→2', '60', '60', ''],
      ['2→3', '90', '150', ''],
      ['3→4', '130', '280', ''],
      ['4→5', '180', '460', ''],
      ['5→6', '240', '700', ''],
      ['6→7', '310', '1010', ''],
      ['7→8', '390', '1400', ''],
      ['8→9', '480', '1880', ''],
      ['9→10', '580', '2460', ''],
      ['10→11', '690', '3150', ''],
      ['11→12', '810', '3960', ''],
      ['12→13', '940', '4900', ''],
      ['13→14', '1080', '5980', ''],
      ['14→15', '1230', '7210', 'Soft target'],
      ['15→16', '3600', '10810', 'Soft-cap'],
      ['16→17', '4200', '15010', ''],
      ['17→18', '4800', '19810', 'Hard cap'],
    ],
    [1800, 1600, 2200, 3400],
  ));
  children.push(p('Σ 1→15 = 7210 · Σ 15→18 = 12600 = boss first-clear grant'));

  children.push(h2('Dungeon node XP'));
  children.push(table(
    ['Floor band', 'Battle', 'Elite', 'Recommended Lv'],
    [
      ['1–3', '120', '200', '1–4'],
      ['4–6', '220', '380', '4–7'],
      ['7–9', '350', '600', '7–10'],
      ['10–12', '500', '850', '10–13'],
      ['13–15', '700', '1200', '13–15'],
    ],
    [2200, 1800, 1800, 3200],
  ));

  children.push(h2('Soft-cap (dungeon only)'));
  children.push(...codeBlock([
    'granted = baseNodeXP',
    'if partyLevel >= 15: granted = floor(granted * 0.12)',
    'if partyLevel > recommendedLv + 2: granted = floor(granted * 0.5)',
    'granted = max(granted, 1)',
  ]));
  children.push(p('Elite F15 @ Lv15 ≈ 144 XP → ~25 elites for 15→16. Boss grant ignores soft-cap.'));

  children.push(h2('Boss F16 first clear'));
  children.push(table(
    ['Rule', 'Value'],
    [
      ['Grant', '+12600 Combat XP'],
      ['Expected entry', 'Lv15 @ 0% into next'],
      ['Expected exit', 'Lv18'],
      ['Underleveled', 'Same grant (may stop <18)'],
      ['Already >15', 'Fill toward 18; overflow discarded'],
      ['Repeat clear (MVP)', '0 Combat XP'],
    ],
    [3000, 6000],
  ));

  children.push(new Paragraph({ children: [new PageBreak()] }));

  children.push(h1('Appendix — Full level tables (optimal)'));
  children.push(p('Generated from Tools/generate-stat-excel.js · CHARACTER_LEVEL_PROGRESS.md', { italics: true, color: '555555' }));

  const sections = [
    { title: 'Ren — DPS · Melody', start: '## Ren —', end: '## Charlotte —' },
    { title: 'Charlotte — Tank · Rhythm', start: '## Charlotte —', end: '## Coda —' },
    { title: 'Coda — Support · Harmony', start: '## Coda —', end: '## Party HP' },
  ];

  for (const sec of sections) {
    const i0 = progress.indexOf(sec.start);
    const i1 = progress.indexOf(sec.end);
    if (i0 < 0 || i1 < 0) continue;
    const chunk = progress.slice(i0, i1);
    const tables = extractTables(chunk);
    children.push(h2(sec.title));
    if (tables[0]) {
      const t = tables[0];
      const narrow = t.headers.map((_, i) => {
        if (i === 0) return 500;
        if (i === t.headers.length - 1) return 1400;
        if (i === t.headers.length - 2) return 1100;
        return 700;
      });
      children.push(table(t.headers, t.rows, narrow));
    }
  }

  children.push(h2('Party HP (optimal)'));
  children.push(table(
    ['Lv', 'Ren', 'Charlotte', 'Coda', 'Total'],
    [
      ['1', '74', '140', '38', '252'],
      ['5', '86', '176', '48', '310'],
      ['10', '100', '218', '60', '378'],
      ['15', '114', '260', '73', '447'],
      ['18', '122', '284', '80', '486'],
    ],
    [1200, 1600, 2000, 1600, 1600],
  ));

  children.push(h1('Out of scope'));
  children.push(bullet('CombatXp / party level runtime'));
  children.push(bullet('Grunt/Elite rewardXp on encounter assets'));
  children.push(bullet('Level-up UI / stat spend screen'));
  children.push(bullet('Persist party level in GameMetaState / RunSnapshot'));

  const doc = new Document({
    creator: 'Fractured Chorus',
    title: 'Arc 1 Combat Level + XP Progression',
    description: 'Design lock — soft target Lv15, soft-cap, boss XP grant 12600',
    styles: {
      default: { document: { styles: [{ id: 'Normal', run: { font: 'Calibri', size: 20 } }] } },
    },
    sections: [{
      properties: {
        page: {
          margin: { top: 720, bottom: 720, left: 720, right: 720 },
        },
      },
      headers: {
        default: new Header({
          children: [new Paragraph({
            children: [new TextRun({ text: 'Fractured Chorus · Arc 1 Combat Level + XP', italics: true, size: 16, color: '888888', font: 'Calibri' })],
          })],
        }),
      },
      footers: {
        default: new Footer({
          children: [new Paragraph({
            alignment: AlignmentType.CENTER,
            children: [
              new TextRun({ text: 'Page ', size: 16, font: 'Calibri', color: '888888' }),
              new TextRun({ children: [PageNumber.CURRENT], size: 16, font: 'Calibri', color: '888888' }),
            ],
          })],
        }),
      },
      children,
    }],
  });

  const buf = await Packer.toBuffer(doc);
  fs.writeFileSync(OUT, buf);
  console.log('Wrote', OUT);
  void spec;
}

main().catch(err => {
  console.error(err);
  process.exit(1);
});
