using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Mobge.Telemetry;
using System;

namespace Mobge.NewCargo {

    [CustomEditor(typeof(AnalyticsReferences), true)]
    public class EAnalyticsReferences : Editor {


        AnalyticsReferences _go;

        protected void OnEnable() {
            _go = target as AnalyticsReferences;
        }

        public override void OnInspectorGUI() {
            base.OnInspectorGUI();
            if (_go == null) {
                return;
            }


            if (GUILayout.Button("reload")) {
                AssetDatabase.ForceReserializeAssets(GetAssets());
            }

            if (GUI.changed) {
                EditorExtensions.SetDirty(_go);
            }

        }

        private IEnumerable<string> GetAssets() {
            yield return AssetDatabase.GetAssetPath(_go);

        }
    }
}