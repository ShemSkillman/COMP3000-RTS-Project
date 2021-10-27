using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using RTSEngine;

[CustomEditor(typeof(Healer))]
public class HealerEditor : Editor
{
    SerializedObject healer_SO;

    public void OnEnable()
    {
        healer_SO = new SerializedObject((Healer)target);
    }

    public override void OnInspectorGUI()
    {
        healer_SO.Update();

        EditorGUILayout.PropertyField(healer_SO.FindProperty("inProgressObject"), new GUIContent("Healing Object"), true);
        EditorGUILayout.PropertyField(healer_SO.FindProperty("healthPerSecond"));
        EditorGUILayout.PropertyField(healer_SO.FindProperty("stoppingDistance"));
        EditorGUILayout.PropertyField(healer_SO.FindProperty("maxDistance"));

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(healer_SO.FindProperty("autoBehavior"), new GUIContent("Auto Heal"), true);

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(healer_SO.FindProperty("sourceEffect"));
        EditorGUILayout.PropertyField(healer_SO.FindProperty("targetEffect"));

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(healer_SO.FindProperty("orderAudio"), new GUIContent("Healing Order Audio"), true);
        EditorGUILayout.PropertyField(healer_SO.FindProperty("healingAudio"), new GUIContent("Healing Audio"), true);

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(healer_SO.FindProperty("taskUI"), true);

        healer_SO.ApplyModifiedProperties(); //apply all modified properties always at the end of this method.
    }
}
