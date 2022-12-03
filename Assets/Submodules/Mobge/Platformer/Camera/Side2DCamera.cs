using System;
using UnityEngine;
using Mobge.Platformer.Character;
using System.Collections.Generic;
using Mobge.Core;

namespace Mobge.Platformer
{
    public class Side2DCamera : ACameraController
    {
        

        private Transform _tr;
        private CameraTarget _target;
        private Vector3 _lastTargetPosition;
        private FrameCalculator _frameCalculator;
        private Vector3 _strictPosition;
        public TransparencySortMode transparencySortMode = TransparencySortMode.Orthographic;
        [HideInInspector] public float matchWidthHeight = 1f;
        private CameraTrigger _trigger;
        private PriorityList<Side2DCameraData> _datalist;
        //private float _lastGroundOffsetY;
        private bool _seperatedFromGround;
        private Vector3 _prevCameraPos;
        private ModifierList _modifiers;

        public Vector3 testFollowForce = new Vector3(10, 10, 10);
        protected void Awake() {
            _tr = transform;
            _modifiers.Initialize();
            _datalist.Initialize();
            if (camera == null) {
                camera = GetComponentInChildren<Camera>();
            }
            if (camera == null || camera.transform == transform || !camera.transform.IsChildOf(transform)) {
                throw new Exception("The camera field has to be set to a Camera behaviour that is child of this object.");
            }
            _strictPosition = _tr.position;
            camera.transparencySortMode = transparencySortMode;
        }
        //public Bounds CurrentFrame => _frameCalculator.CalculateFrame();
        public ModifierList Modifiers => _modifiers;
        protected void OnDestroy() {
            _trigger.EnsureDestruction();
        }
        /// <summary>
        /// Current frame of the camera without camera effects.
        /// </summary>
        public Bounds CurrentFrame => _frameCalculator.CalculateFrame(camera, _tr.position, 0);
        /// <summary>
        /// Current frame of the camera at z without camera effects;
        /// </summary>
        /// <param name="z"></param>
        /// <returns></returns>
        public Bounds GetCurrentFrame(float z) => _frameCalculator.CalculateFrame(camera, _tr.position, z);
        /// <summary>
        /// Current frame that camera currently see including effect offsets.
        /// </summary>
        public Bounds CurrentAbsoluteFrame => _frameCalculator.CalculateFrame(camera, camera.transform.position, 0);
        /// <summary>
        /// Current frame that camera currently see at specified z including effect offsets.
        /// </summary>
        public Bounds GetCurrentAbsoluteFrame(float z) => _frameCalculator.CalculateFrame(camera, camera.transform.position, z);
        private static float DeltaTime => Time.unscaledDeltaTime;
        protected void Update() {
            var data = _datalist.Selected;
            if (data == null) {
                enabled = false;
                return;
            }
            Vector3 targetPos = _target.Position;
            UpdateArgs updateArgs;
            UpdateData updateData;
            updateData.newPos = GetTargetCameraPos(data, targetPos, _target.size, data.maxHorizonralOffsetRate, _target.OnGround, out updateArgs.extends);
            updateData.followForce = data.followForce;
            updateData.effectOffset = Vector3.zero;
            updateData.effectEulerRotation = Vector3.zero;
            updateArgs.cameraPos = Real2Abstract(_tr.position);
            updateArgs.target = new Rect(targetPos, _target.size);
            #region run extra modifiers
            if (data.modifiers != null) {
                for (int i = 0; i < data.modifiers.Length; i++) {
                    data.modifiers[i].UpdatePosition(in updateArgs, ref updateData);
                }
            }
            var e = _modifiers.GetValues();
            var arr = e.array;
            for (int i = 0; i < e.Count; i++) {
                var c = arr[i];
                c.UpdatePosition(updateArgs, ref updateData);
            }
            #endregion
            var camTr = camera.transform;
            camTr.localPosition = updateData.effectOffset;
            camTr.localEulerAngles = updateData.effectEulerRotation;
            //dis.Scale(updateData.followTightness);
            var t = updateData.newPos;
            Vector3 np;
            if (DeltaTime != 0) {
                np = CameraApproach(t, updateArgs.cameraPos, updateData.followForce /*updateData.followTightness*/, _prevCameraPos, DeltaTime);
                //var dt = DeltaTime;
                //var vel = (updateArgs.cameraPos - _prevCameraPos) / dt;
                //np = Vector3.SmoothDamp(updateArgs.cameraPos, t, ref vel, testFollowForce.x, float.PositiveInfinity, dt);
            }
            else {
                np = updateArgs.cameraPos;
            }
            //var dis = updateData.newPos - updateArgs.cameraPos;
            //dis.Scale(updateData.followTightness);
            //var np = updateArgs.cameraPos + dis;
            _prevCameraPos = updateArgs.cameraPos;
            _tr.position = Abstract2Real(np);
        }
        public static Vector3 CameraApproach(Vector3 target, Vector3 currentPos, Vector3 amount, Vector3 oldPos, float dt) {
            //var delta = current - oldPos;
            //var vel = delta / Time.deltaTime;
            //var dif = target - current;
            //var distance = dif.magnitude;
            //float maxDirectVel = 
            //return target;
            return new Vector3(
                CameraApproach(target.x, currentPos.x, amount.x, oldPos.x, dt),
                CameraApproach(target.y, currentPos.y, amount.y, oldPos.y, dt),
                CameraApproach(target.z, currentPos.z, amount.z, oldPos.z, dt));
        }
        public static float CameraApproach(float target, float currentPos, float amount, float oldPos, float dt) {
            var delta = target - currentPos;
            bool negative = delta < 0;
            var idt = 1f / dt;
            var vel = currentPos - oldPos;
            vel *= idt;
            if (negative) {
                vel = -vel;
                delta = -delta;
            }
            var supposedVel = Mathf.Sqrt(2 * delta * amount);
            if (float.IsNaN(supposedVel)) {
                supposedVel = float.PositiveInfinity;
            }
            supposedVel = Math.Min(supposedVel, delta * idt);
            var requiredVel = supposedVel - vel;
            var reqForce = requiredVel * idt;
            //reqForce = Mathf.Clamp(reqForce, -amount, amount);
            vel += reqForce * dt;
            if (negative) {
                vel = -vel;
            }
            return currentPos + vel * dt;
        }

        public static Quaternion ApproachRotationOld(Quaternion target, Quaternion current, float amount, Quaternion prev, float dt) {

            //return Quaternion.RotateTowards(current, target, amount * dt);


            ToComponents(target, out var fTarget, out var uTarget);
            ToComponents(current, out var fCurrent, out var uCurrent);
            ToComponents(prev, out var fPrev, out var uPrev);
            Vector3 a3 = new Vector3(amount, amount, amount);
            var forward = Side2DCamera.CameraApproach(fTarget, fCurrent, a3, fPrev, dt);
            var up = Side2DCamera.CameraApproach(uTarget, uCurrent, a3, uPrev, dt);
            return Quaternion.LookRotation(forward, up);
        }
        public static Quaternion ApproachRotation(Quaternion target, Quaternion current, float amount, Quaternion prev, float dt) {

            //return Quaternion.RotateTowards(current, target, amount * dt);
            var eTarget = target.eulerAngles;
            var eCurrent = current.eulerAngles;
            var ePrev = prev.eulerAngles;

            NormalizeEuler(ref eCurrent, eTarget);
            NormalizeEuler(ref ePrev, eTarget);
            //eTarget = Vector3.zero;

            Vector3 a3 = new Vector3(amount, amount, amount);
            var forward = Side2DCamera.CameraApproach(Vector3.zero, eCurrent, a3, ePrev, dt);
            return Quaternion.Euler(eTarget - forward);
        }

        static void NormalizeEuler(ref Vector3 euler, in Vector3 target) {
            euler.x = Mathf.DeltaAngle(euler.x, target.x);
            euler.y = Mathf.DeltaAngle(euler.y, target.y);
            euler.z = Mathf.DeltaAngle(euler.z, target.z);
        }

        private static void ToComponents(Quaternion quaternion, out Vector3 forward, out Vector3 up) {
            forward = quaternion * Vector3.forward;
            up = quaternion * Vector3.up;
        }
        protected void FixedUpdate() {
            _trigger.TrySetPosition(TargetPosition);
        }

        public void GoTargetImmediate() {
            Vector3 targetPos = _target.Position;
            _lastTargetPosition = targetPos;
            var tcpos = GetTargetCameraPos(_datalist.Selected, targetPos, _target.size, 0, true, out Vector2 extends);
            _prevCameraPos = tcpos;
            _tr.position = _prevCameraPos;
        }
        public float Real2Abstract(float realDistance, Vector2 screenSize) {
            //return realDistance;
            float aspect = Mathf.Lerp(screenSize.x, screenSize.y, matchWidthHeight) / screenSize.y;
            realDistance *= aspect;
            return realDistance;
        }
        public float Abstract2Real(float abstractDistance, Vector2 screenSize) {
            //return abstractDistance;
            float aspect = screenSize.y / Mathf.Lerp(screenSize.x, screenSize.y, matchWidthHeight);
            abstractDistance *= aspect;
            return abstractDistance;

        }
        private Vector3 Abstract2Real(Vector3 abstractPos) {
            abstractPos.z = Abstract2Real(abstractPos.z, new Vector2(Screen.width, Screen.height));
            return abstractPos;
        }
        private Vector3 Real2Abstract(Vector3 realPos) {
            realPos.z = Real2Abstract(realPos.z, new Vector2(Screen.width, Screen.height));
            return realPos;
        }

        private Vector3 GetTargetCameraPos(Side2DCameraData data, Vector3 targetPos, Vector2 targetSize, float maxHorizonralOffsetRate, bool onGround, out Vector2 frameExtends) {
            var p = _strictPosition;
            p = Abstract2Real(p);
            var frame = _frameCalculator.CalculateFrame(camera, p, 0);
            frameExtends = frame.extents;
            var centerPosition = data.centerData.Position;
            Vector2 unitToRate = new Vector2(1f / frameExtends.x, 1f / frameExtends.y);
            // how much the character moved in the last update
            Vector2 deltaTarget = targetPos - _lastTargetPosition;
            _lastTargetPosition = targetPos;
            // update the target pos according to the center weight
            targetPos.x = Mathf.LerpUnclamped(centerPosition.x, targetPos.x, data.movementRate.x);
            targetPos.y = Mathf.LerpUnclamped(centerPosition.y, targetPos.y, data.movementRate.y);
            Vector2 deltaTargetRate = Vector2.Scale(deltaTarget, unitToRate);
            // offset of the character
            Vector2 targetOffsetRate = Vector2.Scale(targetPos - _strictPosition, unitToRate);

            // move the desired target offset in the direction that target is moving
            targetOffsetRate.x -= deltaTargetRate.x * 2;

            ApplyOffsetLimits(data, onGround, maxHorizonralOffsetRate, ref targetOffsetRate);

            _strictPosition.x = targetPos.x - frameExtends.x * targetOffsetRate.x;
            _strictPosition.y = targetPos.y - frameExtends.y * targetOffsetRate.y;
            _strictPosition.z = -data.zOffset;


            return _strictPosition;
        }
        private void ApplyOffsetLimits(Side2DCameraData data, bool onGround, float maxHorizonralOffsetRate, ref Vector2 targetOffsetRate) {
            targetOffsetRate.x = Mathf.Clamp(targetOffsetRate.x, -maxHorizonralOffsetRate - data.horizontalOffset, maxHorizonralOffsetRate - data.horizontalOffset);

            if (onGround) {
                targetOffsetRate.y = data.verticalOffset;
                //_lastGroundOffsetY = targetOffsetRate.y;
                _seperatedFromGround = false;
            }
            else {
                if (!data.snapGround || _seperatedFromGround) {
                    targetOffsetRate.y = data.verticalOffsetAir;
                }
                else {
                    if (targetOffsetRate.y < -data.snapBreakOffset || targetOffsetRate.y > data.snapBreakOffset) {
                        _seperatedFromGround = true;
                        targetOffsetRate.y = data.verticalOffsetAir;
                    }
                }
            }
        }
        public Side2DCameraData Data {
            get => _datalist.Selected;
            set {
                _datalist.Clear();
                _datalist.Add(value, -1);
                UpdateEnabled();
            }
        }

        private void UpdateEnabled() {
            bool nEnabled = Data != null;
            if (nEnabled != enabled) {
                enabled = nEnabled;
            }
        }
        public void AddData(Side2DCameraData data, int priority) {
            _datalist.Add(data, priority);
            UpdateEnabled();
        }
        public void RemoveData(Side2DCameraData data) {
            _datalist.Remove(data);
            UpdateEnabled();
        }
        public Character2D TargetCharacter {
            get => _target.Character;
            set {
                _target.Character = value;
            }
        }
        public Transform Target {
            get => _target.Transform;
            set {
                _target.Transform = value;
            }
        }
        public Vector2 TargetSize {
            get => _target.size;
            set => _target.size = value;
        }
        public Vector3 TargetPosition {
            get => _target.Position;
            set => _target.Position = value;
        }
        public void EnsureTrigger(int layer) {
            _trigger.EnsureExistence(layer, this);
        }
        private struct CameraTrigger
        {
            private Rigidbody2D _body;
            private CircleCollider2D _collider;
            public void EnsureExistence(int layer, Side2DCamera cam) {
                if (_body == null) {
                    _body = new GameObject("camera trigger").AddComponent<Rigidbody2D>();
                    _body.bodyType = RigidbodyType2D.Kinematic;
                    _body.transform.position = cam._lastTargetPosition;
                    _collider = _body.gameObject.AddComponent<CircleCollider2D>();
                    _collider.isTrigger = false;
                    _collider.radius = 0.1f;
                }
                _body.gameObject.layer = layer;
            }
            public void EnsureDestruction() {
                if (_body) {
                    _collider.enabled = false;
                    _body.gameObject.DestroySelf();
                }
            }
            public bool Exists => _body != null;
            public bool TrySetPosition(Vector3 position) {
                if (_body != null) {
                    _body.position = position;
                    return true;
                }
                return false;
            }
        }
        private struct CameraTarget
        {
            public Vector2 size;
            private Transform _tr;
            private Character2D _chr;
            private Vector3 _position;
            public Transform Transform {
                get => _tr;
                set {
                    _tr = value;
                    _chr = null;
                }
            }
            public Character2D Character {
                get => _chr;
                set {
                    _chr = value;
                    _tr = null;
                }
            }
            public Vector3 Position {
                get {
                    UpdatePosition();
                    return _position;
                }
                set {
                    _position = value;
                    _tr = null;
                    _chr = null;
                }
            }
            public bool OnGround {
                get {
                    if (_chr)
                        return _chr.OnGroundStanding;
                    return false;
                }
            }
            private bool UpdatePosition() {
                if (_tr) { _position = _tr.position; return true; }
                if (_chr) {
                    _position = _chr.Center;
                    return true;
                }
                return false;
            }
        }
        public static Vector2 CalculateFrameSize(float fov, float aspect, float distance) {
            float tan = Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad) * 2;
            return CalculateFrameSizeWithTan(tan, aspect, distance);
        }
        private static Vector2 CalculateFrameSizeWithTan(float tanFov, float aspect, float distance) {
            float height = tanFov * distance;
            float width = aspect * height;
            return new Vector2(width, height);
        }
        private struct FrameCalculator
        {
            private float _lastFOV;
            private float _tanFOV;
            public Bounds CalculateFrame(Camera c, Vector3 selfPosition, float targetPlaneZ) {
                float distance = targetPlaneZ - selfPosition.z;
                if (c.fieldOfView != _lastFOV) {
                    _lastFOV = c.fieldOfView;
                    _tanFOV = Mathf.Tan(_lastFOV * 0.5f * Mathf.Deg2Rad) * 2f;
                }
                var size = CalculateFrameSizeWithTan(_tanFOV, Screen.width/(float)Screen.height, distance);
                selfPosition.z = targetPlaneZ;
                return new Bounds(selfPosition, size);
            }
        }
        public interface Modifier
        {
            void UpdatePosition(in UpdateArgs args, ref UpdateData data);
        }
        public struct UpdateArgs
        {
            public Vector3 cameraPos;
            public Rect target;
            public Vector2 extends;
        }
        public struct UpdateData
        {
            public Vector3 newPos;
            public Vector3 followForce;
            public Vector3 effectOffset;
            public Vector3 effectEulerRotation;
        }
        private struct PriorityList<T> where T : class
        {
            private ExposedList<Pair> _elements;
            private int _selected;
            public void Initialize() {
                _elements = new ExposedList<Pair>();
            }
            public T Selected => SelectedPair.element;
            private Pair SelectedPair {
                get {
                    if (_selected < 0) {
                        Pair p;
                        p.element = null;
                        p.priority = int.MinValue;
                        return p;
                    }
                    return _elements.array[_selected];
                }
            }
            public void Add(T element, int priority) {

                int index = IndexOf(element);
                if (index >= 0) {
                    RemoveAt(index);
                }
                Pair pair;
                pair.element = element;
                pair.priority = priority;
                index = _elements.Count;
                _elements.Add(pair);

                if (priority > SelectedPair.priority) {
                    _selected = index;
                }
            }
            public void Remove(T element) {
                var index = IndexOf(element);
                if (index >= 0) {
                    RemoveAt(index);
                }
            }
            private void RemoveAt(int index) {
                var p = _elements.RemoveFast(index);

                UpdateSelected();
            }
            private void UpdateSelected() {
                _selected = -1;
                int priority = int.MinValue;
                var arr = _elements.array;
                for (int i = 0; i < _elements.Count; i++) {
                    var p = arr[i];
                    if (p.priority > priority) {
                        priority = p.priority;
                        _selected = i;
                    }
                }
            }
            private int IndexOf(T t) {
                var arr = _elements.array;
                for (int i = 0; i < _elements.Count; i++) {
                    if (arr[i].element == t) {
                        return i;
                    }
                }
                return -1;
            }
            public void Clear() {
                _elements.Clear();
            }
            private struct Pair
            {
                public int priority;
                public T element;
            }

        }
        public struct ModifierList
        {
            private HashSet<Modifier> _values;
            private ExposedList<Modifier> _temp;
            public void Initialize() {
                _values = new HashSet<Modifier>();
                _temp = new ExposedList<Modifier>();
            }
            public bool Add(Modifier modifier) {
                return _values.Add(modifier);
            }
            public bool Contains(Modifier modifier) {
                return _values.Contains(modifier);
            }
            public bool Remove(Modifier modifier) {
                return _values.Remove(modifier);
            }

            internal ExposedList<Modifier> GetValues() {
                _temp.SetCount(_values.Count);
                var e = _values.GetEnumerator();
                int i = 0;
                while (e.MoveNext()) {
                    _temp.array[i] = e.Current;
                    i++;
                }
                return _temp;
            }
        }

    }
    public abstract class ACameraController : MonoBehaviour
    {
        public new Camera camera;
        public static T FindOrCreateForLevel<T>(LevelPlayer player) where T : ACameraController {
            if (player.TryGetExtra<T>(typeof(T), out T t)) {
                return t;
            }

            var camera = Camera.main;
            if (camera == null) {
                Debug.LogError($"There is no Main Camera! This can cause problems in {nameof(ACameraController)}");
                return null;
            }
            //if (_cachedCamera != null && _cachedCamera.camera == mc) {
            //    return _cachedCamera;
            //}
            var c = camera.GetComponentInParent<T>();
            if (!c) {
                c = camera.gameObject.AddComponent<T>();
            }
            player.SetExtra(typeof(T), c);
            return c;
        }
    }
}