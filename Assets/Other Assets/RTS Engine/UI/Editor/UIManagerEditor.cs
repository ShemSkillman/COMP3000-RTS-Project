using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RTSEngine;
using UnityEditor;

[CustomEditor(typeof(UIManager))]
public class UIManagerEditor : Editor
{
    UIManager manager;
    SerializedObject manager_SO;

    public void OnEnable()
    {
        manager = target as UIManager;
        manager_SO = new SerializedObject(manager);
    }

    public override void OnInspectorGUI()
    {
        manager_SO.Update();

        EditorGUI.BeginChangeCheck();

        manager.tabID = GUILayout.Toolbar(manager.tabID, new string[] { "General", "Task Panel", "Selection Panel", "Hover Health Bar", "Peace Time" });

        if (EditorGUI.EndChangeCheck())
        {
            manager_SO.ApplyModifiedProperties();
            GUI.FocusControl(null);
        }

        EditorGUILayout.Space();
        EditorGUILayout.Space();

        switch (manager.tabID)
        {
            case 0:
                OnGeneralInspectorGUI();
                break;
            case 1:
                OnTaskPanelInspectorGUI();
                break;
            case 2:
                OnSelectionPanelInspectorGUI();
                break;
            case 3:
                OnHoverHealthBarInspectorGUI();
                break;
            case 4:
                OnPeaceTimeInspectorGUI();
                break;
            default:
                break;
        }

        manager_SO.ApplyModifiedProperties();
    }

    private void OnGeneralInspectorGUI ()
    {
        EditorGUILayout.PropertyField(manager_SO.FindProperty("winMenu"));
        EditorGUILayout.PropertyField(manager_SO.FindProperty("loseMenu"));
        EditorGUILayout.PropertyField(manager_SO.FindProperty("pauseMenu"));
        EditorGUILayout.PropertyField(manager_SO.FindProperty("freezeMenu"));

        EditorGUILayout.Space();
        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(manager_SO.FindProperty("tooltipPanel"));
        EditorGUILayout.PropertyField(manager_SO.FindProperty("tooltipText"));

        EditorGUILayout.Space();
        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(manager_SO.FindProperty("populationText"));

        EditorGUILayout.Space();
        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(manager_SO.FindProperty("playerMessageText"));
        EditorGUILayout.PropertyField(manager_SO.FindProperty("playerMessageDuration"));
    }

    private void OnTaskPanelInspectorGUI ()
    {
        EditorGUILayout.PropertyField(manager_SO.FindProperty("taskPanel.taskUIPrefab"));
        EditorGUILayout.PropertyField(manager_SO.FindProperty("taskPanel.taskPanelCategories"), true);
        EditorGUILayout.PropertyField(manager_SO.FindProperty("taskPanel.inProgressTaskPanel"));
    }

    private void OnSelectionPanelInspectorGUI ()
    {
        EditorGUILayout.PropertyField(manager_SO.FindProperty("singleSelection.panel"), new GUIContent("Single Selection Panel"));
        EditorGUILayout.PropertyField(manager_SO.FindProperty("singleSelection.nameText"));
        EditorGUILayout.PropertyField(manager_SO.FindProperty("singleSelection.descriptionText"));
        EditorGUILayout.PropertyField(manager_SO.FindProperty("singleSelection.showPopulationSlots"));
        EditorGUILayout.PropertyField(manager_SO.FindProperty("singleSelection.icon"));
        EditorGUILayout.PropertyField(manager_SO.FindProperty("singleSelection.healthText"));
        EditorGUILayout.PropertyField(manager_SO.FindProperty("singleSelection.healthBar"), true);

        EditorGUILayout.Space();
        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(manager_SO.FindProperty("multipleSelectionPanel"));
        EditorGUILayout.PropertyField(manager_SO.FindProperty("maxMultipleSelectionTasks"));
    }

    private void OnHoverHealthBarInspectorGUI ()
    {
        EditorGUILayout.PropertyField(manager_SO.FindProperty("hoverHealthBar.enabled"));
        if (manager_SO.FindProperty("hoverHealthBar.enabled").boolValue == false)
            return;

        EditorGUILayout.PropertyField(manager_SO.FindProperty("hoverHealthBar.playerFactionOnly"));
        EditorGUILayout.PropertyField(manager_SO.FindProperty("hoverHealthBar.canvas"), new GUIContent("Health Bar Parent Canvas"));
        EditorGUILayout.PropertyField(manager_SO.FindProperty("hoverHealthBar.healthBar"), true);
    }

    private void OnPeaceTimeInspectorGUI ()
    {
        EditorGUILayout.PropertyField(manager_SO.FindProperty("peaceTime.panel"), new GUIContent("Peace Time Panel"));
        EditorGUILayout.PropertyField(manager_SO.FindProperty("peaceTime.timeText"), new GUIContent("Peace Time Text"));
    }

}
