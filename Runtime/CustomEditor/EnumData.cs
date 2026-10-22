using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DataForge {
    [Serializable]
    public class EnumData<TEnum, TItem> : IEnumerable<KeyValuePair<TEnum, TItem>> where TEnum : Enum {
        private readonly TEnum[] enumValues;
        [SerializeField] TItem[] items;

        public EnumData() {
            enumValues = (TEnum[])Enum.GetValues(typeof(TEnum));
            EnsureArraySize();
        }

        public TEnum[] Keys => enumValues;
        public TItem[] Values => items ??= new TItem[enumValues.Length];
        public int Count => Values.Length;

        public TItem this[TEnum x] {
            get => Values[IndexOf(x)];
            set {
                EnsureArraySize();
                items[IndexOf(x)] = value;
            }
        }

        private int IndexOf(TEnum enumValue) => Array.IndexOf(enumValues, enumValue);

        private void EnsureArraySize() {
            int targetLength = enumValues.Length;
            if (items == null || items.Length != targetLength) {
                var resized = new TItem[targetLength];
                if (items != null) {
                    Array.Copy(items, resized, Math.Min(items.Length, targetLength));
                }
                items = resized;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public IEnumerator<KeyValuePair<TEnum, TItem>> GetEnumerator() {
            var keys = Keys;
            EnsureArraySize();
            for (int i = 0; i < keys.Length; i++) {
                yield return new KeyValuePair<TEnum, TItem>(keys[i], items[i]);
            }
        }
    }
}