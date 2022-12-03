using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Mobge.Core {
    public class LevelStateManager
    {
        private StringBuilder _sb = new StringBuilder();
        private static int _zeroIndex;
        private static char _dot;
        static LevelStateManager() {
            _zeroIndex = (int)'0';
            _dot = '.';
        }
        public struct Key {
            public int owner;
            public int id;

            public Key(int owner, int id)
            {
                this.owner = owner;
                this.id = id;
            }
            public override string ToString() {
                return "Key(" + owner + "," + id+ ")";
            }
        }
        private Dictionary<Key, UnityEngine.GameObject> _objects = new Dictionary<Key, UnityEngine.GameObject>();
        public void RegisterObject(int owner, int id, UnityEngine.GameObject obj) {
            _sb.Append(owner);
            _sb.Append(_dot);
            _sb.Append(id);
            obj.name = _sb.ToString();
            _objects.Add(new Key(owner, id), obj);
            _sb.Clear();
        }
        public UnityEngine.GameObject GetObject(Key k) {
            if(k.owner < 0) {
                return null;
            }
            return _objects[k];
        }
        public T GetObject<T>(Key k) where T : class {
            if(k.owner < 0) {
                return null;
            }
            return _objects[k].GetComponent<T>();
        }
        public Key GetKey(UnityEngine.Component component) {
            if(component == null) {
                return new Key(-1,-1);
            }
            return GetKey(component.gameObject);
        }
        public Key GetKey(UnityEngine.GameObject obj) {
            if(obj == null) {
                return new Key(-1,-1);
            }
            var name = obj.name;
            Key k;
            k.owner = 0;
            k.id = 0;
            int i = 0;
            do {
                int n = (int)name[i] - _zeroIndex;
                if(n < 0 || n >= 10) {
                    return new Key(-1, -1);
                }
                k.owner = k.owner * 10 + n;
                i++;
                if(i == name.Length) {
                    return new Key(-1, -1);
                }
            } while(name[i] != '.');
            i++;
            do {
                int n = (int)name[i] - _zeroIndex;
                if (n < 0 || n >= 10) {
                    return new Key(-1, -1);
                }
                k.id = k.id * 10 + n;
                i++;
            } while(i < name.Length);
            return k;
        }
    }
}