using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using RTSEngine;

[CustomEditor(typeof(BuildingPlacer))]
public class BuildingPlacerEditor : Editor
{
    SerializedObject building_SO;
    SerializedObject placer_SO;

    public void OnEnable()
    {
        building_SO = new SerializedObject(((BuildingPlacer)target).gameObject.GetComponent<Building>());
        placer_SO = new SerializedObject((BuildingPlacer)target);
    }

    public override void OnInspectorGUI()
    {
        building_SO.Update();
        placer_SO.Update();

        EditorGUILayout.PropertyField(building_SO.FindProperty("resources"), new GUIContent("Required Resources"), true); ;
        EditorGUILayout.PropertyField(building_SO.FindProperty("factionEntityRequirements"), true);
        EditorGUILayout.PropertyField(building_SO.FindProperty("missingReqData"), new GUIContent("Missing-Requirements Data"), true);
        EditorGUILayout.PropertyField(building_SO.FindProperty("placedByDefault"));

        EditorGUILayout.PropertyField(placer_SO.FindProperty("placeOutsideBorder"));
        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(placer_SO.FindProperty("placeNearResource"));
        if(placer_SO.FindProperty("placeNearResource").boolValue == true)
        {
            EditorGUILayout.PropertyField(placer_SO.FindProperty("resourceType"));
            EditorGUILayout.PropertyField(placer_SO.FindProperty("resourceRange"));
        }

        building_SO.ApplyModifiedProperties(); //apply all modified properties always at the end of this method.
        placer_SO.ApplyModifiedProperties();
    }
}
