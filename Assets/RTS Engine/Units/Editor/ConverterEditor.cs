using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using RTSEngine;

[CustomEditor(typeof(Converter))]
public class ConverterEditor : Editor
{
    SerializedObject converter_SO;

    public void OnEnable()
    {
        converter_SO = new SerializedObject((Converter)target);
    }

    public override void OnInspectorGUI()
    {
        converter_SO.Update();

        EditorGUILayout.PropertyField(converter_SO.FindProperty("inProgressObject"), new GUIContent("Conversion Object"), true);
        EditorGUILayout.PropertyField(converter_SO.FindProperty("duration"), new GUIContent("Conversion Duration"));
        EditorGUILayout.PropertyField(converter_SO.FindProperty("effect"), new GUIContent("Conversion Effect"));
        EditorGUILayout.PropertyField(converter_SO.FindProperty("stoppingDistance"));
        EditorGUILayout.PropertyField(converter_SO.FindProperty("maxDistance"));

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(converter_SO.FindProperty("autoBehavior"), new GUIContent("Auto Convert"), true);

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(converter_SO.FindProperty("sourceEffect"));
        EditorGUILayout.PropertyField(converter_SO.FindProperty("targetEffect"));

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(converter_SO.FindProperty("orderAudio"), new GUIContent("Conversion Order Audio"), true);
        EditorGUILayout.PropertyField(converter_SO.FindProperty("conversionAudio"), new GUIContent("Conversion Audio"), true);

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(converter_SO.FindProperty("taskUI"), true);

        converter_SO.ApplyModifiedProperties(); //apply all modified properties always at the end of this method.
    }
}
