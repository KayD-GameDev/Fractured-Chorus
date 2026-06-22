using FracturedChorus.Combat.Grid;
using UnityEngine;

namespace FracturedChorus.UI
{
    /// <summary>
    /// Visual grid cell — drag in Hierarchy. Column 0 = rightmost (front) for player party.
    /// </summary>
    public class GridCellMarker : MonoBehaviour
    {
        [SerializeField] private GridSide side = GridSide.Player;
        [SerializeField] private int row;
        [SerializeField] private int column;
        [SerializeField] private Transform borderRoot;

        public GridSide Side => side;
        public int Row => row;
        public int Column => column;
        public GridPosition Position => new GridPosition(side, row, column);

        public void Configure(GridSide gridSide, int gridRow, int gridColumn)
        {
            side = gridSide;
            row = gridRow;
            column = gridColumn;
            name = $"Cell_{side}_R{row}_C{column}";
            EnsureBorder();
        }

        public void EnsureBorder()
        {
            if (borderRoot != null)
            {
                return;
            }

            borderRoot = transform.Find("Border");
            if (borderRoot != null)
            {
                return;
            }

            var borderGo = new GameObject("Border");
            borderGo.transform.SetParent(transform, false);
            borderRoot = borderGo.transform;

            CreateEdge("Top", new Vector3(0f, 0.52f, 0f), new Vector3(1.05f, 0.04f, 1f));
            CreateEdge("Bottom", new Vector3(0f, -0.52f, 0f), new Vector3(1.05f, 0.04f, 1f));
            CreateEdge("Left", new Vector3(-0.52f, 0f, 0f), new Vector3(0.04f, 1.05f, 1f));
            CreateEdge("Right", new Vector3(0.52f, 0f, 0f), new Vector3(0.04f, 1.05f, 1f));
        }

        private void CreateEdge(string edgeName, Vector3 localPos, Vector3 scale)
        {
            var edge = GameObject.CreatePrimitive(PrimitiveType.Quad);
            edge.name = edgeName;
            edge.transform.SetParent(borderRoot, false);
            edge.transform.localPosition = localPos;
            edge.transform.localScale = scale;
            var renderer = edge.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = new Material(Shader.Find("Sprites/Default"));
                renderer.material.color = new Color(1f, 1f, 1f, 0.35f);
            }

            var collider = edge.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }
        }
    }
}
