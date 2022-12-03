using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Mobge {
    [SelectionBase]
    public class OnEditorSelectNotifier : MonoBehaviour {
        public Action onSelect;
        public object editableElement;
        public object editor;

        private void OnDrawGizmos() {
            //to be selected with unity own rect and transported to element editor
            var c = Gizmos.color;
            {
                Gizmos.color = Color.clear;
                Gizmos.DrawSphere(transform.position, float.Epsilon);
            }
            Gizmos.color = c;
        }
    }
}
