using System;
using System.Globalization;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace DataForge.CustomEditor {
    [CustomPropertyDrawer(typeof(DisplayAsDateTimeAttribute))]
    public class DisplayAsDateTimeDrawer : PropertyDrawer {
        private const string FORMAT = "yyyy-MM-dd HH:mm:ss,fff";

        public override VisualElement CreatePropertyGUI(SerializedProperty property) {
            var att = (DisplayAsDateTimeAttribute)attribute;
            var root = new VisualElement();
            root.style.flexDirection = FlexDirection.Row;

            var timeTextField = new TextField(property.displayName) {
                isDelayed = true
            };
            timeTextField.style.flexGrow = 1;

            Toggle localToggle = null;
            if (att.Kind == DateTimeKind.Utc) {
                localToggle = new Toggle("Local");
                localToggle.labelElement.style.minWidth = 0;
                localToggle.labelElement.style.width = StyleKeyword.Auto;
                localToggle.RegisterValueChangedCallback(_ => UpdateUI(property, att.Kind, timeTextField, localToggle));
            }

            timeTextField.RegisterValueChangedCallback(evt => OnValueChanged(evt, property, att.Kind, timeTextField, localToggle));

            root.Add(timeTextField);
            if (localToggle != null) {
                root.Add(localToggle);
            }

            root.TrackPropertyValue(property, prop => UpdateUI(prop, att.Kind, timeTextField, localToggle));

            UpdateUI(property, att.Kind, timeTextField, localToggle);

            return root;
        }

        private void OnValueChanged(ChangeEvent<string> evt, SerializedProperty property, DateTimeKind storageKind, TextField timeTextField, Toggle localToggle) {
            string input = evt.newValue;
            DateTimeKind displayKind = GetDisplayKind(storageKind, localToggle);

            // Reset to Now if empty
            if (string.IsNullOrWhiteSpace(input)) {
                DateTime now = displayKind == DateTimeKind.Utc ? DateTime.UtcNow : DateTime.Now;
                ApplyTicks(property, ConvertDisplayedDateToStoredTicks(now, storageKind, displayKind));
                return;
            }

            // Try parse as the specific Date format
            bool isDate = DateTime.TryParseExact(
                input,
                FORMAT,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out DateTime parsedDate
            );

            if (isDate) {
                ApplyTicks(property, ConvertDisplayedDateToStoredTicks(parsedDate, storageKind, displayKind));
                return;
            }

            // If not a date, check if it's a raw long (ticks)
            if (long.TryParse(input, out long rawTicks)) {
                ApplyTicks(property, rawTicks);
            } else {
                UpdateUI(property, storageKind, timeTextField, localToggle);
            }
        }

        private void ApplyTicks(SerializedProperty property, long ticks) {
            property.longValue = ticks;
            property.serializedObject.ApplyModifiedProperties();
            // UpdateUI is called automatically via TrackPropertyValue
        }

        private void UpdateUI(SerializedProperty property, DateTimeKind storageKind, TextField timeTextField, Toggle localToggle) {
            bool isWithinDateTimeRange = IsWithinDateTimeRange(property.longValue);
            SetDisplay(localToggle, isWithinDateTimeRange ? DisplayStyle.Flex : DisplayStyle.None);

            if (!isWithinDateTimeRange) {
                timeTextField.SetValueWithoutNotify(property.longValue.ToString(CultureInfo.InvariantCulture));
                return;
            }

            DateTime storedDate = new(property.longValue, storageKind);
            DateTime displayDate = ConvertStoredDateToDisplay(storedDate, GetDisplayKind(storageKind, localToggle));
            timeTextField.SetValueWithoutNotify(displayDate.ToString(FORMAT, CultureInfo.InvariantCulture));
        }

        private static bool IsWithinDateTimeRange(long ticks) {
            return ticks >= DateTime.MinValue.Ticks && ticks <= DateTime.MaxValue.Ticks;
        }

        private static void SetDisplay(VisualElement element, DisplayStyle displayStyle) {
            if (element != null) {
                element.style.display = displayStyle;
            }
        }

        private static DateTimeKind GetDisplayKind(DateTimeKind storageKind, Toggle displayAsLocalToggle) {
            return storageKind == DateTimeKind.Utc && displayAsLocalToggle is { value: true }
                ? DateTimeKind.Local
                : storageKind;
        }

        private static long ConvertDisplayedDateToStoredTicks(DateTime displayedDate, DateTimeKind storageKind, DateTimeKind displayKind) {
            DateTime normalizedDate = DateTime.SpecifyKind(displayedDate, displayKind);

            if (storageKind == DateTimeKind.Utc && displayKind == DateTimeKind.Local) {
                return normalizedDate.ToUniversalTime().Ticks;
            }

            if (storageKind == DateTimeKind.Local && displayKind == DateTimeKind.Utc) {
                return normalizedDate.ToLocalTime().Ticks;
            }

            return normalizedDate.Ticks;
        }

        private static DateTime ConvertStoredDateToDisplay(DateTime storedDate, DateTimeKind displayKind) {
            if (storedDate.Kind == DateTimeKind.Utc && displayKind == DateTimeKind.Local) {
                return storedDate.ToLocalTime();
            }

            if (storedDate.Kind == DateTimeKind.Local && displayKind == DateTimeKind.Utc) {
                return storedDate.ToUniversalTime();
            }

            return storedDate;
        }
    }
}