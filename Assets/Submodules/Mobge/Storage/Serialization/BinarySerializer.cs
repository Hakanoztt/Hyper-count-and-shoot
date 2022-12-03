using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace Mobge.Serialization {
    public class BinarySerializer : BinarySerializationBase
    {
        private static BinarySerializer _instance;
        public static BinarySerializer Instance {
            get{
                if(_instance == null) {
                    _instance = new BinarySerializer();
                }
                return _instance;
            }
        }
        public static void DestroyInstance() {
            _instance = null;
        }
        private List<UnityEngine.Object> _objects;
        private byte[] _buffer;
        private int _position;

        public BinarySerializer() {
            _buffer = new byte[1024];
            _objects = new List<UnityEngine.Object>();
        }
        private byte[] CurrentArray {
            get {
                byte[] bs = new byte[_position];
                Array.Copy(_buffer, bs, _position);
                return bs;
            }
        }
        private BinaryObjectData PostSerialize() {
            BinaryObjectData cd;
            cd.targets = _objects.ToArray();
            cd.data = CurrentArray;
            _position = 0;
            
            _objects.Clear();
            return cd;
        }
        public BinaryObjectData Serialize(Type type, object value, Formatter formatter = default(Formatter)) {
            Write(type, value, formatter);
            return PostSerialize();
        }
        private void Write(Type type, object value, Formatter formatter) {
            if(value == null || value.Equals(null)) {
                Write(Code.Null);
            }
            else{
                if(type.IsPrimitive) {
                    if(type == typeof(Byte)) {
                        Write(Code.Byte);
                        Write((Byte)value);
                    } 
                    else if(type == typeof(Int16)) {
                        Write(Code.Int16);
                        Write((Int16)value);
                    } 
                    else if(type == typeof(UInt16)) {
                        Write(Code.Int16);
                        Write((Int16)(UInt16)value);
                    } 
                    else if(type == typeof(Int32)) {
                        Write(Code.Int32);
                        Write((Int32)value);
                    } 
                    else if(type == typeof(UInt32)) {
                        Write(Code.Int32);
                        Write((Int32)(UInt32)value);
                    } 
                    else if(type == typeof(Int64)) {
                        Write(Code.Int64);
                        Write((Int64)value);
                    } 
                    else if(type == typeof(UInt64)) {
                        Write(Code.Int64);
                        Write((Int64)(UInt64)value);
                    } 
                    else if(type == typeof(Single)) {
                        Write(Code.Single);
                        Write((Single)value);
                    } 
                    else if(type == typeof(Double)) {
                        Write(Code.Double);
                        Write((Double)value);
                    }
                    else if(type == typeof(Boolean)) {
                        Write(Code.Byte);
                        Write((byte)((bool)value ? 1 : 0));
                    }
                }
                else if(type == typeof(String)) {
                    Write(Code.String);
                    Write((String)value);
                }
                else if(type.IsEnum) {
                    Write(Code.Int32);
                    Write((Int32)value);
                }
                else if(typeof(UnityEngine.Object).IsAssignableFrom(type)) {
                    Write(Code.UnityObject);
                    Write7BitEncodedInt(_objects.Count);
                    _objects.Add((UnityEngine.Object)value);
                }
                else if(type.IsArray) {
                    Write(Code.Array);
                    var array = (Array)value;
                    if(array.Rank == 1) {
                        Write7BitEncodedInt(array.Length);
                        var elementType = type.GetElementType();
                        for(int i = 0; i < array.Length; i++) {
                            Write(elementType, array.GetValue(i), formatter);
                        }
                    }
                    else{
                        Write7BitEncodedInt(0);
                    }
                }
                else if(type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>)) {
                    var collection = (IList)value;
                    var gType = type.GetGenericArguments()[0];
                    Write(Code.Array);
                    var c = collection.Count;
                    Write7BitEncodedInt(collection.Count);
                    for (int i = 0; i < c; i++) {
                        Write(gType, collection[i], formatter);
                    }
                }
                else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>)) {
                    var collection = (IDictionary)value;
                    var gargs = type.GetGenericArguments();
                    var kType = gargs[0];
                    var vType = gargs[1];
                    Write(Code.Dictionary);
                    Write7BitEncodedInt(collection.Count);
                    var e = collection.GetEnumerator();
                    while (e.MoveNext()) {
                        Write(kType, e.Key, formatter);
                        Write(vType, e.Value, formatter);
                    }
                }
                else if(WriteObject(value, type, formatter)) {
                }
                else {
                    Write(Code.Null);
                }
            }
        }
        private void Write7BitEncodedInt(int value)
		{
			uint num;
			for (num = (uint)value; num >= 128u; num >>= 7)
			{
				this.Write((byte)(num | 128u));
			}
			this.Write((byte)num);
		}
        
        private void EnsureAdd(int size) {
            if(_position + size > _buffer.Length) {
                Array.Resize(ref _buffer, _buffer.Length * 2 + size);
            }
        }
        private void Write(byte b) {
            EnsureAdd(1);
            _buffer[_position] = b;
            _position++;
        }
        private void Write(short s) {
            EnsureAdd(2);
            _buffer[_position] = (byte)s;
            _position++;
            _buffer[_position] = (byte)(s>>8);
            _position++;
        }
        private void Write(int i) {
            EnsureAdd(4);
            _buffer[_position] = (byte)i;
            _position++;
            _buffer[_position] = (byte)(i>>8);
            _position++;
            _buffer[_position] = (byte)(i>>16);
            _position++;
            _buffer[_position] = (byte)(i>>24);
            _position++;
        }
        private void Write(long l) {
            EnsureAdd(8);
            _buffer[_position] = (byte)l;
            _position++;
            _buffer[_position] = (byte)(l>>8);
            _position++;
            _buffer[_position] = (byte)(l>>16);
            _position++;
            _buffer[_position] = (byte)(l>>24);
            _position++;
            _buffer[_position] = (byte)(l>>32);
            _position++;
            _buffer[_position] = (byte)(l>>40);
            _position++;
            _buffer[_position] = (byte)(l>>48);
            _position++;
            _buffer[_position] = (byte)(l>>56);
            _position++;
        }
        private unsafe void Write(float value) {
			uint i = *(uint*)(&value);
            Write((int)i);
        }
        private unsafe void Write(double value) {
            long l = (*(long*)(&value));
            Write(l);
        }
        private void Write(string s) {
            int c = _encoding.GetByteCount(s);
            Write7BitEncodedInt(c);
            EnsureAdd(c);
            _position += _encoding.GetBytes(s, 0, s.Length, _buffer, _position);
        }
        private bool WriteObject(object o, Type type, Formatter formatter) {
            if(formatter.surrogates.TryGetSurrogate(type, out ISurrogate surrogate)) {
                var objs = formatter.surrogates.Objects;
                surrogate.Serialize(o, objs);
                Write(Code.Object);
                Write7BitEncodedInt(objs.Count);
                var e = objs.GetEnumerator();
                while(e.MoveNext()) {
                    var p = e.Current;
                    var value = p.Value;
                    if(value == null) {
                        continue;
                    }
                    Write(p.Key);
                    Write(surrogate.FieldType(p.Key), value, default(Formatter));
                }
                objs.Clear();
                return true;
            }
            FieldInfo[] fields;
            if(!TryGetFields(type, out fields)) {
                return false;
            }
            Write(Code.Object);
            Write7BitEncodedInt(fields.Length);
            for(int i = 0; i < fields.Length; i++) {
                var f = fields[i];
                Write(f.Name);
                var val = f.GetValue(o);
                Write(f.FieldType, val, formatter);
            }
            return true;
        }
        //public struct Functions {
        //    internal BinarySerializer _serializer;
        //    public void Write(Byte b) {
        //        _serializer.Write(b);
        //    }
        //    public void Write(Int16 value) {
        //        _serializer.Write(value);
        //    }
        //    public void Write(Int32 value) {
        //        _serializer.Write(value);
        //    }
        //    public void Write7BitEncodedInt(int value) {
        //        _serializer.Write7BitEncodedInt(value);
        //    }
        //    public void WriteInt64(Int64 value) {
        //        _serializer.Write(value);
        //    }
        //    public void WriteSingle(Single value) {
        //        _serializer.Write(value);
        //    }
        //    public void WriteDouble(Double value) {
        //        _serializer.Write(value);
        //    }
        //    public void WriteString(String value) {
        //        _serializer.Write(value);
        //    }
        //}
    }
}