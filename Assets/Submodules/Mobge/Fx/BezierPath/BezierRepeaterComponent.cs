using System;
using System.Collections.Generic;
using Mobge.Core;
using UnityEngine;

namespace Mobge {
    public class BezierRepeaterComponent : ComponentDefinition<BezierRepeaterComponent.Data> {
        [Serializable]
        public class Data : BaseComponent, IRotationOwner {

            public Quaternion rotation = Quaternion.identity;



            public Repeater repeater = new Repeater() {
                sampleStep = new Step() { type = StepType.Percentage, value = 0.1f },
                stepPerSpawn = 1,
            };


            Quaternion IRotationOwner.Rotation { get => rotation; set => rotation = value; }



            //public override LogicConnections Connections { get => connections; set => connections = value; }
            //[SerializeField] [HideInInspector] private LogicConnections connections;
            //private Dictionary<int, BaseComponent> _components;

            //private LevelPlayer _player;

            public override void Start(in InitArgs initData) {
                //_player = initData.player;
                //_components = initData.components;
                
                //string value = "example argument value";
                //Connections.InvokeSimple(this, 0, value, _components);
            }


            public PoseEnumerator GetEnumerator() {
                return new PoseEnumerator(repeater, new Pose(position, rotation));
            }
            //public override object HandleInput(ILogicComponent sender, int index, object input) {
            //    switch (index) {
            //        case 0:
            //            return "example output";
            //    }
            //    return null;
            //}
#if UNITY_EDITOR
            //public override void EditorInputs(List<LogicSlot> slots) {
            //    slots.Add(new LogicSlot("example input", 0));
            //}
            //public override void EditorOutputs(List<LogicSlot> slots) {
            //    slots.Add(new LogicSlot("example output", 0));
            //}
#endif
        }
        public enum StepType {
            Percentage = 0,
            SmallAccurate = 1,

        }
        [Serializable]
        public struct Repeater {
            public Component[] prefabReferences;
            public Step sampleStep;

            public int stepPerSpawn;
            public BezierPath3D path;

            public void EnsurePath() {
                if (path == null || path.Points == null) {
                    path = new BezierPath3D();
                }
            }

            public Component GetRandomReference() {
                return prefabReferences[UnityEngine.Random.Range(0, prefabReferences.Length)];
            }

            public bool IsValid() {
                if (prefabReferences == null || prefabReferences.Length == 0) {
                    return false;
                }
                if (path == null || path.Points.Count < 2) {
                    return false;
                }
                if (stepPerSpawn <= 0 || sampleStep.value < 0.00001f) {
                    return false;
                }
                for (int i = 0; i < prefabReferences.Length; i++) {
                    var pr = prefabReferences[i];
                    if (pr == null) {
                        return false;
                    }
                }
                return true;
            }
        }
        [Serializable]
        public struct Step {
            public StepType type;
            public float value;
        }
        public struct PoseEnumerator {
            private Pose _parent;
            private Repeater _data;
            private BezierPath3D.SegmentEnumerator _enumerator;
            private Pose _current;

            public PoseEnumerator(Repeater data, Pose parent) {
                _parent = parent;
                _data = data;
                _current = default;
                _enumerator = data.path.GetEnumerator(1);
            }

            public bool MoveNext() {
                switch (_data.sampleStep.type) {
                    default:
                    case StepType.Percentage:
                        for(int i = 0; i < _data.stepPerSpawn; i++) {
                            if (!_enumerator.MoveForwardByPercent(_data.sampleStep.value)) {
                                return false;
                            }
                        }
                        break;
                    case StepType.SmallAccurate:
                        for (int i = 0; i < _data.stepPerSpawn; i++) {
                            if (!_enumerator.MoveBezierBySmallAmount(_data.sampleStep.value)) {
                                return false;
                            }
                        }
                        break;
                }
                var dir = _enumerator.CurrentDirection;
                var normal = _enumerator.CurrentNormal;
                _current = new Pose(_enumerator.CurrentPoint, Quaternion.LookRotation(dir, normal));
                return true;
            }

            public Pose Current {
                get {
                    return _current;
                }
            }
            public Pose WorldSpaceCurrent {
                get {
                    return _current.GetTransformedBy(new Pose(_parent.position, _parent.rotation));
                }
            }
        }
    }
}
            