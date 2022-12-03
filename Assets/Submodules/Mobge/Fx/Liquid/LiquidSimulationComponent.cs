using System;
using System.Collections.Generic;
using Mobge;
using Mobge.Core;
using UnityEngine;

namespace Mobge.Fx
{
    public class LiquidSimulationComponent : ComponentDefinition<LiquidSimulationComponent.Data>
    {
        [Serializable]
        public struct Int2
        {
            public int x, y;
            public Int2(int x, int y) {
                this.x = x;
                this.y = y;
            }
            public static explicit operator Vector2(in Int2 @this) {
                return new Vector2(@this.x, @this.y);
            }
        }
        [Serializable]
        public struct Polygon
        {
            public Vector2[] points;
        }
        struct KeyValuePair
        {
            public uint key, value;
        }
        public struct Particle
        {
            public Vector2 position;
            public Vector3 color;
            public Vector2 velocity;
            public Vector2 force;
            public Vector3 nextColor;
            public float pressure;
            public float density;
            public Particle(Vector2 position, Vector2 velocity, Color color) {
                this.position = position;
                this.velocity = velocity;
                this.color = ToVector(color);

                this.force = Vector2.zero;
                this.nextColor = this.color;
                this.pressure = 0;
                this.density = 0;
            }
        }
        private struct Line
        {
            public Vector3 p1, p2;
        }
        public static Vector4 ToVector(Color c) {
            return new Vector4(c.r, c.g, c.b, c.a);
        }
        [Serializable]
        public class Data : BaseComponent, IChild
        {
            public bool gpuMode = false;
            public float textureScale = 0.04f;
            public Int2 simulationResolution = new Int2(128,128);
            public float particleRadius = 0.2f;

            public float wallCollisionEnergyLoss = 0.5f;
            public float viscosityConstant = 0.018f;
            public float pressureConstant = 20;
            public float fluidReferenceDensity = 20;
            public float safeWallRadius = 0.005f;
            public float colorExchangeConstant = 1.0f;
            public float metaballAlphaCutoff = 2;
            public float gravityScale = 1;
            public float drag = 0.99f;
            public float timeScale = 1;
            // public Texture2D mainTexture;

            public int maxParticleCount = 2048;
            public int initialParticleCount = 256;

            //public DebugObject debugObject;

            public Vector2 PhysicalSize => (Vector2)simulationResolution * textureScale;

            public Polygon[] walls;

            [SerializeField] private RendererModule _rendererModule;
            public ShaderControl shaderControl;
            public Transform Transform => this._rendererModule.Transform;

            private Pose _previousPose = Pose.identity;
            private Pose _previousVelocity = Pose.identity;

            [SerializeField, HideInInspector] ElementReference _parent = -1;
            ElementReference IChild.Parent { get => _parent; set => _parent = value; }

            public void SetConstants(ComputeShader shader) {
                var data = this;
                shader.SetFloat("particleRadius", data.particleRadius);
                shader.SetFloat("c_pressureConstant", data.pressureConstant);
                shader.SetFloat("c_fluidReferenceDensity", data.fluidReferenceDensity);
                shader.SetFloat("c_viscosityConstant", data.viscosityConstant);
                shader.SetFloat("c_wallCollisionEnergyLoss", data.wallCollisionEnergyLoss);
                shader.SetFloat("c_safeWallRadius", data.safeWallRadius);
                shader.SetFloat("c_colorExchangeConstant", data.colorExchangeConstant);
                //shader.SetFloat("c_maxForce", data.maxForceBasic);
                shader.SetFloat("c_drag", data.drag);
                shader.SetFloat("c_metaballAlphaCutoff", data.metaballAlphaCutoff);
            }

            public Vector2 ToSimulationSpace(Vector3 position) {
                var tr = Transform;
                position -= tr.position;
                position = Quaternion.Inverse(tr.rotation) * position;
                return position;
            }
            public Vector2 ToSimulationSpaceVector(Vector2 vector) {
                var tr = Transform;
                vector = Quaternion.Inverse(tr.rotation) * vector;
                return vector;
            }

            //[SerializeField] [HideInInspector] private LogicConnections _connections;
            public override void Start(in InitArgs initData) {
                
                shaderControl.Initialize(this, initData.parentTr.rotation);

                _rendererModule.Initialize(PhysicalSize, position, initData.parentTr, shaderControl.ColorMap);
                initData.player.ActionManager.DoRoutine(Update);

                _previousPose = CurrentPose;
                //debugObject.Initialize(this, initData.parentTr);

#if UNITY_EDITOR
                var deb = Transform.gameObject.AddComponent<LiquidSimulationDebugger>();
                deb.shader = shaderControl.RuntimeShader;
                deb.data = this;
#endif

                // new BitonicSortTester(_shaderControl.shader, initData.player.ActionManager, gpuMode);
            }
            public float DeltaTime {
                get {
                    return timeScale / Screen.currentResolution.refreshRate;
                }
            }
            Pose CurrentPose {
                get {
                    Pose p;
                    p.position = this._rendererModule.Transform.position;
                    p.rotation = this._rendererModule.Transform.rotation;
                    return p;
                }
            }
            public int ParticleCount => this.shaderControl.particleManager.ParticleCount;
            public void SpawnParticles(Particle[] particles) {
                SpawnParticles(particles, 0, particles.Length);
            }
            public void SpawnParticles(Particle[] particles, int start, int count) {
                this.shaderControl.particleManager.SpawnParticles(particles, start, count);
            }
            
            public void UpdateWalls(Polygon[] _walls)
            {
                walls = _walls;
                shaderControl.InitWalls(this);
            }

            private void Update(float obj) {

                // initialize variables
                shaderControl.UpdateDeltaTime(DeltaTime);
                
                int updateCount = GetMinCapacityForCount(shaderControl.particleManager.ParticleCount);


                var cp = CurrentPose;
                Pose vel;
                vel.position = cp.position - _previousPose.position;
                vel.rotation = cp.rotation * Quaternion.Inverse(_previousPose.rotation);
                if (vel != _previousVelocity) {


                    var oldM = Matrix4x4.TRS(_previousVelocity.position, _previousVelocity.rotation, Vector3.one);
                    var transform = Matrix4x4.TRS(vel.position, vel.rotation, Vector3.one);
                    transform = oldM * transform.inverse;

                    shaderControl.HandleMovement(this, transform, cp.rotation, vel.position - _previousVelocity.position);
                    
                    //debugObject.Update(transform, 1.0f / Application.targetFrameRate);


                    _previousVelocity = vel;
                }
                else {

                    //debugObject.Update(Matrix4x4.identity, 1.0f / Application.targetFrameRate);
                }


                //var aa = Matrix4x4.TRS(_previousPose.position, _previousPose.rotation, Vector3.one);
                //var aaa = Matrix4x4.TRS(cp.position, cp.rotation, Vector3.one);
                //aaa = aaa.inverse * aa;
                //debugObject.UpdateStatic(aaa, 1.0f / Application.targetFrameRate);


                _previousPose = cp;

                shaderControl.Update(this, updateCount);
            }

            // public override LogicConnections Connections {
            //     get => _connections;
            //     set => _connections = value;
            // }
        }

        [Serializable]
        public struct RendererModule
        {
            public Material material;

            private MeshRenderer _renderer;
            private MeshFilter _filter;
            private Transform _tr;
            public Transform Transform => _tr;
            public void Initialize(Vector2 size,Vector3 position , Transform parent, RenderTexture texture) {
                _renderer = new GameObject("liquid renderer").AddComponent<MeshRenderer>();
                _renderer.material = material;
                _renderer.material.mainTexture = texture;


                _filter = _renderer.transform.gameObject.AddComponent<MeshFilter>();
                _filter.sharedMesh = GenerateQuad();

                _tr = _renderer.transform;

                _tr.SetParent(parent, false);
                _tr.localScale = new Vector3(size.x, size.y, 1);
                _tr.localPosition = position;
            }
            private Mesh GenerateQuad() {
                Mesh m = new Mesh();
                m.vertices = new Vector3[] {
                    new Vector3(-0.5f, -0.5f, 0),
                    new Vector3(-0.5f,  0.5f, 0),
                    new Vector3( 0.5f,  0.5f, 0),
                    new Vector3( 0.5f, -0.5f, 0),
                };
                m.uv = new Vector2[] {
                    new Vector2(0, 0),
                    new Vector2(0, 1),
                    new Vector2(1, 1),
                    new Vector2(1, 0),
                };
                m.triangles = new int[] {
                    0,1,2,
                    0,2,3,
                };
                return m;
            }
        }
        public static int GetMinCapacityForCount(int count) {
            int cap = ShaderControl.c_particleThreadCount;
            while (cap < count) {
                cap <<= 1;
            }
            return cap;
        }

        [Serializable]
        public struct ShaderControl
        {

            public const int c_particleThreadCount = 64;
            public ComputeShader shader;
            private ComputeShader _shader;

            public ComputeShader RuntimeShader => _shader;

            private RenderTexture _colorMap;
            //private RenderTexture _forceField;


            private BufferWrapper<Line> _walls;
            private BufferWrapper<int> _errors;

            internal ParticleManager particleManager;

            public RenderTexture ColorMap => _colorMap;

            private int k_moveParticles;
            private int k_clearMaps, k_clearBuffers, k_bitonicSortMerge, k_hashKeys, k_calculateForces, k_calculateForcesBasic, k_calculateDensities, k_applyTransformationVelocity, k_drawParticles;
            public bool basicMode;
            ///private int k_drawWallsDebug;

            private List<int> _kernels;


            private int v_deltaTime, v_time, v_particleCount, v_calculateCount;
            private int v_gravity;



            public void Initialize(Data data, Quaternion rotation) {
                _shader = Instantiate(shader);
                // initialize kernels
                _kernels = new List<int>();
                k_clearMaps = InitializeKernel("ClearMaps");
                k_clearBuffers = InitializeKernel("ClearBuffers");
                k_moveParticles = InitializeKernel("MoveParticles");
                k_bitonicSortMerge = InitializeKernel("BitonicSortMerge");
                k_hashKeys = InitializeKernel("HashKeys");
                k_calculateForces = InitializeKernel("CalculateForces");
                k_calculateForcesBasic = InitializeKernel("CalculateForcesBasic");
                k_calculateDensities = InitializeKernel("CalculateDensities");
                k_applyTransformationVelocity = InitializeKernel("ApplyTransformatoinVelocity");
                k_drawParticles = InitializeKernel("DrawParticles");
                //k_drawWallsDebug = InitializeKernel("DrawWallsDebug");

                // cache property ids
                v_gravity = Shader.PropertyToID("gravity");
                v_deltaTime = Shader.PropertyToID("deltaTime");
                v_time = Shader.PropertyToID("time");
                v_particleCount = Shader.PropertyToID("particleCount");
                v_calculateCount = Shader.PropertyToID("calculateCount");

                // initialize buffers
                _colorMap = NewTexture(data.simulationResolution);
                SetTexture("ColorMap", _colorMap);
                //RenderTexture rt = NewTexture(new Int2(data.mainTexture.width, data.mainTexture.height));
                //RenderTexture.active = rt;
                //Graphics.Blit(data.mainTexture, rt);


                //SetTexture("MainTexture", rt);
                InitErrors();
                InitWalls(data);
                particleManager.Initialize(data);


                // initialize variables
                _shader.SetFloat("textureScale", data.textureScale);
                _shader.SetVector(v_gravity, Quaternion.Inverse(rotation) * Physics2D.gravity * data.gravityScale);

                UpdateDeltaTime(data.DeltaTime);
                data.SetConstants(_shader);

            }

            private void InitErrors() {
                ExposedList<int> errorList = new ExposedList<int>(10);
                errorList.SetCountFast(10);
                _errors.Initialize(errorList);
                SetBuffer("Errors", _errors.buffer);
            }

            private int InitializeKernel(string name) {
                int kernel = _shader.FindKernel(name);
                _kernels.Add(kernel);
                return kernel;
            }
            private void InitializeEmptyBuffer<T>(ref BufferWrapper<T> buffer, string name, int count) where T : unmanaged{
                buffer.InitializeGarbage(count);
                SetBuffer(name, buffer.buffer);
            }

            public unsafe void InitWalls(Data data) {
                ExposedList<Line> lines = new ExposedList<Line>();
                // add predefined walls
                for (int i = 0; i < data.walls.Length; i++) {
                    var corners = data.walls[i].points;
                    var prev = corners[corners.Length - 1];
                    for(int j = 0; j < corners.Length; j++) {
                        Line l;
                        l.p1 = prev;
                        l.p2 = corners[j];
                        lines.Add(l);
                        prev = l.p2;
                    }
                }
                _walls.Initialize(lines);
                _shader.SetInt("c_wallCount", lines.Count);
                SetBuffer("Walls", _walls.buffer);
            }
            private static float R (float min, float max) {
                return UnityEngine.Random.Range(min, max);
            }
            private RenderTexture NewTexture(Int2 size) {
                var t = new RenderTexture(size.x, size.y, 24);
                t.enableRandomWrite = true;
                // t.format = RenderTextureFormat.ARGBFloat;
                t.Create();
                return t;
            }

            private void SetTexture(string name, Texture texture) {
                for (int i = 0; i < _kernels.Count; i++) {
                    _shader.SetTexture(_kernels[i], name, texture);
                }
            }

            private void SetBuffer(string name, ComputeBuffer buffer) {
                for (int i = 0; i < _kernels.Count; i++) {
                    _shader.SetBuffer(_kernels[i], name, buffer);
                }
            }
            public void HandleMovement(Data data, in Matrix4x4 transformation, in Quaternion rotation, Vector3 debugTranslate) {
                _shader.SetMatrix("transformation", transformation);
                _shader.SetVector("debugTranslate", debugTranslate);
                _shader.SetVector(v_gravity, Quaternion.Inverse(rotation) * Physics2D.gravity * data.gravityScale);
                int count = GetMinCapacityForCount(particleManager.ParticleCount);
                _shader.Dispatch(k_applyTransformationVelocity, count / c_particleThreadCount, 1, 1);

            }
            static System.Text.StringBuilder _sb = new System.Text.StringBuilder();
            internal void Update(Data data, int updateCount) {



                _shader.SetInt(v_particleCount, particleManager.ParticleCount);
                _shader.SetInt(v_calculateCount, updateCount);

                //float time = Time.realtimeSinceStartup;
                _shader.Dispatch(k_clearMaps, _colorMap.width / 8, _colorMap.height / 8, 1);
                _shader.Dispatch(k_clearBuffers, updateCount / c_particleThreadCount, 1, 1);
                _shader.Dispatch(k_moveParticles, updateCount / c_particleThreadCount, 1, 1);
                BitonicSortGpu(updateCount);
                _shader.Dispatch(k_hashKeys, updateCount / c_particleThreadCount, 1, 1);
                _shader.Dispatch(k_calculateDensities, updateCount / c_particleThreadCount, 1, 1);
                if (basicMode) {
                    _shader.Dispatch(k_calculateForcesBasic, updateCount / c_particleThreadCount, 1, 1);
                }
                else {
                    _shader.Dispatch(k_calculateForces, updateCount / c_particleThreadCount, 1, 1);
                }
                _shader.Dispatch(k_drawParticles, _colorMap.width / 8, _colorMap.height / 8, 1);
                //shader.Dispatch(k_moveAndCollide2, _particles.Count / 32, 1, 1);

                //shader.Dispatch(k_drawWallsDebug, 1, 1, 1);
                _errors.BufferToList();
                _sb.Clear();
                for (int i = 0; i  < _errors.list.Count; i++) {
                    if (_errors.list.array[i] != 0) {
                        _sb.Append(_errors.list.array[i] + " ");
                        //Debug.Log("error: " + _errors.list.array[0]+ ", " + _errors.list.array[1]+", " + _errors.list.array[2]);
                    }
                }
                if (_sb.Length != 0) {

                    Debug.Log(_sb);
                }
            }

            internal void UpdateDeltaTime(float deltaTime) {
                _shader.SetFloat(v_deltaTime, deltaTime);
                _shader.SetFloat(v_time, Time.renderedFrameCount * deltaTime);
            }

            private void BitonicSortGpu(int count) {
                for (int i = 1; i < count; i <<= 1) {
                    for (int j = i; j > 0; j >>= 1) {
                        _shader.SetInt("sortPass1", i);
                        _shader.SetInt("sortPass2", j);
                        _shader.Dispatch(k_bitonicSortMerge, count / c_particleThreadCount, 1, 1);

                    }
                }
            }
            internal struct ParticleManager
            {
                private int _particleCapacity;
                private int _particleCount;

                private BufferWrapper<Particle> _particles;
                private BufferWrapper<KeyValuePair> _hashMap;
                private BufferWrapper<KeyValuePair> _sorted;

                public int Capacity {
                    get => _particleCapacity;
                }
                public int ParticleCount {
                    get => _particleCount;
                }
                public void Initialize(Data data) {
                    _particleCount = data.initialParticleCount;
                    _particleCapacity = GetMinCapacityForCount(data.maxParticleCount);

                    
                    data.shaderControl.InitializeEmptyBuffer(ref _hashMap, "HashMap", _particleCapacity * 2);
                    data.shaderControl.InitializeEmptyBuffer(ref _sorted, "Sorted", _particleCapacity);

                    InitializeRandomParticles(data);
                }

                internal void SpawnParticles(Particle[] particles, int start, int count) {
                    count = Mathf.Min(_particleCapacity - _particleCount, count);
                    if(count == 0) {
                        return;
                    }
                    int sourceEnd = start + count;
                    for (int i = start, targetIndex = _particleCount; i < sourceEnd; i++, targetIndex++) {
                        _particles.list.array[targetIndex] = particles[i];
                    }
                    _particles.ListToBuffer(_particleCount, count);
                    _particleCount += count;
                }

                private unsafe void InitializeRandomParticles(Data data) {
                    ExposedList<Particle> list = new ExposedList<Particle>();
                    var area = data.PhysicalSize;
                    var halfArea = area * 0.3f;
                    for (int i = 0; i < Capacity; i++) {
                        Particle p;
                        p.position = new Vector2(R(-halfArea.x, halfArea.x), R(-halfArea.y, halfArea.y));
                        p.velocity = Vector2.zero;
                        p.color = p.position.x < 0 ? ToVector(new Color(0.9f, 0.9f, 0.0f)) : ToVector(new Color(-0.9f, 0.3f, 1));
                        p.nextColor = p.color;
                        p.force = Vector2.zero;
                        p.pressure = 0;
                        p.density = 0;
                        list.Add(p);
                    }
                    _particles.Initialize(list);
                    data.shaderControl.SetBuffer("Particles", _particles.buffer);
                }

            }
        }
        [Serializable]
        public struct DebugObject
        {
            public GameObject resObject;
            private GameObject _instance;
            private Vector2 velocity;
            public void Initialize(Data data, Transform parent) {
                if (resObject) {
                    GameObject go = new GameObject("mid");
                    go.transform.SetParent(parent);
                    go.transform.localPosition = data.position;
                    _instance = Instantiate(resObject, data.position, Quaternion.identity, go.transform);
                }

            }
            public void UpdateStatic(Matrix4x4 translateVelocity, float deltaTime) {
                if (_instance) {
                    _instance.transform.localPosition = translateVelocity.MultiplyPoint(_instance.transform.localPosition);
                }
            }
            public void Update(Matrix4x4 translateVelocity, float deltaTime) {
                if (_instance) {
                    var newPos = translateVelocity.MultiplyPoint(_instance.transform.localPosition);
                    velocity += (Vector2)(newPos - _instance.transform.localPosition) / deltaTime;

                    var pos = _instance.transform.position;
                    pos += (Vector3)velocity * deltaTime;
                    _instance.transform.position = pos;
                }
            }
        }

        private struct BufferWrapper<T> where T : unmanaged
        {
            public ComputeBuffer buffer;
            public ExposedList<T> list;
            public unsafe void Initialize(int count) {
                buffer = new ComputeBuffer(count, sizeof(T));
                list = new ExposedList<T>();
                list.SetCountFast(count);
            }
            public unsafe void InitializeGarbage(int count) {
                buffer = new ComputeBuffer(count, sizeof(T));
            }
            public unsafe void Initialize(ExposedList<T> list) {
                this.list = list;
                buffer = new ComputeBuffer(list.Count, sizeof(T));
                ListToBuffer();
            }
            public void ListToBuffer() {
                buffer.SetData(list.array, 0, 0, list.Count);
            }
            public void ListToBuffer(int start, int count) {
                buffer.SetData(list.array, start, start, count);
            }
            public void BufferToList() {
                BufferToList(0, list.Count);
            }
            public void BufferToList(int start, int count) {
                buffer.GetData(list.array, start, start, count);
            }
            public int Count => list.Count;

        }
        private struct BitonicSortTester
        {
            private List<uint> backup;
            private int bitonicSortMerge;

            public BitonicSortTester(ComputeShader shader, ActionManager actionManager, bool gpuMode) {
                this.backup = new List<uint>();
                int count = 65535;

                int capacity = ((count + ShaderControl.c_particleThreadCount - 1) / ShaderControl.c_particleThreadCount) * ShaderControl.c_particleThreadCount;
                bitonicSortMerge = shader.FindKernel("BitonicSortMerge");
                BufferWrapper<KeyValuePair> buffer = new BufferWrapper<KeyValuePair>();
                BufferWrapper<KeyValuePair> buffer2 = new BufferWrapper<KeyValuePair>();
                ExposedList<KeyValuePair> data = new ExposedList<KeyValuePair>();
                for (int i = 0; i < capacity; i++) {
                    uint val = i < count ? (uint)UnityEngine.Random.Range(0, 10000) : 0xFFFFFFFF;
                    data.Add(new KeyValuePair() {
                        key = (uint)val,
                        value = (uint)val,
                    });
                    this.backup.Add(val);
                }
                buffer.Initialize(data);
                buffer2.Initialize(data);
                //shader.SetBuffer(bitonicSort, "Sorted", buffer.buffer);
                //shader.SetBuffer(bitonicSort, "Sorted2", buffer2.buffer);
                shader.SetBuffer(bitonicSortMerge, "Sorted", buffer.buffer);
                shader.SetBuffer(bitonicSortMerge, "Sorted2", buffer2.buffer);
                LogData("before sort", data);
                if (gpuMode) {
                    //shader.Dispatch(bitonicSort, capacity / ShaderControl.c_particleThreadCount, 1, 1);
                    BitonicSortGpu(shader, data.Count);
                }
                else {
                    BitonicSort(data);
                }
                var @this = this;
                actionManager.DoTimedAction(0.3f, null, (a, b) => {
                    if (gpuMode) {
                        buffer.buffer.GetData(data.array, 0, 0, data.Count);
                    }
                    else {
                    }
                    LogData("after sort", data);
                    Debug.Log("backup count: " + @this.backup.Count);
                    uint next = 0;
                    int mistaken = -1;
                    for (int i = 0; i < data.Count; i++) {
                        var val = data.array[i].value;
                        if (mistaken < 0 && next > val) {
                            mistaken = i;
                        }
                        next = val;
                        if (!@this.backup.Remove(val)) {
                            Debug.Log("fail to remove: " + val);
                        }
                    }
                    Debug.Log("is data sorted properly: " + mistaken);
                    Debug.Log("backup count after remove sorted data: " + @this.backup.Count);
                    for(int i = 0; i < @this.backup.Count; i++) {
                        Debug.Log(@this.backup[i]);
                    }
                });
            }
            private void BitonicSortGpu(ComputeShader shader, int count) {
                for (int i = 1; i < count; i <<= 1) {
                    for (int j = i; j > 0; j >>= 1) {
                        shader.SetInt("sortPass1", i);
                        shader.SetInt("sortPass2", j);
                        shader.Dispatch(bitonicSortMerge, count / ShaderControl.c_particleThreadCount, 1, 1);

                    }
                }
            }


            void BitonicSwapCase(uint index, uint j, bool sign, uint count, KeyValuePair[] Sorted) {

                bool hasMove = index % (j << 1) < j;
                uint target = index + j;
                if (hasMove && target < count) {
                    if ((Sorted[index].value > Sorted[target].value) == sign) {
                        KeyValuePair temp = Sorted[target];
                        Sorted[target] = Sorted[index];
                        Sorted[index] = temp;
                    }
                }
            }
            private void BitonicSort(ExposedList<KeyValuePair> data) {

                uint count = (uint)data.Count;
                var Sorted = data.array;
                for (uint i = 1; i < count * 4; i <<= 1) {
                    for (uint j = i; j > 0; j >>= 1) {

                        for (uint idx = 0; idx < count; idx++) {
                            bool sign = (idx / (i << 1)) % 2 == 0;
                            uint index = idx;
                            BitonicSwapCase(index, j, sign, count, Sorted);
                            

                            
                        }
                        //GroupMemoryBarrierWithGroupSync();
                    }
                }

            }

            private static void LogData(string label, ExposedList<KeyValuePair> data) {
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                for (int i = 0; i < data.Count; i++) {
                    sb.Append(data.array[i].value);
                    sb.Append("  ");
                }
                Debug.Log(label + ": " + sb);
            }
        }
    }
}