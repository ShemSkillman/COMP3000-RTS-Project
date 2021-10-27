using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace RTSEngine
{
    public abstract class ListTabEditorTemplate : Editor
    {
        protected int elementID;
        protected int tabID;

        string tabName;

        protected GUIStyle titleGUIStyle = new GUIStyle();

        public virtual void OnEnable()
        {
            titleGUIStyle.alignment = TextAnchor.MiddleLeft;
            titleGUIStyle.fontStyle = FontStyle.Bold;
        }

        public virtual void OnDisable()
        {
           
        }

        protected abstract void GeneralSettings(SerializedObject so);

        protected virtual void ListTabSettings (SerializedObject so, string title, string listProperty, bool singleTab = false)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(title, titleGUIStyle);
            EditorGUILayout.Space();

            EditorGUILayout.HelpBox("Navigate, add or remove elements using the buttons below", MessageType.Info);
            EditorGUILayout.Space();

            int count = so.FindProperty(listProperty).arraySize;
            if (GUILayout.Button("Add (Count: " + count.ToString() + ")"))
            {
                so.FindProperty(listProperty).InsertArrayElementAtIndex(count);
                
                elementID = count;
                count++;
            }

            GUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("<<"))
                {
                    RTSEditorHelper.Navigate(ref elementID, -1, count);
                }
                if (GUILayout.Button(">>"))
                {
                    RTSEditorHelper.Navigate(ref elementID, 1, count);
                }
            }
            GUILayout.EndHorizontal();

            EditorGUILayout.Space();

            //making sure there are elements to begin with:
            if (count > 0)
            {
                //element to display:
                string elementPath = $"{listProperty}.Array.data[{elementID}]";

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Element ID: " + elementID.ToString(), titleGUIStyle);
                EditorGUILayout.Space();

                if (singleTab) //if we only have one single tab,
                {
                    GeneralElementSettings(so, elementPath);
                }
                else
                {
                    titleGUIStyle.alignment = TextAnchor.MiddleLeft;
                    titleGUIStyle.fontStyle = FontStyle.Bold;

                    EditorGUI.BeginChangeCheck();

                    tabID = GUILayout.Toolbar(tabID, new string[] { "General Settings", "Custom Settings", "Events" });

                    switch (tabID)
                    {
                        case 0:
                            tabName = "General Settings";
                            break;
                        case 1:
                            tabName = "Custom Settings";
                            break;

                        case 2:
                            tabName = "Events";
                            break;
                        default:
                            break;
                    }

                    if (EditorGUI.EndChangeCheck())
                    {
                        so.ApplyModifiedProperties();
                        GUI.FocusControl(null);
                    }

                    EditorGUILayout.Space();

                    switch (tabName)
                    {
                        case "General Settings":
                            GeneralElementSettings(so, elementPath);
                            break;
                        case "Custom Settings":
                            CustomElementSettings(so, elementPath);
                            break;

                        case "Events":
                            ElementEventsSettings(so, elementPath);

                            break;
                        default:
                            break;
                    }
                }

                EditorGUILayout.Space();
                EditorGUILayout.Space();
                if (GUILayout.Button("Delete"))
                {
                    so.FindProperty($"{listProperty}").DeleteArrayElementAtIndex(elementID);
                    if (elementID > 0)
                        RTSEditorHelper.Navigate(ref elementID, -1, count);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("There are no elements, create one using the button above.", MessageType.Warning);
            }
        }

        protected abstract void GeneralElementSettings(SerializedObject so, string path);
        protected virtual void CustomElementSettings(SerializedObject so, string path) { }
        protected virtual void ElementEventsSettings(SerializedObject so, string path) { }

    }
}
