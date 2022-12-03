using Mobge.Platformer;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.Core.Components {
    public abstract class ACameraComponentData : BaseComponent, IRotationOwner, CameraManager.ICamera {
        #region camera utils
        public static Quaternion FlattenRotation(in Quaternion q) {
            var f = q * Vector3.forward;
            f.y = 0;
            return Quaternion.LookRotation(f, Vector3.up);
        }


        public static Quaternion ApproachRotation(Quaternion target, Quaternion current, float amount, Quaternion prev, float dt) {
            ToComponents(target, out var fTarget, out var uTarget);
            ToComponents(current, out var fCurrent, out var uCurrent);
            ToComponents(prev, out var fPrev, out var uPrev);
            Vector3 a3 = new Vector3(amount, amount, amount);
            var forward = Side2DCamera.CameraApproach(fTarget, fCurrent, a3, fPrev, dt);
            var up = Side2DCamera.CameraApproach(uTarget, uCurrent, a3, uPrev, dt);
            return Quaternion.LookRotation(forward, up);
        }
        private static void ToComponents(Quaternion quaternion, out Vector3 forward, out Vector3 up) {
            forward = quaternion * Vector3.forward;
            up = quaternion * Vector3.up;
        }

        #endregion

        public Quaternion rotation = Quaternion.identity;
        public int priority;
        public CameraManager.CameraData data;
        [SerializeField, HideInInspector] private LogicConnections connections;


        public LevelPlayer LevelPlayer { get; private set; }
        private bool _activated;
        protected Dictionary<int, BaseComponent> Components { get; private set; }


        Quaternion IRotationOwner.Rotation { get => rotation; set => rotation = value; }
        public override LogicConnections Connections { get => connections; set => connections = value; }
        int CameraManager.ICamera.Priority => priority;
        CameraManager.CameraData CameraManager.ICamera.Data => this.data;



        public bool Active {
            get => _activated;
            set {
                if (_activated != value) {
                    _activated = value;
                    if (_activated) {
                        CameraManager.Get(LevelPlayer).Activate(this);
                    }
                    else {
                        CameraManager.Get(LevelPlayer).Deactivate(this);
                    }
                }
            }
        }

        public abstract Camera CameraToUpdate { get; set; }

        public override void Start(in InitArgs initData) {
            Components = initData.components;
            LevelPlayer = initData.player;

        }

        public abstract void Activated();
        public abstract void Deactivated();
        public abstract void UpdateCamera();


    }
}