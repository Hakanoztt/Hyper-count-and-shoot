using System;
using UnityEngine;

namespace Mobge {
    [Obsolete("Use RoutineManager instead")]
    public class ActionManager {
        public delegate void ActionComplete(object data, bool completed);
        public delegate void ActionUpdate(in UpdateParams @params);
        ExposedList<InternalAction> _actions;
        ExposedList<InternalRoutine> _routines;
        private ExposedList<int> _exists;
        private int _indexToAdd, _routineToAdd;
        private int _nextId;
        private int _nextRoutine;
#if UNITY_EDITOR
        public string logStuff;
#endif
        public ActionManager() {
            _actions = new ExposedList<InternalAction>();
            _routines = new ExposedList<InternalRoutine>();
            _exists = new ExposedList<int>();
            //  timelessActions = new List<timelessAction>();
        }
        public void Update(float deltaTime) {
            if (_actions.Count > 0) {
                var array = _actions.array;
                int _count = _actions.Count;
                _exists.ClearFast();
                int max = -1;
                for (int i = 0; i < _count; i++) {
                    if (array[i].id != 0) {
                        _exists.Add(i);
                        max = i;
                    }
                    else {
                        if (_indexToAdd > i) {
                            _indexToAdd = i;
                        }
                    }
                }
                _actions.SetCountFast(max + 1);
                var ea = _exists.array;
                for (int i = 0; i < _exists.Count; i++) {
                    UpdateAction(ref _actions.array[ea[i]], deltaTime);
                }
            }
            if (_routines.Count > 0) {
                _exists.ClearFast();
                var array = _routines.array;
                int count = _routines.Count;
                int max = -1;
                for (int i = 0; i < count; i++) {
                    if (array[i].id != 0) {
                        _exists.Add(i);
                        max = i;
                    }
                    else if (_routineToAdd > i) {
                        _routineToAdd = i;
                    }
                }
                _routines.SetCountFast(max + 1);
                var ea = _exists.array;
                for (int i = 0; i < _exists.Count; i++) {
                    UpdateRoutine(ref _routines.array[ea[i]], deltaTime);
                }
            }
        }
        bool UpdateRoutine(ref InternalRoutine routine, float deltaTime) {
            routine.currentTime += deltaTime;
            routine.updateProgress(routine.currentTime);
            return true;
        }
        bool UpdateAction(ref InternalAction action, float deltaTime) {
            action.currentTime += deltaTime;
            if (action.currentTime >= action.time) {
                action.id = 0;
                if (action.updateProgress != null) {
                    action.updateProgress(new UpdateParams(1, action.data));
                    action.updateProgress = null;
                }
                if (action.onFinish != null) {
                    var a = action.onFinish;
                    action.onFinish = null;
                    a(action.data, true);
                }
                return false;
            }
            else {
                UpdateActionImmediate(in action);
                return true;
            }
        }
        void UpdateActionImmediate(in InternalAction action) {
            if (action.updateProgress != null) {
                action.updateProgress(new UpdateParams {
                    progress = action.currentTime / action.time,
                    data = action.data
                });
            }
        }
        public void Update() {
            Update(Time.deltaTime);
        }

        public void StopAllActions() {
            var array = _actions.array;
            for (int i = 0; i < _actions.Count; i++) {
                array[i].Stop();
            }
            _actions.ClearFast();
            _indexToAdd = 0;



            var rarray = _routines.array;
            for (int i = 0; i < _routines.Count; i++) {
                rarray[i].Stop();
            }
            _routines.Clear();
            _routineToAdd = 0;



#if UNITY_EDITOR
            if (logStuff != null) {
                Debug.Log(logStuff + " stopping all");
            }
#endif
            //timelessActions.Clear();
        }
        public int TotalCount => _actions.Count + _routines.Count;
        public int ActionCount {
            get {
                return _actions.Count;
            }
        }
        public int RoutineCount {
            get {
                return _routines.Count;
            }
        }
        private static int GetIndexToAdd<T>(ExposedList<T> l, ref int indexToAdd) where T : struct {
            var array = l.array;
            int index = indexToAdd;
            if (indexToAdd == l.Count) {
                l.AddFast();

                indexToAdd++;
            }
            else {
                do {
                    indexToAdd++;
                } while (indexToAdd < l.Count && array[indexToAdd].GetHashCode() > 0);
            }
            return index;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="time"></param>
        /// <param name="updateProgress">parameter of this function is (0,1]</param>
        /// <param name="onFinish"></param>
        public Action DoTimedAction(float time, ActionUpdate updateProgress, ActionComplete onFinish = null, object data = null) {
            _nextId++;
            int count = _actions.Count;
            int index = GetIndexToAdd(_actions, ref _indexToAdd);

            var array = _actions.array;

            InternalAction a;
            a.updateProgress = updateProgress;
            a.time = time;
            a.onFinish = onFinish;
            a.currentTime = 0;
            a.id = _nextId;
            a.data = data;
            array[index] = a;

            return new Action(index, _nextId, this);
        }
        public Routine DoRoutine(System.Action<float> updateProgress) {
            _nextRoutine++;
            int index = GetIndexToAdd(_routines, ref _routineToAdd);
            var array = _routines.array;
            InternalRoutine r;
            r.id = _nextRoutine;
            r.updateProgress = updateProgress;
            r.currentTime = 0;
            array[index] = r;
            return new Routine(index, _nextRoutine, this);
        }
        private struct InternalRoutine
        {
            public int id;
            public float currentTime;
            public System.Action<float> updateProgress;

            internal void Stop() {
                updateProgress = null;
                id = 0;
            }
            public override int GetHashCode() => id;
        }
        private struct InternalAction
        {

            public int id;
            public float currentTime;
            public float time;
            public ActionUpdate updateProgress;
            public ActionComplete onFinish;
            public object data;

            public void Stop() {
                Reset();
            }

            public override int GetHashCode() => id;
            internal void Reset() {
                var a = onFinish;
                onFinish = null;
                if (a != null) {
                    a(data, false);
                }
                updateProgress = null;
                id = 0;
            }

            public bool IsFinished {
                get {
                    return currentTime >= time;
                }
            }
        }
        public struct Action
        {
            private int _index;
            private int _id;
            private ActionManager _manager;
            public int Index => _index;

            internal Action(int index, int id, ActionManager manager) {
                _index = index;
                _id = id;
                _manager = manager;
            }
            public bool IsFinished() {
                return _id == 0 || _manager._actions.array[_index].id != _id;
            }
            private bool InternalStop(ref InternalAction a) {
                if (a.id != _id) return false;
                _id = 0;
                _manager = null;
                a.Stop();
                return true;
            }
            public bool Stop() {
                if (_id == 0) return false;
                return InternalStop(ref _manager._actions.array[_index]);
            }

            public bool UpdateImmediate() {
                if (IsFinished()) {
                    return false;
                }
                _manager.UpdateActionImmediate(_manager._actions.array[_index]);
                return true;
            }

            public object Data {
                get {
                    if (IsFinished()) {
                        return null;
                    }
                    return _manager._actions.array[_id].data;
                }
                set {
                    if (IsFinished()) {
                        return;
                    }
                    _manager._actions.array[_id].data = value;
                }
            }
            public ActionManager.ActionComplete OnFinish {
                get {
                    if (!IsFinished()) {
                        return _manager._actions.array[_id].onFinish;
                    }
                    return null;
                }
                set {
                    if (IsFinished()) {
                        return;
                    }
                    _manager._actions.array[_id].onFinish = value;
                }
            }
        }


        public struct Routine
        {
            private int _index;
            private int _id;
            private ActionManager _manager;
            internal Routine(int index, int id, ActionManager manager) {
                _index = index;
                _id = id;
                _manager = manager;
            }
            public bool IsFinished() {
                return _id == 0 || _manager._routines.array[_index].id != _id;
            }
            public bool Stop() {
                if (_id == 0) return false;
                return InternalStop(ref _manager._routines.array[_index]);

            }

            private bool InternalStop(ref InternalRoutine internalRoutine) {
                if (internalRoutine.id != _id) return false;
                internalRoutine.Stop();
                _manager = null;
                _id = 0;
                return true;
            }
            public void EnsureRunning(ActionManager manager, System.Action<float> update) {
                if (IsFinished()) {
                    this = manager.DoRoutine(update);
                }
            }
        }
        public struct UpdateParams
        {
            public float progress;
            public object data;

            public UpdateParams(float dt, object data) : this() {
                this.progress = dt;
                this.data = data;
            }
        }
    }
}