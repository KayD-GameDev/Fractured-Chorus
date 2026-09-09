import fs from 'fs';

const scenePath = 'D:/Fractured-Chorus1/Assets/FracturedChorus/Scenes/CharacterBuild.unity';
const text = fs.readFileSync(scenePath, 'utf8');
const parts = text.split(/\r?\n--- !u!/);

const rows = [
  'StatRow_Strength',
  'StatRow_Magic',
  'StatRow_Endurance',
  'StatRow_HeartBeat',
  'StatRow_Luck',
];

function findGo(name) {
  for (const b of parts) {
    if (b.includes(`m_Name: ${name}`) && /^1 &/.test(b)) {
      return { id: b.match(/^1 &(\d+)/)[1], b };
    }
  }
  return null;
}

for (const r of rows) {
  const go = findGo(r);
  console.log(`\n== ${r} go=${go && go.id}`);
  if (!go) continue;
  let kids = [];
  for (const b of parts) {
    if (/^224 &/.test(b) && b.includes(`m_GameObject: {fileID: ${go.id}}`)) {
      kids = [...b.matchAll(/- \{fileID: (\d+)\}/g)].map((m) => m[1]);
      console.log(' tf children', kids.join(','));
    }
    if (
      /^114 &/.test(b) &&
      b.includes(`m_GameObject: {fileID: ${go.id}}`) &&
      b.includes('CharacterBuildStatRowView')
    ) {
      const lines = b
        .split(/\r?\n/)
        .filter((l) =>
          /kind:|nameLabel|valueLabel|icon:|barFill|barHandle|minus|spent|plus|alloc|nameLabelArt/.test(
            l,
          ),
        );
      console.log(lines.join(' | '));
    }
  }
  for (const kid of kids) {
    const tf = parts.find((b) => b.startsWith(`224 &${kid}`));
    if (!tf) continue;
    const gid = tf.match(/m_GameObject: \{fileID: (\d+)\}/)[1];
    const g = parts.find((b) => b.startsWith(`1 &${gid}`));
    const nm = g.match(/m_Name: (.+)/)[1].trim();
    const comps = [...g.matchAll(/component: \{fileID: (\d+)\}/g)].map((m) => m[1]);
    let sprite = '-';
    let imgId = '-';
    for (const c of comps) {
      const img = parts.find((b) => b.startsWith(`114 &${c}`) && b.includes('UI.Image'));
      if (img) {
        imgId = c;
        sprite = (img.match(/m_Sprite: (.+)/) || [])[1];
      }
    }
    console.log(`  ${nm} go=${gid} tf=${kid} img=${imgId} sprite=${sprite}`);
  }
}
