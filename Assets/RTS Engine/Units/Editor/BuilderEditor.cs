using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using RTSEngine;

[CustomEditor(typeof(Builder))]
public class BuilderEditor : Editor
{
    SerializedObject builder_SO;

    public void OnEnable()
    {
        builder_SO = new SerializedObject((Builder)target);
    }

    public override void OnInspectorGUI()
    {
        builder_SO.Update();

        EditorGUILayout.PropertyField(builder_SO.FindProperty("inProgressObject"), new GUIContent("Construction Object"));
        EditorGUILayout.PropertyField(builder_SO.FindProperty("constructFreeBuildings"));
        EditorGUILayout.PropertyField(builder_SO.FindProperty("healthPerSecond"));

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(builder_SO.FindProperty("autoBehavior"), new GUIContent("Auto Build"), true);

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(builder_SO.FindProperty("restrictions"), true);
        EditorGUILayout.PropertyField(builder_SO.FindProperty("restrictBuildingPlacementOnly"));

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(builder_SO.FindProperty("sourceEffect"));
        EditorGUILayout.PropertyField(builder_SO.FindProperty("targetEffect"));

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(builder_SO.FindProperty("orderAudio"), new GUIContent("Construction Order Audio"), true);
        EditorGUILayout.PropertyField(builder_SO.FindProperty("constructionAudio"), true);

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(builder_SO.FindProperty("taskUI"), true);

        builder_SO.ApplyModifiedProperties(); //apply all modified properties always at the end of this method.
    }
}
