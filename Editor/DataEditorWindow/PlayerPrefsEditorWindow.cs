using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DataForge {
    internal class PlayerPrefsEditorWindow : EditorWindow {
        private enum PrefType { String, Int, Float }

        private class PrefEntry {
            public string Key;
            public PrefType Type;
            public string StringValue;
            public int IntValue;
            public float FloatValue;
            public bool Dirty;
        }

        private Vector2 scroll;
        private string searchFilter = "";
        private PrefType newType = PrefType.String;
        private string newKey = "";
        private string newValue = "";
        private bool addFoldout = true;
        private readonly List<PrefEntry> entries = new();
        private bool loaded = false;
        private string status = "";

        [MenuItem("Data Forge/PlayerPrefs Editor", false, 100)]
        public static void ShowWindow() => GetWindow<PlayerPrefsEditorWindow>("PlayerPrefs Editor");

        [MenuItem("Data Forge/Delete PlayerPrefs/Yes", false, 101)]
        public static void DeleteAllPlayerPrefs() {
            PlayerPrefs.DeleteAll();
            Debug.Log("Delete PlayerPrefs Complete");
        }

        private void OnGUI() {
            EditorGUILayout.Space(4);

            if (GUILayout.Button("Reload", GUILayout.Width(72)))
                LoadEntries();

            GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
            if (GUILayout.Button("Delete All", GUILayout.Width(80))) {
                if (EditorUtility.DisplayDialog("Delete All", "Delete ALL PlayerPrefs?", "Yes", "No")) {
                    PlayerPrefs.DeleteAll();
                    LoadEntries();
                }
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Search:", GUILayout.Width(50));
            searchFilter = EditorGUILayout.TextField(searchFilter);
            if (GUILayout.Button("✕", GUILayout.Width(22)))
                searchFilter = "";

            EditorGUILayout.Space(4);
            DrawAddPanel();

            if (!string.IsNullOrEmpty(status)) {
                EditorGUILayout.HelpBox(status, MessageType.Info);
            }

            EditorGUILayout.Space(2);

            if (!loaded)
                LoadEntries();

            DrawEntries();
        }

        private void DrawAddPanel() {
            addFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(addFoldout, "Add / Edit Entry");
            if (addFoldout) {
                EditorGUI.indentLevel++;
                using (new EditorGUILayout.HorizontalScope()) {
                    EditorGUILayout.LabelField("Type", GUILayout.Width(40));
                    newType = (PrefType)EditorGUILayout.EnumPopup(newType, GUILayout.Width(80));
                    EditorGUILayout.LabelField("Key", GUILayout.Width(28));
                    newKey = EditorGUILayout.TextField(newKey);
                    EditorGUILayout.LabelField("Value", GUILayout.Width(38));
                    newValue = EditorGUILayout.TextField(newValue);

                    GUI.backgroundColor = new Color(0.4f, 0.9f, 0.4f);
                    if (GUILayout.Button("Set", GUILayout.Width(40)))
                        CommitNewEntry();
                    GUI.backgroundColor = Color.white;
                }
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawEntries() {
            scroll = EditorGUILayout.BeginScrollView(scroll);

            string filter = searchFilter.ToLowerInvariant();

            foreach (var entry in entries) {
                if (!string.IsNullOrEmpty(filter) &&
                    !entry.Key.ToLowerInvariant().Contains(filter))
                    continue;

                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox)) {
                    using (new EditorGUILayout.HorizontalScope()) {
                        EditorGUILayout.LabelField(
                            new GUIContent(entry.Key, entry.Key),
                            GUILayout.Width(180), GUILayout.MinWidth(80));

                        EditorGUILayout.LabelField(entry.Type.ToString(),
                            EditorStyles.miniLabel, GUILayout.Width(46));

                        EditorGUI.BeginChangeCheck();
                        switch (entry.Type) {
                            case PrefType.String:
                                entry.StringValue = EditorGUILayout.TextField(entry.StringValue);
                                break;
                            case PrefType.Int:
                                entry.IntValue = EditorGUILayout.IntField(entry.IntValue);
                                break;
                            case PrefType.Float:
                                entry.FloatValue = EditorGUILayout.FloatField(entry.FloatValue);
                                break;
                        }
                        if (EditorGUI.EndChangeCheck())
                            entry.Dirty = true;

                        GUI.backgroundColor = entry.Dirty ? new Color(0.4f, 0.9f, 0.4f) : Color.white;
                        if (GUILayout.Button("Save", GUILayout.Width(44))) {
                            SaveEntry(entry);
                            entry.Dirty = false;
                            status = $"Saved: {entry.Key}";
                        }
                        GUI.backgroundColor = Color.white;

                        GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
                        if (GUILayout.Button("✕", GUILayout.Width(22))) {
                            PlayerPrefs.DeleteKey(entry.Key);
                            LoadEntries();
                            break;
                        }
                        GUI.backgroundColor = Color.white;
                    }
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void LoadEntries() {
            if (Application.platform == RuntimePlatform.WindowsEditor) {
                entries.Clear();

                string company = Application.companyName;
                string product = Application.productName;

                object registryKey = null;

                var registryType = Type.GetType("Microsoft.Win32.Registry, Microsoft.Win32.Registry")
                                   ?? Type.GetType("Microsoft.Win32.Registry");

                if (registryType != null) {
                    var currentUserProp = registryType.GetProperty("CurrentUser", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                    var currentUser = currentUserProp?.GetValue(null);
                    if (currentUser != null) {
                        var openSubKey = currentUser.GetType().GetMethod("OpenSubKey", new Type[] { typeof(string) });
                        registryKey = openSubKey?.Invoke(currentUser, new object[] { "Software\\Unity\\UnityEditor\\" + company + "\\" + product });
                    }
                }

                if (registryKey != null) {
                    var getValueNamesMethod = registryKey.GetType().GetMethod("GetValueNames", Type.EmptyTypes);
                    string[] valueNames = (string[])getValueNamesMethod.Invoke(registryKey, null);
                    foreach (string valueName in valueNames) {
                        if (valueName.StartsWith("unity.")) continue; // Skip Unity internal keys

                        string key = valueName;

                        // Remove the _h193410979 style suffix used on player pref keys in Windows registry
                        int index = key.LastIndexOf("_");
                        if (index > 0) key = key[..index];

                        // Get the value from the registry via reflection
                        var getValueMethod = registryKey.GetType().GetMethod("GetValue", new Type[] { typeof(string) });
                        object ambiguousValue = getValueMethod.Invoke(registryKey, new object[] { valueName });

                        PrefEntry entry = new() { Key = key };

                        // Unfortunately floats will come back as an int (at least on 64 bit) because the float is stored as
                        // 64 bit but marked as 32 bit - which confuses the GetValue() method greatly! 
                        if (ambiguousValue.GetType() == typeof(int)) {
                            // If the player pref is not actually an int then it must be a float, this will evaluate to true
                            // (impossible for it to be 0 and -1 at the same time)
                            if (PlayerPrefs.GetInt(key, -1) == -1 && PlayerPrefs.GetInt(key, 0) == 0) {
                                // Fetch the float value from PlayerPrefs in memory
                                entry.Type = PrefType.Float;
                                entry.FloatValue = PlayerPrefs.GetFloat(key);
                            } else {
                                entry.Type = PrefType.Int;
                                entry.IntValue = (int)ambiguousValue;
                            }
                        } else if (ambiguousValue.GetType() == typeof(byte[])) {
                            // On Unity 5 a string may be stored as binary, so convert it back to a string
                            entry.Type = PrefType.String;
                            entry.StringValue = System.Text.Encoding.Default.GetString((byte[])ambiguousValue);
                        }
                    }
                }
                loaded = true;

            } else {
                throw new NotSupportedException("PlayerPrefsEditor doesn't support this Unity Editor platform");
            }
        }

        private void CommitNewEntry() {
            if (string.IsNullOrWhiteSpace(newKey)) {
                status = "Key cannot be empty.";
                return;
            }

            var entry = new PrefEntry { Key = newKey, Type = newType };
            switch (newType) {
                case PrefType.String:
                    entry.StringValue = newValue;
                    break;
                case PrefType.Int:
                    entry.IntValue = int.TryParse(newValue, out int iv) ? iv : 0;
                    break;
                case PrefType.Float:
                    entry.FloatValue = float.TryParse(newValue, out float fv) ? fv : 0f;
                    break;
            }

            SaveEntry(entry);
            status = $"Set: {newKey}";
            newKey = "";
            newValue = "";
            LoadEntries();
        }

        private static void SaveEntry(PrefEntry entry) {
            switch (entry.Type) {
                case PrefType.String: PlayerPrefs.SetString(entry.Key, entry.StringValue); break;
                case PrefType.Int: PlayerPrefs.SetInt(entry.Key, entry.IntValue); break;
                case PrefType.Float: PlayerPrefs.SetFloat(entry.Key, entry.FloatValue); break;
            }
            PlayerPrefs.Save();
        }
    }
}
