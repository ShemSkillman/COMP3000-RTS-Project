using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace RTSEngine
{
    [CustomEditor(typeof(Scenario))]
    public class ScenarioEditor : ListTabEditorTemplate
    {
        SerializedObject scenario_SO;

        public override void OnEnable()
        {
            base.OnEnable();
            scenario_SO = new SerializedObject((Scenario)target);
        }

        public override void OnInspectorGUI()
        {

            scenario_SO.Update(); //Always update the Serialized Object.

            GeneralSettings(scenario_SO);

            ListTabSettings(scenario_SO, "Mission Settings", "missions");
     
            scenario_SO.ApplyModifiedProperties(); //Apply all modified properties always at the end of this method.
        }

        protected override void GeneralSettings(SerializedObject so)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("General Scenario Settings", titleGUIStyle);
            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(so.FindProperty("code"));
            EditorGUILayout.PropertyField(so.FindProperty("_name"));
            EditorGUILayout.PropertyField(so.FindProperty("description"));
        }

        protected override void GeneralElementSettings(SerializedObject so, string path)
        {
            EditorGUILayout.PropertyField(so.FindProperty(path + ".code"));
            EditorGUILayout.PropertyField(so.FindProperty(path + ".type"));

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(so.FindProperty(path + ".name"));
            EditorGUILayout.PropertyField(so.FindProperty(path + ".description"));
            EditorGUILayout.PropertyField(so.FindProperty(path + ".icon"));

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(so.FindProperty(path + ".timeCondition.survivalTimeEnabled"));
            if(so.FindProperty(path+ ".timeCondition.survivalTimeEnabled").boolValue)
                EditorGUILayout.PropertyField(so.FindProperty(path + ".timeCondition.survivalTime"));

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(so.FindProperty(path + ".timeCondition.timeLimitEnabled"));
            if(so.FindProperty(path+ ".timeCondition.timeLimitEnabled").boolValue)
                EditorGUILayout.PropertyField(so.FindProperty(path + ".timeCondition.timeLimit"));

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(so.FindProperty(path + ".defendFactionEntities"), true);

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(so.FindProperty(path + ".completeAudio"));
            EditorGUILayout.PropertyField(so.FindProperty(path + ".completeResources"), true);
        }

        protected override void CustomElementSettings(SerializedObject so, string path)
        {
            switch ((Mission.Type)so.FindProperty(path+".type").intValue)
            {
                case Mission.Type.collectResource:
                    CollectResourceSettings(so, path);

                    break;

                case Mission.Type.eliminate:
                    EliminateProduceSettings(so, path);

                    break;

                case Mission.Type.produce:
                    EliminateProduceSettings(so, path);

                    break;

                default:
                    EditorGUILayout.HelpBox("No custom settings for the current type.", MessageType.None);

                    break;
            }
        }

        protected override void ElementEventsSettings(SerializedObject so, string path)
        {
            EditorGUILayout.PropertyField(so.FindProperty(path + ".startEvent"));
            EditorGUILayout.PropertyField(so.FindProperty(path + ".completeEvent"));
            EditorGUILayout.PropertyField(so.FindProperty(path + ".failEvent"));
        }

        protected void CollectResourceSettings (SerializedObject so, string path)
        {
            EditorGUILayout.PropertyField(so.FindProperty(path + ".targetResource"));
            EditorGUILayout.PropertyField(so.FindProperty(path + ".targetAmount"));
        }

        protected void EliminateProduceSettings (SerializedObject so, string path)
        {
            EditorGUILayout.PropertyField(so.FindProperty(path + ".targetCode"), true);
            EditorGUILayout.PropertyField(so.FindProperty(path + ".targetAmount"));
        }

    }
}
