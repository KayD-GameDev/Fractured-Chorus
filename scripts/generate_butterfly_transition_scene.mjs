import fs from "node:fs";

const OUT = "d:/Fractured-Chorus1/Assets/FracturedChorus/Scenes/PrologueVN.unity";
const META = OUT + ".meta";

const SPR = {
  closed: "32b6df55edad4af39212506f028bc8dd",
  quarter: "771cbfe961294059a8e284ef0fb6c72c",
  half: "87c90725ed18472cb7e651237156c39b",
  open: "e18f4827d3714c72a4de540a777d0c6e",
  star: "bae0590139c64b3e8d7dcc7dfa3da4a5",
  glitter: "94c2083478484257986423eb1726f91c",
  triLg: "82d4ce1e103f42dc9da45fc63ce6d143",
  triSm: "377d2a9984924b689237d4a471d57c1e",
  diamond: "9821a737a0fe4c8d8d1b5514527f55eb",
  shard: "1de133ea42fb4160a97a3157c13cfe18",
  note: "9f77c636a97b4928bb454ffdb8adf3e6",
  freq: "bb4d260d705e45ee9df26591b353b5aa",
  waveform: "466692bdeac34e35b888994fea873c60",
  bloom: "c30371efb7aa4fea812a4266e6bc4f6e",
};

const SCRIPT = {
  controller: "32975777de2e5364a9ca5b422de71721",
  bezier: "f4f356a30696bd044928e0802be49920",
  wings: "d6b84b5050eb65242b22d3790ba55339",
  trail: "a0e944e5260d01e4ea32eeff72bf6ed0",
  wave: "7dfc8726ea5501d4d8d978dc969a30d4",
};

function sprite(guid) {
  return `{fileID: 21300000, guid: ${guid}, type: 3}`;
}

function go(id, name, comps, active = 1) {
  const lines = comps.map((c) => `  - component: {fileID: ${c}}`).join("\n");
  return `--- !u!1 &${id}
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
${lines}
  m_Layer: 5
  m_Name: ${name}
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: ${active}
`;
}

function rect(id, goId, father, children, extra) {
  const childBlock =
    children.length === 0
      ? "  m_Children: []"
      : "  m_Children:\n" + children.map((c) => `  - {fileID: ${c}}`).join("\n");
  return `--- !u!224 &${id}
RectTransform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${goId}}
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: ${extra.scale ?? 1}, y: ${extra.scale ?? 1}, z: ${extra.scale ?? 1}}
  m_ConstrainProportionsScale: 0
${childBlock}
  m_Father: {fileID: ${father}}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
  m_AnchorMin: {x: ${extra.minX}, y: ${extra.minY}}
  m_AnchorMax: {x: ${extra.maxX}, y: ${extra.maxY}}
  m_AnchoredPosition: {x: ${extra.x ?? 0}, y: ${extra.y ?? 0}}
  m_SizeDelta: {x: ${extra.w ?? 0}, y: ${extra.h ?? 0}}
  m_Pivot: {x: ${extra.px ?? 0.5}, y: ${extra.py ?? 0.5}}
`;
}

function cr(id, goId) {
  return `--- !u!222 &${id}
CanvasRenderer:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${goId}}
  m_CullTransparentMesh: 1
`;
}

function image(id, goId, spriteRef, color, raycast, preserve) {
  return `--- !u!114 &${id}
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
  m_Color: {r: ${color[0]}, g: ${color[1]}, b: ${color[2]}, a: ${color[3]}}
  m_RaycastTarget: ${raycast}
  m_RaycastPadding: {x: 0, y: 0, z: 0, w: 0}
  m_Maskable: 1
  m_OnCullStateChanged:
    m_PersistentCalls:
      m_Calls: []
  m_Sprite: ${spriteRef}
  m_Type: 0
  m_PreserveAspect: ${preserve}
  m_FillCenter: 1
  m_FillMethod: 4
  m_FillAmount: 1
  m_FillClockwise: 1
  m_FillOrigin: 0
  m_UseSpriteMesh: 0
  m_PixelsPerUnitMultiplier: 1
`;
}

function canvasGroup(id, goId, alpha) {
  return `--- !u!225 &${id}
CanvasGroup:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${goId}}
  m_Enabled: 1
  m_Alpha: ${alpha}
  m_Interactable: 0
  m_BlocksRaycasts: 0
  m_IgnoreParentGroups: 0
`;
}

const stretch = { minX: 0, minY: 0, maxX: 1, maxY: 1, x: 0, y: 0, w: 0, h: 0, px: 0.5, py: 0.5 };
const center = (w, h, x = 0, y = 0) => ({
  minX: 0.5,
  minY: 0.5,
  maxX: 0.5,
  maxY: 0.5,
  x,
  y,
  w,
  h,
  px: 0.5,
  py: 0.5,
});

const parts = [];

parts.push(`%YAML 1.1
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
  m_AmbientSkyColor: {r: 0.043, g: 0.071, b: 0.125, a: 1}
  m_AmbientEquatorColor: {r: 0.043, g: 0.071, b: 0.125, a: 1}
  m_AmbientGroundColor: {r: 0.031, g: 0.051, b: 0.094, a: 1}
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
    m_PVRFilteringGaussRadiusIndirect: 5
    m_PVRFilteringGaussRadiusAO: 2
    m_PVRFilteringAtrousPositionSigmaDirect: 0.5
    m_PVRFilteringAtrousPositionSigmaIndirect: 2
    m_PVRFilteringAtrousPositionSigmaAO: 1
    m_ExportTrainingData: 0
    m_TrainingDataDestination: TrainingData
    m_LightProbeSampleCountMultiplier: 4
  m_LightingDataAsset: {fileID: 0}
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
`);

parts.push(`--- !u!1 &100001
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: 100002}
  - component: {fileID: 100003}
  - component: {fileID: 100004}
  - component: {fileID: 100005}
  m_Layer: 0
  m_Name: Main Camera
  m_TagString: MainCamera
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &100002
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 100001}
  serializedVersion: 2
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: -10}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {fileID: 0}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
--- !u!20 &100003
Camera:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 100001}
  m_Enabled: 1
  serializedVersion: 2
  m_ClearFlags: 2
  m_BackGroundColor: {r: 0.043, g: 0.071, b: 0.125, a: 1}
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
--- !u!81 &100004
AudioListener:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 100001}
  m_Enabled: 1
--- !u!114 &100005
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 100001}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: a79441f348de89743a2939f4d699eac1, type: 3}
  m_Name: 
  m_EditorClassIdentifier: Unity.RenderPipelines.Universal.Runtime::UnityEngine.Rendering.Universal.UniversalAdditionalCameraData
  m_RenderShadows: 0
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
`);

parts.push(`--- !u!1 &100010
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: 100011}
  - component: {fileID: 100012}
  - component: {fileID: 100013}
  m_Layer: 0
  m_Name: EventSystem
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &100011
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 100010}
  serializedVersion: 2
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {fileID: 0}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
--- !u!114 &100012
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 100010}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: 76c392e42b5098c458856cdf6ecaaaa1, type: 3}
  m_Name: 
  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.EventSystems.EventSystem
  m_FirstSelected: {fileID: 0}
  m_sendNavigationEvents: 1
  m_DragThreshold: 10
--- !u!114 &100013
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 100010}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: 01614664b831546d2ae94a42149d80ac, type: 3}
  m_Name: 
  m_EditorClassIdentifier: Unity.InputSystem::UnityEngine.InputSystem.UI.InputSystemUIInputModule
  m_SendPointerHoverToParent: 1
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
`);

const canvasKids = [100201, 100301, 100401, 100501];
parts.push(go(100100, "FC_ButterflyTransition", [100101, 100102, 100103, 100104, 100105, 100106, 100107, 100108, 100109]));
parts.push(
  rect(100101, 100100, 0, canvasKids, {
    minX: 0,
    minY: 0,
    maxX: 0,
    maxY: 0,
    x: 0,
    y: 0,
    w: 0,
    h: 0,
    px: 0,
    py: 0,
    scale: 0,
  })
);
parts.push(`--- !u!223 &100102
Canvas:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 100100}
  m_Enabled: 1
  serializedVersion: 3
  m_RenderMode: 0
  m_Camera: {fileID: 0}
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
  m_SortingOrder: 80
  m_TargetDisplay: 0
--- !u!114 &100103
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 100100}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: 0cd44c1031e13a943bb63640046fad76, type: 3}
  m_Name: 
  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.CanvasScaler
  m_UiScaleMode: 1
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
--- !u!114 &100104
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 100100}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: dc42784cf147c0c48a680349fa168899, type: 3}
  m_Name: 
  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.GraphicRaycaster
  m_IgnoreReversedGraphics: 1
  m_BlockingObjects: 0
  m_BlockingMask:
    serializedVersion: 2
    m_Bits: 4294967295
`);
parts.push(canvasGroup(100105, 100100, 1));
parts.push(`--- !u!114 &100106
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 100100}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: ${SCRIPT.bezier}, type: 3}
  m_Name: 
  m_EditorClassIdentifier: Assembly-CSharp::FracturedChorus.VFX.ButterflyBezierFlight
  canvasRect: {fileID: 100101}
  target: {fileID: 100301}
  startPoint: {x: 1.1, y: 0.78}
  controlPointA: {x: 0.74, y: 0.46}
  controlPointB: {x: 0.34, y: 0.4}
  endPoint: {x: 0.04, y: 0.86}
  speedCurve:
    serializedVersion: 2
    m_Curve:
    - serializedVersion: 3
      time: 0
      value: 0
      inSlope: 0
      outSlope: 0.85
      tangentMode: 0
      weightedMode: 0
      inWeight: 0
      outWeight: 0
    - serializedVersion: 3
      time: 1
      value: 1
      inSlope: 0.55
      outSlope: 0
      tangentMode: 0
      weightedMode: 0
      inWeight: 0
      outWeight: 0
    m_PreInfinity: 2
    m_PostInfinity: 2
    m_RotationOrder: 4
  rotationLimit: 20
  oscillationAmplitude: 18
  oscillationFrequency: 1.15
--- !u!114 &100107
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 100100}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: ${SCRIPT.wings}, type: 3}
  m_Name: 
  m_EditorClassIdentifier: Assembly-CSharp::FracturedChorus.VFX.ButterflyWingAnimator
  butterflySprite: {fileID: 100313}
  wingFrames:
  - ${sprite(SPR.closed)}
  - ${sprite(SPR.quarter)}
  - ${sprite(SPR.half)}
  - ${sprite(SPR.open)}
  wingAnimationSpeed: 8
--- !u!114 &100108
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 100100}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: ${SCRIPT.trail}, type: 3}
  m_Name: 
  m_EditorClassIdentifier: Assembly-CSharp::FracturedChorus.VFX.ButterflyTrailController
  starDustRoot: {fileID: 100411}
  fragmentRoot: {fileID: 100421}
  prismRoot: {fileID: 100431}
  musicRoot: {fileID: 100441}
  waveformRoot: {fileID: 100451}
  starDustSprite: ${sprite(SPR.star)}
  glitterSprite: ${sprite(SPR.glitter)}
  prismSprites:
  - ${sprite(SPR.triLg)}
  - ${sprite(SPR.triSm)}
  - ${sprite(SPR.diamond)}
  - ${sprite(SPR.shard)}
  musicSprites:
  - ${sprite(SPR.note)}
  - ${sprite(SPR.freq)}
  - ${sprite(SPR.waveform)}
  additiveMaterial: {fileID: 0}
  starDustEmission: 28
  fragmentEmission: 6
  musicFragmentEmission: 1.4
  trailLifetime: 2.1
  trailBrightness: 1
--- !u!114 &100109
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 100100}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: ${SCRIPT.controller}, type: 3}
  m_Name: 
  m_EditorClassIdentifier: Assembly-CSharp::FracturedChorus.VFX.ButterflyTransitionController
  duration: 6.5
  autoPlay: 0
  playOnEnable: 0
  butterflySprite: {fileID: 100313}
  butterflyGlow: {fileID: 100323}
  butterflyRoot: {fileID: 100301}
  trailOrigin: {fileID: 100331}
  wingFrames:
  - ${sprite(SPR.closed)}
  - ${sprite(SPR.quarter)}
  - ${sprite(SPR.half)}
  - ${sprite(SPR.open)}
  wingAnimationSpeed: 8
  scaleCurve:
    serializedVersion: 2
    m_Curve:
    - serializedVersion: 3
      time: 0
      value: 0.7
      inSlope: 0
      outSlope: 0.8
      tangentMode: 0
      weightedMode: 0
      inWeight: 0
      outWeight: 0
    - serializedVersion: 3
      time: 0.38
      value: 1
      inSlope: 0
      outSlope: 0
      tangentMode: 0
      weightedMode: 0
      inWeight: 0
      outWeight: 0
    - serializedVersion: 3
      time: 1
      value: 0.35
      inSlope: -0.8
      outSlope: 0
      tangentMode: 0
      weightedMode: 0
      inWeight: 0
      outWeight: 0
    m_PreInfinity: 2
    m_PostInfinity: 2
    m_RotationOrder: 4
  rotationLimit: 20
  startPoint: {x: 1.1, y: 0.78}
  controlPointA: {x: 0.74, y: 0.46}
  controlPointB: {x: 0.34, y: 0.4}
  endPoint: {x: 0.04, y: 0.86}
  speedCurve:
    serializedVersion: 2
    m_Curve:
    - serializedVersion: 3
      time: 0
      value: 0
      inSlope: 0
      outSlope: 0.85
      tangentMode: 0
      weightedMode: 0
      inWeight: 0
      outWeight: 0
    - serializedVersion: 3
      time: 1
      value: 1
      inSlope: 0.55
      outSlope: 0
      tangentMode: 0
      weightedMode: 0
      inWeight: 0
      outWeight: 0
    m_PreInfinity: 2
    m_PostInfinity: 2
    m_RotationOrder: 4
  starDustEmission: 28
  fragmentEmission: 6
  musicFragmentEmission: 1.4
  trailLifetime: 2.1
  particleEmissionCurve:
    serializedVersion: 2
    m_Curve:
    - serializedVersion: 3
      time: 0
      value: 0
      inSlope: 0
      outSlope: 0
      tangentMode: 0
      weightedMode: 0
      inWeight: 0
      outWeight: 0
    - serializedVersion: 3
      time: 0.22
      value: 1
      inSlope: 0
      outSlope: 0
      tangentMode: 0
      weightedMode: 0
      inWeight: 0
      outWeight: 0
    - serializedVersion: 3
      time: 1
      value: 0
      inSlope: 0
      outSlope: 0
      tangentMode: 0
      weightedMode: 0
      inWeight: 0
      outWeight: 0
    m_PreInfinity: 2
    m_PostInfinity: 2
    m_RotationOrder: 4
  butterflyBrightness: 1
  trailBrightness: 1
  bloomIntensity: 0.55
  glowCurve:
    serializedVersion: 2
    m_Curve:
    - serializedVersion: 3
      time: 0
      value: 0
      inSlope: 0
      outSlope: 0
      tangentMode: 0
      weightedMode: 0
      inWeight: 0
      outWeight: 0
    - serializedVersion: 3
      time: 0.35
      value: 1
      inSlope: 0
      outSlope: 0
      tangentMode: 0
      weightedMode: 0
      inWeight: 0
      outWeight: 0
    - serializedVersion: 3
      time: 1
      value: 0
      inSlope: 0
      outSlope: 0
      tangentMode: 0
      weightedMode: 0
      inWeight: 0
      outWeight: 0
    m_PreInfinity: 2
    m_PostInfinity: 2
    m_RotationOrder: 4
  rootGroup: {fileID: 100105}
  fadeOverlay: {fileID: 100504}
  fadeInDuration: 0.5
  fadeOutDuration: 1
  onTransitionStarted:
    m_PersistentCalls:
      m_Calls: []
  onButterflyExit:
    m_PersistentCalls:
      m_Calls: []
  onTransitionFinished:
    m_PersistentCalls:
      m_Calls: []
`);

parts.push(go(100200, "Background", [100201, 100202, 100203]));
parts.push(rect(100201, 100200, 100101, [], stretch));
parts.push(cr(100202, 100200));
parts.push(image(100203, 100200, "{fileID: 0}", [0.043, 0.071, 0.125, 1], 1, 0));

parts.push(go(100300, "ButterflyRoot", [100301], 0));
parts.push(rect(100301, 100300, 100101, [100321, 100311, 100331], center(240, 240)));

parts.push(go(100310, "ButterflySprite", [100311, 100312, 100313]));
parts.push(rect(100311, 100310, 100301, [], center(240, 240)));
parts.push(cr(100312, 100310));
parts.push(image(100313, 100310, sprite(SPR.open), [1, 1, 1, 1], 0, 1));

parts.push(go(100320, "ButterflyGlow", [100321, 100322, 100323]));
parts.push(rect(100321, 100320, 100301, [], center(320, 320)));
parts.push(cr(100322, 100320));
parts.push(image(100323, 100320, sprite(SPR.bloom), [0.55, 0.85, 1, 0.35], 0, 1));

parts.push(go(100330, "TrailOrigin", [100331]));
parts.push(rect(100331, 100330, 100301, [], center(8, 8, -28, 0)));

parts.push(go(100400, "VFX", [100401]));
parts.push(rect(100401, 100400, 100101, [100411, 100421, 100431, 100441, 100451], stretch));

function emptyFolder(goId, rectId, name, father) {
  parts.push(go(goId, name, [rectId]));
  parts.push(rect(rectId, goId, father, [], stretch));
}
emptyFolder(100410, 100411, "StarDust", 100401);
emptyFolder(100420, 100421, "SmallFragments", 100401);
emptyFolder(100430, 100431, "PrismFragments", 100401);
emptyFolder(100440, 100441, "MusicFragments", 100401);

parts.push(go(100450, "WaveformTrails", [100451]));
parts.push(rect(100451, 100450, 100401, [100461, 100471, 100481], stretch));

function wave(goId, rectId, crId, scriptId, name, father, color) {
  parts.push(go(goId, name, [rectId, crId, scriptId]));
  parts.push(rect(rectId, goId, father, [], stretch));
  parts.push(cr(crId, goId));
  parts.push(`--- !u!114 &${scriptId}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${goId}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: ${SCRIPT.wave}, type: 3}
  m_Name: 
  m_EditorClassIdentifier: Assembly-CSharp::FracturedChorus.VFX.ButterflyWaveformTrail
  m_Material: {fileID: 0}
  m_Color: {r: ${color[0]}, g: ${color[1]}, b: ${color[2]}, a: ${color[3]}}
  m_RaycastTarget: 0
  m_Maskable: 1
  width: 2.4
`);
}
wave(100460, 100461, 100462, 100463, "Wave_0", 100451, [0, 0.83, 1, 0.55]);
wave(100470, 100471, 100472, 100473, "Wave_1", 100451, [0.55, 0.85, 1, 0.32]);
wave(100480, 100481, 100482, 100483, "Wave_2", 100451, [0.92, 0.98, 1, 0.18]);

parts.push(go(100500, "FadeOverlay", [100501, 100502, 100503, 100504]));
parts.push(rect(100501, 100500, 100101, [], stretch));
parts.push(cr(100502, 100500));
parts.push(image(100503, 100500, "{fileID: 0}", [0.031, 0.051, 0.094, 1], 0, 0));
parts.push(canvasGroup(100504, 100500, 1));

parts.push(`--- !u!1660057539 &9223372036854775807
SceneRoots:
  m_ObjectHideFlags: 0
  m_Roots:
  - {fileID: 100002}
  - {fileID: 100011}
  - {fileID: 100101}
`);

fs.writeFileSync(OUT, parts.join(""));
fs.writeFileSync(
  META,
  `fileFormatVersion: 2
guid: 7c4e9a1b2d584f0ea6c8b3d1e5f2079a
DefaultImporter:
  externalObjects: {}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
`
);
console.log("wrote", OUT);
