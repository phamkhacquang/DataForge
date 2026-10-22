using System;
using UnityEngine;

namespace DataForge.CustomEditor {
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class DisplayAsDateTimeAttribute : PropertyAttribute {
        public DateTimeKind Kind;

        public DisplayAsDateTimeAttribute(DateTimeKind kind = DateTimeKind.Local) {
            Kind = kind;
        }
    }
}