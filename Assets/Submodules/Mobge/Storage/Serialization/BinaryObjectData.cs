using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mobge.Serialization;

namespace Mobge.Serialization {
    [Serializable]
    public struct BinaryObjectData
    {
        
        public byte[] data;
        public UnityEngine.Object[] targets;
        public BinaryObjectData(object o, BinaryDeserializer.Formatter formatter = default(BinaryDeserializer.Formatter)) {
            var t = o == null ? typeof(object) : o.GetType();
            this = BinarySerializer.Instance.Serialize(o.GetType(), o, formatter);
        }
        public BinaryObjectData(Type type, object o, BinaryDeserializer.Formatter formatter = default(BinaryDeserializer.Formatter)) {
            this = BinarySerializer.Instance.Serialize(type, o, formatter);
        }
        public T GetObject<T>() {
            return BinaryDeserializer.Instance.Deserialize<T>(this);
        }
        public object GetObject(Type type, BinaryDeserializer.Formatter formatter = default(BinaryDeserializer.Formatter)) {
            
            return BinaryDeserializer.Instance.Deserialize(this, type, formatter); ;
        }
        public void UpdateValues(object target, BinaryDeserializer.Formatter formatter = default(BinaryDeserializer.Formatter)) {
            BinaryDeserializer.Instance.DeserializeTo(this, target, formatter);
        }
    }
}