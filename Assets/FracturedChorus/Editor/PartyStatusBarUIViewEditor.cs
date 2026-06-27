#if UNITY_EDITOR
using FracturedChorus.UI;
using UnityEditor;
using UnityEngine;

namespace FracturedChorus.Editor
{
    [CustomEditor(typeof(PartyStatusBarUIView))]
    public class PartyStatusBarUIViewEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var bar = (PartyStatusBarUIView)target;
            var cardsRow = bar.transform.Find("CardsRow");

            EditorGUILayout.HelpBox(
                "Chỉnh trực tiếp trong Hierarchy (không có slider Spacing ở đây):\n\n" +
                "CombatCanvas → PartyStatusBarUI → CardsRow\n" +
                "  • CardsRow: Horizontal Layout Group → Spacing (khoảng cách thẻ)\n" +
                "  • PartyCard_0/1/2: Rect Transform (kích thước thẻ)\n" +
                "  • Frame / Avatar / ElementIcon: Image → Source Image (sprite)\n" +
                "  • HpBar → Fill: Image Filled (thanh máu)",
                MessageType.Info);

            if (cardsRow == null)
            {
                EditorGUILayout.HelpBox(
                    "Chưa có object con. Bấm nút bên dưới để tạo đủ Hierarchy.",
                    MessageType.Warning);

                if (GUILayout.Button("Tạo / Rebuild Party Status Bar Hierarchy"))
                {
                    CombatSceneSetupEditor.RebuildPartyStatusBarInScene();
                }
            }
            else if (GUILayout.Button("Rebuild lại Hierarchy (mất chỉnh tay trên thẻ)"))
            {
                if (EditorUtility.DisplayDialog(
                        "Rebuild Party Status Bar",
                        "Rebuild sẽ xóa PartyStatusBarUI hiện tại và tạo lại mặc định. Tiếp tục?",
                        "Rebuild",
                        "Hủy"))
                {
                    CombatSceneSetupEditor.RebuildPartyStatusBarInScene();
                }
            }

            EditorGUILayout.Space();
            DrawDefaultInspector();
        }
    }
}
#endif
