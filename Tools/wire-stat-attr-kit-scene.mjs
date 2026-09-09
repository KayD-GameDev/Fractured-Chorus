import fs from 'fs';

const scenePath = 'D:/Fractured-Chorus1/Assets/FracturedChorus/Scenes/CharacterBuild.unity';
const nl = '\r\n';
let text = fs.readFileSync(scenePath, 'utf8');

const SPR = {
  icon: {
    Strength: 'b1a10000c2d34e5f678901234567890a',
    Magic: 'b1a10001c2d34e5f678901234567890a',
    Endurance: 'b1a10002c2d34e5f678901234567890a',
    HeartBeat: 'b1a10003c2d34e5f678901234567890a',
    Luck: 'b1a10004c2d34e5f678901234567890a',
  },
  label: {
    Strength: 'b1a20000c2d34e5f678901234567890a',
    Magic: 'b1a20001c2d34e5f678901234567890a',
    Endurance: 'b1a20002c2d34e5f678901234567890a',
    HeartBeat: 'b1a20003c2d34e5f678901234567890a',
    Luck: 'b1a20004c2d34e5f678901234567890a',
  },
  track: 'c3d40111e2f34a5b9c0d1e2f3a4b5c6d',
  fill: 'c3d40222e2f34a5b9c0d1e2f3a4b5c6d',
  handle: 'c3d40333e2f34a5b9c0d1e2f3a4b5c6d',
};

function spr(guid) {
  return `{fileID: 21300000, guid: ${guid}, type: 3}`;
}

const rowDefs = [
  { key: 'Strength', name: 'StatRow_Strength' },
  { key: 'Magic', name: 'StatRow_Magic' },
  { key: 'Endurance', name: 'StatRow_Endurance' },
  { key: 'HeartBeat', name: 'StatRow_HeartBeat' },
  { key: 'Luck', name: 'StatRow_Luck' },
];

const parts = text.split(/\r?\n--- !u!/);
const header = parts[0];
const blocks = parts.slice(1).map((b) => {
  const m = b.match(/^(\d+) &(\d+)/);
  return {
    classId: m[1],
    id: m[2],
    body: b,
    full: `--- !u!${b}`,
  };
});

function findBlock(classId, id) {
  return blocks.find((b) => b.classId === classId && b.id === String(id));
}

function findGoByName(name) {
  return blocks.find((b) => b.classId === '1' && b.body.includes(`m_Name: ${name}`));
}

function setSprite(imgId, guid) {
  const b = findBlock('114', imgId);
  if (!b) throw new Error(`missing image ${imgId}`);
  b.body = b.body.replace(/m_Sprite: .*/, `m_Sprite: ${spr(guid)}`);
}

function setColor(imgId, rgba) {
  const b = findBlock('114', imgId);
  b.body = b.body.replace(/m_Color: \{[^}]+\}/, `m_Color: ${rgba}`);
}

function disableEnabled(compId) {
  const b = findBlock('114', compId);
  if (!b) return;
  b.body = b.body.replace(/m_Enabled: 1/, 'm_Enabled: 0');
}

function addChild(tfId, childTfId, prepend = true) {
  const b = findBlock('224', tfId);
  if (!b) throw new Error(`missing tf ${tfId}`);
  if (/m_Children: \[\]/.test(b.body)) {
    b.body = b.body.replace(
      /m_Children: \[\]/,
      `m_Children:${nl}  - {fileID: ${childTfId}}`,
    );
    return;
  }
  if (prepend) {
    b.body = b.body.replace(
      /m_Children:\r?\n/,
      `m_Children:${nl}  - {fileID: ${childTfId}}${nl}`,
    );
  } else {
    b.body = b.body.replace(
      /(m_Children:\r?\n(?:  - \{fileID: \d+\}\r?\n)*)/,
      `$1  - {fileID: ${childTfId}}${nl}`,
    );
  }
}

function patchView(viewId, { iconImg, labelImg, handleImg }) {
  const b = findBlock('114', viewId);
  if (!b) throw new Error(`missing view ${viewId}`);
  let body = b.body;
  if (!/nameLabelArt:/.test(body)) {
    body = body.replace(
      /(nameLabel: \{fileID: \d+\})/,
      `$1${nl}  nameLabelArt: {fileID: ${labelImg}}`,
    );
  } else {
    body = body.replace(/nameLabelArt: \{fileID: [^}]+\}/, `nameLabelArt: {fileID: ${labelImg}}`);
  }
  if (!/\n  icon:/.test(body)) {
    body = body.replace(
      /(valueLabel: \{fileID: \d+\})/,
      `$1${nl}  icon: {fileID: ${iconImg}}`,
    );
  } else {
    body = body.replace(/icon: \{fileID: [^}]+\}/, `icon: {fileID: ${iconImg}}`);
  }
  if (!/barHandle:/.test(body)) {
    body = body.replace(
      /(barFill: \{fileID: \d+\})/,
      `$1${nl}  barHandle: {fileID: ${handleImg}}`,
    );
  } else {
    body = body.replace(/barHandle: \{fileID: [^}]+\}/, `barHandle: {fileID: ${handleImg}}`);
  }
  b.body = body;
}

let maxId = 0;
for (const b of blocks) {
  const id = Number(b.id);
  if (Number.isSafeInteger(id) && id > 0 && id < 2_000_000_000) {
    maxId = Math.max(maxId, id);
  }
}
let next = maxId + 100;
const alloc = () => String(++next);
console.log('id base', next);

function pushImage({ name, fatherTf, amin, amax, sizeDelta, guid, type = 0, preserveAspect = 1 }) {
  const goId = alloc();
  const tfId = alloc();
  const crId = alloc();
  const imgId = alloc();
  const size =
    sizeDelta != null
      ? `  m_SizeDelta: {x: ${sizeDelta[0]}, y: ${sizeDelta[1]}}`
      : '  m_SizeDelta: {x: 0, y: 0}';
  const body = `1 &${goId}
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: ${tfId}}
  - component: {fileID: ${crId}}
  - component: {fileID: ${imgId}}
  m_Layer: 0
  m_Name: ${name}
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!224 &${tfId}
RectTransform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${goId}}
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {fileID: ${fatherTf}}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
  m_AnchorMin: {x: ${amin[0]}, y: ${amin[1]}}
  m_AnchorMax: {x: ${amax[0]}, y: ${amax[1]}}
  m_AnchoredPosition: {x: 0, y: 0}
${size}
  m_Pivot: {x: 0.5, y: 0.5}
--- !u!222 &${crId}
CanvasRenderer:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${goId}}
  m_CullTransparentMesh: 1
--- !u!114 &${imgId}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${goId}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: fe87c0e1cc204ed48ad3b37840f39efc, type: 3}
  m_Name: 
  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.Image
  m_Material: {fileID: 0}
  m_Color: {r: 1, g: 1, b: 1, a: 1}
  m_RaycastTarget: 0
  m_RaycastPadding: {x: 0, y: 0, z: 0, w: 0}
  m_Maskable: 1
  m_OnCullStateChanged:
    m_PersistentCalls:
      m_Calls: []
  m_Sprite: ${spr(guid)}
  m_Type: ${type}
  m_PreserveAspect: ${preserveAspect}
  m_FillCenter: 1
  m_FillMethod: 0
  m_FillAmount: 1
  m_FillClockwise: 1
  m_FillOrigin: 0
  m_UseSpriteMesh: 0
  m_PixelsPerUnitMultiplier: 1`;

  // split multi-docs into blocks
  const segs = body.split(/\n--- !u!/);
  for (let i = 0; i < segs.length; i++) {
    const seg = segs[i];
    const hm = seg.match(/^(\d+) &(\d+)/);
    blocks.push({
      classId: hm[1],
      id: hm[2],
      body: seg,
      full: i === 0 ? `--- !u!${seg}` : `--- !u!${seg}`,
    });
  }
  return { goId, tfId, imgId };
}

for (const def of rowDefs) {
  const go = findGoByName(def.name);
  if (!go) throw new Error(`missing ${def.name}`);
  const goId = go.id;
  const tf = blocks.find((b) => b.classId === '224' && b.body.includes(`m_GameObject: {fileID: ${goId}}`));
  const view = blocks.find(
    (b) =>
      b.classId === '114' &&
      b.body.includes(`m_GameObject: {fileID: ${goId}}`) &&
      b.body.includes('CharacterBuildStatRowView'),
  );
  const kids = [...tf.body.matchAll(/- \{fileID: (\d+)\}/g)].map((m) => m[1]);
  let nameLabelText = null;
  let barTrackTf = null;
  let barTrackImg = null;
  let barFillImg = null;

  for (const kid of kids) {
    const kidTf = findBlock('224', kid);
    const kidGoId = kidTf.body.match(/m_GameObject: \{fileID: (\d+)\}/)[1];
    const kidGo = findBlock('1', kidGoId);
    const nm = kidGo.body.match(/m_Name: (.+)/)[1].trim();
    if (nm === 'NameLabel') {
      const comps = [...kidGo.body.matchAll(/component: \{fileID: (\d+)\}/g)].map((m) => m[1]);
      nameLabelText = comps.find((c) => findBlock('114', c));
    }
    if (nm === 'BarTrack') {
      barTrackTf = kid;
      const comps = [...kidGo.body.matchAll(/component: \{fileID: (\d+)\}/g)].map((m) => m[1]);
      barTrackImg = comps.find((c) => {
        const bb = findBlock('114', c);
        return bb && bb.body.includes('UI.Image');
      });
      const fillChild = [...kidTf.body.matchAll(/- \{fileID: (\d+)\}/g)].map((m) => m[1]);
      for (const fc of fillChild) {
        const ftf = findBlock('224', fc);
        const fgoId = ftf.body.match(/m_GameObject: \{fileID: (\d+)\}/)[1];
        const fgo = findBlock('1', fgoId);
        if (fgo.body.match(/m_Name: (.+)/)[1].trim() === 'BarFill') {
          const fcomps = [...fgo.body.matchAll(/component: \{fileID: (\d+)\}/g)].map((m) => m[1]);
          barFillImg = fcomps.find((c) => {
            const bb = findBlock('114', c);
            return bb && bb.body.includes('UI.Image');
          });
        }
      }
    }
  }

  if (!barTrackImg || !barFillImg || !nameLabelText) {
    throw new Error(`incomplete ${def.name} track=${barTrackImg} fill=${barFillImg} name=${nameLabelText}`);
  }

  setSprite(barTrackImg, SPR.track);
  setSprite(barFillImg, SPR.fill);
  setColor(barFillImg, '{r: 1, g: 1, b: 1, a: 1}');
  disableEnabled(nameLabelText);

  // skip if already wired
  if (tf.body.includes('NameLabelArt') || /m_Name: NameLabelArt/.test(text)) {
    // continue creating only if not present under this row
  }
  const existingKids = kids.map((kid) => {
    const kidTf = findBlock('224', kid);
    const kidGoId = kidTf.body.match(/m_GameObject: \{fileID: (\d+)\}/)[1];
    return findBlock('1', kidGoId).body.match(/m_Name: (.+)/)[1].trim();
  });
  if (existingKids.includes('Icon') && existingKids.includes('NameLabelArt')) {
    console.log('skip existing', def.name);
    continue;
  }

  const icon = pushImage({
    name: 'Icon',
    fatherTf: tf.id,
    amin: [0.02, 0.48],
    amax: [0.12, 0.96],
    guid: SPR.icon[def.key],
  });
  const label = pushImage({
    name: 'NameLabelArt',
    fatherTf: tf.id,
    amin: [0.14, 0.5],
    amax: [0.48, 1],
    guid: SPR.label[def.key],
  });
  const handle = pushImage({
    name: 'BarHandle',
    fatherTf: barTrackTf,
    amin: [0.3, 0.5],
    amax: [0.3, 0.5],
    sizeDelta: [28, 28],
    guid: SPR.handle,
  });

  addChild(tf.id, icon.tfId, true);
  addChild(tf.id, label.tfId, true);
  addChild(barTrackTf, handle.tfId, false);
  patchView(view.id, { iconImg: icon.imgId, labelImg: label.imgId, handleImg: handle.imgId });

  console.log('wired', def.name, {
    icon: icon.imgId,
    label: label.imgId,
    handle: handle.imgId,
    track: barTrackImg,
    fill: barFillImg,
  });
}

const out =
  header +
  blocks
    .map((b) => {
      // rebuild full from classId/id/body head
      const rest = b.body.replace(/^\d+ &\d+\r?\n/, '');
      return `--- !u!${b.classId} &${b.id}${nl}${rest}`;
    })
    .join(nl) +
  nl;

fs.writeFileSync(scenePath, out);
console.log('saved', scenePath, 'blocks', blocks.length);
