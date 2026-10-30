using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;
using Label = UnityEngine.UIElements.Label;

namespace DataForge {
    public abstract class DataEditorWindow : EditorWindow {
        private const string SELECTED_IDX = "DataForge.DataEditorWindow.SelectedIdx";

        protected Object wrapper;
        private FieldInfo[] fields = Array.Empty<FieldInfo>();
        private SerializedObject so;
        protected FieldInfo cField;
        private VisualElement rightPane;
        private VisualElement saveBtn;
        private string snapshot;

        protected virtual void OnDisable() {
            if (wrapper != null) DestroyImmediate(wrapper);
            so?.Dispose();
        }

        protected static void ShowWindow<TWindow>(string title) where TWindow : EditorWindow {
            var window = GetWindow<TWindow>();
            window.titleContent = new GUIContent(title);
            window.minSize = new Vector2(600, 400);
            window.Show();
        }

        public void CreateGUI() {
            BuildUI();
            rootVisualElement.focusable = true;
            rootVisualElement.RegisterCallback<KeyDownEvent>(OnRootKeyDown);
        }

        private void BuildUI() {
            rootVisualElement.Clear();

            var wrapperType = Type.GetType("GeneratedDataWrapper, Assembly-CSharp");
            if (wrapperType != null) {
                wrapper = CreateInstance(wrapperType);
                fields = wrapperType.GetFields(BindingFlags.Instance | BindingFlags.Public);
                so = new SerializedObject(wrapper);
            }

            VisualElement main;
            if (fields == null || fields.Length == 0) {
                main = new VisualElement() {
                    style = { justifyContent = Justify.Center, alignItems = Align.Center }
                };
                main.Add(new Label("No data classes found") {
                    style = { unityTextAlign = TextAnchor.MiddleCenter }
                });
                main.Add(new Button(CreateConfig) {
                    text = "Create",
                    style = { width = 250, height = 30 }
                });
            } else {
                main = DrawMain();
            }

            main.style.flexGrow = 1;
            rootVisualElement.Add(main);
            rootVisualElement.Add(GUICopyright());
        }

        private static void CreateConfig() {
            byte[] key = new byte[16];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(key);
            string filePath = EditorUtility.SaveFilePanel("Save Config", "Assets", "MyConfig.cs", "cs");
            if (!string.IsNullOrEmpty(filePath)) {
                var className = Path.GetFileNameWithoutExtension(filePath);
                var configContent =
$@"public class {className} : DataForge.Config {{
    protected override byte[] Key => new byte[] {{ {string.Join(", ", key)} }};
}}";
                File.WriteAllText(filePath, configContent);
                AssetDatabase.Refresh();
            }
        }

        protected VisualElement SetPadding(VisualElement element, float t, float b, float l, float r) {
            element.style.paddingTop = t;
            element.style.paddingBottom = b;
            element.style.paddingLeft = l;
            element.style.paddingRight = r;
            return element;
        }

        protected virtual VisualElement DrawMain() {
            var splitView = new TwoPaneSplitView(0, 250, TwoPaneSplitViewOrientation.Horizontal);

            var leftListView = new ListView(fields, 30, () => SetPadding(new Label(), 0, 0, 7, 7),
            (e, i) => {
                var label = e as Label;
                label.text = fields[i].FieldType.Name;
                label.style.unityTextAlign = TextAnchor.MiddleLeft;
            });
            splitView.Add(leftListView);
            rightPane = new ScrollView(ScrollViewMode.VerticalAndHorizontal);
            splitView.Add(SetPadding(rightPane, 7, 7, 7, 7));

            leftListView.selectedIndicesChanged += UpdateRightPane;
            leftListView.selectedIndex = EditorPrefs.GetInt(SELECTED_IDX, 0);
            return splitView;
        }

        private VisualElement GUICopyright() => new Label("©quangpk") {
            style = { color = Color.gray, marginLeft = 5, marginBottom = 5 }
        };

        private void UpdateRightPane(IEnumerable<int> selectedIndices) {
            rightPane.Clear();

            var idx = selectedIndices.FirstOrDefault();
            EditorPrefs.SetInt(SELECTED_IDX, idx);

            saveBtn = new Button(Save) { text = "Save", style = { width = 150, height = 24, marginBottom = 7 } };
            rightPane.Add(saveBtn);

            cField = fields[idx];
            DrawData(rightPane);
        }

        private void HideFoldoutHeader(VisualElement field) {
            var foldout = field.Q<Foldout>();
            if (foldout == null) return;

            foldout.value = true;
            var toggle = foldout.Q<Toggle>();
            if (toggle != null) toggle.style.display = DisplayStyle.None;

            var content = foldout.Q<VisualElement>("unity-content");
            if (content != null) content.style.marginLeft = 0;
        }

        protected virtual void DrawData(VisualElement root) {
            Load();
            var prop = so.FindProperty(cField.Name);
            snapshot = JsonUtility.ToJson(prop.serializedObject.targetObject);
            var field = new PropertyField(prop);
            root.Add(field);
            root.Bind(so);
            root.TrackPropertyValue(prop, UpdateSaveButton);
            field.RegisterCallback<GeometryChangedEvent>(_ => HideFoldoutHeader(field));
        }

        private void UpdateSaveButton(SerializedProperty prop) {
            bool dirty = snapshot != JsonUtility.ToJson(prop.serializedObject.targetObject);
            saveBtn.style.backgroundColor = dirty ? new Color(0.15f, 0.65f, 0.2f) : StyleKeyword.Null;
        }

        private void OnRootKeyDown(KeyDownEvent evt) {
            if ((evt.ctrlKey || evt.commandKey) && evt.keyCode == KeyCode.S && saveBtn != null && saveBtn.enabledSelf) {
                Save();
                evt.StopPropagation();
                evt.PreventDefault();
            }
        }

        private void Save() {
            so.ApplyModifiedProperties();
            var obj = cField.GetValue(wrapper);
            Save(obj);
            saveBtn.style.backgroundColor = StyleKeyword.Null;
        }

        protected abstract void Load();
        protected abstract void Save(object obj);
    }
}