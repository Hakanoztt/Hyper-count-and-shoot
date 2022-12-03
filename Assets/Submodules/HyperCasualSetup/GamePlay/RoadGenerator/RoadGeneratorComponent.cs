using System;
using System.Collections.Generic;
using Mobge.Core;
using Mobge.Core.Components;
using UnityEngine;

namespace Mobge.HyperCasualSetup.RoadGenerator {
    public class RoadGeneratorComponent : ComponentDefinition<RoadGeneratorComponent.Data> {
        private static BezierPath3D s_tempPath = new BezierPath3D();
        [Serializable]
        public class Data : BaseComponent , IRotationOwner, IParent {

            private static Quaternion s_backRotation = Quaternion.LookRotation(Vector3.back, Vector3.up);

            public ComponentRef[] prefabReferences;

            public Quaternion rotation = Quaternion.identity;

            [SerializeField] public Item[] items;

            public float defaultCubicness = 0.3f;


            private bool _started;
            private Transform _fakeRoot;

            //public override LogicConnections Connections { get => connections; set => connections = value; }
            //[SerializeField] [HideInInspector] private LogicConnections connections;
            //private Dictionary<int, BaseComponent> _components;

            //private LevelPlayer _player;

            Quaternion IRotationOwner.Rotation { get => rotation; set => rotation = value; }

            Transform IParent.Transform => _fakeRoot;

            public override void Start(in InitArgs initData) {
                EnsureRoad(initData.player);
                for (int i = 0; i < items.Length; i++) {
                    if (items[i].instance is IComponentExtension c) {
                        c.Start(initData);
                    }
                }
            }


            public bool IsValid() {
                if(prefabReferences == null || items == null) {
                    return false;
                }
                int max = 0;
                for(int i = 0; i < items.Length; i++) {
                    var it = items[i];
                    var id = it.id;
                    //if(it.scale.sqrMagnitude <= 0.0000001f) {
                    //    it.scale = Vector3.one;
                    //    items[i] = it;
                    //}
                    if (id < 0) {
                        return false;
                    }
                    if(id > max) {
                        max = id;
                    }
                }
                if(prefabReferences.Length <= max) {
                    return false;
                }
                for(int i = 0; i < prefabReferences.Length; i++) {
                    var pr = prefabReferences[i];
                    if(pr.res == null) {
                        return false;
                    }
                }
                return true;
            }

            public void EnsureRoad(LevelPlayer player) {
                if (_started) {
                    return;
                }
                _fakeRoot = new GameObject("road temp").transform;
                _fakeRoot.SetParent(player.transform, false);
                _fakeRoot.localPosition = position;
                _fakeRoot.localRotation = rotation;
                Transform parent = player.transform;
                _started = true;
                InitReferences();
                var pose = new Pose(this.position, this.rotation);
                for(int i = 0; i < this.items.Length;i++) {
                    var item = this.items[i];
                    var pref = prefabReferences[item.id];
                    var piece = Instantiate(pref.res, parent, false);
                    item.instance = piece;
                    items[i] = item;
                    //piece.transform.localScale = Vector3.Scale(piece.transform.localScale, item.scale);
                    PlacePiece(piece, pref.StartPose, pref.EndPose, ref pose);
                    if (item.flipZ) {
                        FlipItem(piece.transform);
                    }
                }
            }
            public static void FlipItem(Transform tr) {
                var scl = tr.localScale;
                scl.z = -scl.z;
                tr.localScale = scl;
            }

            public bool InitReferences() {
                for (int i = 0; i < prefabReferences.Length; i++) {
                    if(!Init(ref prefabReferences[i])) {
                        return false;
                    }
                }
                return true;
            }

            bool Init(ref ComponentRef cRef) {
                return cRef.Init(defaultCubicness);
            }
            public void PlacePiece(Component piece, in Pose start, in Pose end, ref Pose nextPose) {

                var ptr = piece.transform;
                var pose = IteratePose(ptr.localScale, start, end, ref nextPose);
                ptr.localPosition = pose.position;
                ptr.localRotation = pose.rotation;
            }
            public static Quaternion Reverse(Quaternion q) {
                var up = q * Vector3.up;
                var forward = q * Vector3.forward;
                return Quaternion.LookRotation(-forward, up);
            }
            public Pose IteratePose(Vector3 componentScale, in Pose start, in Pose end, ref Pose nextPose) {
                //Debug.Log(start + " - " + end);
                var scale = componentScale;// ptr.localScale;
                Vector3 offset = -start.position;
                offset.Scale(scale);
                var rotation = nextPose.rotation * Quaternion.Inverse(Reverse(start.rotation));
                var worldOffset = rotation * offset;
                var position = nextPose.position + worldOffset;
                var worldEndOffset = end.position;
                worldEndOffset.Scale(scale);
                worldEndOffset = rotation * worldEndOffset;
                nextPose.position = position + worldEndOffset;
                nextPose.rotation = rotation * end.rotation;

                return new Pose(position, rotation);
            }

            public override object HandleInput(ILogicComponent sender, int index, object input) {
                switch (index) {
                    case 0:
                        return this;
                    case 1:
                        return new RoadTracker(this);
                }
                return null;
            }
            public RoadTracker NewTracker() => new RoadTracker(this);
            public ItemEnumerator NewItemEnumerator() => new ItemEnumerator(new Pose(position, rotation), this);
#if UNITY_EDITOR
            public override void EditorInputs(List<LogicSlot> slots) {
                slots.Add(new LogicSlot("this", 0, null, typeof(Data)));
                slots.Add(new LogicSlot("tracker", 1, null, typeof(RoadTracker)));
            }
            //public override void EditorOutputs(List<LogicSlot> slots) {
            //    slots.Add(new LogicSlot("example output", 0));
            //}
#endif
        }


        public static void UpdateBezier(BezierPath3D bezier, in Pose startPoint, in Pose endPoint, float cubicness) {
            bezier.Points.ClearFast();
            float m = (endPoint.position - startPoint.position).magnitude * cubicness;
            bezier.Points.Add(new BezierPath3D.Point() {
                position = startPoint.position,
                rightControl = startPoint.position + (startPoint.rotation * Vector3.back) * m,
                normalOffset = -startPoint.rotation.eulerAngles.z,
            });
            bezier.Points.Add(new BezierPath3D.Point() {
                position = endPoint.position,
                leftControl = endPoint.position - (endPoint.rotation * Vector3.forward) * m,
                normalOffset = endPoint.rotation.eulerAngles.z
            });
            bezier.controlMode = BezierPath3D.ControlMode.Free;
            bezier.normalAlgorithm = BezierPath3D.NormalAlgorithmType.TangentAlgorithm;
        }
        public static Pose SampleBezier(Pose start, Pose end, float cubicness, float percentage) {
            UpdateBezier(s_tempPath, start, end, cubicness);
            var pos = s_tempPath.Evaluate(0, percentage);
            var normal = s_tempPath.EvaluateNormal(0, percentage);
            var direction = s_tempPath.EvaluateDerivative(0, percentage);
            return new Pose(pos, Quaternion.LookRotation(direction, normal));
        }
        public struct ItemEnumerator {
            private Data _data;
            private int _index;
            private Pose _currentPose;
            private Pose _lastItemPose;

            public ItemEnumerator(Pose startPose, Data data) {
                _data = data;
                _index = -1;
                _currentPose = startPose;
                _lastItemPose = Pose.identity;
            }

            public bool MoveNext() {
                _index++;
                if(_index >= _data.items.Length) {
                    return false;
                }
                var item = _data.items[_index];
                var @ref = _data.prefabReferences[item.id];
                _lastItemPose = _data.IteratePose(@ref.res.transform.localScale, @ref.StartPose, @ref.EndPose, ref _currentPose);
                return true;
            }
            public int CurrentIndex => _index;
            public Pose Current { get => _currentPose; set => _currentPose = value; }
            public Pose LastItemPose => _lastItemPose;

        }


        [Serializable]
        public struct ComponentRef {


            public Component res;
            private Pose _start, _end;
            public Pose StartPose => _start;
            public Pose EndPose => _end;

            private IRoadPiece _roadPiece;

            public Pose SampleFromPose(Data road, float percentage, Pose pose) {
                if (_roadPiece != null) {
                    return _roadPiece.SampleFromPose(percentage, pose, 0, 1);
                }
                else {
                    return SampleBezier(_start.GetTransformedBy(pose), _end.GetTransformedBy(pose), road.defaultCubicness, percentage);
                }
            }


            public bool Init(float defaultCubicness) {
                if (res != null) {
                    if (res is IRoadPiece piece) {
                        _roadPiece = piece;
                        _start = piece.GetLocalEndPoint(0);
                        _end = piece.GetLocalEndPoint(1);
                        return true;
                    }
                    else {
                        _roadPiece = null;
                        var mf = res.GetComponentInChildren<MeshFilter>();
                        if (mf) {
                            var m = mf.sharedMesh;
                            var b = m.bounds;
                            float center = b.center.z;
                            float hs = b.size.z * 0.5f;
                            _start = new Pose(new Vector3(0, 0, center - hs), Quaternion.LookRotation(-Vector3.forward));
                            _end = new Pose(new Vector3(0, 0, center + hs), Quaternion.LookRotation(Vector3.forward));

                            return true;
                        }
                        else {
                            return false;
                        }
                    }
                }
                return false;
            }
            public override string ToString() {
                return "" + res;
            }
        }
        [Serializable]
        public struct Item {
            public int id;
            public bool flipZ;
            [NonSerialized] public Component instance;
        }

        public interface IRoadPiece {
            int EndPointCount { get; }
            Pose GetLocalEndPoint(int index);
            void UpdateBezier(BezierPath3D path, int endpoint1, int endpoint2);
            Pose SampleFromPose(float percentage, Pose pose, int endpoint1, int endpoint2);
        }
    }
}
            