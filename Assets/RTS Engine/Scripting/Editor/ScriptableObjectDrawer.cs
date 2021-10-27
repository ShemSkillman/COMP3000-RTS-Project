using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

using RTSEngine.UI;

/* ScriptableObjectDrawer editor script created by Oussama Bouanani,  SoumiDelRio
 * This script is part of the RTS Engine */

#if UNITY_EDITOR
namespace RTSEngine
{
    public class ScriptableObjectDrawer<T> : PropertyDrawer where T : ScriptableObject, IAssetFile
    {
        public static void Draw(Rect position, SerializedProperty property, GUIContent label, IEnumerable<T> collection, bool codeInput = false, string attributeName = "null")
        {
            label = EditorGUI.BeginProperty(position, label, property);

            float height = position.height - EditorGUIUtility.standardVerticalSpacing * 4;

            Rect popupRect = new Rect(position.x, position.y, position.width, height / 2);

            if ((codeInput && property.propertyType != SerializedPropertyType.String)
                || (!codeInput && property.propertyType != SerializedPropertyType.ObjectReference))
            {
                EditorGUI.LabelField(popupRect, label.text, $"Use [{attributeName}] with '{typeof(T).ToString()}' reference fields.");
                EditorGUI.EndProperty();
                return;
            }

            if (!RTSEditorHelper.GetAssetFilesDic(out Dictionary<string, T> dictionary, collection))
            {
                EditorGUI.LabelField(popupRect, label.text, $"Duplicate keys detected, see console!");
                EditorGUI.EndProperty();
                return;
            }

            int index = codeInput 
                ? dictionary.Keys.ToList().IndexOf(property.stringValue)
                : dictionary.Values.ToList().IndexOf(property.objectReferenceValue as T);

            if (index < 0)
                index = 0;

            string[] keys = dictionary.Keys.ToArray();

            index = EditorGUI.Popup(popupRect, label.text, index, keys);

            if (codeInput)
                property.stringValue = keys[index];
            else
                property.objectReferenceValue = dictionary[keys[index]] as Object;

            GUI.enabled = false;
            Rect contentRect = new Rect(position.x, position.y + height / 2 + EditorGUIUtility.standardVerticalSpacing, position.width, height / 2);
            EditorGUI.PropertyField(contentRect, property, GUIContent.none);
            GUI.enabled = true;

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return base.GetPropertyHeight(property, label) * 2 + EditorGUIUtility.standardVerticalSpacing * 4; 
        }
    }

    [CustomPropertyDrawer(typeof(EntityComponentTaskUIData))]
    public class EntityComponentTaskUIDataDrawer : ScriptableObjectDrawer<EntityComponentTaskUIData>
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Draw(position, property, label, RTSEditorHelper.EntityComponentTaskUIDataTypes);
        }
    }

    [CustomPropertyDrawer(typeof(FactionTypeInfo))]
    public class FactionTypeDrawer : ScriptableObjectDrawer<FactionTypeInfo>
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Draw(position, property, label, RTSEditorHelper.FactionTypes);
        }
    }

    [CustomPropertyDrawer(typeof(NPCTypeInfo))]
    public class NPCTypeDrawer : ScriptableObjectDrawer<NPCTypeInfo>
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Draw(position, property, label, RTSEditorHelper.NPCTypes);
        }
    }

    [CustomPropertyDrawer(typeof(ResourceTypeInfo)), CustomPropertyDrawer(typeof(ResourceTypeAttribute))]
    public class ResourceTypeDrawer : ScriptableObjectDrawer<ResourceTypeInfo>
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Draw(position, property, label, RTSEditorHelper.ResourceTypes, property.propertyType == SerializedPropertyType.String, "ResourceType");
        }
    }
}
#endif
