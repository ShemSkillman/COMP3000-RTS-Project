using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using RTSEngine;

[CustomEditor(typeof(Building)), CanEditMultipleObjects]
public class BuildingEditor : Editor {

    SerializedObject building_SO;

    public void OnEnable()
    {
        building_SO = new SerializedObject((Building) target);
    }

    public override void OnInspectorGUI ()
    {
        building_SO.Update();

        EditorGUILayout.PropertyField(building_SO.FindProperty("_name"));
        EditorGUILayout.PropertyField(building_SO.FindProperty("code"));
        EditorGUILayout.PropertyField(building_SO.FindProperty("category"));
        EditorGUILayout.PropertyField(building_SO.FindProperty("description"));
        EditorGUILayout.PropertyField(building_SO.FindProperty("icon"));
        EditorGUILayout.PropertyField(building_SO.FindProperty("free"));
        EditorGUILayout.PropertyField(building_SO.FindProperty("taskPanelCategory"));
        EditorGUILayout.PropertyField(building_SO.FindProperty("radius"));
        EditorGUILayout.PropertyField(building_SO.FindProperty("factionID"));
        EditorGUILayout.PropertyField(building_SO.FindProperty("addPopulation"), new GUIContent("Add Population Slots"));

        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(building_SO.FindProperty("initResources"), true);
        EditorGUILayout.PropertyField(building_SO.FindProperty("disableResources"), true);

        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(building_SO.FindProperty("coloredRenderers"), new GUIContent("Faction Colored Renderers"), true);
        EditorGUILayout.PropertyField(building_SO.FindProperty("plane"), new GUIContent("Selection Plane"));
        EditorGUILayout.PropertyField(building_SO.FindProperty("model"));
        EditorGUILayout.PropertyField(building_SO.FindProperty("selection"), new GUIContent("Building Selection"));
        EditorGUILayout.PropertyField(building_SO.FindProperty("bonusResources"), true);
        EditorGUILayout.PropertyField(building_SO.FindProperty("spawnPosition"));
        EditorGUILayout.PropertyField(building_SO.FindProperty("gotoPosition"));
        EditorGUILayout.PropertyField(building_SO.FindProperty("selectionAudio"), true);

        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(building_SO.FindProperty("regulatorData"), true);

        building_SO.ApplyModifiedProperties(); //apply all modified properties always at the end of this method.
    }
}
