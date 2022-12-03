using System;
using System.Collections.Generic;
using Mobge.Core;
using UnityEngine;
using UnityEngine.AI;

namespace Mobge.NavMesh
{
    public class NavMeshComponent : ComponentDefinition<NavMeshComponent.Data>
    {
        [Serializable]
        public class Data : BaseComponent
        {

            private static readonly List<NavMeshBuildMarkup> s_tempBuildMarkups = new List<NavMeshBuildMarkup>();
            private static readonly List<NavMeshBuildSource> s_tempBuildSources = new List<NavMeshBuildSource>();

            static readonly List<Data> s_NavMeshSurfaces = new List<Data>();

            [LayerMask] public int objectLayerMask = 1;

            private NavMeshDataInstance _navMeshDataInstance;
            private NavMeshData _navMeshData;
            private LevelPlayer _player;

            public override void Start(in InitArgs initData)
            {

                _player = initData.player;
                //_components = initData.components;

                // invoke example output
                //string value = "example argument value";
                //Connections.InvokeSimple(this, 0, value, _components);
                _player.RoutineManager.DoAction(DelayedStart);
            }

            private void DelayedStart(bool complete, object data)
            {
                BuildNavMesh();
                AddData();
            }


            public override void End()
            {
                base.End();
                RemoveData();
            }

            static Vector3 Abs(Vector3 v)
            {
                return new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
            }
            static Bounds GetWorldBounds(Matrix4x4 mat, Bounds bounds)
            {
                var absAxisX = Abs(mat.MultiplyVector(Vector3.right));
                var absAxisY = Abs(mat.MultiplyVector(Vector3.up));
                var absAxisZ = Abs(mat.MultiplyVector(Vector3.forward));
                var worldPosition = mat.MultiplyPoint(bounds.center);
                var worldSize = absAxisX * bounds.size.x + absAxisY * bounds.size.y + absAxisZ * bounds.size.z;
                return new Bounds(worldPosition, worldSize);
            }


            Bounds CalculateBounds(List<NavMeshBuildSource> sources)
            {

                Matrix4x4 worldToLocal = Matrix4x4.TRS(_player.transform.position, _player.transform.rotation, Vector3.one);
                worldToLocal = worldToLocal.inverse;

                var result = new Bounds();
                for (int i = 0; i < sources.Count; i++)
                {
                    var src = sources[i];
                    switch (src.shape)
                    {
                        case NavMeshBuildSourceShape.Mesh:
                            {
                                var m = src.sourceObject as Mesh;
                                result.Encapsulate(GetWorldBounds(worldToLocal * src.transform, m.bounds));
                                break;
                            }
                        case NavMeshBuildSourceShape.Terrain:
                            {
                                // Terrain pivot is lower/left corner - shift bounds accordingly
                                var t = src.sourceObject as TerrainData;
                                result.Encapsulate(GetWorldBounds(worldToLocal * src.transform, new Bounds(0.5f * t.size, t.size)));
                                break;
                            }
                        default:
                        case NavMeshBuildSourceShape.Box:
                        case NavMeshBuildSourceShape.Sphere:
                        case NavMeshBuildSourceShape.Capsule:
                        case NavMeshBuildSourceShape.ModifierBox:
                            result.Encapsulate(GetWorldBounds(worldToLocal * src.transform, new Bounds(Vector3.zero, src.size)));
                            break;
                    }
                }
                result.Expand(0.1f);
                return result;
            }
            void BuildNavMesh() {
                _navMeshData = new NavMeshData(0);
                s_tempBuildSources.Clear();
                NavMeshBuilder.CollectSources(_player.transform, objectLayerMask, NavMeshCollectGeometry.PhysicsColliders, 0, s_tempBuildMarkups, s_tempBuildSources);

                for (int i = 0; i < s_tempBuildSources.Count; i++) {
                    var s = s_tempBuildSources[i];
                    if (s.component.TryGetComponent<NavMeshArea>(out var behaviour)) {
                        s.area = behaviour.areaType;
                    }

                    s_tempBuildSources[i] = s;
                }

                for (int i = 0; i < s_tempBuildSources.Count; i++) {
                    var tb = s_tempBuildSources[i];
                    if (tb.component is Collider c) {
                        if (c.isTrigger) {
                            s_tempBuildSources.RemoveAt(i);
                            i--;
                        }
                    }
                }

                var bounds = CalculateBounds(s_tempBuildSources);

                NavMeshBuilder.UpdateNavMeshData(_navMeshData, UnityEngine.AI.NavMesh.GetSettingsByID(0), s_tempBuildSources, bounds);

                s_tempBuildSources.Clear();
            }

            public void AddData()
            {
                if (_navMeshDataInstance.valid)
                    return;

                if (_navMeshData != null)
                {
                    _navMeshDataInstance =  UnityEngine.AI.NavMesh.AddNavMeshData(_navMeshData, Vector3.zero, Quaternion.identity);
                    _navMeshDataInstance.owner = _player;
                }

            }

            public void RemoveData()
            {
                if (_navMeshDataInstance.valid)
                {
                    _navMeshDataInstance.Remove();
                    _navMeshDataInstance = new NavMeshDataInstance();
                }
            }

            //public override object HandleInput(ILogicComponent sender, int index, object input) {
            //    switch (index) {
            //        case 0:
            //            //on example input fired
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

    }
}
