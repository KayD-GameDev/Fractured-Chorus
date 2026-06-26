#if UNITY_EDITOR
using FracturedChorus.Combat.Bootstrap;
using FracturedChorus.UI;
using UnityEditor;
using UnityEngine;

namespace FracturedChorus.Editor
{
    [CustomEditor(typeof(CombatPrototypeBootstrap))]
    public class CombatPrototypeBootstrapEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var bootstrap = (CombatPrototypeBootstrap)target;
            var partyBar = bootstrap.GetComponentInChildren<PartyStatusBarUIView>(true);
            if (partyBar == null)
            {
                partyBar = Object.FindAnyObjectByType<PartyStatusBarUIView>(FindObjectsInactive.Include);
            }

            if (partyBar == null)
            {
                EditorGUILayout.HelpBox(
                    "Party Status Bar chưa có trong scene.\n" +
                    "Bấm nút bên dưới → object xuất hiện dưới CombatCanvas → PartyStatusBarUI.",
                    MessageType.Warning);

                if (GUILayout.Button("Tạo Party Status Bar (Hierarchy)"))
                {
                    CombatSceneSetupEditor.RebuildPartyStatusBarInScene();
                }

                EditorGUILayout.Space();
            }

            DrawDefaultInspector();
        }
    }
}
#endif
