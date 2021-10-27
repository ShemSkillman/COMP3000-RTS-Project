using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RTSEngine;
using UnityEditor;

[CustomEditor(typeof(SelectionManager))]
public class SelectionManagerEditor : Editor
{
    SerializedObject manager_SO;
    int tabID = 0;

    public void OnEnable()
    {
        manager_SO = new SerializedObject((SelectionManager)target);
    }

    public override void OnInspectorGUI()
    {
        manager_SO.Update();

        EditorGUI.BeginChangeCheck();

        tabID = GUILayout.Toolbar(tabID, new string[] { "General", "Selection Box", "Selection Options", "Selection Flash"});

        if (EditorGUI.EndChangeCheck())
        {
            manager_SO.ApplyModifiedProperties();
            GUI.FocusControl(null);
        }

        EditorGUILayout.Space();
        EditorGUILayout.Space();

        switch (tabID)
        {
            case 0:
                OnGeneralInspectorGUI();
                break;
            case 1:
                OnSelectionBoxInspectorGUI();
                break;
            case 2:
                OnSelectionOptionsInspectorGUI();
                break;
            case 3:
                OnSelectionFlashInspectorGUI();
                break;
            default:
                break;
        }

        manager_SO.ApplyModifiedProperties();
    }

    private void OnGeneralInspectorGUI ()
    {
        EditorGUILayout.PropertyField(manager_SO.FindProperty("rayLayerMask"));
        EditorGUILayout.PropertyField(manager_SO.FindProperty("freeSelectionColor"));
        EditorGUILayout.PropertyField(manager_SO.FindProperty("doubleClickSelectRange"), new GUIContent("Double Click Selection Range"));

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(manager_SO.FindProperty("idleUnitsSelection.enabled"), new GUIContent("Enable Idle Units Selection"));
        if(manager_SO.FindProperty("idleUnitsSelection.enabled").boolValue == true)
        {
            EditorGUILayout.PropertyField(manager_SO.FindProperty("idleUnitsSelection.key"), new GUIContent("Select Idle Units Key"));
            EditorGUILayout.PropertyField(manager_SO.FindProperty("idleUnitsSelection.workersOnly"), new GUIContent("Select Idle Workers Only"));
        }

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(manager_SO.FindProperty("multipleSelectionKey"));

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(manager_SO.FindProperty("camFollow"), new GUIContent("Camera Follow"), true);
    }

    private void OnSelectionBoxInspectorGUI ()
    {
        EditorGUILayout.PropertyField(manager_SO.FindProperty("box.canvasTransform"), new GUIContent("Canvas"));
        EditorGUILayout.PropertyField(manager_SO.FindProperty("box.image"), new GUIContent("Image"));
        EditorGUILayout.PropertyField(manager_SO.FindProperty("box.minSize"), new GUIContent("Minimum Size"));
    }

    private void OnSelectionOptionsInspectorGUI ()
    {
        EditorGUILayout.PropertyField(manager_SO.FindProperty("selected.selectionOptions"), new GUIContent("Options"), true);
    }

    private void OnSelectionFlashInspectorGUI ()
    {
        EditorGUILayout.PropertyField(manager_SO.FindProperty("flashTime"));
        EditorGUILayout.PropertyField(manager_SO.FindProperty("flashRepeatTime"));
        EditorGUILayout.PropertyField(manager_SO.FindProperty("friendlyFlashColor"));
        EditorGUILayout.PropertyField(manager_SO.FindProperty("enemyFlashColor"));
    }
}
