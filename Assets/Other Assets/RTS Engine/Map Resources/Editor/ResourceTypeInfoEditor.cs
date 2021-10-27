using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using RTSEngine;

namespace RTSEngine.EditorOnly
{
    [CustomEditor(typeof(ResourceTypeInfo))]
    public class ResourceTypeInfoEditor : Editor
    {

        SerializedObject target_SO;

        public void OnEnable()
        {
            target_SO = new SerializedObject((ResourceTypeInfo)target);
            RTSEditorHelper.RefreshResourceTypes(true, target as ResourceTypeInfo);
        }

        public override void OnInspectorGUI()
        {
            target_SO.Update(); //Always update the Serialized Object.

            EditorGUILayout.PropertyField(target_SO.FindProperty("_name"));
            EditorGUILayout.PropertyField(target_SO.FindProperty("startingAmount"));

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(target_SO.FindProperty("icon"));
            EditorGUILayout.PropertyField(target_SO.FindProperty("minimapIconColor"));

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(target_SO.FindProperty("collectionAudio"), true);

            target_SO.ApplyModifiedProperties(); //Apply all modified properties always at the end of this method.
        }
    }
}
