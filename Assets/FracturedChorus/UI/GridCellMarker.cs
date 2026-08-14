using FracturedChorus.Combat.Grid;
using UnityEngine;

namespace FracturedChorus.UI
{
    /// <summary>
    /// Giữ slot Hierarchy cũ (Cell_Player_R*_C*). Hex = child "Hexagon Flat Top" 1.5× — không đụng MeshFilter parent.
    /// </summary>
    [ExecuteAlways]
    public class GridCellMarker : MonoBehaviour
    {
        private const string HexFloorChildName = "Hexagon Flat Top";
        private const string LaneIconChildName = "LaneIcon";

        [SerializeField] private GridSide side = GridSide.Player;
        [SerializeField] private int row;
        [SerializeField] private int column;
        [SerializeField] private Sprite floorSprite;
        [SerializeField] private Vector2 hexScale = new Vector2(HexSpriteUtil.DefaultScaleX, HexSpriteUtil.DefaultScaleY);
        [SerializeField] private bool useCustomFloorColor;
        [SerializeField] private Color customFloorColor = Color.white;
        [Tooltip("Keep visual/active state authored in the scene — do not rebuild hex when selecting the object or entering Play.")]
        [SerializeField] private bool preserveSceneVisuals = true;

        private static readonly Color EnemyFill = new Color(0.48f, 0.14f, 0.14f, 0.35f);
        private static readonly Color DropNeonColor = new Color(0.15f, 0.82f, 1f, 0.92f);

        public GridSide Side => side;
        public int Row => row;
        public int Column => column;
        public GridPosition Position => new GridPosition(side, row, column);

        public static Vector3 ResolveWorld(GridPosition position, float sideGap = HexBoardLayout.DefaultSideGap)
        {
            var markers = FindObjectsByType<GridCellMarker>(FindObjectsInactive.Include);
            for (var i = 0; i < markers.Length; i++)
            {
                var marker = markers[i];
                if (marker == null)
                {
                    continue;
                }

                var slot = marker.Position;
                if (slot.Side == position.Side && slot.Row == position.Row && slot.Column == position.Column)
                {
                    return marker.transform.position;
                }
            }

            return HexBoardLayout.GetWorldPosition(position, sideGap);
        }

#if UNITY_EDITOR
        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                if (preserveSceneVisuals)
                {
                    HideLegacyQuadMesh();
                }
                else
                {
                    SyncEditModeVisuals();
                }
            }
        }
#endif

        public void HideLegacyMeshOnly()
        {
            HideLegacyQuadMesh();
        }

        public void Configure(GridSide gridSide, int gridRow, int gridColumn)
        {
            side = gridSide;
            row = gridRow;
            column = gridColumn;
            name = $"Cell_{side}_R{row}_C{column}";
        }

        public void SnapToLayoutPosition(float sideGap = HexBoardLayout.DefaultSideGap)
        {
            var world = HexBoardLayout.GetWorldPosition(Position, sideGap);
            transform.position = new Vector3(world.x, world.y, 0f);
        }

        public void SetFloorSprite(Sprite sprite)
        {
            if (sprite != null)
            {
                floorSprite = sprite;
            }
        }

        public void SetDropHighlight(bool active)
        {
            var glow = transform.Find("DropGlow");
            if (glow == null)
            {
                EnsureDropGlow();
                glow = transform.Find("DropGlow");
            }

            if (glow != null)
            {
                glow.gameObject.SetActive(active);
            }
        }

        /// <summary>Ẩn/hiện hex floor (giữ Transform/collider cho snap).</summary>
        public void SetFloorVisible(bool visible)
        {
            var hexRoot = transform.Find(HexFloorChildName);
            if (hexRoot != null)
            {
                hexRoot.gameObject.SetActive(visible);
            }

            var laneIcon = transform.Find(LaneIconChildName);
            if (laneIcon != null)
            {
                laneIcon.gameObject.SetActive(visible && side == GridSide.Player);
            }

            if (!visible)
            {
                SetDropHighlight(false);
            }
        }

        public void EnsureBorder()
        {
            EnsureVisuals();
        }

        public void RebuildVisuals()
        {
            HideLegacyQuadMesh();
            RemoveObsoleteChildren();
            transform.localScale = Vector3.one;
            EnsureVisuals();
        }

        public void RebuildVisualsForPlay()
        {
            PrepareForPlay();
        }

        /// <summary>
        /// Play mode: giữ visual đã chỉnh trong scene (màu, active hex). Chỉ tạo mesh thiếu.
        /// </summary>
        public void PrepareForPlay()
        {
            HideLegacyQuadMesh();

            if (!preserveSceneVisuals)
            {
                transform.localScale = Vector3.one;
            }

            var hexRoot = transform.Find(HexFloorChildName);
            if (hexRoot == null)
            {
                EnsureHexFloor();
            }
            else
            {
                ApplyFloorColor(hexRoot.GetComponent<SpriteRenderer>());
            }

            EnsureLaneIcon();
            EnsureDropGlow();
            SetDropHighlight(false);

            if (GetComponent<BoxCollider>() == null)
            {
                EnsureCollider();
            }
        }

#if UNITY_EDITOR
        private void SyncEditModeVisuals()
        {
            RebuildVisuals();
        }
#endif

        private void HideLegacyQuadMesh()
        {
            var meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                meshRenderer.enabled = false;
            }
        }

        private void RemoveObsoleteChildren()
        {
            if (Application.isPlaying)
            {
                return;
            }

            for (var i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
                if (child.name is "Border" or "Fill")
                {
                    DestroyImmediate(child.gameObject);
                }
            }

            var meshFilter = GetComponent<MeshFilter>();
            if (meshFilter != null)
            {
                DestroyImmediate(meshFilter);
            }

            var meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                DestroyImmediate(meshRenderer);
            }
        }

        public void EnsureVisuals()
        {
            HideLegacyQuadMesh();
            transform.localScale = Vector3.one;
            EnsureHexFloor();
            EnsureDropGlow();
            EnsureCollider();
        }

        private Transform GetOrCreateHexFloorRoot()
        {
            var existing = transform.Find(HexFloorChildName);
            if (existing != null)
            {
                return existing;
            }

            var go = new GameObject(HexFloorChildName);
            go.transform.SetParent(transform, false);
            return go.transform;
        }

        private void EnsureHexFloor()
        {
            var hexRoot = GetOrCreateHexFloorRoot();
            hexRoot.localPosition = Vector3.zero;
            hexRoot.localScale = new Vector3(hexScale.x, hexScale.y, 1f);

            hexRoot.gameObject.SetActive(true);

            var spriteRenderer = hexRoot.GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                spriteRenderer = hexRoot.gameObject.AddComponent<SpriteRenderer>();
            }

            var sprite = floorSprite != null ? floorSprite : HexSpriteUtil.ResolveHexagonFlatTop();
            if (sprite == null)
            {
                return;
            }

            spriteRenderer.sprite = sprite;
            ApplyFloorColor(spriteRenderer);
            spriteRenderer.sortingOrder = 0;
            EnsureLaneIcon();
        }

        private void ApplyFloorColor(SpriteRenderer spriteRenderer)
        {
            if (spriteRenderer == null)
            {
                return;
            }

            if (side == GridSide.Player)
            {
                spriteRenderer.color = FormationLaneVisuals.FloorColor(column);
                return;
            }

            if (useCustomFloorColor)
            {
                spriteRenderer.color = customFloorColor;
                return;
            }

            spriteRenderer.color = EnemyFill;
        }

        private void EnsureLaneIcon()
        {
            if (side != GridSide.Player)
            {
                var existingEnemy = transform.Find(LaneIconChildName);
                if (existingEnemy != null)
                {
                    existingEnemy.gameObject.SetActive(false);
                }

                return;
            }

            var iconRoot = transform.Find(LaneIconChildName);
            if (iconRoot == null)
            {
                var go = new GameObject(LaneIconChildName);
                go.transform.SetParent(transform, false);
                iconRoot = go.transform;
            }

            iconRoot.localPosition = new Vector3(0f, -0.08f, 0f);
            iconRoot.localScale = Vector3.one * 0.42f;

            var spriteRenderer = iconRoot.GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                spriteRenderer = iconRoot.gameObject.AddComponent<SpriteRenderer>();
            }

            var icon = FormationLaneVisuals.LoadLaneIcon(column);
            if (icon != null)
            {
                spriteRenderer.sprite = icon;
            }

            spriteRenderer.color = Color.white;
            spriteRenderer.sortingOrder = 1;
            iconRoot.gameObject.SetActive(true);
        }

        public void ApplyFloorColorFromRenderer()
        {
            var hexRoot = transform.Find(HexFloorChildName);
            if (hexRoot == null)
            {
                return;
            }

            var spriteRenderer = hexRoot.GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                return;
            }

            useCustomFloorColor = true;
            customFloorColor = spriteRenderer.color;
        }

        private void EnsureDropGlow()
        {
            var glowRoot = transform.Find("DropGlow");
            if (glowRoot == null)
            {
                var glowGo = new GameObject("DropGlow");
                glowGo.transform.SetParent(transform, false);
                glowGo.transform.localScale = Vector3.one * 1.06f;
                glowRoot = glowGo.transform;

                var spriteRenderer = glowGo.AddComponent<SpriteRenderer>();
                var sprite = floorSprite != null ? floorSprite : HexSpriteUtil.ResolveHexagonFlatTop();
                if (sprite != null)
                {
                    spriteRenderer.sprite = sprite;
                }

                spriteRenderer.color = DropNeonColor;
                spriteRenderer.sortingOrder = 2;
                glowGo.SetActive(false);
            }
        }

        private void EnsureCollider()
        {
            var box = GetComponent<BoxCollider>();
            if (box == null)
            {
                box = gameObject.AddComponent<BoxCollider>();
            }

            box.size = new Vector3(hexScale.x, hexScale.y * 0.9f, 0.2f);
            box.center = Vector3.zero;
        }
    }
}
