#if UNITY_EDITOR
using FracturedChorus.Combat.Damage;
using FracturedChorus.Data;
using UnityEditor;
using UnityEngine;

namespace FracturedChorus.Editor
{
    [CustomEditor(typeof(UnitStatBlockSO))]
    public class UnitStatBlockSOEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var block = (UnitStatBlockSO)target;
            serializedObject.Update();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("blockId"));
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Pre-condition (element)", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("element"));

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Strength", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Chọn Physical hoặc Magical, rồi nhập chỉ số Strength.", MessageType.None);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("strengthType"), new GUIContent("Damage Type"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("strength"), new GUIContent("Strength"));

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Other Core Stats", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("endurance"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("heartBeat"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("baseLuck"), new GUIContent("Crit Chance (%)"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("critMultiplier"), new GUIContent("Crit Damage Mult"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("maxHp"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("baseSpeed"));

            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif
