using System;
using System.Collections;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace DataForge.CustomEditor {
    [CustomPropertyDrawer(typeof(EnumData<,>))]
    public class EnumDataDrawer : PropertyDrawer {
        public override VisualElement CreatePropertyGUI(SerializedProperty property) {
            var root = new Foldout { text = property.displayName, value = true };

            var itemsSP = property.FindPropertyRelative("items")
                ?? throw new InvalidOperationException($"Missing 'items' property in {property.displayName}");
            var itemType = GetEnumType();
            var itemValues = Enum.GetNames(itemType);
            if (itemsSP.arraySize != itemValues.Length)
                itemsSP.arraySize = itemValues.Length;

            property.isExpanded = true;

            var container = new VisualElement();
            for (int i = 0; i < itemValues.Length; i++) {
                var itemProperty = itemsSP.GetArrayElementAtIndex(i);
                container.Add(new PropertyField(itemProperty, itemValues[i]));
            }

            root.Add(container);
            root.RegisterValueChangedCallback(evt => property.isExpanded = evt.newValue);
            return root;
        }

        private Type GetEnumType() {
            var fieldType = fieldInfo.FieldType;
            if (fieldType.IsArray) {
                fieldType = fieldType.GetElementType();
            } else if (fieldType.IsGenericType && typeof(IList).IsAssignableFrom(fieldType)) {
                fieldType = fieldType.GenericTypeArguments[0];
            }
            if (fieldType.GetGenericTypeDefinition() == typeof(EnumData<,>)) {
                return fieldType.GenericTypeArguments[0];
            }
            throw new InvalidOperationException($"Unsupported type: {fieldInfo.FieldType}");
        }
    }
}