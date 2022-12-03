using System;
using System.Collections;
using System.Collections.Generic;
using Mobge.Platformer.PropertyModifier;
using UnityEngine;
using Mobge.Animation;

namespace Mobge.Platformer.Character {
    [RequireComponent(typeof(Rigidbody2D))]
    public partial class Character2D : MonoBehaviour, IAnimationOwner {

        [SerializeField] [OwnComponent(true)] private Collider2D _body;
        [SerializeField] [OwnComponent(true)] private AAnimation _animation;
        [OwnComponent(typeof(BaseMoveModule))]
        [SerializeField] private UnityEngine.Object[] _moveModules;

        [OwnComponent(typeof(BaseJumpModule))]
        [SerializeField] private UnityEngine.Object[] _jumpModules;
        [OwnComponent(typeof(IControlModule))] [SerializeField] private UnityEngine.Object[] _controlModules;
        [OwnComponent(typeof(DamageHandlerState), true)] [SerializeField] private DamageHandlerState _damageHandler;
        [SerializeField] private InputMapper _inputMapper;
        [SerializeField] private Health _health;
        [SerializeField] private Poise _poise;

#if UNITY_EDITOR
        public bool editorPrints;
#endif

        private float _attackMultiplayer;
        private Rigidbody2D _rigidbody;
        private int _currentMoveModule = -1, _currentJumpModule = -1;
        private GroundContact _groundContact;
        private int _airJumpCount;
        private float _lastGroundTime;
        private float _disableWalkTill;
        private CharacterInput _input;
        private ActiveState _activeState;
        private State _state;
        private GameSetup _gameSetup;
        private ActionManager _actions;
        private ModifierManager _modifiers;
        public Action<Character2D> OnDeath { get; set; }
        public event Action<bool> VisibilityChanged {
            add {
                Animation.VisibilityChanged += value;
            }
            remove {
                Animation.VisibilityChanged -= value;
            }
        }

        public AAnimation Animation {
            get {
#if UNITY_EDITOR
                if (!Application.isPlaying) {
                    if (_animation == null) {
                        _animation = GetComponentInChildren<AAnimation>();
                    }
                    if (!_animation.PrepareForEditor()) {
                        return null;
                    }
                }
#endif
                return _animation;
            }
        }
        public DamageHandlerState DamageHandler {
            get {
                return _damageHandler;
            }
        }
        public GameSetup GameSetup {
            get => _gameSetup;
        }
        public int CurrentMoveModuleIndex => _currentMoveModule;
        public int CurrentJumpModuleIndex => _currentJumpModule;
        public int Team { get; set; }
        public SurfaceFilter groundFilter;
        public ModuleList<BaseMoveModule> MoveModules => new ModuleList<BaseMoveModule>(_moveModules);
        public ModuleList<BaseJumpModule> JumpModules => new ModuleList<BaseJumpModule>(_jumpModules);
        protected void Awake() {
            _rigidbody = GetComponent<Rigidbody2D>();
            _input = new CharacterInput();
            _actions = new ActionManager();
            Animation.EnsureInit();
            if (groundFilter == null) {
                groundFilter = SurfaceFilter.Default;
            }
            for (int i = 0; i < _inputMapper.mappings.Length; i++) {
                _inputMapper.mappings[i].Initialize(this);
            }
            Reset(Mobge.GameSetup.DefaultSetup);
        }
        protected void OnDestroy() {
            for (int i = 0; i < _inputMapper.mappings.Length; i++) {
                _inputMapper.mappings[i].Destyoy();
            }
        }
        public bool ControlModulesEnabled {
            set {
                var c = ControlModules;
                for (int i = 0; i < c.Count; i++) {
                    c[i].Enabled = value;
                }
            }
        }
        public Collider2D BodyCollider {
            get => _body;
            set {
                if (value.gameObject != gameObject) {
                    throw new Exception(nameof(BodyCollider) + " property has to be a collider that is attached to the root game object of the " + GetType());
                }
                _body = value;
            }
        }
        public State BodyState {
            get => _state;
        }
        public ModuleList<IControlModule> ControlModules {
            get {
                return new ModuleList<IControlModule>(_controlModules);
            }
        }
        public void Reset(GameSetup gameSetup, int team = 0) {
            _gameSetup = gameSetup;
            _attackMultiplayer = 1.0f;
            _modifiers.Reset();
            this.Team = team;
            _activeState.InterruptModule(this);
            _health.Current = _health.Max;
            _input.Reset();
            _poise.Reset(_gameSetup.PoiseRecoverRate);
            _groundContact.ground = null;
            _disableWalkTill = 0;
            _lastGroundTime = 0;
            _airJumpCount = int.MaxValue;
            enabled = true;
            _actions.StopAllActions();
            var c = ControlModules;
            for (int i = 0; i < c.Count; i++) {
                c[i].Initialize(this);
            }
        }
        public int CurrentAirJumpCount => _airJumpCount;
        public void AddPropertyModifier(PropertyModifier.IModifierDescription description, Modifier modifier) {
            _modifiers.Ensure();
            _modifiers.AddModifier(this, description, modifier);
        }
        public void AddOrUpdatePropertyModifier() {

        }
        public bool Active {
            get {
                return gameObject.activeSelf;
            }
            set {
                gameObject.SetActive(value);
            }
        }
        public bool OnGroundStanding {
            get {
                return _groundContact.ground && this.groundFilter.IsGroundNormal(_groundContact.normal);
            }
        }
        public bool OnGround {
            get => _groundContact.ground;
        }
        public float MaxHealth {
            get { return _health.Max; }
            set { _health.Max = value; }
        }
        public float Health {
            get { return _health.Current; }
        }
        public float MaxPoise {
            get { return _poise.Max; }
            set { _poise.Max = value; }
        }
        public CharacterInput Input {
            get {
                return _input;
            }
        }
        public IStateModule CurrentState {
            get {
                return _activeState.stt;
            }
        }
        public float LastGroundTime {
            get {
                return _lastGroundTime;
            }
        }
        public GroundContact GroundContact {
            get { return _groundContact; }
        }
        public float Mass { get => _rigidbody.mass; set => _rigidbody.mass = value; }
        public float GravityScale { get => _rigidbody.gravityScale; }
#if UNITY_EDITOR
        public void EditorPrint(object log) {
            if (editorPrints)
                Debug.Log(log + "- " + CurrentVelocity.x, this);
        }
#endif
        public Vector3 CurrentVelocity {
            get => _rigidbody.velocity;
            set {
                _rigidbody.velocity = value;
#if UNITY_EDITOR
                EditorPrint("set velocity: " + value);
#endif
            }
        }
        public float AngularVelocity {
            get => _rigidbody.angularVelocity;
            set {
                _rigidbody.angularVelocity = value;
#if UNITY_EDITOR
                EditorPrint("set angular velocity: " + value);
#endif
            }
        }
        public float PhysicalRotation {
            get => _rigidbody.rotation;
            set => _rigidbody.rotation = value;
        }
        public bool IsRigidbodySleeping {
            get => _rigidbody.IsSleeping();
        }
        public RigidbodyType2D RigidbodyType {
            get => _rigidbody.bodyType;
            set => _rigidbody.bodyType = value;
        }
        public void AddTorque(float torque, ForceMode2D mode = ForceMode2D.Force) {
            _rigidbody.AddTorque(torque, mode);
        }
        public void AddForce(Vector2 force, ForceMode2D mode = ForceMode2D.Force) {
            _rigidbody.AddForce(force, mode);
#if UNITY_EDITOR
            EditorPrint("add force: " + force);
#endif
        }
        public bool Alive { get => _health.Alive; }
        public void WakeRigidbody() {
            _rigidbody.WakeUp();
        }
        public Vector3 TransformPoint(Vector3 point) {
            return transform.TransformPoint(point);
        }
        public Vector3 InverseTransformPoint(Vector3 point) {
            return transform.InverseTransformPoint(point);
        }
        public InputMapper States {
            get {
                return _inputMapper;
            }
        }
        private void EnsureActionIndex(int index) {
            if (_inputMapper.mappings.Length <= index) {
                Array.Resize(ref _inputMapper.mappings, index + 1);
            }
        }
        public void SetModule<T>(int index, T state, int actionIndex = 0) where T : UnityEngine.Object, IStateModule {
            EnsureActionIndex(index);
            _inputMapper.mappings[index].SetModule(this, state);
        }
        public void SetModuleData(int index, StateModuleData data, int actionIndex = 0) {
            EnsureActionIndex(index);
            _inputMapper.mappings[index].SetModuleData(this, data);
        }
        public Vector3 PhysicalPosition {
            get => _rigidbody.position;
            set => _rigidbody.position = value;
        }
        public Vector3 Position {
            get {
                return transform.position;
            }
            set {
                // setting position from rigidbody causes interpolation even documentation claims otherwise
                //_rigidbody.position = value;
                transform.position = value;
                _rigidbody.velocity = Vector2.zero;
            }
        }
        public Vector3 PhysicalCenter {
            get {
                return Bounds.center;
            }
        }
        public Vector3 Center {
            get {
                var s = Size;
                //Debug.DrawLine(transform.position, transform.TransformPoint(new Vector3(0, s.y * 0.5f, 0)));
                return transform.TransformPoint(new Vector3(0, s.y * 0.5f, 0));
            }
        }
        public Vector3 LocalCenter {
            get {
                return new Vector3(0, Size.y * 0.5f, 0);
            }
        }
        public bool IsEnemy(Character2D character) {
            if (!character.Alive) return false;
            return IsEnemy(character.GameSetup, character.Team);
        }
        public bool IsEnemy(GameSetup gameSetup, int team) {
            if (this.GameSetup != gameSetup) {
                return false;
            }
            if (!Alive) return false;
            return this.GameSetup.Teams.IsEnemy(this.Team, team);
        }
        public void Attack(Character2D target, ref DamageData data) {
            target.TakeDamage(data);
        }
        public void TakeDamage(in DamageData damage) {
            if (_damageHandler) {
                if (_damageHandler.HandleDamage(this, ref _health, ref _poise, damage)) {
                    _activeState.SetModule(this, _damageHandler, 0, true);
                }
            }
            else {
                _health.Current -= damage.damage;
                if (!_health.Alive) {
                    _activeState.InterruptModule(this);
                    enabled = false;
                }
                else {
                    if (_poise.DecreasePoise(damage.damage)) {
                        _activeState.InterruptModule(this);
                    }
                }
            }
            if (!_health.Alive) {
                OnDeath?.Invoke(this);
            }
        }
        protected void FixedUpdate() {
            _modifiers.FixedUpdate();
            UpdateControlModules();
            UpdateState();
            UpdateMoveModules();
            UpdateJumpModules();
            _actions.Update(Time.fixedDeltaTime);
            _input.Consumed();
        }
        public ActionManager ActionManager => _actions;
        public Bounds Bounds {
            get {
                return _body.bounds;
            }
        }
        private void UpdateControlModules() {
            var c = ControlModules;
            for (int i = 0; i < c.Count; i++) {
                if (c[i].Enabled) {
                    c[i].UpdateModule(this);
                }
            }
        }
        public bool IsWalkDisabled {
            get {
                return _disableWalkTill > Time.fixedTime;
            }
        }
        private void UpdateMoveModules() {
            int move = -1;
            if (!IsWalkDisabled) {
                for (int i = _moveModules.Length - 1; i >= 0; i--) {
                    var mm = MoveModules[i];
                    if (mm.UpdateModule(this, ref _groundContact, _state.walkMode)) {
                        move = i;
                        break;
                    }
                }
            }
            if (_groundContact.OnGround) {
                _lastGroundTime = Time.fixedTime;
                _airJumpCount = 0;
            }
            SetEnabledModule(MoveModules, ref _currentMoveModule, move);
        }
        private void UpdateJumpModules() {
            int jump = -1;
            for (int i = 0; i < _jumpModules.Length; i++) {
                var jm = JumpModules[i];
                if (jm.UpdateModule(this, ref _airJumpCount, _state.walkMode)) {
                    jump = i;
                    break;
                }
            }
            SetEnabledModule(JumpModules, ref _currentJumpModule, jump);
        }
        private void UpdateState() {
            if (_activeState.stt == null) {
                var mappings = _inputMapper.mappings;
                if (mappings != null) {
                    for (int i = 0; i < mappings.Length; i++) {
                        var b = _input.Actions[i];
                        if (b) {
                            var m = mappings[i];
                            var stt = m.Module;
                            if (stt != null && stt.TryEnable(this, b, m.actionIndex)) {
                                _activeState.SetModule(this, stt, i, true);
                            }
                        }
                    }
                }
            }
            if (_activeState.stt != null) {
                var b = _input.Actions[_activeState.inputIndex];
                var result = _activeState.stt.UpdateModule(this, in b, ref _state);
                if (!result) {
                    _activeState.ReleaseModule(this);
                }
            }
        }
        public void InterruptCurrentState() {
            _activeState.InterruptModule(this);
        }
        public bool TrySetCurrentState(StateModuleData moduleData, int actionIndex = 0) {
            return TrySetCurrentState(moduleData, actionIndex, out IStateModule module);
        }
        public bool TrySetCurrentState(StateModuleData moduleData, int actionIndex, out IStateModule module) {
            module = moduleData.GetModule(this);
            if (module.TryEnable(this, new CharacterInput.Button(), 0)) {
                SetExternalState(module, actionIndex);
                return true;
            }
            return false;
        }
        public bool TrySetCurrentState(IStateModule module, int actionIndex = 0, bool interruptCurrent = true) {
            if (!interruptCurrent) {
                if (CurrentState != null) {
                    return false;
                }
            }
            if (module.TryEnable(this, new CharacterInput.Button(), 0)) {
                SetExternalState(module, actionIndex);
                return true;
            }
            return false;
        }
        private void SetExternalState(IStateModule state, int actionIndex) {

            _activeState.SetModule(this, state, actionIndex, false);
        }
        private void SetEnabledModule<T>(ModuleList<T> modules, ref int currentModule, int next) where T : class, BaseModule {

            if (next != currentModule) {
                if (currentModule >= 0) {
                    modules[currentModule].SetEnabled(this, false);
                }
                //Debug.Log(currentModule + " => " + next + ": " + Time.time);
                currentModule = next;
                if (currentModule >= 0) {
                    modules[currentModule].SetEnabled(this, true);
                }
            }
        }
        public void JumpStart(float airTime = 0.2f) {
            _disableWalkTill = Time.fixedTime + airTime;
            _groundContact.ground = null;
        }
        public ContactPoint2DList GetContacts(ContactFilter2D filter) {
            return new ContactPoint2DList(_rigidbody, in filter);
        }
        public Rigidbody2D FindGround(out Vector2 normal) {
            return groundFilter.FindGround(_rigidbody, out normal);
        }
        public Rigidbody2D FindWallTop(out Vector2 point, out Vector2 normal) {
            return groundFilter.FindWallTop(_rigidbody, out point, out normal);
        }
        public Rigidbody2D FindCeiling(out Vector2 point, out Vector2 normal) {
            return groundFilter.FindCeiling(_rigidbody, out point, out normal);
        }
        public bool IsTouchingGround() {
            return groundFilter.IsTouchingGround(_rigidbody);
        }
        public bool IsTouchingLayers(int layerMask) {
            return groundFilter.IsTouchingLayers(_rigidbody, layerMask);
        }
        public bool IsTouchingLayers() {
            return groundFilter.IsTouchingLayers(_rigidbody, Physics2D.AllLayers);
        }
        public Rigidbody2D FindGroundOnDirection(Vector2 direction, out Vector2 point, out Vector2 normal) {
            return groundFilter.FindGroundOnDirection(_rigidbody, direction, out point, out normal);
        }
        public Collider2dList OverlapAreaForGround(Vector2 point) {
            var pos = _rigidbody.position;
            var offset = point - pos;
            return groundFilter.OverlapBoundsWithOffset(_body, in offset);
        }
        public Vector2 Size {
            get {
                if (_body is CapsuleCollider2D) {
                    return ((CapsuleCollider2D)_body).size;
                }
                if (_body is BoxCollider2D) {
                    return ((BoxCollider2D)_body).size;
                }
                if (_body is CircleCollider2D) {
                    var r = ((CircleCollider2D)_body).radius * 2;
                    return new Vector2(r, r);
                }
                return Vector2.zero;
            }
        }
        /// <summary>
        /// Finds ground for character at specified position.
        /// Returns false if there is no room for character at specified position, returns true otherwise.
        /// Note that the function may return true even if no ground is found.
        /// </summary>
        public bool FindGroundAtPosition(Vector2 position, float checkDistance, out RaycastHit2D result) {
            Vector2 pos = this.Position;
            var dif = position - pos;
            var size = this.Size;
            var offset = _body.offset;
            float rRadius = size.x * 0.5f;
            var vRadius = size.y * 0.5f - rRadius;
            var rTop = new Vector2(offset.x, offset.y + vRadius);
            var rBottom = new Vector2(offset.x, offset.y - vRadius);
            Vector2 top = transform.TransformPoint(rTop);
            Vector2 bottom = transform.TransformPoint(rBottom);
            var bodyVector = top - bottom;
            var bodyHeight = bodyVector.magnitude;
            float radius = bodyVector.y * 0.5f / vRadius * rRadius;
            var r = new Ray2D(top + dif, -bodyVector);
            result = this.groundFilter.CircleCast(in r, radius, bodyHeight + checkDistance);
            if (!result.collider) {
                result.distance = checkDistance;
                return true;
            }
            else {
                result.distance -= bodyHeight;
                return result.distance > 0;
            }
        }

        public float Direction {
            get {
                return transform.localScale.x;
            }
            set {
                if (value == 0) return;
                bool target = value > 0;
                var scl = transform.localScale;
                bool current = scl.x > 0;
                if (target != current) {
                    scl.x = -scl.x;
                    transform.localScale = scl;
                }
            }
        }
        /// <summary>
        /// Try not to use as much as possible. Using this makes debugging harder. Use functions like <see cref="AddForce(Vector2, ForceMode2D)"/>, <see cref="AngularVelocity"/> to manage physical properties of the character.
        /// </summary>
        public Rigidbody2D Rigidbody => _rigidbody;


#if UNITY_EDITOR
        protected void OnDrawGizmosSelected() {
            if (_body == null) {
                _body = GetComponent<CapsuleCollider2D>();
            }
        }
#endif


        public static Character2D FromCollider(Collider2D col) {
            var rb = col.attachedRigidbody;
            return !rb ? null : rb.GetComponent<Character2D>();
        }
        public static Character2D FromRigidbody(Rigidbody2D rb) {
            return rb.GetComponent<Character2D>();
        }
        public struct Collection<T> {
            private T[] _content;
            public Collection(T[] content) {
                _content = content;
            }
            public T this[int index] {
                get { return _content[index]; }
                set { _content[index] = value; }
            }
            public int Count {
                get => _content.Length;
            }
        }
    }

    public struct GroundContact {
        public Rigidbody2D ground;
        public Vector2 normal;
        public bool OnGround {
            get { return ground != null; }
        }
    }
    public enum WalkMode {
        /// Apply walk sequence normally
        Normal = 0,
        /// Apply walk without animation
        NoAnimation = 1,
        /// Do not move or animate, but handle ground interaction
        NoMoveOrAnimate = 2,
    }
}