"""Tutorial QTE hand and manga bubble sprites."""
import os
import uuid
from PIL import Image, ImageDraw

ART = r"F:\Unity_Project\Fractured Chorus\Assets\FracturedChorus\Art\UI\Tutorial"
RES = r"F:\Unity_Project\Fractured Chorus\Assets\FracturedChorus\Resources\UI\Tutorial"
INK = (42, 28, 22, 255)
SKIN = (244, 214, 186, 255)
SKIN_SHADOW = (214, 170, 140, 255)
PAPER = (255, 248, 236, 255)


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
  maxTextureSize: 512
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
    maxTextureSize: 512
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


def save_pair(name, image, guids):
    os.makedirs(ART, exist_ok=True)
    os.makedirs(RES, exist_ok=True)
    for folder, guid in ((ART, guids[0]), (RES, guids[1])):
        path = os.path.join(folder, name)
        image.save(path, "PNG")
        with open(path + ".meta", "w", newline="\n") as handle:
            handle.write(meta_text(guid))
        print(path, image.size)


def stroke_round(draw, box, fill, outline, width):
    x0, y0, x1, y1 = box
    pad = width
    draw.rounded_rectangle((x0 - pad, y0 - pad, x1 + pad, y1 + pad), radius=(x1 - x0) / 2 + pad, fill=outline)
    draw.rounded_rectangle(box, radius=(x1 - x0) / 2, fill=fill)


def hand(scale=4):
    # Five separate digits: long index, three shorter fingers, thumb to the side.
    w, h = 220 * scale, 220 * scale
    img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    s = scale

    def finger(box):
        stroke_round(draw, box, SKIN, INK, 4 * s)

    stroke_round(draw, (62 * s, 108 * s, 150 * s, 196 * s), SKIN, INK, 6 * s)
    finger((78 * s, 10 * s, 108 * s, 128 * s))
    finger((114 * s, 36 * s, 140 * s, 124 * s))
    finger((144 * s, 58 * s, 168 * s, 128 * s))
    finger((170 * s, 82 * s, 194 * s, 138 * s))
    finger((22 * s, 118 * s, 70 * s, 156 * s))
    draw.ellipse((86 * s, 18 * s, 102 * s, 34 * s), fill=SKIN_SHADOW)
    # Index points right so the cursor lies horizontal beside the QTE ring.
    img = img.transpose(Image.Transpose.ROTATE_270)
    return img.resize((220, 220), Image.Resampling.LANCZOS)


def bubble(scale=4):
    w, h = 480 * scale, 200 * scale
    img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    margin = 16 * scale
    body = [margin, margin, w - margin, h - 46 * scale]
    draw.rounded_rectangle(body, radius=36 * scale, fill=INK)
    inset = 8 * scale
    draw.rounded_rectangle(
        [body[0] + inset, body[1] + inset, body[2] - inset, body[3] - inset],
        radius=28 * scale,
        fill=PAPER,
    )
    tail = [
        (w - 120 * scale, h - 54 * scale),
        (w - 36 * scale, h - 10 * scale),
        (w - 168 * scale, h - 54 * scale),
    ]
    draw.polygon(tail, fill=INK)
    tail_in = [
        (w - 118 * scale, h - 62 * scale),
        (w - 52 * scale, h - 28 * scale),
        (w - 158 * scale, h - 62 * scale),
    ]
    draw.polygon(tail_in, fill=PAPER)
    return img.resize((480, 200), Image.Resampling.LANCZOS)


if __name__ == "__main__":
    save_pair(
        "tutorial_qte_hand_v1.png",
        hand(),
        ("e3a91c4b7d204f6a8b5e1c9d2f7048aa", "f4b02d5c8e31507b9c6f2dae308159bb"),
    )
    save_pair(
        "tutorial_qte_bubble_v1.png",
        bubble(),
        ("a5c13e6d9f42618c0d703ebf41926acc", "b6d24f7e0a53729d1e814fc052a37bdd"),
    )
