using Mobge;
using Mobge.Serialization;
using UnityEngine;


namespace Mobge {
    public class SettingDataHolder : ScriptableObject {
        public BinaryObjectData data;
        public T GetObject<T>() {
            return data.GetObject<T>();
        }
        public void SetData<T>(T value) {
            data = new BinaryObjectData(typeof(T), value);
        }
    }
}
