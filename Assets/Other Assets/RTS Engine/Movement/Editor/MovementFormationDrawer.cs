using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

/* MovementFormationDrawer script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine.CustomDrawer
{
    [CustomPropertyDrawer(typeof(MovementFormation))]
    public class MovementFormationDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            label = EditorGUI.BeginProperty(position, label, property);

            int heightMultiplier = GetHeightMultiplier(property);
            float height = (position.height - (heightMultiplier * EditorGUIUtility.standardVerticalSpacing)) / heightMultiplier;

            Rect nextRect = new Rect(position.x, position.y, position.width, height);
            EditorGUI.PropertyField(nextRect, property.FindPropertyRelative("type"), new GUIContent("Movement Formation"));

            nextRect.y += nextRect.height + EditorGUIUtility.standardVerticalSpacing;
            bool showProperties = property.FindPropertyRelative("showProperties").boolValue;
            showProperties = EditorGUI.Foldout(nextRect, showProperties, new GUIContent("Formation Properties"));

            if (showProperties)
            {
                EditorGUI.indentLevel++;

                nextRect.y += nextRect.height + EditorGUIUtility.standardVerticalSpacing;
                EditorGUI.PropertyField(nextRect, property.FindPropertyRelative("spacing"));

                if ((MovementFormation.Type)property.FindPropertyRelative("type").enumValueIndex == MovementFormation.Type.row)
                {
                    nextRect.y += nextRect.height + EditorGUIUtility.standardVerticalSpacing;
                    EditorGUI.PropertyField(nextRect, property.FindPropertyRelative("amount"), new GUIContent("Amount Per Row"));

                    nextRect.y += nextRect.height + EditorGUIUtility.standardVerticalSpacing;
                    EditorGUI.PropertyField(nextRect, property.FindPropertyRelative("maxEmpty"), new GUIContent("Max Empty Rows"));
                }

                EditorGUI.indentLevel--;
            }

            property.FindPropertyRelative("showProperties").boolValue = showProperties;

            EditorGUI.EndProperty();
        }

        //override this function to add space below the new property drawer
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return GetHeightMultiplier(property) * (base.GetPropertyHeight(property, label) + EditorGUIUtility.standardVerticalSpacing);
        }

        private int GetHeightMultiplier(SerializedProperty property)
        {
            int extraProperties = 0;

            switch((MovementFormation.Type)property.FindPropertyRelative("type").enumValueIndex)
            {
                case MovementFormation.Type.circle:
                    extraProperties = 1;
                    break;

                case MovementFormation.Type.row:
                    extraProperties = 3;
                    break;
            }

            return 2 + (property.FindPropertyRelative("showProperties").boolValue ? extraProperties : 0);
        }
    }
}
