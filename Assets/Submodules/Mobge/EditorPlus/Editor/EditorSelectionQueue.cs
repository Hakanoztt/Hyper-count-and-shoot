using System.Collections.Generic;

namespace Mobge{
    public class EditorSelectionQueue<T>{
        private Dictionary<T, long> _priorities = new Dictionary<T, long>();
        private T _current;
        private long _currentPriority = 0;
        private long _nextPriority = 0;

        public void AddCandidate(T candidate) {
            long index;
            if(_priorities.TryGetValue(candidate, out index)){
                if(index < _currentPriority) {
                    _currentPriority = index;
                    _current = candidate;
                }
            }
            else{
                _currentPriority = -1;
                    _current = candidate;
            }
        }
        public bool SelectIfEquals(T checkObj, out T obj, bool silent = false) {
            return Select(true, checkObj, out obj, silent);
        }
        private bool Select(bool checkEquality, T checkObj, out T obj, bool silent = false) {
            if(checkEquality){
                if(typeof(T).IsClass){
                    if(((object)_current) == null){
                        obj = default(T);
                        return false;
                    }
                }
                if(!_current.Equals(checkObj)){
                    obj = default(T);
                    return false;
                }
            }
            obj = _current;
            _current = default(T);
            
            if(_currentPriority< 0 || _currentPriority < _nextPriority) {
                if(!silent) {
                    _priorities[obj] = _nextPriority;
                    _nextPriority++;
                }
                _currentPriority = _nextPriority;
                return true;
            }
            return false;
        }
        public bool SelectOne(out T obj, bool silent = false) {
            return Select(false, default(T), out obj, silent);
        }
        public void Clear(){
            _priorities.Clear();
        }
    }
}