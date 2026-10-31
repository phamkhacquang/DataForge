
using System;
using UnityEditor;
using UnityEngine;

namespace DataForge.CustomEditor
{
    [CustomPropertyDrawer(typeof(EnumData<,>), true)]
    public class EnumDataDrawer : PropertyDrawer
    {
        private Type GetEnumType()
        {
            Type type = fieldInfo.FieldType;

            if (type.IsArray)
                type = type.GetElementType();

            if (type.IsGenericType &&
                type.GetGenericTypeDefinition() == typeof(EnumData<,>))
            {
                return type.GetGenericArguments()[0];
            }

            throw new InvalidOperationException(
                $"Unsupported type: {fieldInfo.FieldType}"
            );
        }

        public override void OnGUI(
            Rect position,
            SerializedProperty property,
            GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var items = property.FindPropertyRelative("items");

            if (items == null)
            {
                EditorGUI.LabelField(position, label.text, "Missing items");
                EditorGUI.EndProperty();
                return;
            }

            var enumNames = Enum.GetNames(GetEnumType());

            if (items.arraySize != enumNames.Length)
                items.arraySize = enumNames.Length;

            property.isExpanded = EditorGUI.Foldout(
                new Rect(
                    position.x,
                    position.y,
                    position.width,
                    EditorGUIUtility.singleLineHeight
                ),
                property.isExpanded,
                label,
                true
            );

            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;

                float y = position.y + EditorGUIUtility.singleLineHeight + 2;

                for (int i = 0; i < enumNames.Length; i++)
                {
                    var item = items.GetArrayElementAtIndex(i);

                    float height = EditorGUI.GetPropertyHeight(item, true);

                    var rect = new Rect(
                        position.x,
                        y,
                        position.width,
                        height
                    );

                    EditorGUI.PropertyField(
                        rect,
                        item,
                        new GUIContent(enumNames[i]),
                        true
                    );

                    y += height + 2;
                }

                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(
            SerializedProperty property,
            GUIContent label)
        {
            float height = EditorGUIUtility.singleLineHeight;

            if (!property.isExpanded)
                return height;

            var items = property.FindPropertyRelative("items");

            if (items == null)
                return height;

            for (int i = 0; i < items.arraySize; i++)
            {
                var item = items.GetArrayElementAtIndex(i);

                height += EditorGUI.GetPropertyHeight(item, true) + 2;
            }

            return height;
        }
    }
}
