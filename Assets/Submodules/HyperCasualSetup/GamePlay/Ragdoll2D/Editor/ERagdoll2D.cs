using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Mobge.HyperCasualSetup.GamePlay
{
    [CustomEditor(typeof(Ragdoll2D))]
    public class ERagdoll2D : Editor
    {
        private Ragdoll2D _go;

        private void OnEnable() {
            _go = target as Ragdoll2D;
        }
        public override void OnInspectorGUI() {
            if (_go) {

                if (GUILayout.Button("Auto update bones")) {
                    //if (_go.bones != null && _go.bones.Length > 0) 
                        {
                        var rect = GUILayoutUtility.GetLastRect();
                        //if (EditorUtility.DisplayDialog("Warning", "Bones are already initialized. Parent information may be lost if you continue (Unflatten option may be used before initializing bones automatically to prevent data loss.).", "Continue", "Abort")) 
                            {
                            //bool flatten = true;
                            float density = 1;
                            Transform rootBone = null;
                            float boneWidth = 0.3f;
                            float boneMovementLimit = 20;
                            float drag = 0;
                            float angularDrag = 0;
                            bool forceUpdateCollider = false;
                            var popup = new EditorPopup((rects, pp) => {
                                var allTransforms = _go.GetComponentsInChildren<Transform>();
                                var rootIndex = EditorDrawer.Popup(rects, "root bone", allTransforms, System.Array.IndexOf(allTransforms, rootBone));
                                if (rootIndex >= 0 && rootIndex < allTransforms.Length) {
                                    rootBone = allTransforms[rootIndex];
                                }
                                else {
                                    rootBone = null;
                                }
                                // flatten = EditorGUI.Toggle(rects.NextRect(), "Flatten", flatten);
                                density = EditorGUI.FloatField(rects.NextRect(), "Density", density);
                                boneWidth = EditorGUI.FloatField(rects.NextRect(), "Bone width", boneWidth);
                                boneMovementLimit = EditorGUI.FloatField(rects.NextRect(), "Bone movement limit", boneMovementLimit);
                                drag = EditorGUI.FloatField(rects.NextRect(), "Drag", drag);
                                angularDrag = EditorGUI.FloatField(rects.NextRect(), "Angular Drag", angularDrag);
                                forceUpdateCollider = EditorGUI.Toggle(rects.NextRect(), "Force Update Colliders", forceUpdateCollider);
                                var en = GUI.enabled;
                                GUI.enabled = en && rootBone != null;
                                if (GUI.Button(rects.NextRect(), "initialize")) {
                                    var allBones = rootBone.GetComponentsInChildren<Transform>();
                                    _go.bones = new Ragdoll2D.Bone[allBones.Length];
                                    float[] lengths = new float[_go.bones.Length];
                                    for(int i = 0; i < _go.bones.Length; i++) {
                                        Ragdoll2D.Bone bone;
                                        bone.bone = allBones[i];
                                        bone.parent = System.Array.IndexOf(allBones, bone.bone.parent);
                                        
                                        bone.body = AddOrGet<Rigidbody2D>(bone.bone.gameObject);
                                        bone.body.drag = drag;
                                        bone.body.angularDrag = angularDrag;
                                        bone.body.useAutoMass = false;
                                        bone.body.mass = 1;
                                        bone.body.collisionDetectionMode = CollisionDetectionMode2D.Discrete;
                                        bone.body.interpolation = RigidbodyInterpolation2D.Interpolate;
                                        
                                        var cc = bone.bone.gameObject.GetComponent<CapsuleCollider2D>();
                                        
                                        if (forceUpdateCollider || cc == null) {
                                            if (cc == null) {
                                                cc = bone.bone.gameObject.AddComponent<CapsuleCollider2D>();
                                            }
                                            cc.direction = CapsuleDirection2D.Vertical;
                                            var size = new Vector2(boneWidth, boneWidth + CalculateLength(bone.bone, allBones));
                                            size /= bone.bone.lossyScale.x;
                                            cc.size = size;
                                            cc.offset = new Vector2(0, (size.y  - size.x) * 0.5f);
                                            
                                        }
                                        bone.collider = cc;
                                        bone.joint = null;
                                        _go.bones[i] = bone;
                                    }
                                    for(int i = 0; i < _go.bones.Length; i++)
                                    {
                                        var bone = _go.bones[i];
                                        if (bone.parent >= 0)
                                        {
                                            bone.joint = AddOrGet<HingeJoint2D>(bone.bone.gameObject);
                                            bone.joint.connectedBody = _go.bones[bone.parent].body;
                                            bone.joint.useLimits = false;
                                            bone.joint.autoConfigureConnectedAnchor = false;
                                            bone.joint.anchor = Vector3.zero;
                                            bone.joint.connectedAnchor =
                                                bone.joint.connectedBody.transform.InverseTransformPoint(bone.bone.TransformPoint(bone.joint.anchor));

                                            if (boneMovementLimit >= 0) {
                                                JointAngleLimits2D limits = new JointAngleLimits2D();
                                                var refAngle = bone.joint.connectedBody.rotation - bone.body.rotation;
                                                limits.min = refAngle - boneMovementLimit;
                                                limits.max = refAngle + boneMovementLimit;
                                                bone.joint.limits = limits;
                                                bone.joint.useLimits = true;
                                            }
                                        }
                                        else
                                        {
                                            var j = bone.bone.gameObject.GetComponent<HingeJoint2D>();
                                            if (j != null)
                                            {
                                                j.DestroySelf();
                                            }
                                        }
                                        _go.bones[i] = bone;
                                    }
                                }
                                GUI.enabled = en;
                            });
                            popup.Show(rect, new Vector2(300, 80));
                        }
                    }
                }
                //if(_go.bones != null && _go.bones.Length > 0) {
                //    if (GUILayout.Button("Unflatten bone hierarchy")) {
                //        Unflatten();
                //    }
                //    if (GUILayout.Button("Unflatten bone hierarchy and delete data")) {
                //        Unflatten();
                //        _go.bones = new Ragdoll2D.Bone[0];
                //    }
                //}
            }
            base.OnInspectorGUI();
            if (GUI.changed) {
                SetDirty();
            }
        }
        float CalculateLength(Transform bone, Transform[] bones) {
            var up = bone.up;
            float length = 0;
            for(int i = 0; i  < _go.bones.Length; i++) {
                var c = bones[i];
                if (c.parent == bone) {
                    length = Mathf.Max(length, Vector2.Dot(up, c.position - bone.position));
                }
            }
            return length;
        }
        private static T AddOrGet<T>(GameObject obj) where T : Component{
            var t = obj.GetComponent<T>();
            if (t) {
                return t;
            }
            return obj.AddComponent<T>();
        }
        new void SetDirty() {
            EditorExtensions.SetDirty(_go);
            EditorExtensions.SetDirty(_go.gameObject);
        }
        //private void Flatten() {
        //    var bones = _go.bones;
        //    for (int i = 0; i < bones.Length; i++) {
        //        var bone = bones[i];
        //        if (bone.bone) {
        //            bone.bone.parent = _go.transform;
        //        }
        //    }
        //}
        //private void Unflatten() {
        //    var bones = _go.bones;
        //    for(int i = 0; i < bones.Length; i++) {
        //        var bone = bones[i];
        //        if(bone.bone && bone.parent) {
        //            bone.bone.parent = bone.parent;
        //        }
        //    }
        //}
    }
}