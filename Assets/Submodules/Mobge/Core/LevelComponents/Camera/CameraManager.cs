using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.Core.Components {
    public class CameraManager {
        public const string c_tag = "mgCmMng";
        private AutoIndexedMap<ICamera> _activeDatas;
        private int _activeId;

        public static CameraManager Get(LevelPlayer player) {
            if (!player.TryGetExtra(c_tag, out CameraManager m)) {
                m = new CameraManager();
                player.SetExtra(c_tag, m);
                player.RoutineManager.DoRoutine(m.CameraUpdate);
            }
            return m;
        }


        public CameraManager() {
            _activeDatas = new AutoIndexedMap<ICamera>();
            _activeId = -1;
        }
        public ICamera ActiveCamera {
            get {
                if(_activeId  < 0) {
                    return null;
                }
                return _activeDatas[_activeId];
            }
        }
        public CameraData ActiveData {
            get {
                if (_activeId < 0) {
                    return null;
                }
                return _activeDatas[_activeId].Data;
            }
        }

        public int ActivePriority {
            get {
                if (_activeId < 0) {
                    return -1;
                }
                return _activeDatas[_activeId].Priority;
            }
        }


        private void CameraUpdate(float progress, object data) {
            if (_activeId >= 0) {
                var d = _activeDatas[_activeId];
                d.UpdateCamera();
            }

        }


        public void Activate(ICamera cam) {
            var ap = ActivePriority;
            int newId = _activeDatas.AddElement(cam);
            if (cam.Priority > ap) {
                if (ap >= 0) {
                    _activeDatas[_activeId].Deactivated();
                }
                _activeId = newId;
                _activeDatas[_activeId].Activated();
            }
        }

        public bool Deactivate(ICamera data) {
            int maxIndex = -1;
            int maxPriority = -1;
            int indexToRemove = -1;
            var e = _activeDatas.GetPairEnumerator();
            while (e.MoveNext()) {
                var c = e.Current;
                if (c.Value == data) {
                    indexToRemove = c.Key;
                }
                else {
                    if (c.Value.Priority > maxPriority) {
                        maxPriority = c.Value.Priority;
                        maxIndex = c.Key;
                    }
                }
            }
            if (indexToRemove >= 0) {
                var old = _activeDatas[indexToRemove];
                _activeDatas.RemoveElement(indexToRemove);
                if (indexToRemove == _activeId) {
                    old.Deactivated();
                    _activeId = maxIndex;
                    if (_activeId >= 0) {
                        _activeDatas[_activeId].Activated();

                    }
                }
                return true;
            }
            return false;
        }

        private struct DataRef {
            public CameraData data;
            public int priority;
        }




        public interface ICamera {
            int Priority { get; }
            CameraData Data { get; }
            Camera CameraToUpdate { get; }
            void Activated();
            void Deactivated();
            void UpdateCamera();

        }


        [Serializable]
        public class CameraData {
            public Vector3 moveForce = new Vector3(10, 10, 10);
            public float angularMoveForce = 1;
            public Pose offset;
        }
    }
}