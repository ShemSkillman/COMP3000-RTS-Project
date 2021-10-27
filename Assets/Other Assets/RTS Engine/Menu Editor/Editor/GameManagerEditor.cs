using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

/* Game Manager Editor script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
    [CustomEditor(typeof(GameManager))]
    public class GameManagerEditor : ListTabEditorTemplate
    {
        GameManager source;
        SerializedObject gameManager_SO;

        public override void OnEnable()
        {
            base.OnEnable();

            source = target as GameManager;

            elementID = source.editorElementID;
            tabID = source.editorTabID;

            gameManager_SO = new SerializedObject((GameManager)target);
        }

        public override void OnDisable()
        {
            base.OnDisable();

            source.editorElementID = elementID;
            source.editorTabID = tabID;
        }

        protected override void GeneralSettings(SerializedObject so)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("General Settings", titleGUIStyle);
            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(so.FindProperty("mainMenuScene"));
            EditorGUILayout.PropertyField(so.FindProperty("defeatCondition"));
            EditorGUILayout.PropertyField(so.FindProperty("speedModifier"));
            EditorGUILayout.PropertyField(so.FindProperty("randomFactionSlots"));
            EditorGUILayout.PropertyField(so.FindProperty("peaceTime"));

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(so.FindProperty("winGameAudio"));
            EditorGUILayout.PropertyField(so.FindProperty("loseGameAudio"));
        }

        public override void OnInspectorGUI()
        {
            gameManager_SO.Update(); //Always update the Serialized Object.

            titleGUIStyle.fontSize = 13;
            titleGUIStyle.alignment = TextAnchor.MiddleCenter;
            titleGUIStyle.fontStyle = FontStyle.Bold;

            GeneralSettings(gameManager_SO);
            ListTabSettings(gameManager_SO, "Faction Slots", "factions");

            gameManager_SO.ApplyModifiedProperties(); //Apply all modified properties always at the end of this method.
        }

        protected override void GeneralElementSettings(SerializedObject so, string path)
        {
            EditorGUILayout.PropertyField(so.FindProperty(path).FindPropertyRelative("name"));
            EditorGUILayout.PropertyField(so.FindProperty(path).FindPropertyRelative("typeInfo"), new GUIContent("Type"));
            EditorGUILayout.PropertyField(so.FindProperty(path).FindPropertyRelative("color"));
            EditorGUILayout.PropertyField(so.FindProperty(path).FindPropertyRelative("playerControlled"));
            EditorGUILayout.PropertyField(so.FindProperty(path).FindPropertyRelative("maxPopulation"));
            EditorGUILayout.PropertyField(so.FindProperty(path).FindPropertyRelative("capitalBuilding"));
            EditorGUILayout.PropertyField(so.FindProperty(path).FindPropertyRelative("camLookAtPos"), new GUIContent("Camera Look At Position"));
            EditorGUILayout.PropertyField(so.FindProperty(path).FindPropertyRelative("npcType"));
            EditorGUILayout.PropertyField(so.FindProperty(path).FindPropertyRelative("defaultFactionEntities"), true);
        }
    }
}
