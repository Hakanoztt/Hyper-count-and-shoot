using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Mobge.IdleGame {

    public class IdleGameTagManager {

        [UnityEditor.Callbacks.DidReloadScripts]
        private static void OnScriptsReloaded() {
            EnsureTag(WalletComponent.c_tag);
            EnsureTag(PopupOpener.c_tag);
            EnsureTag(BaseItemStack.c_tag);
        }

        public static bool EnsureTag(string tagName) {
            // Open tag manager
            SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            // Tags Property
            SerializedProperty tagsProp = tagManager.FindProperty("tags");
            
            // if not found, add it
            if (!PropertyExists(tagsProp, 0, tagsProp.arraySize, tagName)) {
                int index = tagsProp.arraySize;
                // Insert new array element
                tagsProp.InsertArrayElementAtIndex(index);
                SerializedProperty sp = tagsProp.GetArrayElementAtIndex(index);
                // Set array element to tagName
                sp.stringValue = tagName;
                Debug.Log("Tag: " + tagName + " has been added");
                // Save settings
                tagManager.ApplyModifiedProperties();
                EditorExtensions.SetDirty(tagManager.targetObject);

                return true;
            }
            else {
                //Debug.Log ("Tag: " + tagName + " already exists");
            }
            return false;
        }
        /// <summary>
        /// Checks if the value exists in the property.
        /// </summary>
        /// <returns><c>true</c>, if exists was propertyed, <c>false</c> otherwise.</returns>
        /// <param name="property">Property.</param>
        /// <param name="start">Start.</param>
        /// <param name="end">End.</param>
        /// <param name="value">Value.</param>
        private static bool PropertyExists(SerializedProperty property, int start, int end, string value) {
            for (int i = start; i < end; i++) {
                SerializedProperty t = property.GetArrayElementAtIndex(i);
                if (t.stringValue.Equals(value)) {
                    return true;
                }
            }
            return false;
        }
    }
}