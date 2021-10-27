using UnityEngine;
using UnityEditor;

namespace RTSEngine
{
    public class NewMapSetup : EditorWindow
    {
        string myString = "Hello World";
        bool groupEnabled;
        bool myBool = true;
        float myFloat = 1.23f;

        GameManager manager;
        SerializedObject manager_SO;

        //[MenuItem("RTS Engine/Configure New Map", false, 51)]
        private static void Enable()
        {
            NewMapSetup window = EditorWindow.GetWindow(typeof(NewMapSetup), false, "New Map Setup") as NewMapSetup;
            window.Init();
        }

        private void Init()
        {
            this.Show();
            GameObject newMapPrefab = Resources.Load("NewMap", typeof(GameObject)) as GameObject;
            manager = newMapPrefab.GetComponentInChildren<GameManager>();
            manager_SO = new SerializedObject(manager);
            manager_SO.FindProperty("mainMenuScene").stringValue = "LAAA";

            manager_SO.ApplyModifiedProperties();

            Debug.Log(manager.GetFactionCount());
        }

        void OnGUI()
        {
            EditorGUILayout.PropertyField(manager_SO.FindProperty("mainMenuScene"), true);
            GUILayout.Label("Base Settings", EditorStyles.boldLabel);
            myString = EditorGUILayout.TextField("Text Field", myString);

            groupEnabled = EditorGUILayout.BeginToggleGroup("Optional Settings", groupEnabled);
            myBool = EditorGUILayout.Toggle("Toggle", myBool);
            myFloat = EditorGUILayout.Slider("Slider", myFloat, -3, 3);
            EditorGUILayout.EndToggleGroup();
        }
    }
}
