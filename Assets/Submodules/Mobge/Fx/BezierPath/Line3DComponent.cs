using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mobge.Core;

namespace Mobge {
    public class Line3DComponent : ComponentDefinition<Line3DComponent.Data>
    {
        const string c_lineData = "l3ddt";
        private class Line3DShared {
            public Rigidbody rigidbody;
            public static Line3DShared GetForPlayer (LevelPlayer player){
                if(player.TryGetExtra(c_lineData, out Line3DShared data)) {
                    return data;
                }
                data = new Line3DShared();
                data.rigidbody = new GameObject("line rb").AddComponent<Rigidbody>();
                data.rigidbody.isKinematic = true;
                data.rigidbody.transform.SetParent(player.transform, false);
                player.SetExtra(c_lineData, data);
                return data;
            }
        }

        [Serializable]
        public class Data : BaseComponent, IRotationOwner
        {
            public Material material;
            public BezierPath3D path;
            public Mesh referenceMesh;
            public Vector3 meshScale = Vector3.one;
            public bool hasCollider;
            [HideInInspector, SerializeField]
            Quaternion _rotation;
            
			public Vector3 scale = Vector3.one;



            public Quaternion Rotation { get => _rotation; set => _rotation = value; }
            public override void Start(in InitArgs initData) {
                var ld = Line3DShared.GetForPlayer(initData.player);
                var line = CreateLine(ld.rigidbody.transform);
                var ltr = line.transform;
                ltr.localPosition = position;
                ltr.localRotation = _rotation;
            }

            public Line3D CreateLine(Transform parent) {
                var go = new GameObject().AddComponent<Line3D>();
                go.transform.SetParent(parent, false);
                UpdateVisuals(go);
                if (hasCollider) {
                    var mc = go.gameObject.AddComponent<MeshCollider>();
                    mc.sharedMesh = go.MeshFilter.sharedMesh;
                }
                return go;
            }
            public void UpdateVisuals(Line3D line, bool forced = false) {
                if (line != null) {
                    line.Mesh = referenceMesh;
                    line.Renderer.sharedMaterial = material;
                    line.path = path;
                    line.MeshScale = meshScale;
                    line.transform.localScale = scale;
                    if (forced) {

                        line.SetDirty();
                    }
                    line.ReconstructImmediate();

                }
            }
            public override object HandleInput(ILogicComponent sender, int index, object input) {
                switch (index) {
                    case 1024:
                    default:
                        return this;
                }
            }

#if UNITY_EDITOR
            public override void EditorInputs(List<LogicSlot> slots) {
                slots.Add(new LogicSlot("this", 1024, null, typeof(Data)));
            }
#endif
        }
    }
}