"""One-shot generator for tutorial guide dash + arrowhead sprites."""
from PIL import Image, ImageDraw
import os
import uuid

ART = r"F:\Unity_Project\Fractured Chorus\Assets\FracturedChorus\Art\UI\Tutorial"
RES = r"F:\Unity_Project\Fractured Chorus\Assets\FracturedChorus\Resources\UI\Tutorial"
FILL = (255, 210, 64, 255)
EDGE = (140, 88, 8, 230)


def meta_text(guid):
    sprite_id = uuid.uuid4().hex
    return f"""fileFormatVersion: 2
guid: {guid}
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {{}}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 0
    sRGBTexture: 1
    linearTexture: 0
    fadeOut: 0
    borderMipMap: 0
    mipMapsPreserveCoverage: 0
    alphaTestReferenceValue: 0.5
    mipMapFadeDistanceStart: 1
    mipMapFadeDistanceEnd: 3
  bumpmap:
    convertToNormalMap: 0
    externalNormalMap: 0
    heightScale: 0.25
    normalMapFilter: 0
    flipGreenChannel: 0
  isReadable: 0
  streamingMipmaps: 0
  streamingMipmapsPriority: 0
  vTOnly: 0
  ignoreMipmapLimit: 0
  grayScaleToAlpha: 0
  generateCubemap: 6
  cubemapConvolution: 0
  seamlessCubemap: 0
  textureFormat: 1
  maxTextureSize: 256
  textureSettings:
    serializedVersion: 2
    filterMode: 1
    aniso: 1
    mipBias: 0
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  lightmap: 0
  compressionQuality: 50
  spriteMode: 1
  spriteExtrude: 1
  spriteMeshType: 1
  alignment: 0
  spritePivot: {{x: 0.5, y: 0.5}}
  spritePixelsToUnits: 100
  spriteBorder: {{x: 0, y: 0, z: 0, w: 0}}
  spriteGenerateFallbackPhysicsShape: 0
  alphaUsage: 1
  alphaIsTransparency: 1
  spriteTessellationDetail: -1
  textureType: 8
  textureShape: 1
  singleChannelComponent: 0
  flipbookRows: 1
  flipbookColumns: 1
  maxTextureSizeSet: 0
  compressionQualitySet: 0
  textureFormatSet: 0
  ignorePngGamma: 0
  applyGammaDecoding: 0
  swizzle: 50462976
  cookieLightType: 0
  platformSettings:
  - serializedVersion: 4
    buildTarget: DefaultTexturePlatform
    maxTextureSize: 256
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 1
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  spriteSheet:
    serializedVersion: 2
    sprites: []
    outline: []
    physicsShape: []
    bones: []
    spriteID: {sprite_id}
    internalID: 0
    vertices: []
    indices: 
    edges: []
    weights: []
  secondaryTextures: []
  nameFileIdTable: {{}}
  texturePipelineSettings:
    enableBackbufferAndDepthTexture: 0
    colorSpace: 0
    maxTextureSize: 0
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 1
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""


def save_pair(name, img, guids):
    os.makedirs(ART, exist_ok=True)
    os.makedirs(RES, exist_ok=True)
    for folder, guid in ((ART, guids[0]), (RES, guids[1])):
        path = os.path.join(folder, name)
        img.save(path, "PNG")
        with open(path + ".meta", "w", newline="\n") as handle:
            handle.write(meta_text(guid))
        print(path, img.size)


def chevron(scale=4):
    w, h = 128 * scale, 48 * scale
    img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    margin = 3 * scale
    notch = 16 * scale
    body = [
        (margin + notch, margin),
        (w - margin - notch, margin),
        (w - margin, h // 2),
        (w - margin - notch, h - margin),
        (margin + notch, h - margin),
        (margin, h // 2),
    ]
    outline = [(x + (scale if x > w / 2 else -scale), y + (scale if y > h / 2 else -scale if y != h // 2 else 0)) for x, y in body]
    # Expand outline uniformly.
    cx, cy = w / 2, h / 2
    outline = []
    for x, y in body:
        dx, dy = x - cx, y - cy
        length = max((dx * dx + dy * dy) ** 0.5, 1)
        outline.append((x + dx / length * scale * 2, y + dy / length * scale * 2))
    draw.polygon(outline, fill=EDGE)
    draw.polygon(body, fill=FILL)
    return img.resize((128, 48), Image.Resampling.LANCZOS)


def arrowhead(scale=4):
    w, h = 72 * scale, 64 * scale
    img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    margin = 4 * scale
    stem = 14 * scale
    half = 8 * scale
    body = [
        (margin, h // 2 - half),
        (margin + stem, h // 2 - half),
        (margin + stem, margin),
        (w - margin, h // 2),
        (margin + stem, h - margin),
        (margin + stem, h // 2 + half),
        (margin, h // 2 + half),
    ]
    cx, cy = w / 2, h / 2
    outline = []
    for x, y in body:
        dx, dy = x - cx, y - cy
        length = max((dx * dx + dy * dy) ** 0.5, 1)
        outline.append((x + dx / length * scale * 2.2, y + dy / length * scale * 2.2))
    draw.polygon(outline, fill=EDGE)
    draw.polygon(body, fill=FILL)
    return img.resize((72, 64), Image.Resampling.LANCZOS)


if __name__ == "__main__":
    save_pair(
        "tutorial_guide_dash_v1.png",
        chevron(),
        ("a4e8c17b2d904f6a9b3e15c7d8f04261", "b5f9d28c3e015a7b0c4f26d8e9a15372"),
    )
    save_pair(
        "tutorial_guide_arrowhead_v1.png",
        arrowhead(),
        ("c6a0e39d4f126b8c1d5037e9fab26483", "d7b1f4ae50237c9d2e6148f0abc37594"),
    )
