using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using RTSEngine;

[CustomEditor(typeof(BuildingDropOff))]
public class BuildingDropOffEditor : Editor
{
    SerializedObject dropoff_SO;

    public void OnEnable()
    {
        dropoff_SO = new SerializedObject((BuildingDropOff)target);
    }

    public override void OnInspectorGUI()
    {
        dropoff_SO.Update();

        EditorGUILayout.PropertyField(dropoff_SO.FindProperty("isActive"));
        if (dropoff_SO.FindProperty("isActive").boolValue == true)
        {
            EditorGUILayout.PropertyField(dropoff_SO.FindProperty("dropOffPosition"));

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(dropoff_SO.FindProperty("acceptAllResources"));
            if (dropoff_SO.FindProperty("acceptAllResources").boolValue == false)
            {
                EditorGUILayout.PropertyField(dropoff_SO.FindProperty("acceptedResources"), true);
            }
        }

        dropoff_SO.ApplyModifiedProperties(); //apply all modified properties always at the end of this method.
    }
}
