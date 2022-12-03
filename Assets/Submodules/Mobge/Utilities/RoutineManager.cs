using System;
using UnityEngine;

namespace Mobge {
    public class RoutineManager {
        private AutoIndexedMap<InternalRoutine> _routines = new AutoIndexedMap<InternalRoutine>();
        private int _uniqueIdCounter = 0;
        private float _currentTime = 0f;
        public delegate void RoutineFinish(bool complete, object data);
        public delegate void RoutineUpdate(float progress, object data);
        public int RoutineCount => _routines.Count;

        private float _deltaTime;

        public void Update() {
            Update(Time.deltaTime);
        }
        public void Update(float deltaTime) {
            _deltaTime = deltaTime;
            _currentTime += deltaTime;
            var e = _routines.GetPairEnumerator(); while (e.MoveNext()) {
                var pair = e.Current;
                var internalRoutine = pair.Value;
                internalRoutine.Update();
            }
        }

        internal void DoAction(Action<bool, object> p, object setActiveDelay) {
            throw new NotImplementedException();
        }

        public Routine DoAction(RoutineFinish onFinish, float time = 0f, RoutineUpdate updateProgress = null, object data = null) {
            return DoRoutine(updateProgress, time, onFinish, data);
        }
        public Routine DoRoutine(RoutineUpdate updateProgress, float duration = -1f, RoutineFinish onFinish = null, object data = null) {
            var routine = new InternalRoutine() {
                uniqueId = _uniqueIdCounter++,
                updateProgress = updateProgress,
                duration = duration,
                onFinish = onFinish,
                startTime = _currentTime,
                data = data,
                routineManager = this,
            };
            var id = _routines.AddElement(routine);
            routine.id = id;
            _routines[id] = routine;
            return new Routine(routine.uniqueId, id, this);
        }


        public void StopAllRoutines() {
            var e = _routines.GenericEnumerator();
            while (e.MoveNext()) {
                e.Current.Stop(false);
            }
            e.Dispose();
        }
        private struct InternalRoutine {
            public int id;
            public int uniqueId;
            public float startTime;
            public float duration;
            public RoutineUpdate updateProgress;
            public RoutineFinish onFinish;
            public object data;
            public RoutineManager routineManager;
            private float CurrentTime => routineManager._currentTime - startTime;
            public bool IsFinished => duration >= 0 && CurrentTime >= duration;
            public float Progress => duration > 0 ? Mathf.Clamp01(CurrentTime / duration) : CurrentTime;
            public void Stop(bool completed) {
                routineManager._routines.RemoveElement(id);
                updateProgress = null;
                if (onFinish != null) {
                    var a = onFinish;
                    onFinish = null;
                    a(completed, data);
                }
            }
            public void Update() {
                if (updateProgress != null) {
                    updateProgress(Progress, data);
                }
                if (IsFinished) {
                    Stop(true);
                    return;
                }
            }
        }

        public struct Routine {
            internal int _uniqueId;
            internal int _index;
            private RoutineManager _manager;

            public RoutineManager Manager => _manager;
            public float DeltaTime => _manager._deltaTime;
            internal Routine(int uniqueId, int index, RoutineManager manager) {
                _uniqueId = uniqueId;
                _index = index;
                _manager = manager;
            }
            private bool TryGetInternalRoutine(out InternalRoutine internalRoutine) {
                if (_manager == null) {
                    internalRoutine = default;
                    return false;
                }
                if (!_manager._routines.ContainsKey(_index)) {
                    internalRoutine = default;
                    return false;
                }
                internalRoutine = _manager._routines[_index];
                if (internalRoutine.uniqueId != _uniqueId) return false;
                return true;
            }
            public bool IsFinished {
                get {
                    if (!TryGetInternalRoutine(out var internalRoutine)) return true;
                    return internalRoutine.IsFinished;
                }
            }
            public float Progress {
                get {
                    if (!TryGetInternalRoutine(out var internalRoutine)) return 0f;
                    return internalRoutine.Progress;
                }
            }
            public void Stop() {
                if (!TryGetInternalRoutine(out var internalRoutine)) return;
                internalRoutine.Stop(false);
            }
            public RoutineFinish OnFinish {
                get {
                    if (!TryGetInternalRoutine(out var internalRoutine)) return null;
                    if (internalRoutine.IsFinished) return null;
                    return internalRoutine.onFinish;
                }
                set {
                    if (!TryGetInternalRoutine(out var internalRoutine)) return;
                    if (internalRoutine.IsFinished) return;
                    internalRoutine.onFinish = value;
                    _manager._routines[_index] = internalRoutine;
                }
            }
        }
    }
}