using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Mobge.Serialization {

    public class GameProgressData<T> : GameProgressData where T : class {

        private List<Action<T>> _listeners;

        public GameProgressData(RoutineManager routineManager, string filePath) : base(routineManager, typeof(T), filePath) {}

        public void RequestValue(Action<T> onReady) {
            if (_listeners == null) {
                _listeners = new List<Action<T>>();
            }
            _listeners.Add(onReady);
            base.RequestValue(FireListeners);
        }

        private void FireListeners(object obj) {
            T t = (T)obj;
            for (int i = 0; i < _listeners.Count; i++) {
                _listeners[i](t);
            }
            _listeners.Clear();
        }

        public new T ValueUnsafe { get => (T)base.ValueUnsafe; }
    }

    public class GameProgressData {

        public BinarySerializationBase.Formatter formatter;
        public Type DataType { get; private set; }

        protected string _path;
        private object _t;
        private bool _directoryEnsured = false;
        private List<Action<object>> _listeners;

        public bool IsReady { get => true; }

        public float cooldown = 0;

        private string _backupSavePath => _path + "_bak";
        private RoutineManager _routineManager;
        private RoutineManager.Routine _saveRoutine;


        public GameProgressData(RoutineManager routineManager, Type dataType, string filePath) {
            _routineManager = routineManager;
            DataType = dataType;
            _path = Path.Combine(Application.persistentDataPath, filePath);
            EnsureDirectory();
            _t = ReadFile(out bool @new);

            _routineManager.DoAction(InitializationFinished,0,null, @new);
        }

        private void InitializationFinished(bool complete, object data) {
            if (complete) {
                BecomeReady((bool)data);
            }
        }

        public virtual void BecomeReady(bool dataNewlyCreated) {

        }

        public void RequestValue(Action<object> onReady) {
            if (_listeners == null) {
                _listeners = new List<Action<object>>();
            }
            _listeners.Add(onReady);
            FireWaitingListeners(_t);
        }

        protected void FireWaitingListeners(object o) {
            for(int i = 0; i < _listeners.Count; i++) {
                _listeners[i](_t);
            }
            _listeners.Clear();
        }

        public object ValueUnsafe { get => _t; }

        public void SaveValue(object t) {
            //Debug.Log("save");
            _t = t;
            if (cooldown > 0) {
                if (_saveRoutine.IsFinished) {
                    _saveRoutine = _routineManager.DoAction(SaveCooldownOnFinish, cooldown);
                }
            } else {
                WriteToDisk();
            }
        }

        private void SaveCooldownOnFinish(bool complete, object data) => WriteToDisk();

        public void ForceSave() {
            if(!_saveRoutine.IsFinished) {
                _saveRoutine.Stop();
            }
        }

        public void WriteToDisk() {
            //Debug.Log("write");
            if (File.Exists(_path)) {
                //Debug.Log("backup");
                File.Delete(_backupSavePath);
                File.Move(_path, _backupSavePath);
            }

            //Debug.Log("save to disk");
            var bod = BinarySerializer.Instance.Serialize(DataType, _t, formatter);
            File.WriteAllBytes(_path, bod.data);

            if (!File.Exists(_backupSavePath)) {
                //Debug.Log("create backup");
                File.WriteAllBytes(_backupSavePath, bod.data);
            }
        }

        private object ReadFile(out bool @new) {
            @new = false;
            //Debug.Log("read");
            object t = null;
            if (File.Exists(_path)) {
                try {
                    BinaryObjectData bod = new BinaryObjectData { data = File.ReadAllBytes(_path) };
                    t = BinaryDeserializer.Instance.Deserialize(bod, DataType, formatter);
                } catch {
                    //Debug.Log("corrupt");
                    File.Delete(_path);
                    File.Copy(_backupSavePath, _path);
                    return ReadFile(out @new);
                }
            }
            if (t == null) {
                //Debug.Log("null");
                t = CreateDefaultData();
                @new = true;
            }
            return t;
        }
        public virtual object CreateDefaultData() {
            return Activator.CreateInstance(DataType);
        }
        private void EnsureDirectory() {
            if (!_directoryEnsured) {
                _directoryEnsured = true;
                var d = Directory.GetParent(_path);
                Directory.CreateDirectory(d.FullName);
            }
        }
    }
}