using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace RTSEngine
{
    [CustomEditor(typeof(TaskLauncher)), CanEditMultipleObjects]
    public class TaskLauncherEditor : ListTabEditorTemplate
    {
        TaskLauncher source;
        SerializedObject taskLauncher_SO;

        public override void OnEnable()
        {
            base.OnEnable();

            source = target as TaskLauncher;

            elementID = source.editorElementID;
            tabID = source.editorTabID;

            taskLauncher_SO = new SerializedObject(source);
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
            EditorGUILayout.LabelField("Task Launcher Settings", titleGUIStyle);
            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(so.FindProperty("isActive"));
            EditorGUILayout.PropertyField(so.FindProperty("code"));
            EditorGUILayout.PropertyField(so.FindProperty("minHealth"));
            EditorGUILayout.PropertyField(so.FindProperty("maxTasks"));
            EditorGUILayout.PropertyField(so.FindProperty("launchDeclinedAudio"), true);
        }

        protected override void GeneralElementSettings(SerializedObject so, string path)
        {
            EditorGUILayout.PropertyField(so.FindProperty(path).FindPropertyRelative("code"));
            EditorGUILayout.PropertyField(so.FindProperty(path).FindPropertyRelative("_isAvailable"), new GUIContent("Available by default?"));
            EditorGUILayout.PropertyField(so.FindProperty(path).FindPropertyRelative("factionTypes"), true);

            SerializedProperty allowedTypeProperty = so.FindProperty(path).FindPropertyRelative("allowedType");
            SerializedProperty typeProperty = so.FindProperty(path).FindPropertyRelative("type");
            EditorGUILayout.PropertyField(allowedTypeProperty);
            switch ((FactionEntityTask.AllowedTaskTypes)allowedTypeProperty.intValue)
            {
                case FactionEntityTask.AllowedTaskTypes.createUnit:
                    typeProperty.intValue = (int)TaskTypes.createUnit;
                    break;
                case FactionEntityTask.AllowedTaskTypes.destroy:
                    typeProperty.intValue = (int)TaskTypes.destroy;
                    break;
                case FactionEntityTask.AllowedTaskTypes.upgrade:
                    typeProperty.intValue = (int)TaskTypes.upgrade;
                    break;
                case FactionEntityTask.AllowedTaskTypes.lockAttack:
                    typeProperty.intValue = (int)TaskTypes.lockAttack;
                    break;
                case FactionEntityTask.AllowedTaskTypes.custom:
                    typeProperty.intValue = (int)TaskTypes.customTask;
                    break;
                default:
                    break;
            }

            EditorGUILayout.PropertyField(so.FindProperty(path).FindPropertyRelative("description"));
            EditorGUILayout.PropertyField(so.FindProperty(path).FindPropertyRelative("taskPanelCategory"));
            EditorGUILayout.PropertyField(so.FindProperty(path).FindPropertyRelative("icon"));
            EditorGUILayout.PropertyField(so.FindProperty(path).FindPropertyRelative("reloadTime"));

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(so.FindProperty(path).FindPropertyRelative("requiredResources"), true);
            EditorGUILayout.PropertyField(so.FindProperty(path).FindPropertyRelative("missingReqData"), new GUIContent("Missing-Requirements Data"), true);
            EditorGUILayout.PropertyField(so.FindProperty(path).FindPropertyRelative("completeResources"), true);

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(so.FindProperty(path).FindPropertyRelative("useMode"));

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("Input codes of tasks that you want to enable when this task is completed.", MessageType.Info);
            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(so.FindProperty(path).FindPropertyRelative("updateTasksOnComplete"), true);
            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(so.FindProperty(path).FindPropertyRelative("launchAudio"), true);
            EditorGUILayout.PropertyField(so.FindProperty(path).FindPropertyRelative("completeAudio"), true);
            EditorGUILayout.PropertyField(so.FindProperty(path).FindPropertyRelative("cancelAudio"), true);
        }

        private void CreateUnitSettings(SerializedObject so, string path)
        {
            EditorGUILayout.LabelField("Unit Creation Settings:", titleGUIStyle);
            EditorGUILayout.HelpBox("The prefabs list allows multiple unit prefabs to be added but only one (randomly selected) will be created each time.", MessageType.Warning);
            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(so.FindProperty(path).FindPropertyRelative("unitCreationAttributes.prefabs"), true);
        }

        //Upgrade (Unit+Building) settings:
        private void UpgradeSettings(SerializedObject so, string path)
        {
            EditorGUILayout.PropertyField(so.FindProperty(path).FindPropertyRelative("upgrade"));

            EditorGUILayout.PropertyField(so.FindProperty(path + ".upgradeTargetID"));
            EditorGUILayout.PropertyField(so.FindProperty(path + ".oneInstanceUpgrade"));

            EditorGUILayout.HelpBox("Upgrade settings can be only modified from the prefab directly. You can only pick the ID of the upgrade target here.", MessageType.Warning);
        }

        //Lock (unlock) attack types in the task holder:
        private void LockAttackSettings(SerializedObject so, string path)
        {
            EditorGUILayout.PropertyField(so.FindProperty(path).FindPropertyRelative("lockAttackTypes"), true);
            EditorGUILayout.PropertyField(so.FindProperty(path).FindPropertyRelative("unlockAttackTypes"), true);

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("If 'Switch Attack' is enabled, you may enter the code of the attack type that'll be enabled when this task is completed.", MessageType.Info);
            EditorGUILayout.PropertyField(so.FindProperty(path).FindPropertyRelative("switchAttack"));
            if(so.FindProperty(path).FindPropertyRelative("switchAttack").boolValue)
                EditorGUILayout.PropertyField(so.FindProperty(path).FindPropertyRelative("targetAttackType"));
        }

        protected override void ElementEventsSettings(SerializedObject so, string path)
        {
            EditorGUILayout.HelpBox("In addition to the delegate events (which are called for all tasks), you can trigger events for this task independently.", MessageType.Info);
            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(so.FindProperty(path).FindPropertyRelative("launchEvent"));
            EditorGUILayout.PropertyField(so.FindProperty(path).FindPropertyRelative("startEvent"));
            EditorGUILayout.PropertyField(so.FindProperty(path).FindPropertyRelative("completeEvent"));
            EditorGUILayout.PropertyField(so.FindProperty(path).FindPropertyRelative("cancelEvent"));
        }

        protected override void CustomElementSettings(SerializedObject so, string path)
        {
            switch ((TaskTypes)so.FindProperty(path + ".type").intValue)
            {
                case TaskTypes.createUnit: //Unit creation task:

                    CreateUnitSettings(so, path);

                    break;

                case TaskTypes.upgrade: //upgrade task

                    UpgradeSettings(so, path);

                    break;

                case TaskTypes.lockAttack: //lock (unlock) attack types

                    LockAttackSettings(so, path);

                    break;

                default:
                    EditorGUILayout.HelpBox("No custom settings for the current task type.", MessageType.None);

                    break;
            }
        }

        public override void OnInspectorGUI()
        {
            taskLauncher_SO.Update(); //Always update the Serialized Object.

            titleGUIStyle.fontSize = 13;
            titleGUIStyle.alignment = TextAnchor.MiddleCenter;
            titleGUIStyle.fontStyle = FontStyle.Bold;

            GeneralSettings(taskLauncher_SO);
            ListTabSettings(taskLauncher_SO, "Task Settings", "tasksList");

            taskLauncher_SO.ApplyModifiedProperties(); //Apply all modified properties always at the end of this method.
        }
    }
}
