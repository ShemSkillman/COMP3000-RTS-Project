using System.Collections.Generic;
using UnityEditor;

namespace RTSEngine.EditorOnly
{
    [CustomEditor(typeof(NPCTypeInfo))]
    public class NPCTypeInfoEditor : Editor
    {
        private SerializedObject target_SO;

        public void OnEnable()
        {
            target_SO = new SerializedObject(target as NPCTypeInfo);
            RTSEditorHelper.RefreshNPCTypes(true, target as NPCTypeInfo);
        }

        public override void OnInspectorGUI()
        {
            target_SO.Update(); //Always update the Serialized Object.

            EditorGUILayout.PropertyField(target_SO.FindProperty("_name"));
            EditorGUILayout.PropertyField(target_SO.FindProperty("code"));
            EditorGUILayout.PropertyField(target_SO.FindProperty("npcManagers"), true);

            target_SO.ApplyModifiedProperties(); //Apply all modified properties always at the end of this method.
        }
    }
}
