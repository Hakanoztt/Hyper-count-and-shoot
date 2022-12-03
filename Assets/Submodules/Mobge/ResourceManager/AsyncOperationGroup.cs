using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Mobge {
    public class AsyncOperationGroup
    {
        private int _completedCount;
        private bool _started;
        private bool _failed;
        private List<AsyncOperationHandle> _operations = new List<AsyncOperationHandle>();
        private Action<AsyncOperationGroup> _completed;
        private AsyncOperationStatus _status;
        private Exception _exception;

        public int OperationCount => _operations.Count;
        public AsyncOperationHandle GetOperation(int index) {
            return _operations[index];
        }

        public static AsyncOperationGroup New() {
            return new AsyncOperationGroup();
        }
        private AsyncOperationGroup() {
        }
        protected void Destroy() {
            _completedCount = 0;
            _started = false;
            _failed = false;
            _operations.Clear();
        }
        public void Add<T>(AsyncOperationHandle<T> operation) {
            _operations.Add(operation);
            operation.CompletedTypeless += OperationCompleted;
            UpdateStatus(false);
        }
        public void Start() {
            Execute();
        }
        protected void Execute()
        {
            _started = true;
            UpdateStatus(false);
        }
        private void OperationCompleted(AsyncOperationHandle operation) {
            _completedCount++;
            var fail = operation.Status == AsyncOperationStatus.Failed;
            if(fail) {
                _exception = operation.OperationException;
            }
            UpdateStatus(fail);
        }
        public float Progress {
            get{
                if(!_started) {
                    return 0f;
                }
                if(_operations.Count == 0){
                    return 1f;
                }
                float percentage = 0f;
                for(int i = 0; i < _operations.Count; i++) {
                    percentage += _operations[i].PercentComplete;
                }
                return percentage / _operations.Count;
            }
        }
        public event Action<AsyncOperationGroup> OnCompleted {
            add {
                _completed += value;
            }
            remove {
                _completed -= value;
            }
        }
        private void UpdateStatus(bool fail) {
            if(!_failed) {
                if(fail){
                    _failed = true;
                }
            }
            if(_started) {
                // Debug.Log(_completedCount + " " + _operations.Count);
                if(_completedCount == _operations.Count) {
                    _status = _failed ? AsyncOperationStatus.Failed : AsyncOperationStatus.Succeeded;
                    if(_completed != null) {
                        _completed(this);
                    }
                }
            }
        }
        public Exception Exception => _exception;
        public AsyncOperationStatus Status {
            get => _status;
        }
        public override string ToString() {
            return GetType() + ": " + _operations.Count;
        }

    }
}