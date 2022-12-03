using Mobge.Core.Components;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge {
    [Serializable]
    public class LogicConnections
    {
        [SerializeField]
        private List<LogicConnection> _connections = new List<LogicConnection>();
        public bool AddConnection(LogicConnection connection) {
            if(IndexOf(connection) >= 0) return false;
            _connections.Add(connection);
            return true;
        }
        public bool RemoveConnection(LogicConnection connection) {
            int index = IndexOf(connection);
            if(index < 0) {
                return false;
            }
            _connections.RemoveAt(index);
            return true;
        }
        private int IndexOf(LogicConnection connection) {
            for(int i = 0; i < _connections.Count; i++) {
                if(_connections[i].Equals(connection)) {
                    return i;
                }
            }
            return -1;
        }
        public List<LogicConnection> List => _connections;
        public object InvokeSimple<T>(ILogicComponent sender, int id, object parameter, Dictionary<int, T> allComponents) {
            object result = null;
            for(int i = 0; i < _connections.Count; i++) {
                var c = _connections[i];
                if(c.output == id) {
					result = ((ILogicComponent)allComponents[c.target]).HandleInput(sender, c.input, parameter);
				}
			}
            return result;
        }
        public InvokeEnumerator<T> Invoke<T>(ILogicComponent sender, int id, object parameter, Dictionary<int, T> allComponents) {
            return new InvokeEnumerator<T>(this, id, allComponents, sender, parameter);
        }
        public Enumerator GetConnections(int id) {
            return new Enumerator(this, id);
        }
        public int GetConnectionCount(int id) {
            int count = 0;
            for(int i = 0; i < _connections.Count; i++) {
                if(_connections[i].output == id) {
                    count++;
                }
            }
            return count;
        }

        public struct InvokeEnumerator<T> : IEnumerator<object>
        {
            private object _next;
            private Dictionary<int, T> _components;
            private int _index;
            private object _parameter;
            private ILogicComponent _sender;
            private int _output;
            private LogicConnections _connections;
            public object Current => _next;
            
            public InvokeEnumerator(LogicConnections connections, int output, Dictionary<int, T> components, ILogicComponent sender, object parameter) {
                _index = -1;
                _parameter = parameter;
                _sender = sender;
                _output = output;
                _connections = connections;
                _next = null;
                _components = components;
            }
            public void Dispose()
            {
                
            }

            public bool MoveNext()
            {
                var c = _connections._connections;
                do {
                    _index++;
                    if(_index >= c.Count) {
                        return false;
                    }
                } while(c[_index].output != _output);
                var cc = c[_index];
                _next = ((ILogicComponent)_components[cc.target]).HandleInput(_sender, cc.input, _parameter);
                return true;
            }

            public void Reset()
            {
                _index = -1;
                _next = null;
            }
        }
        public struct Enumerator : IEnumerator<LogicConnection> {
            private int _index;
            private int _output;
            private LogicConnections _connections;
            public Enumerator(LogicConnections connections, int output) {
                _index = -1;
                _output = output;
                _connections = connections;
            }

            public LogicConnection Current => _connections._connections[_index];

            object IEnumerator.Current => _connections._connections[_index];

            public void Dispose()
            {
                
            }

            public bool MoveNext()
            {
                do {
                    _index++;
                    if(_index >= _connections._connections.Count) {
                        return false;
                    }
                }
                while(_connections._connections[_index].output != _output);
                return true;
            }

            public void Reset()
            {
                _index = -1;
            }
        }
    }
    [Serializable]
    public struct LogicConnection {
        public int input;
        public int output;
        public ElementReference target;
        public bool Equals(LogicConnection other) => input == other.input && output == other.output && target.id == other.target.id;
    }
}