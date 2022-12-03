using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using UnityEngine;

namespace Mobge.Serialization {
    public class BinarySerializationBase
    {
        protected static StreamingContext s_context = new StreamingContext(StreamingContextStates.All);
        protected StreamingContext Context => s_context;
        protected Encoding _encoding;
        protected BinarySerializationBase() {
            _encoding = Encoding.UTF8;
        }
        private static Dictionary<Type, FieldInfo[]> _fieldCache;
        private static List<FieldInfo> _tempFields;
        static BinarySerializationBase() {
            _fieldCache = new Dictionary<Type, FieldInfo[]>();
            _tempFields = new List<FieldInfo>();
            FindFields(typeof(Vector4));
            FindFields(typeof(Vector3));
            FindFields(typeof(Vector2));
            FindFields(typeof(Quaternion));
            FindFields(typeof(Matrix4x4));
            FindFields(typeof(Vector2Int));
			FindFields(typeof(Vector3Int));
			FindFields(typeof(Color32));
			FindFields(typeof(Color));
            FindFields(typeof(GradientColorKey));
            FindFields(typeof(GradientAlphaKey));
        }
        protected static bool TryGetFields(Type t, out FieldInfo[] fields) {
            if(_fieldCache.TryGetValue(t, out fields)) {
                return true;
            }
            if(!t.IsSerializable) {
                fields = null;
                return false;
            }
            fields = FindFields(t);
            return true;
        }
        private static FieldInfo[] FindFields(Type t)
        {
			var privateT = t;
			do {
				var privates = privateT.GetFields(BindingFlags.Instance | BindingFlags.NonPublic);
				for (int i = 0; i < privates.Length; i++) {
					var f = privates[i];
					if (f.GetCustomAttribute<SerializeField>() != null) {
						if(!_tempFields.Contains(f))
							_tempFields.Add(f);
					}
				}
				privateT = privateT.BaseType;
			} while (privateT != null);


			var publics = t.GetFields(BindingFlags.Instance | BindingFlags.Public);
			for (int i = 0; i < publics.Length; i++) {
                var f = publics[i];
                if(f.GetCustomAttribute<NonSerializedAttribute>() == null) {
                    _tempFields.Add(f);
                }
            }
            var r = _tempFields.ToArray();
            _fieldCache.Add(t, r);
            _tempFields.Clear();
            return r;
        }

        public static class Code {
            public const byte Null = 0;
            public const byte Byte = 1;
            public const byte Int16 = 2;
            public const byte Int32 = 3;
            public const byte Int64 = 4;
            public const byte Single = 5;
            public const byte Double = 6;
            public const byte String = 10;
            public const byte Array = 15;
            public const byte Object = 20;
            public const byte UnityObject = 21;
            public const byte Dictionary = 22;
        }
        
        public struct Formatter
        {
            public Surrogates surrogates;
        }
        public interface ISurrogate {
            void Deserialize(ref object obj, Dictionary<string, object> values);
            Type FieldType(string key);
            void Serialize(object obj, Dictionary<string, object> values);
        }
        public struct Surrogates {
            private Dictionary<Type, ISurrogate> _surrogates;
            private Dictionary<string, object> _objects;
            public Dictionary<string, object> Objects => _objects;
            public void AddSurrogate(Type type, ISurrogate surrogate) {
                EnsureSurrogates();
                _surrogates.Add(type, surrogate);
            }
            public bool TryGetSurrogate(Type type, out ISurrogate surrogate) {
                if(_surrogates == null) {
                    surrogate = null;
                    return false;
                }
                return _surrogates.TryGetValue(type, out surrogate);
            }
            private void EnsureSurrogates() {
                if(_surrogates == null) {
                    _objects = new Dictionary<string, object>();
                    _surrogates = new Dictionary<Type, ISurrogate>();
                }
            }
        }
    }
}