using System;

using UnityEngine;
using UnityEngine.Events;

using UnityEditor;

namespace Mobge.HyperCasualSetup.MeshDeformer {

    public class DeformableRootObject : MonoBehaviour {

        [Tooltip("")]
        public float globalBreakableDamageFalloff = 1;

        [Layer] public int[] ignoreDamageFrom;

        public Deformable[] deformables;
        public Breakable[] breakables;
        public BreakableJoint[] breakableJoints;

        private void Start() {
            for(int i = 0; i < deformables.Length; i++) {
                ref Deformable d = ref deformables[i];
                d.Create(this);
                if (d.GameObject.GetComponent<MeshFilter>() == null) continue;
                d.Init();
                DeformableCollisionListener cl = d.GameObject.AddComponent<DeformableCollisionListener>();
                cl.root = this;
                cl.index = i;
                cl.type = Type.Deformable;
            }

            for (int i = 0; i < breakables.Length; i++) {
                ref Breakable b = ref breakables[i];
                b.Create(this);
                if (b.GameObject.GetComponent<MeshFilter>() == null) continue;
                b.Init();
                DeformableCollisionListener cl = b.GameObject.AddComponent<DeformableCollisionListener>();
                cl.root = this;
                cl.index = i;
                cl.type = Type.Breakable;
            }

            for (int i = 0; i < breakableJoints.Length; i++) {
                ref BreakableJoint bj = ref breakableJoints[i];
                bj.Create(this);
                if (bj.GameObject.GetComponent<Rigidbody>() == null) continue;
                bj.Init();
                DeformableCollisionListener cl = bj.GameObject.AddComponent<DeformableCollisionListener>();
                cl.root = this;
                cl.index = i;
                cl.type = Type.BreakableJoint;
            }

            Collider[] childrenColliders = GetComponentsInChildren<Collider>();
            foreach (Collider childCollider in childrenColliders) {
                Collider myCollider = GetComponent<Collider>();
                if (myCollider != childCollider) {
                    Physics.IgnoreCollision(myCollider, childCollider);
                }
            }
        }

        [Serializable]
        public struct Deformable {

            [Tooltip("Min distance from an arbitrary vertex to the point of impulse if the vertex will be displaced. Recommended: 0.5")]
            [SerializeField]
            private float minDeformRadius;

            [Tooltip("Max distance a vertice can get displaced from its original position. Recommended: 0.05")]
            [SerializeField]
            private float maxDeformDistance;

            [Tooltip("Factor applied when calculating the distance vector of impact. Recommended: 1")]
            [SerializeField]
            private float damageFalloff;

            [Tooltip("Factor applied to deformation vector. Recommended: 1")]
            [SerializeField]
            private float damageMultiplier;

            [Tooltip("Impulse of magnitude lower than this value is ignored.")]
            [SerializeField]
            private float minDamageTreshold;

            [Tooltip("If selected, the magnitude of impulses applied to the object will be logged to the console.")]
            [SerializeField]
            private bool logImpulseMagnitude;

            [SerializeField]
            private MeshFilter meshFilter;

            [SerializeField]
            private UnityEvent<Deformable, float> onDamageCallbacks;

            private GameObject gameObject;
            public GameObject GameObject { get { return gameObject; } }

            private DeformableRootObject root;
            private Transform transform;
            private Collider collider;
            private Vector3[] undeformedVertices;
            private Vector3[] meshVertices;

            public void Create(DeformableRootObject root) {
                this.root = root;
                gameObject = meshFilter.gameObject;
            }

            public void Init() {
                collider = gameObject.GetComponent<Collider>();
                if (collider == null) {
                    collider = gameObject.AddComponent<MeshCollider>();

                    MeshCollider meshCollider = collider as MeshCollider;
                    meshCollider.convex = true;
                    meshCollider.sharedMesh = meshFilter.sharedMesh;
                }

                if (gameObject.GetComponent<Rigidbody>() == null) {
                    gameObject.AddComponent<Rigidbody>();
                }

                if (!gameObject.TryGetComponent<DeformableRootObject>(out _)) {
                    if (gameObject.GetComponent<Joint>() == null) {
                        Joint j = gameObject.AddComponent<FixedJoint>();
                        Rigidbody parentBody = gameObject.transform.parent.GetComponent<Rigidbody>();
                        if (parentBody == null) {
                            parentBody = gameObject.transform.parent.gameObject.AddComponent<Rigidbody>();
                        }
                        j.connectedBody = parentBody;
                        j.enableCollision = false;
                    }
                }
                transform = gameObject.transform;
                undeformedVertices = meshFilter.mesh.vertices;
                meshVertices = meshFilter.mesh.vertices;
            }

            public void HandleCollision(Collision collision) {
                float impulseMagnitude = collision.impulse.magnitude;

                if(logImpulseMagnitude) {
                    Debug.Log(impulseMagnitude);
			    }

                if (impulseMagnitude < minDamageTreshold) {
                    return;
                }

                foreach (ContactPoint contact in collision.contacts) {
                    for (int i = 0; i < meshVertices.Length; i++) {
                        Vector3 vertex = meshVertices[i];
                        Vector3 point = transform.InverseTransformPoint(contact.point);
                        float collisionDst = Vector3.Distance(vertex, point);
                        float originalDst = Vector3.Distance(undeformedVertices[i], vertex);

                        // If within collision radius and within max deform
                        if (collisionDst < minDeformRadius && originalDst < maxDeformDistance) {
                            float falloff = 1 - (collisionDst / minDeformRadius) * damageFalloff;

                            float xDeform = point.x * falloff;
                            float yDeform = point.y * falloff;
                            float zDeform = point.z * falloff;

                            xDeform = Mathf.Clamp(xDeform, -maxDeformDistance, maxDeformDistance);
                            yDeform = Mathf.Clamp(yDeform, -maxDeformDistance, maxDeformDistance);
                            zDeform = Mathf.Clamp(zDeform, -maxDeformDistance, maxDeformDistance);

                            Vector3 deform = new Vector3(xDeform, yDeform, zDeform);
                            meshVertices[i] -= deform * damageMultiplier;
                        }
                    }
                }

                meshFilter.mesh.vertices = meshVertices;

                MeshCollider meshCollider = collider as MeshCollider;
                if (meshCollider != null && !meshCollider.convex) {
                    meshCollider.sharedMesh = meshFilter.mesh;
                }

                onDamageCallbacks.Invoke(this, impulseMagnitude);
            }
        }

        [Serializable]
        public struct Breakable {

            [Tooltip("Impulse of magnitude lower than this value is will not break the mesh. Recommended: 1")]
            [SerializeField]
            private float breakTreshold;
            public float BreakTreshold { get { return breakTreshold; } }

            [Tooltip("If selected, the magnitude of impulses applied to the object will be logged to the console.")]
            [SerializeField]
            private bool logImpulseMagnitude;

            [SerializeField]
            private MeshFilter meshFilter;
            [SerializeField]
            private MeshFilter[] pieces;
            [SerializeField]
            private PhysicMaterial piecesPhysicMaterial;

            [SerializeField]
            private UnityEvent<Breakable, float> onDamageCallbacks;

            private GameObject gameObject;
            public GameObject GameObject { get { return gameObject; } }

            private DeformableRootObject root;
            private Transform transform;
            private MeshRenderer renderer;
            private Collider collider;

            private bool broken;
            public bool Broken { get { return broken; } }

            public void Create(DeformableRootObject root) {
                this.root = root;
                gameObject = meshFilter.gameObject;
            }

            public void Init() {
                transform = gameObject.transform;
                renderer = gameObject.GetComponent<MeshRenderer>();

                collider = gameObject.GetComponent<Collider>();
                if (collider == null) {
                    collider = gameObject.AddComponent<MeshCollider>();
                    ((MeshCollider)collider).convex = true;
                    ((MeshCollider)collider).sharedMesh = meshFilter.sharedMesh;
			    }

                if (gameObject.GetComponent<Rigidbody>() == null) {
                    gameObject.AddComponent<Rigidbody>();
                }

                if (gameObject.GetComponent<Joint>() == null) {
                    Joint j = gameObject.AddComponent<FixedJoint>();
                    Rigidbody parentBody = gameObject.transform.parent.GetComponent<Rigidbody>();
                    if (parentBody == null) {
                        parentBody = gameObject.transform.parent.gameObject.AddComponent<Rigidbody>();
                    }
                    j.connectedBody = parentBody;
                    j.enableCollision = false;
                }


                foreach (MeshFilter piece in pieces) {
                    GameObject pieceObject = piece.gameObject;

                    if (pieceObject.GetComponent<MeshCollider>() == null) {
                        MeshCollider c = pieceObject.AddComponent<MeshCollider>();
                        c.convex = true;
                        c.sharedMesh = piece.sharedMesh;
                        c.material = piecesPhysicMaterial;
                    }

                    if (pieceObject.GetComponent<Rigidbody>() == null) {
                        pieceObject.AddComponent<Rigidbody>();
                    }

                    pieceObject.SetActive(false);
                }

                broken = false;
            }

            public void HandleCollision(Collision collision) {
                if (broken) return;

                float avgCollisionDst = 0;
                foreach (ContactPoint contact in collision.contacts) {
                    Vector3 point = transform.InverseTransformPoint(contact.point);
                    avgCollisionDst += Vector3.Distance(transform.InverseTransformPoint(transform.position), point);
			    }
                avgCollisionDst /= collision.contacts.Length;

                float impulseMagnitude = collision.impulse.magnitude / (avgCollisionDst *  avgCollisionDst * root.globalBreakableDamageFalloff + 1);

                if (logImpulseMagnitude) Debug.Log(impulseMagnitude);

                if (impulseMagnitude >= breakTreshold) {
                    if (logImpulseMagnitude) Debug.Log("BREAK [" + transform.name + "]");

                    renderer.enabled = false;
                    collider.enabled = false;
                    foreach (MeshFilter piece in pieces) {
                        piece.gameObject.SetActive(true);
                    }
                    broken = true;

                    onDamageCallbacks.Invoke(this, impulseMagnitude);
                }
            }
        }

        [Serializable]
        public struct BreakableJoint {

            [Tooltip("If the tension on joint is lower than this value, the joint will not wear out. See Total Durability for more info.")]
            [SerializeField]
            private float minTensionTreshold;

            [Tooltip("Factor applied to the tensions if tension is bigger than Min Tension Treshold.")]
            [SerializeField]
            private float tensionMultiplier;

            [Tooltip("When the tensions on the joint is larger than Min Tension Treshold, the durability will start to decrease. When this durability reaches 0, the joint breaks.")]
            [SerializeField]
            private float totalDurability;
            public float TotalDurability { get { return totalDurability; } }

            [SerializeField]
            [ReadOnly]
            private float currentDurability;
            public float CurrentDurability {
                get { return currentDurability; }
                set { currentDurability = value; }
            }

            [Tooltip("If selected, the tension on the joint will be logged to the console.")]
            [SerializeField]
            private bool logTensionMagnitude;

            [Tooltip("The joint attached to this body will break on impact. If there are no joints, a FixedJoint will be attached by this script.")]
            [SerializeField]
            private Rigidbody body;

            [Tooltip("TIf no joints are present, the script will automatically attach a FixedJoint connecting \"body\" to its parent. If this field is assigned, \"body\" will get connected to this body instead.")]
            [SerializeField]
            private Rigidbody attachedBodyOverride;

            [SerializeField]
            private UnityEvent<BreakableJoint, float> onDamageCallbacks;

            private GameObject gameObject;
            public GameObject GameObject { get { return gameObject; } }

            public bool Broken { get { return currentDurability < 0; } }

            private DeformableRootObject root;
            private Collider collider;
            private Joint joint;
            private float timeAfterBreak; // used for a dirty physics hack

            public void Create(DeformableRootObject root) {
                this.root = root;
                gameObject = body.gameObject;
            }

            public void Init() {
                timeAfterBreak = 0;

                collider = gameObject.GetComponent<Collider>();
                if (collider == null) {
                    collider = gameObject.AddComponent<MeshCollider>();
                    ((MeshCollider)collider).convex = true;
                    ((MeshCollider)collider).sharedMesh = gameObject.GetComponent<MeshFilter>().sharedMesh;
                }

                joint = gameObject.GetComponent<Joint>();
                if (joint == null) {
                    joint = gameObject.AddComponent<FixedJoint>();
                    Rigidbody connectedBody = attachedBodyOverride;
                    if(connectedBody == null) {
                        connectedBody = gameObject.transform.parent.GetComponent<Rigidbody>();
                        if (connectedBody == null) {
                            connectedBody = gameObject.transform.parent.gameObject.AddComponent<Rigidbody>();
                        }
                    }
                    joint.connectedBody = connectedBody;
                    joint.enableCollision = false;
                }

                currentDurability = totalDurability;
            }

            public void HandleJoint() {
                if (body.IsSleeping()) return;
                if (Broken) {
                    { // dirty hack for unity physics to wake up and uncollide broken joint bodies!
                        if (timeAfterBreak >= 0) {
                            timeAfterBreak += Time.fixedDeltaTime;
                            if (timeAfterBreak > .05) {
                                collider.enabled = false;
                                if (timeAfterBreak > .06) {
                                    collider.enabled = true;
                                    timeAfterBreak = -1;
                                }
                            }
                        }
                    }
                    return;
                }

                float tensionMagnitude = joint.currentForce.magnitude;
                if(logTensionMagnitude) Debug.Log(tensionMagnitude);

                if (tensionMagnitude > minTensionTreshold) {
                    float totalDamage = tensionMagnitude * tensionMultiplier * Time.fixedDeltaTime;
                    currentDurability -= totalDamage;
                    if (Broken) {
                        if (logTensionMagnitude) {
                            Debug.Log("BREAK JOINT [" + joint.name + "]");
                        }
                        Physics.IgnoreCollision(body.GetComponent<Collider>(), joint.connectedBody.GetComponent<Collider>(), false);
                        Destroy(joint);
                        body.useGravity = true;
                        body.isKinematic = false;
                        joint = null;
                    }
                    onDamageCallbacks.Invoke(this, totalDamage);
                }
            }
        }

	    private void FixedUpdate() {
            for (int i = 0; i < breakableJoints.Length; i++) {
                ref BreakableJoint bj = ref breakableJoints[i];
                bj.HandleJoint();
            }
        }

        private void OnValidate() {
            if(globalBreakableDamageFalloff < 0) {
                globalBreakableDamageFalloff = 0;
            }
            if (Application.isPlaying) return;
            for (int i = 0; i < breakableJoints.Length; i++) {
                ref BreakableJoint bj = ref breakableJoints[i];
                bj.CurrentDurability = bj.TotalDurability;
            }
        }

        private class DeformableCollisionListener : MonoBehaviour {

            public DeformableRootObject root;
            public int index;
            public Type type;

		    private void OnCollisionEnter(Collision collision) {
                if (Array.Exists(root.ignoreDamageFrom, element => element == collision.gameObject.layer)) {
                    return;
                }

                if (type == Type.Deformable) {
                    root.deformables[index].HandleCollision(collision);
                } else if (type == Type.Breakable) {
                    root.breakables[index].HandleCollision(collision);
                }

                foreach (Transform child in transform) {
                    if (child.TryGetComponent(out DeformableCollisionListener dcl)) {
                        dcl.OnCollisionEnter(collision);
                    }
                }
            }
        }

        private enum Type {
            Deformable,
            Breakable,
            BreakableJoint
	    }

        private class ReadOnlyAttribute : PropertyAttribute {}

#if UNITY_EDITOR
        [CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
        private class ReadOnlyDrawer : PropertyDrawer {

            public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
                return EditorGUI.GetPropertyHeight(property, label, true);
            }

            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
                GUI.enabled = false;
                EditorGUI.PropertyField(position, property, label, true);
                GUI.enabled = true;
            }
        }
#endif
    }
}