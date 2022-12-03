using System;

namespace Mobge {

    [Serializable]
    public class DataRef {

        [Obsolete("Don't use _data, use Value or GetValue")]
        public _Data _data;
        [Obsolete("Don't use _index, use Value or GetValue")]
        public int _index;

        public object Value => _data.GetValueOf(_index);
        public T GetValue<T>() => (T)Value;
        public bool IsValid() => _data != null && _data.HasIndex(_index);
        public override string ToString() {
            if (IsValid()) {
#if UNITY_EDITOR
                return _data.name + "/" + _data.GetNameOf(_index);
#else
                return _data.name + " : " + _index;
#endif
            } else {
                return "NULL DATA!!";
            }
        }
    }
}
