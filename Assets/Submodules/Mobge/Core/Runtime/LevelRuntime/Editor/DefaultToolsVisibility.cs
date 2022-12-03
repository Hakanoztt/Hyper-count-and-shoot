using System;
using System.Reflection;
using UnityEditor;

namespace Mobge.Utility {
    public static class DefaultToolsVisibility {
        private static bool Hidden {
            get {
                Type type = typeof (Tools);
                FieldInfo field = type.GetField ("s_Hidden", BindingFlags.NonPublic | BindingFlags.Static);
                return ((bool) field.GetValue (null));
            }
            set {
                Type type = typeof (Tools);
                FieldInfo field = type.GetField ("s_Hidden", BindingFlags.NonPublic | BindingFlags.Static);
                field.SetValue (null, value);
            }
        }
        public static void HideTools() {
            Hidden = true;
        }
        public static void UnHideTools() {
            Hidden = false;
        }
    }
}

