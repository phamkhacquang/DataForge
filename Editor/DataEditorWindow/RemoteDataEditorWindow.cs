using System;
using System.Net.Http;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace DataForge {
    internal class RemoteDataEditorWindow : DataEditorWindow {
        private HttpClient client;
        private TextField ipAddress;
        private Label statusLabel;
        private Type dataType;

        [MenuItem("Data Forge/Remote Data Editor", false, 2)]
        public static void ShowWindow() => ShowWindow<RemoteDataEditorWindow>("Remote Data Editor");

        private void OnEnable() {
            client = new HttpClient();
        }

        protected override void OnDisable() {
            base.OnDisable();
            client.Dispose();
        }

        protected override VisualElement DrawMain() {
            var root = new VisualElement();

            var controlGroup = new VisualElement() {
                style = { flexDirection = FlexDirection.Row }
            };
            var reloadButton = new Button(LoadDataAsync) {
                text = "Load",
                style = { width = 100 }
            };
            controlGroup.Add(reloadButton);
            ipAddress = new TextField("Address") {
                value = "192.168.1.1",
                style = { width = 250 }
            };
            ipAddress.labelElement.style.minWidth = 0;
            ipAddress.labelElement.style.width = StyleKeyword.Auto;
            controlGroup.Add(ipAddress);
            statusLabel = new Label() {
                style = { unityTextAlign = TextAnchor.MiddleLeft }
            };
            controlGroup.Add(statusLabel);

            root.Add(SetPadding(controlGroup, 7, 7, 7, 7));
            root.Add(base.DrawMain());
            return root;
        }

        private void Log(string s) => statusLabel.text = s;
        private string GetURL() => $"http://{ipAddress.value}:{BaseDataStreamServer.PORT}/{dataType.Name}";

        private async void LoadDataAsync() {
            Log($"Loading...");
            try {
                string json = await client.GetStringAsync(GetURL());
                var data = JsonUtility.FromJson(json, dataType);
                Log($"Loaded successfully");
                cField.SetValue(wrapper, data);
            } catch (Exception ex) {
                Log($"Load error: {ex.Message}");
            }
        }

        protected override void Load() {
            dataType = cField.FieldType;
        }

        protected override async void Save(object data) {
            Log($"Saving...");
            try {
                var json = JsonUtility.ToJson(data);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var resp = await client.PostAsync(GetURL(), content);
                string result = await resp.Content.ReadAsStringAsync();
                Log($"Save response: {result}");
            } catch (Exception ex) {
                Log($"Save error: {ex.Message}");
            }
        }
    }
}