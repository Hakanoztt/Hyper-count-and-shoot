using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using UnityEngine;

namespace Mobge.Serialization {
    public class BinaryDeserializer : BinarySerializationBase
    {

        private static BinaryDeserializer _instance;
        public static BinaryDeserializer Instance {
            get{
                if(_instance == null) {
                    _instance = new BinaryDeserializer();
                }
                return _instance;
            }
        }
        public static void DestroyInstance() {
            _instance = null;
        }
        private byte[] _bytes;
        private BinaryObjectData _data;
        private int _position;
        private Stack<Dictionary<string, FieldInfo>> _fastFields;
        public int Position {
            get => _position;
        }
        public BinaryDeserializer() {
            _fastFields = new Stack<Dictionary<string, FieldInfo>>();
        }
        private Dictionary<string, FieldInfo> NewFastField() {
            if(_fastFields.Count == 0) {
                return new Dictionary<string, FieldInfo>();
            }
            return _fastFields.Pop();
        }
        private void DestroyFastField(Dictionary<string, FieldInfo> f) {
            f.Clear();
            _fastFields.Push(f);
        }
        public T Deserialize<T>(BinaryObjectData data) {
            var type = typeof(T);
            var t = Deserialize(data, type);
            return t == null ? default(T) : (T)t;
        }
        private void PreDeserialize(BinaryObjectData data) {
            _data = data;
            _bytes = data.data;
        }
        private void PostDeserialize() {
            _position = 0;
            _data = default(BinaryObjectData);
            _bytes = null;
        }
        public void Reset() {
            PostDeserialize();
        }
        public object Deserialize(BinaryObjectData data, Type type, Formatter formatter = default(Formatter)) {
            PreDeserialize(data);
            object t = null;
            try {
                t = Read(type, formatter);
            } finally {
                PostDeserialize();
            }
            return t;
        }
        public void DeserializeTo(BinaryObjectData data, object target, Formatter formatter = default(Formatter)) {
            PreDeserialize(data);
            var stype = ReadByte();
            if(stype != Code.Object) {
                throw new SerializationException("Specified data is not an object. " + nameof(DeserializeTo) + " method only works when data is an object.");
            }
            try {
                ReadObject(target.GetType(), target, formatter);
            } finally {
                PostDeserialize();
            }
        }
        private byte ReadByte() {
            var b = _bytes[_position];
            _position++;
            return b;
        }
        private short ReadInt16() {
            int b0 = _bytes[_position];
            _position++;
            int b1 = _bytes[_position];
            _position++;
            return (short)(b0 | (b1<<8));
        }
        private int ReadInt32() {
            int b0 = _bytes[_position];
            _position++;
            int b1 = _bytes[_position];
            _position++;
            int b2 = _bytes[_position];
            _position++;
            int b3 = _bytes[_position];
            _position++;
            return (b0 | (b1<<8) | (b2<<16) | (b3<<24));
        }
        private int Read7BitEncodedInt() {
            int num = 0;
			int num2 = 0;
            do {
                byte b = this.ReadByte();
                num |= (int)(b & 127) << num2;
                num2 += 7;
                if ((b & 128) == 0) {
                    return num;
                }
            }
            while (num2 != 35);
			throw new FormatException("Bad 7 bit encoded int.");
        }
        private long ReadInt64() {
            // ulong l1 = (ulong)ReadInt32();
            // ulong l2 = ((ulong)ReadInt32()) << 32;
            // return (long)(l1 | l2);

            long b0 = _bytes[_position];
            _position++;
            long b1 = _bytes[_position];
            _position++;
            long b2 = _bytes[_position];
            _position++;
            long b3 = _bytes[_position];
            _position++;
            long b4 = _bytes[_position];
            _position++;
            long b5 = _bytes[_position];
            _position++;
            long b6 = _bytes[_position];
            _position++;
            long b7 = _bytes[_position];
            _position++;
            return (b0 | (b1<<8) | (b2<<16) | (b3<<24) |  (b4<<32) | (b5<<40) | (b6<<48) |  (b7<<56));
        }
        private unsafe float ReadSingle() {
            uint i = (uint)ReadInt32();
            return *(float*)(&i);
        }
        private unsafe double ReadDouble() {
            ulong i = (ulong)ReadInt64();
            return *(double*)(&i);
        }
        private string ReadString() {
            int count = Read7BitEncodedInt();
            var s = _encoding.GetString(_bytes, _position, count);
            _position += count;
            return s;
        }
        private object Read(Type type, in Formatter formatter) {
            var sType = ReadByte();
            switch (sType) {
                case Code.Null:
                    return Default(type);
                case Code.Byte:
                    byte b = ReadByte();
                    return Convert(type, b, b);
                case Code.Int16:
                    Int16 i16 = ReadInt16();
                    return Convert(type, i16, i16);
                case Code.Int32:
                    Int32 i32 = ReadInt32();
                    return Convert(type, i32, i32);
                case Code.Int64:
                    Int64 i64 = ReadInt64();
                    return Convert(type, i64, i64);
                case Code.Single:
                    Single flt = ReadSingle();
                    return Convert(type, (long)flt, flt);
                case Code.Double:
                    Double dbl = ReadDouble();
                    return Convert(type, (long)dbl, dbl);
                case Code.String:
                    var str = ReadString();
                    return type.IsAssignableFrom(typeof(string)) ? str : Default(type);
                case Code.Array:
                    int length = Read7BitEncodedInt();
                    if (type.IsArray) {
                        var elementType = type.GetElementType();
                        var array = Array.CreateInstance(elementType, length);
                        for (int i = 0; i < length; i++) {
                            var e = Read(elementType, formatter);
                            if(e!=null&& elementType.IsAssignableFrom(e.GetType())) {
                                array.SetValue(e, i);
                            }
                        }
                        return array;
                    }
                    else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>)) {
                        IList l = (IList)NewInstance(type);
                        var gType = type.GetGenericArguments()[0];
                        for (int i = 0; i < length; i++) {
                            l.Add(Read(gType, formatter));
                        }
                        return l;
                    }
                    else {
                        for (int i = 0; i < length; i++) {
                            Read(typeof(object), formatter);
                        }
                    }
                    return Default(type);
                case Code.Dictionary:
                    int count = Read7BitEncodedInt();
                    if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>)) {
                        IDictionary d = (IDictionary)NewInstance(type);
                        var gargs = type.GetGenericArguments();
                        var kType = gargs[0];
                        var vType = gargs[1];
                        for (int i = 0; i < count; i++) {
                            d.Add(Read(kType, formatter), Read(vType, formatter));
                        }
                        return d;
                    }
                    else {
                        for (int i = 0; i < count; i++) {
                            Read(typeof(object), formatter);
                            Read(typeof(object), formatter);
                        }
                    }
                    break;
                case Code.Object:
                    return ReadObject(type, formatter);
                case Code.UnityObject:
                    int index = Read7BitEncodedInt();
                    if (typeof(UnityEngine.Object).IsAssignableFrom(type)) {
                        return _data.targets[index];
                    }
                    return Default(type);
            }
            return null;
        }
        private void ReadObject(Type t, object target, Formatter formatter) {
            var count = Read7BitEncodedInt();
            if (formatter.surrogates.TryGetSurrogate(t, out ISurrogate surrogate)) {
                var o = formatter.surrogates.Objects;
                for (int i = 0; i < count; i++) {
                    var name = ReadString();
                    var obj = Read(surrogate.FieldType(name), default(Formatter));
                    o[name] = obj;
                }
                surrogate.Deserialize(ref target, o);
                o.Clear();
                return;
            }
            FieldInfo[] fields;
            if (!TryGetFields(t, out fields)) {
                fields = new FieldInfo[0];
            }
            var fastFields = NewFastField();
            for (int i = 0; i < fields.Length; i++) {
                var f = fields[i];
                fastFields[f.Name] = f;
            }
            for (int i = 0; i < count; i++) {
                var name = ReadString();
                FieldInfo fi;
                if (fastFields.TryGetValue(name, out fi)) {
                    var o = Read(fi.FieldType, formatter);
                    if (fi.FieldType.IsClass) {
                        fi.SetValue(target, fi.FieldType.IsInstanceOfType(o) ? o : null);
                    }
                    else {
                        fi.SetValue(target, o);
                    }

                }
                else {
                    Read(typeof(object), formatter);
                }
            }
            DestroyFastField(fastFields);
        }
        private void ReadObject(object target, in Formatter formatter) {
            var type = target.GetType();
            ReadObject(type, target, formatter);
        }
        private object ReadObject(Type t, in Formatter formatter) {

            var target = NewUninitialized(t);
            ReadObject(t, target, formatter);
            return target;
        }
        private object NewInstance(Type t) {
            return Activator.CreateInstance(t);
        }
        private object NewUninitialized(Type t) {
            if(t.IsValueType) {
                return Activator.CreateInstance(t);
            }

            return Activator.CreateInstance(t);
            //return FormatterServices.GetUninitializedObject(t);
        }
        private object Default(Type t) {
            if(t.IsValueType) {
                return Activator.CreateInstance(t);
            }
            return null;
        }
        private object Convert(Type t, long l, double d) {
            if(t.IsEnum) {
                return (Int32)l;
            }
            if(!t.IsPrimitive) return Default(t);
            if(t == typeof(Int32)) {
                return (Int32)l;
            }
            if(t == typeof(Single)) {
                return (float)d;
            }
            if(t == typeof(Byte)) {
                return (Byte)l;
            }
            if(t == typeof(Int16)) {
                return (Int16)l;
            }
            if(t == typeof(UInt16)) {
                return (UInt16)l;
            }
            if(t == typeof(UInt32)) {
                return (UInt32)l;
            }
            if(t == typeof(Int64)) {
                return (Int64)l;
            }
            if(t == typeof(UInt64)) {
                return (UInt64)l;
            }
            if(t == typeof(Double)) {
                return d;
            }
            if(t == typeof(Boolean)) {
                return ((Byte)l) != 0;
            }
            {
                return Default(t);
            }
        }
        private struct Functions {
            internal BinaryDeserializer _deserializer;
            public Byte ReadByte() {
                return _deserializer.ReadByte();
            }
            public Int16 ReadInt16() {
                return _deserializer.ReadInt16();
            }
            public Int32 ReadInt32() {
                return _deserializer.ReadInt32();
            }
            public Int32 Read7BitEncodedInt() {
                return _deserializer.Read7BitEncodedInt();
            }
            public Int64 ReadInt64() {
                return _deserializer.ReadInt64();
            }
            public Single ReadSingle() {
                return _deserializer.ReadSingle();
            }
            public Double ReadDouble() {
                return _deserializer.ReadDouble();
            }
            public String ReadString() {
                return _deserializer.ReadString();
            }
        }
    }
}
