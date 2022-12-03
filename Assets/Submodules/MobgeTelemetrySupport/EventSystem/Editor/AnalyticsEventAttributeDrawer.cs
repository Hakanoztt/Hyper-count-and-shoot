using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static Mobge.UnityExtensions;


namespace Mobge.Telemetry {
    [CustomPropertyDrawer(typeof(AnalyticsEvent))]
    public class AnalyticsEventAttributeDrawer : BasePropertyDrawer {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {

            var nameP= property.FindPropertyRelative(AnalyticsEvent.NameFieldName);
            var collectionP = property.FindPropertyRelative(AnalyticsEvent.CollectionFieldName);
            EditorGUI.PropertyField(position, nameP, label);
            collectionP.objectReferenceValue = property.serializedObject.targetObject;
        }
        
    }
}