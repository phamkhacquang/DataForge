using System.Reflection;
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace DataForge {
    public class LocalDataEditorWindow : DataEditorWindow {
        [MenuItem("Data Forge/Data Editor", false, 1)]
        public static void ShowWindow() => ShowWindow<LocalDataEditorWindow>("Data Editor");

        protected static Type[] GetAllDataTypes() {
            var wrapperType = Type.GetType("GeneratedDataWrapper, Assembly-CSharp");
            return wrapperType?.GetFields(BindingFlags.Instance | BindingFlags.Public)
                               .Select(f => f.FieldType)
                               .ToArray() ?? Array.Empty<Type>();
        }

        [MenuItem("Data Forge/Delete Data/Yes", false, 3)]
        public static void DeleteData() {
            foreach (var dataType in GetAllDataTypes()) {
                var deleteMethod = dataType.BaseType.GetMethod("Delete");
                deleteMethod.Invoke(null, null);
            }
            Debug.Log("Finished deleting all data");
        }

        protected override void Load() {
            var baseDataType = cField.FieldType.BaseType;
            var cacheProp = baseDataType.GetProperty("Cache", BindingFlags.Public | BindingFlags.Static);
            var cacheValue = cacheProp.GetValue(null);
            cField.SetValue(wrapper, cacheValue);
        }

        protected override void Save(object obj) {
            var saveMethod = obj.GetType().GetMethod("Save", BindingFlags.Public | BindingFlags.Instance);
            saveMethod?.Invoke(obj, null);
        }
    }
}