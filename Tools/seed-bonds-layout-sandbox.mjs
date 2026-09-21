import fs from "fs";
import path from "path";

const ROOT = "D:/Fractured-Chorus1";
const SCENE_PATH = path.join(ROOT, "Assets/FracturedChorus/Scenes/Bonds.unity");
const META_PATH = `${SCENE_PATH}.meta`;

const GUIDS = {
  scene: "7b4e1d29c6af4f6a9d8e3b2c1a0f5d74",
  bondsMenuUi: "8fd773c4b2d5498d9e42d7f1a8b3c6e5",
  hubCornerInfoHud: "f3e4d5c6b7a8901234567890abcdef01",
  socialStatsNodeView: "478c5ded5148ddc418337fb10e6954ac",
  socialStatsRadarGraphic: "c53c19fcc1d5ec745bf5efd29d943871",
  inputSystemModule: "01614664b831546d2ae94a42149d80ac",
  eventSystem: "76c392e42b5098c458856cdf6ecaaaa1",
  image: "fe87c0e1cc204ed48ad3b37840f39efc",
  text: "5f7201a12d95ffc409449d95f23cf332",
  button: "4e29b1a8efbd4b44bb3f3716e73f07ff",
  canvasScaler: "0cd44c1031e13a943bb63640046fad76",
  graphicRaycaster: "dc42784cf147c0c48a680349fa168899",
  urpCamera: "a79441f348de89743a2939f4d699eac1",
  displayFont: "d4e5f6a7b8c94091a2b3c4d5e6f70891",
  bodyFont: "787fda44816c480a9cc3cadfdc29ce24",
  background: "a7c3e91f4b2d6840b1e5f809c3d47c11",
  mockGuide: "d4ed2ad8e4ec474a9bf73f98a5630edc",
  sun: "01667d25060e423ea7cb957281a474cc",
  moon: "d9fa06efb3ad418fa101d69635333b04",
  dawn: "cae5be8b83eb4708b3224b4d18c55966",
  panel: "5cc6c195c8ed412d90ce5721e2b13cb2",
  header: "313e540b200d468bb1c08ae54fe24f7a",
  navSelected: "344d883f1ed94693809ca82ef9270734",
  chipNormal: "07bef7c40361444c98c277897ee3c384",
  chipLocked: "03308c1b4e3f4e78ae80f430bc80929d",
  episodeRow: "e64c6f9566614eff946f3ea1e32d3091",
  barTrack: "f8cf6c1570084075b9bb648190cd3a8a",
  barFill: "2916c933b4bc426293d114d27a49da2d",
  promo: "c517c443f3db409e9ed32a067b80b6c2",
  silhouette: "46a2f120e5a9437d9f9f8b2444c4c10b",
  people: "a81967d7d05746dbb04650794f2c5a92",
  socialStats: "37f505e3f0514d5db852d7fdd1f2e4e3",
  link: "e06a9728ded54b75904d3bb8f5caa28f",
  conversations: "bc9c028ace934e42a0d3e8be6dd88aca",
  memories: "8d249dde8caf40c98ad3c48694eb4398",
  gallery: "41abd2636af54dd9aaaf1f151987ecc7",
  resonance: "d3c10aa02a96432ba0f1738496672ee7",
  cadence: "fbf1d4fab0be43caa3f66f5a30d21406",
  pulse: "4404ec061d1a4203aad367bd434cfcab",
  harmony: "debfe9e27c094f91abc61655451c713c",
  rhythm: "d00d02ab826e477ab5c13b28328375fc",
  lock: "7bcfe9ec20d9445fbc2b5c829cd8bf04",
  play: "dc9995d80a4e476d81ca6a596834db8f",
  chevron: "4544ebeb766c4912b326b5645b135eb1",
  compass: "a1bce2f1bdff480fad352e8f27ab638c",
  ren: "953c91274e2e403a8a8b556cfb1e2038",
  charlotte: "f7a2c8e14b3d4f6a9e0b1c2d3e4f5a6b",
  coda: "e8b3d9f25c4e5a7b0f1c2d3e4f5a6b7c",
  astra: "ac0327a8d7b5ae04da49dbfd0a69df2c",
};

let nextId = 931000000;
const nodes = [];

function allocId() {
  nextId += 1;
  return String(nextId);
}

function color(r, g, b, a = 1) {
  return { r, g, b, a };
}

function vec(x, y) {
  return { x, y };
}

function rectDefaults() {
  return {
    anchorMin: vec(0.5, 0.5),
    anchorMax: vec(0.5, 0.5),
    anchoredPosition: vec(0, 0),
    sizeDelta: vec(100, 100),
    pivot: vec(0.5, 0.5),
  };
}

function stretchDefaults() {
  return {
    anchorMin: vec(0, 0),
    anchorMax: vec(1, 1),
    anchoredPosition: vec(0, 0),
    sizeDelta: vec(0, 0),
    pivot: vec(0.5, 0.5),
  };
}

function createUiNode(name, parent = null, rect = rectDefaults(), active = true) {
  const node = {
    kind: "ui",
    name,
    goId: allocId(),
    transformId: allocId(),
    parentId: parent ? parent.transformId : "0",
    rect,
    active,
    children: [],
    components: [],
  };
  if (parent) {
    parent.children.push(node.transformId);
  }
  nodes.push(node);
  return node;
}

function createRootNode(name) {
  const node = {
    kind: "root",
    name,
    goId: allocId(),
    transformId: allocId(),
    parentId: "0",
    active: true,
    children: [],
    components: [],
  };
  nodes.push(node);
  return node;
}

function addCanvasRenderer(node) {
  const id = allocId();
  node.components.push({ kind: "canvasRenderer", id });
  return id;
}

function addImage(node, spriteGuid = null, options = {}) {
  if (!node.components.some((component) => component.kind === "canvasRenderer")) {
    addCanvasRenderer(node);
  }
  const id = allocId();
  node.components.push({
    kind: "image",
    id,
    spriteGuid,
    imageType: options.imageType ?? 0,
    color: options.color ?? color(1, 1, 1, 1),
    raycastTarget: options.raycastTarget ?? false,
    preserveAspect: options.preserveAspect ?? false,
    enabled: options.enabled ?? true,
  });
  return id;
}

function addText(node, textValue, role = "body", options = {}) {
  if (!node.components.some((component) => component.kind === "canvasRenderer")) {
    addCanvasRenderer(node);
  }
  const id = allocId();
  node.components.push({
    kind: "text",
    id,
    textValue,
    fontGuid: role === "display" ? GUIDS.displayFont : GUIDS.bodyFont,
    fontSize: options.fontSize ?? 18,
    fontStyle: options.fontStyle ?? 0,
    alignment: options.alignment ?? 4,
    color: options.color ?? color(1, 1, 1, 1),
  });
  return id;
}

function addButton(node, targetGraphicId, interactable) {
  const id = allocId();
  node.components.push({
    kind: "button",
    id,
    targetGraphicId,
    interactable,
  });
  return id;
}

function addCanvas(node, cameraId) {
  const canvasId = allocId();
  const scalerId = allocId();
  const raycasterId = allocId();
  const menuId = allocId();
  node.components.push(
    { kind: "canvas", id: canvasId, cameraId },
    { kind: "canvasScaler", id: scalerId },
    { kind: "graphicRaycaster", id: raycasterId },
    { kind: "bondsMenuUi", id: menuId },
  );
  return { canvasId, scalerId, raycasterId, menuId };
}

function addHubCorner(node, refs) {
  const id = allocId();
  node.components.push({
    kind: "hubCorner",
    id,
    refs,
  });
  return id;
}

function addSocialStatsNode(node) {
  const id = allocId();
  const component = {
    kind: "socialStatsNode",
    id,
    refs: {
      iconImageId: "0",
      nameTextId: "0",
      rankTextId: "0",
      flavorTextId: "0",
    },
  };
  node.components.push(component);
  return component;
}

function addRadar(node) {
  if (!node.components.some((component) => component.kind === "canvasRenderer")) {
    addCanvasRenderer(node);
  }
  const id = allocId();
  node.components.push({ kind: "radar", id });
  return id;
}

function addCamera(node) {
  const cameraId = allocId();
  const listenerId = allocId();
  const urpId = allocId();
  node.position = { x: 0, y: 0, z: -10 };
  node.components.push(
    { kind: "camera", id: cameraId },
    { kind: "audioListener", id: listenerId },
    { kind: "urpCamera", id: urpId },
  );
  return { cameraId, listenerId, urpId };
}

function addEventSystem(node) {
  const inputId = allocId();
  const eventId = allocId();
  node.components.push(
    { kind: "inputSystem", id: inputId },
    { kind: "eventSystem", id: eventId },
  );
  return { inputId, eventId };
}

function buildScene() {
  const mainCamera = createRootNode("Main Camera");
  const { cameraId } = addCamera(mainCamera);

  const eventSystem = createRootNode("EventSystem");
  addEventSystem(eventSystem);

  const bondsCanvas = createUiNode("BondsCanvas", null, stretchDefaults());
  addCanvas(bondsCanvas, cameraId);

  addImage(createUiNode("Background", bondsCanvas, stretchDefaults()), GUIDS.background);

  const cornerHud = createUiNode("CornerHud", bondsCanvas);
  addImage(cornerHud, null, { color: color(1, 1, 1, 0) });
  const dateLabel = createUiNode("DateLabel", cornerHud);
  const dayLabel = createUiNode("DayLabel", cornerHud);
  const phaseIcon = createUiNode("PhaseIcon", cornerHud);
  const locationLabel = createUiNode("LocationLabel", cornerHud);
  const taglineLabel = createUiNode("TaglineLabel", cornerHud);
  const dateTextId = addText(dateLabel, "09 / 11", "display");
  const dayTextId = addText(dayLabel, "Fri", "display");
  const phaseImageId = addImage(phaseIcon, GUIDS.sun, { preserveAspect: true });
  const locationTextId = addText(locationLabel, "HIMA CITY", "display");
  const taglineTextId = addText(taglineLabel, "Music Lives in You", "body");
  addHubCorner(cornerHud, {
    dateTextId,
    dayTextId,
    phaseImageId,
    locationTextId,
    taglineTextId,
  });

  const headerBonds = createUiNode("HeaderBonds", bondsCanvas);
  addImage(headerBonds, GUIDS.header, { imageType: 1 });
  addImage(createUiNode("Icon", headerBonds), GUIDS.people, { preserveAspect: true });
  addText(createUiNode("Label", headerBonds), "BONDS", "display", { fontSize: 28 });
  addText(createUiNode("LabelJp", headerBonds), "絆", "display", { fontSize: 22 });

  const leftNav = createUiNode("LeftNav", bondsCanvas);
  navRow(leftNav, "Row_SocialStats", GUIDS.socialStats, "Social Stats", true);
  navRow(leftNav, "Row_Link", GUIDS.link, "Link", false);
  navRow(leftNav, "Row_Conversations", GUIDS.conversations, "Conversations", false);
  navRow(leftNav, "Row_Memories", GUIDS.memories, "Memories", false);
  navRow(leftNav, "Row_Gallery", GUIDS.gallery, "Gallery", false);

  const centerStats = createUiNode("CenterStats", bondsCanvas);
  addImage(centerStats, GUIDS.panel, { imageType: 1 });
  addText(createUiNode("Title", centerStats), "SOCIAL STATS", "display", { fontSize: 24 });
  addText(createUiNode("TitleJp", centerStats), "共鳴ステータス", "display", { fontSize: 18 });
  const chartRoot = createUiNode("ChartRoot", centerStats);
  const radar = createUiNode("Radar", chartRoot);
  addRadar(radar);
  statNode(centerStats, "Node_Resonance");
  statNode(centerStats, "Node_Cadence");
  statNode(centerStats, "Node_Pulse");
  statNode(centerStats, "Node_Harmony");
  statNode(centerStats, "Node_Rhythm");
  const roster = createUiNode("Roster", centerStats);
  chip(roster, 0, GUIDS.ren, GUIDS.chipNormal, false, "Ren", "Player");
  chip(roster, 1, GUIDS.charlotte, GUIDS.chipNormal, false, "Charlotte", "");
  chip(roster, 2, GUIDS.coda, GUIDS.chipNormal, false, "Coda", "");
  chip(roster, 3, GUIDS.astra, GUIDS.chipNormal, false, "Astra", "");
  chip(roster, 4, GUIDS.silhouette, GUIDS.chipLocked, true, "Ryo", "");
  chip(roster, 5, GUIDS.silhouette, GUIDS.chipLocked, true, "Mei Lin", "");
  chip(roster, 6, GUIDS.silhouette, GUIDS.chipLocked, true, "Reserved", "");
  addImage(createUiNode("Chevron", roster), GUIDS.chevron, { preserveAspect: true });

  const detailCard = createUiNode("DetailCard", bondsCanvas);
  addImage(detailCard, GUIDS.panel, { imageType: 1 });
  addImage(createUiNode("Portrait", detailCard), GUIDS.charlotte, { preserveAspect: true });
  addText(createUiNode("Name", detailCard), "Charlotte", "display", { fontSize: 24 });
  addText(createUiNode("Rank", detailCard), "Rank 1", "display", { fontSize: 18 });
  addText(
    createUiNode("Bio", detailCard),
    "A quiet yet passionate girl who always stays close to music.",
    "body",
    { fontSize: 16, alignment: 3 },
  );
  addText(
    createUiNode("Quote", detailCard),
    "Maybe... music can make the world a little kinder, right?",
    "body",
    { fontSize: 16, alignment: 3 },
  );
  const expTrack = createUiNode("ExpTrack", detailCard);
  addImage(expTrack, GUIDS.barTrack, { imageType: 1 });
  addImage(createUiNode("ExpFill", expTrack, stretchDefaults()), GUIDS.barFill, { imageType: 1 });
  addText(createUiNode("ExpLabel", detailCard), "0 / 10", "display", { fontSize: 18 });
  addText(createUiNode("NextRank", detailCard), "NEXT RANK", "display", { fontSize: 18 });
  addText(createUiNode("NextHint", detailCard), "A small step,\na closer heart.", "body", { fontSize: 16 });

  const linkEpisodes = createUiNode("LinkEpisodes", bondsCanvas);
  addImage(linkEpisodes, GUIDS.panel, { imageType: 1 });
  addText(createUiNode("Title", linkEpisodes), "LINK EPISODES", "display", { fontSize: 24 });
  addText(createUiNode("TitleJp", linkEpisodes), "リンクエピソード", "display", { fontSize: 18 });
  addText(
    createUiNode("Hint", linkEpisodes),
    "Reach higher rank to unlock new episodes.",
    "body",
    { fontSize: 16, alignment: 3 },
  );
  const promoFrame = createUiNode("PromoFrame", linkEpisodes);
  addImage(promoFrame, GUIDS.panel, { imageType: 1 });
  addImage(createUiNode("PromoImage", promoFrame), GUIDS.promo, { preserveAspect: true });
  addText(createUiNode("PromoCaption", linkEpisodes), "Music People Connect The World", "body", { fontSize: 16 });
  episodeRow(linkEpisodes, 1, "A Usual Day", true);
  episodeRow(linkEpisodes, 2, "After Class", false);
  episodeRow(linkEpisodes, 3, "A Different Melody", false);
  episodeRow(linkEpisodes, 4, "Unspoken Words", false);
  episodeRow(linkEpisodes, 5, "Toward Tomorrow", false);

  const footer = createUiNode("Footer", bondsCanvas);
  addText(createUiNode("ConfirmLabel", footer), "Confirm", "display", { fontSize: 18 });
  addText(createUiNode("BackLabel", footer), "Back", "display", { fontSize: 18 });

  const wordmark = createUiNode("Wordmark", bondsCanvas);
  addText(createUiNode("Title", wordmark), "FRACTURE CHORUS", "display", { fontSize: 22 });
  addText(createUiNode("Sub", wordmark), "MUSIC PEOPLE CONNECT THE WORLD", "body", { fontSize: 14 });

  addText(createUiNode("TaglineRight", bondsCanvas), "PEOPLE MAKE MUSIC.", "display", { fontSize: 18 });
  addImage(createUiNode("Compass", bondsCanvas), GUIDS.compass, { preserveAspect: true });
  addImage(createUiNode("MockGuide", bondsCanvas, stretchDefaults(), false), GUIDS.mockGuide, {
    color: color(1, 1, 1, 0.4),
  });
}

function navRow(parent, name, iconGuid, label, interactable) {
  const row = createUiNode(name, parent);
  const imageId = addImage(row, GUIDS.navSelected, { imageType: 1, raycastTarget: true });
  addButton(row, imageId, interactable);
  addImage(createUiNode("Icon", row), iconGuid, { preserveAspect: true });
  addText(createUiNode("Label", row), label, "body", { fontSize: 16 });
}

function statNode(parent, name) {
  const node = createUiNode(name, parent);
  const socialStatsNode = addSocialStatsNode(node);
  const iconImageId = addImage(createUiNode("Icon", node), null, { preserveAspect: true });
  const nameTextId = addText(createUiNode("Name", node), "", "body", { fontSize: 16 });
  const rankTextId = addText(createUiNode("Rank", node), "", "display", { fontSize: 16 });
  const flavorTextId = addText(createUiNode("Flavor", node), "", "body", { fontSize: 14 });
  socialStatsNode.refs = {
    iconImageId,
    nameTextId,
    rankTextId,
    flavorTextId,
  };
  return node;
}

function chip(parent, index, faceGuid, frameGuid, locked, displayName, role) {
  const root = createUiNode(`Chip_${index}`, parent);
  addImage(createUiNode("Frame", root), frameGuid, { imageType: 1 });
  addImage(createUiNode("Face", root), faceGuid, { preserveAspect: true });
  addImage(createUiNode("Lock", root), GUIDS.lock, {
    color: locked ? color(1, 1, 1, 1) : color(1, 1, 1, 0),
    preserveAspect: true,
  });
  addText(createUiNode("Name", root), displayName, "body", { fontSize: 16 });
  addText(createUiNode("Role", root), role, "body", { fontSize: 14 });
}

function episodeRow(parent, index, label, unlocked) {
  const row = createUiNode(`Row_${String(index).padStart(2, "0")}`, parent);
  const imageId = addImage(row, GUIDS.episodeRow, { imageType: 1, raycastTarget: true });
  addButton(row, imageId, unlocked);
  addImage(createUiNode("Icon", row), unlocked ? GUIDS.play : GUIDS.lock, { preserveAspect: true });
  addText(createUiNode("Index", row), String(index).padStart(2, "0"), "display", { fontSize: 16 });
  addText(createUiNode("Label", row), label, "body", { fontSize: 16, alignment: 3 });
}

function yamlHeader() {
  return `%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!29 &1
OcclusionCullingSettings:
  m_ObjectHideFlags: 0
  serializedVersion: 2
  m_OcclusionBakeSettings:
    smallestOccluder: 5
    smallestHole: 0.25
    backfaceThreshold: 100
  m_SceneGUID: 00000000000000000000000000000000
  m_OcclusionCullingData: {fileID: 0}
--- !u!104 &2
RenderSettings:
  m_ObjectHideFlags: 0
  serializedVersion: 10
  m_Fog: 0
  m_FogColor: {r: 0.5, g: 0.5, b: 0.5, a: 1}
  m_FogMode: 3
  m_FogDensity: 0.01
  m_LinearFogStart: 0
  m_LinearFogEnd: 300
  m_AmbientSkyColor: {r: 0.212, g: 0.227, b: 0.259, a: 1}
  m_AmbientEquatorColor: {r: 0.114, g: 0.125, b: 0.133, a: 1}
  m_AmbientGroundColor: {r: 0.047, g: 0.043, b: 0.035, a: 1}
  m_AmbientIntensity: 1
  m_AmbientMode: 3
  m_SubtractiveShadowColor: {r: 0.42, g: 0.478, b: 0.627, a: 1}
  m_SkyboxMaterial: {fileID: 0}
  m_HaloStrength: 0.5
  m_FlareStrength: 1
  m_FlareFadeSpeed: 3
  m_HaloTexture: {fileID: 0}
  m_SpotCookie: {fileID: 10001, guid: 0000000000000000e000000000000000, type: 0}
  m_DefaultReflectionMode: 0
  m_DefaultReflectionResolution: 128
  m_ReflectionBounces: 1
  m_ReflectionIntensity: 1
  m_CustomReflection: {fileID: 0}
  m_Sun: {fileID: 0}
  m_UseRadianceAmbientProbe: 0
--- !u!157 &3
LightmapSettings:
  m_ObjectHideFlags: 0
  serializedVersion: 13
  m_BakeOnSceneLoad: 0
  m_GISettings:
    serializedVersion: 2
    m_BounceScale: 1
    m_IndirectOutputScale: 1
    m_AlbedoBoost: 1
    m_EnvironmentLightingMode: 0
    m_EnableBakedLightmaps: 0
    m_EnableRealtimeLightmaps: 0
  m_LightmapEditorSettings:
    serializedVersion: 12
    m_Resolution: 2
    m_BakeResolution: 40
    m_AtlasSize: 1024
    m_AO: 0
    m_AOMaxDistance: 1
    m_CompAOExponent: 1
    m_CompAOExponentDirect: 0
    m_ExtractAmbientOcclusion: 0
    m_Padding: 2
    m_LightmapParameters: {fileID: 0}
    m_LightmapsBakeMode: 1
    m_TextureCompression: 1
    m_ReflectionCompression: 2
    m_MixedBakeMode: 2
    m_BakeBackend: 2
    m_PVRSampling: 1
    m_PVRDirectSampleCount: 32
    m_PVRSampleCount: 512
    m_PVRBounces: 2
    m_PVREnvironmentSampleCount: 256
    m_PVREnvironmentReferencePointCount: 2048
    m_PVRFilteringMode: 1
    m_PVRDenoiserTypeDirect: 1
    m_PVRDenoiserTypeIndirect: 1
    m_PVRDenoiserTypeAO: 1
    m_PVRFilterTypeDirect: 0
    m_PVRFilterTypeIndirect: 0
    m_PVRFilterTypeAO: 0
    m_PVREnvironmentMIS: 1
    m_PVRCulling: 1
    m_PVRFilteringGaussRadiusDirect: 1
    m_PVRFilteringGaussRadiusIndirect: 1
    m_PVRFilteringGaussRadiusAO: 1
    m_PVRFilteringAtrousPositionSigmaDirect: 0.5
    m_PVRFilteringAtrousPositionSigmaIndirect: 2
    m_PVRFilteringAtrousPositionSigmaAO: 1
    m_ExportTrainingData: 0
    m_TrainingDataDestination: TrainingData
    m_LightProbeSampleCountMultiplier: 4
  m_LightingDataAsset: {fileID: 20201, guid: 0000000000000000f000000000000000, type: 0}
  m_LightingSettings: {fileID: 0}
--- !u!196 &4
NavMeshSettings:
  serializedVersion: 2
  m_ObjectHideFlags: 0
  m_BuildSettings:
    serializedVersion: 3
    agentTypeID: 0
    agentRadius: 0.5
    agentHeight: 2
    agentSlope: 45
    agentClimb: 0.4
    ledgeDropHeight: 0
    maxJumpAcrossDistance: 0
    minRegionArea: 2
    manualCellSize: 0
    cellSize: 0.16666667
    manualTileSize: 0
    tileSize: 256
    buildHeightMesh: 0
    maxJobWorkers: 0
    preserveTilesOutsideBounds: 0
    debug:
      m_Flags: 0
  m_NavMeshData: {fileID: 0}
`;
}

function renderScene() {
  const roots = nodes.filter((node) => node.parentId === "0");
  const sceneRoots = `--- !u!1660057539 &9223372036854775807
SceneRoots:
  m_ObjectHideFlags: 0
  m_Roots:
${roots.map((node) => `  - {fileID: ${node.transformId}}`).join("\n")}
`;
  return `${yamlHeader()}${nodes.map(renderNode).join("")}${sceneRoots}`;
}

function renderNode(node) {
  const componentIds = [node.transformId, ...node.components.map((component) => component.id)];
  return `${renderGameObject(node, componentIds)}${node.kind === "ui" ? renderRectTransform(node) : renderTransform(node)}${node.components.map((component) => renderComponent(node, component)).join("")}`;
}

function renderGameObject(node, componentIds) {
  const tag = node.name === "Main Camera" ? "MainCamera" : "Untagged";
  return `--- !u!1 &${node.goId}
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
${componentIds.map((id) => `  - component: {fileID: ${id}}`).join("\n")}
  m_Layer: 0
  m_Name: ${node.name}
  m_TagString: ${tag}
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: ${node.active ? 1 : 0}
`;
}

function renderTransform(node) {
  return `--- !u!4 &${node.transformId}
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${node.goId}}
  serializedVersion: 2
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: ${node.position?.x ?? 0}, y: ${node.position?.y ?? 0}, z: ${node.position?.z ?? 0}}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children: [${node.children.map((id) => `{fileID: ${id}}`).join(", ")}]
  m_Father: {fileID: ${node.parentId}}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
`;
}

function renderRectTransform(node) {
  const rect = node.rect;
  const childrenBlock = node.children.length > 0
    ? `  m_Children:\n${node.children.map((id) => `  - {fileID: ${id}}`).join("\n")}\n`
    : "  m_Children: []\n";
  return `--- !u!224 &${node.transformId}
RectTransform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${node.goId}}
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
${childrenBlock}  m_Father: {fileID: ${node.parentId}}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
  m_AnchorMin: {x: ${rect.anchorMin.x}, y: ${rect.anchorMin.y}}
  m_AnchorMax: {x: ${rect.anchorMax.x}, y: ${rect.anchorMax.y}}
  m_AnchoredPosition: {x: ${rect.anchoredPosition.x}, y: ${rect.anchoredPosition.y}}
  m_SizeDelta: {x: ${rect.sizeDelta.x}, y: ${rect.sizeDelta.y}}
  m_Pivot: {x: ${rect.pivot.x}, y: ${rect.pivot.y}}
`;
}

function renderComponent(node, component) {
  switch (component.kind) {
    case "canvasRenderer":
      return `--- !u!222 &${component.id}
CanvasRenderer:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${node.goId}}
  m_CullTransparentMesh: 1
`;
    case "image":
      return monoHeader(node.goId, component.id, GUIDS.image, "UnityEngine.UI::UnityEngine.UI.Image", component.enabled) + `  m_Material: {fileID: 0}
  m_Color: ${renderColor(component.color)}
  m_RaycastTarget: ${component.raycastTarget ? 1 : 0}
  m_RaycastPadding: {x: 0, y: 0, z: 0, w: 0}
  m_Maskable: 1
  m_OnCullStateChanged:
    m_PersistentCalls:
      m_Calls: []
  m_Sprite: ${renderSprite(component.spriteGuid)}
  m_Type: ${component.imageType}
  m_PreserveAspect: ${component.preserveAspect ? 1 : 0}
  m_FillCenter: 1
  m_FillMethod: 4
  m_FillAmount: 1
  m_FillClockwise: 1
  m_FillOrigin: 0
  m_UseSpriteMesh: 0
  m_PixelsPerUnitMultiplier: 1
`;
    case "text":
      return monoHeader(node.goId, component.id, GUIDS.text, "UnityEngine.UI::UnityEngine.UI.Text", true) + `  m_Material: {fileID: 0}
  m_Color: ${renderColor(component.color)}
  m_RaycastTarget: 0
  m_RaycastPadding: {x: 0, y: 0, z: 0, w: 0}
  m_Maskable: 1
  m_OnCullStateChanged:
    m_PersistentCalls:
      m_Calls: []
  m_FontData:
    m_Font: {fileID: 12800000, guid: ${component.fontGuid}, type: 3}
    m_FontSize: ${component.fontSize}
    m_FontStyle: ${component.fontStyle}
    m_BestFit: 0
    m_MinSize: 10
    m_MaxSize: 40
    m_Alignment: ${component.alignment}
    m_AlignByGeometry: 0
    m_RichText: 0
    m_HorizontalOverflow: 0
    m_VerticalOverflow: 0
    m_LineSpacing: 1
  m_Text: ${escapeYaml(component.textValue)}
`;
    case "button":
      return monoHeader(node.goId, component.id, GUIDS.button, "UnityEngine.UI::UnityEngine.UI.Button", true) + `  m_Navigation:
    m_Mode: 3
    m_WrapAround: 0
    m_SelectOnUp: {fileID: 0}
    m_SelectOnDown: {fileID: 0}
    m_SelectOnLeft: {fileID: 0}
    m_SelectOnRight: {fileID: 0}
  m_Transition: 1
  m_Colors:
    m_NormalColor: {r: 1, g: 1, b: 1, a: 1}
    m_HighlightedColor: {r: 0.9607843, g: 0.9607843, b: 0.9607843, a: 1}
    m_PressedColor: {r: 0.78431374, g: 0.78431374, b: 0.78431374, a: 1}
    m_SelectedColor: {r: 0.9607843, g: 0.9607843, b: 0.9607843, a: 1}
    m_DisabledColor: {r: 0.78431374, g: 0.78431374, b: 0.78431374, a: 0.5019608}
    m_ColorMultiplier: 1
    m_FadeDuration: 0.1
  m_SpriteState:
    m_HighlightedSprite: {fileID: 0}
    m_PressedSprite: {fileID: 0}
    m_SelectedSprite: {fileID: 0}
    m_DisabledSprite: {fileID: 0}
  m_AnimationTriggers:
    m_NormalTrigger: Normal
    m_HighlightedTrigger: Highlighted
    m_PressedTrigger: Pressed
    m_SelectedTrigger: Selected
    m_DisabledTrigger: Disabled
  m_Interactable: ${component.interactable ? 1 : 0}
  m_TargetGraphic: {fileID: ${component.targetGraphicId}}
  m_OnClick:
    m_PersistentCalls:
      m_Calls: []
`;
    case "canvas":
      return `--- !u!223 &${component.id}
Canvas:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${node.goId}}
  m_Enabled: 1
  serializedVersion: 3
  m_RenderMode: 1
  m_Camera: {fileID: ${component.cameraId}}
  m_PlaneDistance: 100
  m_PixelPerfect: 0
  m_ReceivesEvents: 1
  m_OverrideSorting: 0
  m_OverridePixelPerfect: 0
  m_SortingBucketNormalizedSize: 0
  m_VertexColorAlwaysGammaSpace: 0
  m_UseReflectionProbes: 0
  m_AdditionalShaderChannelsFlag: 0
  m_UpdateRectTransformForStandalone: 0
  m_SortingLayerID: 0
  m_SortingOrder: 0
  m_TargetDisplay: 0
`;
    case "canvasScaler":
      return monoHeader(node.goId, component.id, GUIDS.canvasScaler, "UnityEngine.UI::UnityEngine.UI.CanvasScaler", true) + `  m_UiScaleMode: 1
  m_ReferencePixelsPerUnit: 100
  m_ScaleFactor: 1
  m_ReferenceResolution: {x: 1920, y: 1080}
  m_ScreenMatchMode: 0
  m_MatchWidthOrHeight: 0.5
  m_PhysicalUnit: 3
  m_FallbackScreenDPI: 96
  m_DefaultSpriteDPI: 96
  m_DynamicPixelsPerUnit: 1
  m_PresetInfoIsWorld: 0
`;
    case "graphicRaycaster":
      return monoHeader(node.goId, component.id, GUIDS.graphicRaycaster, "UnityEngine.UI::UnityEngine.UI.GraphicRaycaster", true) + `  m_IgnoreReversedGraphics: 1
  m_BlockingObjects: 0
  m_BlockingMask:
    serializedVersion: 2
    m_Bits: 4294967295
`;
    case "bondsMenuUi":
      return monoHeader(node.goId, component.id, GUIDS.bondsMenuUi, "Assembly-CSharp::FracturedChorus.Hub.BondsMenuUI", true);
    case "hubCorner":
      return monoHeader(node.goId, component.id, GUIDS.hubCornerInfoHud, "Assembly-CSharp::FracturedChorus.Hub.HubCornerInfoHud", true) + `  dateLabel: {fileID: ${component.refs.dateTextId}}
  dayLabel: {fileID: ${component.refs.dayTextId}}
  phaseIcon: {fileID: ${component.refs.phaseImageId}}
  locationLabel: {fileID: ${component.refs.locationTextId}}
  taglineLabel: {fileID: ${component.refs.taglineTextId}}
  sunSprite: {fileID: 21300000, guid: ${GUIDS.sun}, type: 3}
  moonSprite: {fileID: 21300000, guid: ${GUIDS.moon}, type: 3}
  dawnSprite: {fileID: 21300000, guid: ${GUIDS.dawn}, type: 3}
`;
    case "socialStatsNode":
      return monoHeader(node.goId, component.id, GUIDS.socialStatsNodeView, "Assembly-CSharp::FracturedChorus.Hub.SocialStatsNodeView", true) + `  iconImage: {fileID: ${component.refs.iconImageId}}
  nameLabel: {fileID: ${component.refs.nameTextId}}
  rankLabel: {fileID: ${component.refs.rankTextId}}
  flavorLabel: {fileID: ${component.refs.flavorTextId}}
`;
    case "radar":
      return monoHeader(node.goId, component.id, GUIDS.socialStatsRadarGraphic, "Assembly-CSharp::FracturedChorus.Hub.SocialStatsRadarGraphic", true) + `  m_Material: {fileID: 0}
  m_Color: {r: 1, g: 1, b: 1, a: 1}
  m_RaycastTarget: 0
  m_RaycastPadding: {x: 0, y: 0, z: 0, w: 0}
  m_Maskable: 1
  m_OnCullStateChanged:
    m_PersistentCalls:
      m_Calls: []
  maxRank: 10
  axisColor: {r: 0, g: 0.83137256, b: 1, a: 0.45}
  ringColor: {r: 0, g: 0.83137256, b: 1, a: 0.22}
  fillColor: {r: 0.56078434, g: 0.9411765, b: 1, a: 0.28}
  strokeColor: {r: 0.56078434, g: 0.9411765, b: 1, a: 1}
  strokeWidth: 2.5
`;
    case "camera":
      return `--- !u!20 &${component.id}
Camera:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${node.goId}}
  m_Enabled: 1
  serializedVersion: 2
  m_ClearFlags: 2
  m_BackGroundColor: {r: 0.011764706, g: 0.03529412, b: 0.078431375, a: 1}
  m_projectionMatrixMode: 1
  m_GateFitMode: 2
  m_FOVAxisMode: 0
  m_Iso: 200
  m_ShutterSpeed: 0.005
  m_Aperture: 16
  m_FocusDistance: 10
  m_FocalLength: 50
  m_BladeCount: 5
  m_Curvature: {x: 2, y: 11}
  m_BarrelClipping: 0.25
  m_Anamorphism: 0
  m_SensorSize: {x: 36, y: 24}
  m_LensShift: {x: 0, y: 0}
  m_NormalizedViewPortRect:
    serializedVersion: 2
    x: 0
    y: 0
    width: 1
    height: 1
  near clip plane: 0.3
  far clip plane: 1000
  field of view: 60
  orthographic: 1
  orthographic size: 5
  m_Depth: -1
  m_CullingMask:
    serializedVersion: 2
    m_Bits: 4294967295
  m_RenderingPath: -1
  m_TargetTexture: {fileID: 0}
  m_TargetDisplay: 0
  m_TargetEye: 3
  m_HDR: 1
  m_AllowMSAA: 1
  m_AllowDynamicResolution: 0
  m_ForceIntoRT: 0
  m_OcclusionCulling: 1
  m_StereoConvergence: 10
  m_StereoSeparation: 0.022
`;
    case "urpCamera":
      return monoHeader(node.goId, component.id, GUIDS.urpCamera, "Unity.RenderPipelines.Universal.Runtime::UnityEngine.Rendering.Universal.UniversalAdditionalCameraData", true) + `  m_RenderShadows: 1
  m_RequiresDepthTextureOption: 2
  m_RequiresOpaqueTextureOption: 2
  m_CameraType: 0
  m_Cameras: []
  m_RendererIndex: -1
  m_VolumeLayerMask:
    serializedVersion: 2
    m_Bits: 1
  m_VolumeTrigger: {fileID: 0}
  m_VolumeFrameworkUpdateModeOption: 2
  m_RenderPostProcessing: 0
  m_Antialiasing: 0
  m_AntialiasingQuality: 2
  m_StopNaN: 0
  m_Dithering: 0
  m_ClearDepth: 1
  m_AllowXRRendering: 1
  m_AllowHDROutput: 1
  m_UseScreenCoordOverride: 0
  m_ScreenSizeOverride: {x: 0, y: 0, z: 0, w: 0}
  m_ScreenCoordScaleBias: {x: 0, y: 0, z: 0, w: 0}
  m_RequiresDepthTexture: 0
  m_RequiresColorTexture: 0
  m_TaaSettings:
    m_Quality: 3
    m_FrameInfluence: 0.1
    m_JitterScale: 1
    m_MipBias: 0
    m_VarianceClampScale: 0.9
    m_ContrastAdaptiveSharpening: 0
  m_Version: 2
`;
    case "audioListener":
      return `--- !u!81 &${component.id}
AudioListener:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${node.goId}}
  m_Enabled: 1
`;
    case "inputSystem":
      return monoHeader(node.goId, component.id, GUIDS.inputSystemModule, "Unity.InputSystem::UnityEngine.InputSystem.UI.InputSystemUIInputModule", true) + `  m_SendPointerHoverToParent: 1
  m_MoveRepeatDelay: 0.5
  m_MoveRepeatRate: 0.1
  m_XRTrackingOrigin: {fileID: 0}
  m_ActionsAsset: {fileID: -944628639613478452, guid: ca9f5fa95ffab41fb9a615ab714db018, type: 3}
  m_PointAction: {fileID: -1654692200621890270, guid: ca9f5fa95ffab41fb9a615ab714db018, type: 3}
  m_MoveAction: {fileID: -8784545083839296357, guid: ca9f5fa95ffab41fb9a615ab714db018, type: 3}
  m_SubmitAction: {fileID: 392368643174621059, guid: ca9f5fa95ffab41fb9a615ab714db018, type: 3}
  m_CancelAction: {fileID: 7727032971491509709, guid: ca9f5fa95ffab41fb9a615ab714db018, type: 3}
  m_LeftClickAction: {fileID: 3001919216989983466, guid: ca9f5fa95ffab41fb9a615ab714db018, type: 3}
  m_MiddleClickAction: {fileID: -2185481485913320682, guid: ca9f5fa95ffab41fb9a615ab714db018, type: 3}
  m_RightClickAction: {fileID: -4090225696740746782, guid: ca9f5fa95ffab41fb9a615ab714db018, type: 3}
  m_ScrollWheelAction: {fileID: 6240969308177333660, guid: ca9f5fa95ffab41fb9a615ab714db018, type: 3}
  m_TrackedDevicePositionAction: {fileID: 6564999863303420839, guid: ca9f5fa95ffab41fb9a615ab714db018, type: 3}
  m_TrackedDeviceOrientationAction: {fileID: 7970375526676320489, guid: ca9f5fa95ffab41fb9a615ab714db018, type: 3}
  m_DeselectOnBackgroundClick: 1
  m_PointerBehavior: 0
  m_CursorLockBehavior: 0
  m_ScrollDeltaPerTick: 6
`;
    case "eventSystem":
      return monoHeader(node.goId, component.id, GUIDS.eventSystem, "UnityEngine.UI::UnityEngine.EventSystems.EventSystem", true) + `  m_FirstSelected: {fileID: 0}
  m_sendNavigationEvents: 1
  m_DragThreshold: 10
`;
    default:
      throw new Error(`Unknown component kind: ${component.kind}`);
  }
}

function monoHeader(gameObjectId, componentId, guid, classIdentifier, enabled) {
  return `--- !u!114 &${componentId}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${gameObjectId}}
  m_Enabled: ${enabled ? 1 : 0}
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: ${guid}, type: 3}
  m_Name: 
  m_EditorClassIdentifier: ${classIdentifier}
`;
}

function renderSprite(guid) {
  return guid ? `{fileID: 21300000, guid: ${guid}, type: 3}` : "{fileID: 0}";
}

function renderColor(value) {
  return `{r: ${value.r}, g: ${value.g}, b: ${value.b}, a: ${value.a}}`;
}

function escapeYaml(value) {
  return JSON.stringify(value ?? "");
}

function ensureParent(filePath) {
  fs.mkdirSync(path.dirname(filePath), { recursive: true });
}

function writeSceneFiles() {
  ensureParent(SCENE_PATH);
  const sceneText = renderScene();
  const metaText = `fileFormatVersion: 2
guid: ${GUIDS.scene}
DefaultImporter:
  externalObjects: {}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
`;
  fs.writeFileSync(SCENE_PATH, sceneText.replace(/\r\n/g, "\n"), "utf8");
  fs.writeFileSync(META_PATH, metaText, "utf8");
}

try {
  buildScene();
  writeSceneFiles();
  console.log("Seeded BondsLayoutSandbox scene and meta.");
} catch (error) {
  console.error("Failed to seed Bonds layout sandbox:", error);
  process.exitCode = 1;
}
