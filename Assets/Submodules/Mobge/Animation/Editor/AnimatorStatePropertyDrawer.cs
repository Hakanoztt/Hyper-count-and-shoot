using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEditor.Animations;
using System.Linq;
using static Mobge.UnityExtensions;

namespace Mobge.Animation {

    [CustomPropertyDrawer(typeof(AnimatorStateAttribute))]
    public class AnimatorStateAttributeDrawer : BasePropertyDrawer {
        private readonly LayoutRectSource source = new LayoutRectSource();
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {

            if (property.serializedObject.targetObjects.Length > 1) {
                var p1 = position;
                var p2 = position;
                p1.width = EditorGUIUtility.labelWidth;
                p2.width -= p1.width;
                p2.x += p1.width;
                GUI.Label(p1, label);
                GUI.Label(p2, "Multiple object editing is not supported.");
                return;
            }

            source.Reset(position);

            var target = property.serializedObject.targetObject;
            //if(!(target is Component)) {
            //    var c = GUI.contentColor;
            //    GUI.contentColor = Color.red;
            //    EditorGUI.LabelField(position, "ERROR! [AnimatorState] or [AnimatorParameter] attribute can only be used at a Component property! " + GetType());
            //    GUI.contentColor = c;
            //    return;
            //}
            if (property.propertyType != SerializedPropertyType.Integer && property.propertyType != SerializedPropertyType.String) {
                var c = GUI.contentColor;
                GUI.contentColor = Color.red;
                EditorGUI.LabelField(position, "ERROR! [AnimatorState] or [AnimatorParameter] attribute can only be used at property of type int or string! " + GetType());
                GUI.contentColor = c;
                return;
            }
            var runtimeAnimatorController = AnimatorUtility.FindController(target, out bool error);
            if (error) {
                using (Scopes.GUIBackgroundColor(Color.red)) {
                        EditorGUI.LabelField(position, "ERROR! [AnimatorState] or [AnimatorParameter] attribute could not find a suitable Animator! " + GetType());
                    }
            }
            if (runtimeAnimatorController == null) {
                using (Scopes.GUIBackgroundColor(Color.red)) {
                    EditorGUI.LabelField(position, "ERROR! [AnimatorState] or [AnimatorParameter] attribute could not find AnimatorController on selected Object! " + GetType());
                }
                return;
            }
            var controller = runtimeAnimatorController;
            var att = (AnimatorStateAttribute)this.attribute;
            DrawPicker(position, property, label, controller, att.noneOption);
            SetHeight(property, source.Height);
        }

        protected virtual void DrawPicker(Rect position, SerializedProperty property, GUIContent label, AnimatorController controller, string noneOption) {
            AnimatorUtility.AnimatorStateField(position, property, label, controller, noneOption);
        }
    }
    public static class AnimatorUtility {
        public static readonly Dictionary<int, string> hashToName = new Dictionary<int, string>();
        public static readonly Dictionary<string, int> nameToHash = new Dictionary<string, int>();
        public static Animator FindAnimator(Object obj) {

            if (!(obj is Component c)) {
                return null;
            }
            if (!(c is IAnimatorOwner ao)) {
                ao = GetComponentInParent<IAnimatorOwner>(c.transform);
            }
            return ao != null ? ao.GetAnimator() : c.GetComponentInChildren<Animator>();
        }
        public static AnimatorController FindController(UnityEngine.Object target, out bool error) {
            error = false;
            AnimatorController c = null;
            if (target is IAnimatorControllerOwner o) {
                c = o.GetAnimatorController() as AnimatorController;
            }
            else if (target is Component self) {
                var animator = FindAnimator(self);
                if (animator == null) {
                    error = true;
                    return c;
                }
                c = animator.runtimeAnimatorController as AnimatorController;

            }
            return c;
        }
        private static bool UpdateHashes(Rect position, AnimatorController controller, int selectedLayer) {
            hashToName.Clear();
            nameToHash.Clear();
            var layers = controller.layers;
            for (int i = 0; i < layers.Length; i++) {
                if (selectedLayer < 0 || selectedLayer == i) {
                    foreach (var state in layers[i].stateMachine.states) {
                        hashToName.Add(state.state.nameHash, state.state.name);
                        nameToHash.Add(state.state.name, state.state.nameHash);
                    }
                }
            }
            if (hashToName.Keys.Count <= 0) {
                var c = GUI.contentColor;
                GUI.contentColor = Color.red;
                EditorGUI.LabelField(position, "ERROR on [AnimatorState] attribute. Attached animators do not have any states! ");
                GUI.contentColor = c;
                return false;
            }
            return true;
        }
        private static int IntStateField(Rect position, GUIContent label, int state, string noneOption = "<none>") {
            hashToName.TryGetValue(state, out string inputState);
            EditorGUI.BeginChangeCheck();
            var name = EditorDrawer.Popup(position, label.text, hashToName.Values.ToList(),
                        inputState, (c) => c, noneOption);
            if (EditorGUI.EndChangeCheck()) {
                if (name != null && nameToHash.TryGetValue(name, out int val)) {
                    state = val;
                }
                else {
                    state = 0;
                }
            }
            return state;
        }
        public static int AnimatorStateField(Rect position, GUIContent label, AnimatorController controller, int state, int layer = -1, string noneOption = "<none>") {
            if (!UpdateHashes(position, controller, layer)) {
                return state;
            }
            return IntStateField(position, label, state, noneOption);
        }
        public static int DrawStatePopupLayout(GUIContent label, AnimatorController animator, int state, int layer = -1, string noneOption = "<none>") {
            return AnimatorStateField(EditorGUILayout.GetControlRect(), label, animator, state, layer, noneOption);
        }
        public static void AnimatorStateField(Rect position, SerializedProperty property, GUIContent label, AnimatorController controller, string noneOptipon = null) {
            if (!UpdateHashes(position, controller, -1)) {
                return;
            }

            EditorGUI.BeginChangeCheck();
            if (property.propertyType == SerializedPropertyType.Integer) {
                //if (!hashToName.ContainsKey(property.intValue)) {
                //    property.intValue = hashToName.Keys.ToArray()[0];
                //}
                property.intValue = IntStateField(position, label, property.intValue, noneOptipon);
            }
            else {
                if (!nameToHash.ContainsKey(property.stringValue)) {
                    property.stringValue = nameToHash.Keys.ToArray()[0];
                }
                property.stringValue =
                    EditorDrawer.Popup(position, label.text, hashToName.Values.ToList(),
                        property.stringValue, (c) => c, noneOptipon);
            }
            if (EditorGUI.EndChangeCheck()) {
                property.serializedObject.ApplyModifiedProperties();
            }
        }

    }
}