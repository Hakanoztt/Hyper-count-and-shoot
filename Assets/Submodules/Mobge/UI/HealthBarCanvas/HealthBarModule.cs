using Mobge.Core;
using Mobge.Platformer.Character;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.UI {

    [Serializable]
    public struct HealthBarModule {

        [OwnComponent] public Renderer mainRenderer;

        public Vector3 offset;
        private Notifier _notifier;


        public void SetEnabled(LevelPlayer player, in Health health) {
            if (_notifier == null) {
                _notifier = mainRenderer.gameObject.AddComponent<Notifier>();
            }
            _notifier.SetEnabled(mainRenderer, health, HealthBarCanvas.GetManager(player));
            _notifier.offset = offset;
        }
        public void SetDisabled() {
            _notifier.SetDisabled();
        }
        public void UpdateHealth(in Health health) {
            _notifier.health = health;
        }

        public void OnDrawGizmos() {
            if (mainRenderer == null) {
                return;
            }
            var m = Gizmos.matrix;
            Gizmos.matrix = mainRenderer.transform.localToWorldMatrix;


            Gizmos.DrawLine(offset + new Vector3(0.2f, 0, 0), offset + new Vector3(-0.2f, 0, 0));
            Gizmos.DrawLine(offset + new Vector3(0, 0, 0.2f), offset + new Vector3(0, 0, -0.2f));


            Gizmos.matrix = m;
        }

        public class Notifier : MonoBehaviour {
            private HealthBarCanvas.Manager _canvas;
            public Vector3 offset;
            public Health health;
            private Transform _tr;
            public Vector3 WorldPoint => _tr.TransformPoint(offset);
            public bool Visible { get; private set; }
            public void SetEnabled(Renderer renderer, in Health health, HealthBarCanvas.Manager canvas) {
                _tr = renderer.transform;
                Visible = renderer.isVisible;
                this.health = health;
                if (_canvas == null) {
                    _canvas = canvas;
                    if (Visible) {
                        _canvas.Add(this);
                    }
                }
            }
            public void SetDisabled() {
                Visible = false;
                _canvas = null;
            }
            private void OnBecameVisible() {
                if (_canvas != null) {
                    Visible = true;
                    _canvas.Add(this);
                }
            }
            private void OnBecameInvisible() {
                Visible = false;
            }
        }
    }

}