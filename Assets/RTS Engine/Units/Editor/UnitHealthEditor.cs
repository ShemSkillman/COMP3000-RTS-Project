using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using RTSEngine;

[CustomEditor(typeof(UnitHealth))]
public class UnitHealthEditor : Editor
{
    SerializedObject health_SO;

    public void OnEnable()
    {
        health_SO = new SerializedObject((UnitHealth)target);
    }

    public override void OnInspectorGUI()
    {
        health_SO.Update();

        EditorGUILayout.PropertyField(health_SO.FindProperty("maxHealth"));
        EditorGUILayout.PropertyField(health_SO.FindProperty("hoverHealthBarY"));

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(health_SO.FindProperty("canBeAttacked"));
        if (health_SO.FindProperty("canBeAttacked").boolValue == true)
        {
            EditorGUILayout.PropertyField(health_SO.FindProperty("takeDamage"));
            if (health_SO.FindProperty("takeDamage").boolValue == true)
            {
                EditorGUILayout.PropertyField(health_SO.FindProperty("damageEffect"));
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(health_SO.FindProperty("destroyObject"));
        if (health_SO.FindProperty("destroyObject").boolValue == true)
        {
            EditorGUILayout.PropertyField(health_SO.FindProperty("destroyObjectTime"));
            EditorGUILayout.PropertyField(health_SO.FindProperty("destroyAward"), new GUIContent("Destruction Award"), true);
            EditorGUILayout.PropertyField(health_SO.FindProperty("destructionAudio"), true);
            EditorGUILayout.PropertyField(health_SO.FindProperty("destructionEffect"));
        }

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(health_SO.FindProperty("stopMovingOnDamage"));
        EditorGUILayout.PropertyField(health_SO.FindProperty("enableDamageAnimation"));
        EditorGUILayout.PropertyField(health_SO.FindProperty("damageAnimationDuration"));

        health_SO.ApplyModifiedProperties(); //apply all modified properties always at the end of this method.
    }
}
