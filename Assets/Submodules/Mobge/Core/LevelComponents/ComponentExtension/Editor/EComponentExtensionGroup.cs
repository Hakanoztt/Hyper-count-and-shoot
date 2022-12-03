using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Mobge.Core.Components {

    [CustomEditor(typeof(ComponentExtensionGroup))]
    public class EComponentExtensionGroup : Editor {

        private ComponentExtensionGroup ceg;

        protected void OnEnable() {
            ceg = target as ComponentExtensionGroup;
        }

        public override void OnInspectorGUI() {
            base.OnInspectorGUI();
            if (ceg == null) return;

            if (ceg.componentExtensions == null) {
                ceg.componentExtensions = new Component[0];
            }
            var inspectorItems = GetExtensionsInHierarchy();
            if (!Compare(ceg.componentExtensions, inspectorItems)) {
                ceg.componentExtensions = inspectorItems.ConvertAll(extension => (Component)extension).ToArray();
                GUI.changed = true;
            }

            if (GUI.changed) {
                EditorExtensions.SetDirty(ceg);
            }
        }

        private List<IComponentExtension> tempItems = new List<IComponentExtension>();
        private List<IComponentExtension> GetExtensionsInHierarchy() {
            tempItems.Clear();
            ceg.GetComponentsInChildren(tempItems);
            return tempItems;
        }

        private bool Compare(Component[] a1, List<IComponentExtension> a2) {
            if (a1.Length != a2.Count) return false;
            for (int i = 0; i < a1.Length; i++) {
                var a2i = a2[i];
                if(a2i is Component component) {
                    if (a1[i] != component) {
                        return false;
                    }
                } else {
                    return false;
                }
            }
            return true;
        }
    }
}
