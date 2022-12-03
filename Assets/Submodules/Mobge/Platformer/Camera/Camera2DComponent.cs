using System;
using System.Collections;
using System.Collections.Generic;
using Mobge.Core;
using Mobge.Core.Components;
using UnityEngine;

namespace Mobge.Platformer {
    public class Camera2DComponent : ComponentDefinition<Camera2DComponent.Data>
    {
        private static Collider2DShape DefaultShape {
            get {
#if UNITY_EDITOR
                Collider2DShape shape = new Collider2DShape();
                shape.shape = Collider2DShape.Shape.Rectangle;
                shape.EnsureData();
                shape.Size = new Vector2(2,2);
                return shape;
#else
                return default(Collider2DShape);
#endif
            }
        }
        [Serializable]
        public class Data : BaseData
        {
            public Side2DCameraData data;
            public override void Start(in InitArgs initData) {
                base.Start(initData, data);
            }
        }

        public abstract class BaseData : BaseComponent, Side2DCamera.Modifier, Trigger2DListener, IChild
        {

            public int priority;
            [HideInInspector] public float xMin = float.NaN, xMax = float.NaN, yMin = float.NaN, yMax = float.NaN;
            [HideInInspector] public Collider2DShape shape = DefaultShape;
            [SerializeField] public bool _startDisabled;
            [HideInInspector, SerializeField] private ElementReference _parent = -1;
            
            
            private Collider2D _collider;
            protected Side2DCameraData _data;
            protected LevelPlayer _player;
            private int _cPriority;
            private bool _forcedMode;
            private bool _touchingTarget;
            private bool _active;
            private Side2DCamera Camera { get; set; }

            ElementReference IChild.Parent { get => _parent; set => _parent = value; }


            public void Start(in InitArgs initData, Side2DCameraData data) {
                _data = data;
                _cPriority = priority;
                _player = initData.player;
                Camera = ACameraController.FindOrCreateForLevel<Side2DCamera>(_player);
                var layer = initData.player.level.GameSetup.camera.layer;
                Camera.EnsureTrigger(layer);
                if (!float.IsNaN(xMin) || !float.IsNaN(xMax) || !float.IsNaN(yMin) || !float.IsNaN(yMax)) {
                    data.modifiers = new Side2DCamera.Modifier[] {
                        this,
                    };
                }
                var tr = new GameObject("camera").transform;
                tr.SetParent(initData.parentTr, false);
                tr.localPosition = position;
                tr.gameObject.layer = layer;
                _collider = shape.AddCollider(tr.gameObject);
                if (_collider) {
                    _collider.isTrigger = true;
                    _collider.enabled = !_startDisabled;
                }
                var cbs = tr.gameObject.AddComponent<Trigger2DCallbacks>();
                cbs.listener = this;
                data.centerData.CenterObject = tr;
            }
            public override void End() {
                if (Camera) {
                    Camera.RemoveData(this._data);
                }
            }

            protected virtual void Activate() {
                Camera.AddData(this._data, _cPriority);
                _active = true;
            }
            protected virtual void Deactivate() {

                Camera.RemoveData(this._data);
                _active = false;
            }
            void UpdateState() {
                bool state = _forcedMode || _touchingTarget;
                if (state != _active) {
                    if (state) {
                        Activate();
                    }
                    else {
                        Deactivate();
                    }
                }
            }
            void Side2DCamera.Modifier.UpdatePosition(in Side2DCamera.UpdateArgs args, ref Side2DCamera.UpdateData data) {
                Vector3 pos = data.newPos;
                var spos = WorldCenter;
                if (!float.IsNaN(xMin)) {
                    pos.x = Mathf.Max(pos.x, spos.x + xMin + args.extends.x);
                }
                if (!float.IsNaN(xMax)) {
                    pos.x = Mathf.Min(pos.x, spos.x + xMax - args.extends.x);
                }
                if (!float.IsNaN(yMin)) {
                    pos.y = Mathf.Max(pos.y, spos.y + yMin + args.extends.y);
                }
                if (!float.IsNaN(yMax)) {
                    pos.y = Mathf.Min(pos.y, spos.y + yMax - args.extends.y);
                }
                data.newPos = pos;
            }
            void Trigger2DListener.OnTriggerEnter2D(Trigger2DCallbacks sender, Collider2D collider) {
                if (!collider.isTrigger) {
                    _touchingTarget = true;
                    UpdateState();
                }
            }

            void Trigger2DListener.OnTriggerExit2D(Trigger2DCallbacks sender, Collider2D collider) {
                if (!collider.isTrigger) {
                    _touchingTarget = false;
                    UpdateState();
                }
            }
            private Vector3 WorldCenter {
                get {
                    return _data.centerData.CenterObject.position;
                }
            }
            public override object HandleInput(ILogicComponent sender, int index, object input) {
                switch (index) {
                    case 0:
                        if (_collider) {
                            _collider.enabled = true;
                        }
                        break;
                    case 1:
                        if (_collider) {
                            _collider.enabled = false;
                        }
                        _touchingTarget = false;
                        UpdateState();
                        break;
                    case 2:
                        _cPriority = (int)(float)input;
                        if (_active) {
                            Camera.AddData(this._data, _cPriority);
                        }
                        break;
                    case 3:
                        _forcedMode = true;
                        UpdateState();
                        break;
                    case 4:
                        _forcedMode = false;
                        UpdateState();
                        break;
                }
                return null;
            }

#if UNITY_EDITOR
            public override void EditorInputs(List<LogicSlot> slots) {
                slots.Add(new LogicSlot("enable", 0));
                slots.Add(new LogicSlot("disable", 1));
                slots.Add(new LogicSlot("set priority", 2, typeof(float)));
                slots.Add(new LogicSlot("enter forced mode", 3));
                slots.Add(new LogicSlot("exit forced mode", 4));
            }
#endif
        }
    }
}