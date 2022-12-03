#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using Mobge.Core;
using UnityEditor;

namespace Mobge {
    public static class ComponentFixer {
        public delegate bool ComponentDataFixerFunction<T> (Piece piece, int id, ref T component);
        public delegate bool ComponentShouldBeDeletedFunction<T> (Piece piece, int id, ref T component);
        public delegate bool ConnectionInputFixerFunction<T>(Piece piece, int id, ref T component, ref int? input);
        public delegate bool ConnectionOutputFixerFunction<T>(Piece piece, int id, ref T component, ref int? output);

        public delegate bool ComponentDataFixerFunction(Piece piece, int id, ref BaseComponent component);
        public delegate bool ComponentShouldBeDeletedFunction(Piece piece, int id, ref BaseComponent component);
        public delegate bool ConnectionInputFixerFunction(Piece piece, int id, ref BaseComponent component, ref int? input);
        public delegate bool ConnectionOutputFixerFunction(Piece piece, int id, ref BaseComponent component, ref int? output);


        /// <summary>
        /// traverse all project and find all components with type T
        /// call fixer function for the component
        /// if fixer function returns true save changes
        /// if component == null delete element from piece
        /// </summary>
        public static void FixComponentData<T>(ComponentDataFixerFunction<T> fixerFunction, IEnumerable<Piece> pieces = null) where T : BaseComponent {
            FixComponentData(
                (Piece piece, int id, ref BaseComponent component) => {
                    var o = (T) component;
                    var isThereChange = fixerFunction(piece, id, ref o);
                    component = (BaseComponent) o;
                    return isThereChange;
                }
                , typeof(T)
                , pieces);
        }
        public static void FixComponentData(ComponentDataFixerFunction fixerFunction, Type type = null, IEnumerable<Piece> pieces = null) {
            if (pieces == null) pieces = new PieceEnumerable();
            foreach (var piece in pieces) {
                var e = piece.Components.GetPairEnumerator();
                while (e.MoveNext()) {
                    var id = e.Current.Key;
                    var dataHolder = e.Current.Value;
                    if (type != null && !type.IsAssignableFrom(dataHolder.Definition.DataType)) continue;
                    var obj = dataHolder.GetObject() as BaseComponent;
                    if (!fixerFunction(piece, id, ref obj)) continue;
                    if (obj == null) 
                        piece.Components.RemoveElement(id);
                    else 
                        dataHolder.SetObject(obj);
                    EditorUtility.SetDirty(piece);
                }
            }
        }
        public static void FixComponentInputs<T>(ConnectionInputFixerFunction<T> fixerFunction, IEnumerable<Piece> pieces = null) where T : BaseComponent, ILogicComponent {
            FixComponentInputs(
                (Piece piece, int id, ref BaseComponent component, ref int? input) => {
                    var o = (T) component;
                    var isThereChange = fixerFunction(piece, id, ref o, ref input);
                    component = (BaseComponent)o;
                    return isThereChange;
                }
                , typeof(T)
                , pieces);
        }
        public static void FixComponentInputs(ConnectionInputFixerFunction fixerFunction, Type type, IEnumerable<Piece> pieces = null) {
            if (pieces == null) pieces = new PieceEnumerable();
            foreach (var piece in pieces) {
                var e = piece.Components.GetPairEnumerator();
                while (e.MoveNext()) {
                    //components inside piece
                    var dataHolder = e.Current.Value;
                    var obj = dataHolder.GetObject();
                    var logic = obj as ILogicComponent;
                    if (logic == null) continue;
                    if (logic.Connections == null) continue;
                    bool changed = false;
                    for (int i = 0; i < logic.Connections.List.Count; i++) {
                        //foreach connection output span out from component
                        var connection = logic.Connections.List[i];
                        //if its a connection outwards a piece
                        if (!piece.Components.ContainsKey(connection.target)) continue;
                        //if this output component is a component we are interested in
                        var targetDataHolder = piece.Components[connection.target];
                        if (type != null && !type.IsAssignableFrom(targetDataHolder.Definition.DataType)) continue;
                        var targetHolder = piece.Components[connection.target];
                        var target = targetHolder.GetObject() as BaseComponent;
                        var id = connection.target.id;
                        int? input = connection.input;
                        if (!fixerFunction(piece, id, ref target, ref input)) continue;
                        logic.Connections.RemoveConnection(connection);
                        if (input != null) {
                            var newConn = new LogicConnection {
                                input = (int)input, output = connection.output, target = connection.target
                            };
                            logic.Connections.AddConnection(newConn);
                        }
                        else {
                            i--;
                        }
                        if (target == null) 
                            piece.Components.RemoveElement(id);
                        else 
                            targetHolder.SetObject(target);
                        changed = true;
                    }
                    if (changed) {
                        dataHolder.SetObject(obj);
                        EditorUtility.SetDirty(piece);
                    }
                }
                //piece inner, outer connections
                for (int i = 0; i < piece.Connections.List.Count; i++) {
                    var connection = piece.Connections.List[i];
                    if (!piece.Components.ContainsKey(connection.target)) continue;
                    var componentData = piece.Components[connection.target];
                    var id = connection.target.id;
                    if (type != null && !type.IsAssignableFrom(componentData.Definition.DataType)) continue;
                    var componentObject = componentData.GetObject() as BaseComponent;
                    int? input = connection.input;
                    if (!fixerFunction(piece, id, ref componentObject, ref input)) continue;
                    piece.Connections.RemoveConnection(connection);
                    if (input != null) {
                        var newConn = new LogicConnection {
                            input = (int)input, output = connection.output, target = connection.target
                        };
                        piece.Connections.AddConnection(newConn);
                    }
                    else {
                        i--;
                    }
                    
                    if (componentObject == null) {
                        piece.Components.RemoveElement(id);
                    }
                    else {
                        componentData.SetObject(componentObject);
                    }
                    EditorUtility.SetDirty(piece);
                }
            }
        }
        public static void FixComponentOutputs<T>(ConnectionOutputFixerFunction<T> fixerFunction, IEnumerable<Piece> pieces = null) where T : BaseComponent {
            FixComponentOutputs(
                (Piece piece, int id, ref BaseComponent component, ref int? output) => {
                    var o = (T) component;
                    var isThereChange = fixerFunction(piece, id, ref o, ref output);
                    component = (BaseComponent)o;
                    return isThereChange;
                }
                , typeof(T)
                , pieces);
        }
        public static void FixComponentOutputs(ConnectionOutputFixerFunction fixerFunction, Type type = null, IEnumerable<Piece> pieces = null) {
            if (pieces == null) pieces = new PieceEnumerable();
            foreach (var piece in pieces) {
                var e = piece.Components.GetPairEnumerator();
                while (e.MoveNext()) {
                    //components inside piece
                    var dataHolder = e.Current.Value;
                    var id = e.Current.Key;
                    if (type != null && !type.IsAssignableFrom(dataHolder.Definition.DataType)) continue;
                    var obj = dataHolder.GetObject() as BaseComponent;
                    var logic = obj as ILogicComponent;
                    if (logic == null) continue;
                    if (logic.Connections == null) continue;
                    for (int i = 0; i < logic.Connections.List.Count; i++) {
                        //foreach connection output span out from component
                        var connection = logic.Connections.List[i];
                        //if its not a connection outwards a piece
                        // if (!piece.Components.ContainsKey(connection.target)) continue;
                        int? output = connection.output;
                        if(!fixerFunction(piece, id, ref obj, ref output)) continue;
                        logic.Connections.RemoveConnection(connection);
                        if (output != null) {
                            var newConn = new LogicConnection {
                                input = connection.input, output = (int)output, target = connection.target
                            };
                            logic.Connections.AddConnection(newConn);
                        }
                        dataHolder.SetObject(obj);
                        EditorUtility.SetDirty(piece);
                    }
                }
            }
        }
        public static void DeleteComponentData<T>(ComponentShouldBeDeletedFunction<T> fixerFunction, IEnumerable<Piece> pieces = null) where T : BaseComponent {
            DeleteComponentData(
                (Piece piece, int id, ref BaseComponent component) => {
                    var o = (T) component;
                    var shouldBeDeleted = fixerFunction(piece, id, ref o);
                    component = (BaseComponent) o;
                    return shouldBeDeleted;
                }
                , typeof(T)
                , pieces);
        }
        public static void DeleteComponentData(ComponentShouldBeDeletedFunction fixerFunction, Type type = null, IEnumerable<Piece> pieces = null) {
            if (pieces == null) pieces = new PieceEnumerable();
            foreach (var piece in pieces) {
                var e = piece.Components.GetPairEnumerator();
                while (e.MoveNext()) {
                    var id = e.Current.Key;
                    var dataHolder = e.Current.Value;
                    if (type != null && !type.IsAssignableFrom(dataHolder.Definition.DataType)) continue;
                    var obj = dataHolder.GetObject() as BaseComponent;
                    if (!fixerFunction(piece, id, ref obj)) continue;
                    piece.Components.RemoveElement(id);
                    EditorUtility.SetDirty(piece);
                }
            }
        }
        public static IEnumerable<BaseComponent> GetAllComponentsInPieces(IEnumerable<Piece> pieces = null, Type type = null) {
            return new ComponentInPieceEnumerable(type, pieces);
        }
        public static IEnumerable<T> GetAllComponentsInPieces<T>(IEnumerable<Piece> pieces = null) where T:BaseComponent {
            return new TypedComponentInPieceEnumerable<T>(pieces);
        }
        private class TypedComponentInPieceEnumerable<T> : IEnumerable<T> where T : BaseComponent {
            private readonly IEnumerable<Piece> _pieces;
            public TypedComponentInPieceEnumerable(IEnumerable<Piece> pieces) { _pieces = pieces; }
            public IEnumerator<T> GetEnumerator() { return new TypedComponentInPieceEnumerator<T>(new ComponentInPieceEnumerator(typeof(T), _pieces)); }
            IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
        }
        private class TypedComponentInPieceEnumerator<T> : IEnumerator<T> where T:BaseComponent {
            private readonly ComponentInPieceEnumerator _componentInPieceEnumerator;
            public TypedComponentInPieceEnumerator(ComponentInPieceEnumerator componentInPieceEnumerator) { _componentInPieceEnumerator = componentInPieceEnumerator; }
            public bool MoveNext() { return _componentInPieceEnumerator.MoveNext(); }
            public void Reset() { _componentInPieceEnumerator.Reset(); }
            public T Current => _componentInPieceEnumerator.Current as T;
            object IEnumerator.Current => Current;
            public void Dispose() { _componentInPieceEnumerator.Dispose(); }
        }
        private class ComponentInPieceEnumerable : IEnumerable<BaseComponent> {
            private readonly Type _type;
            private readonly IEnumerable<Piece> _pieces;
            public ComponentInPieceEnumerable(Type type, IEnumerable<Piece> pieces) { _type = type; _pieces = pieces; }
            public IEnumerator<BaseComponent> GetEnumerator() { return new ComponentInPieceEnumerator(_type, _pieces); }
            IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
        }
        private class ComponentInPieceEnumerator : IEnumerator<BaseComponent> {
            private readonly IEnumerator<Piece> _pieceEnumerator;
            private AutoIndexedMap<LevelComponentData>.PairEnumerator _componentEnumerator;
            private readonly Type _type;
            private bool _componentEnumeratorInitialized;
            private bool _finished;
            public ComponentInPieceEnumerator(Type type = null, IEnumerable<Piece> pieces = null) {
                Current = null;
                _type = type;
                _finished = false;
                _componentEnumeratorInitialized = false;
                _pieceEnumerator = (pieces ?? GetAllPieces()).GetEnumerator();
            }
            public bool MoveNext() {
                if (_finished) return false;
                if (MoveToNextComponent()) return true;
                if (MoveToNextPiece()) return true;
                _finished = true;
                return false;
            }
            private bool MoveToNextPiece() {
                while (true) {
                    if (!_pieceEnumerator.MoveNext()) {
                        _finished = true;
                        return false;
                    }
                    var piece = _pieceEnumerator.Current;
                    if (piece == null)  continue;
                    _componentEnumerator = piece.Components.GetPairEnumerator();
                    _componentEnumeratorInitialized = true;
                    if (!MoveToNextComponent()) continue;
                    return true;
                }
            }
            private bool MoveToNextComponent() {
                while (true) {
                    if (!_componentEnumeratorInitialized) return false;
                    if (!_componentEnumerator.MoveNext()) return false;
                    if (!ValidateAndSetCurrent()) continue;
                    return true;
                }
            }
            private bool ValidateAndSetCurrent() {
                var dataHolder = _componentEnumerator.Current.Value;
                if (!(dataHolder.GetObject() is BaseComponent obj)) return false;
                //todo might be reverse,, this expression
                if (_type != null && !_type.IsInstanceOfType(obj)) return false;
                Current = obj;
                return true;
            }
            public void Reset() {
                throw new NotImplementedException();
            }
            public BaseComponent Current { get; private set; }
            object IEnumerator.Current => Current;
            public void Dispose() {
                _pieceEnumerator.Dispose();
                _componentEnumerator.Dispose();
            }
        }
        public static IEnumerable<Piece> GetAllPieces() {
            return new PieceEnumerable();
        }
        private class PieceEnumerable : IEnumerable<Piece> {
            public IEnumerator<Piece> GetEnumerator() { return new PieceEnumerator(); }
            IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
        }
        private class PieceEnumerator : IEnumerator<Piece> {
            private readonly IEnumerator _guidEnumerator;
            public PieceEnumerator() {
                var searchString = "t:" + typeof(Piece);
                var pieceAssetGuids = AssetDatabase.FindAssets(searchString);
                _guidEnumerator = pieceAssetGuids.GetEnumerator();
            }
            public bool MoveNext() => _guidEnumerator.MoveNext();
            public void Reset() => _guidEnumerator.Reset(); 
            public Piece Current {
                get {
                    var path = AssetDatabase.GUIDToAssetPath((string) _guidEnumerator.Current);
                    var piece = AssetDatabase.LoadAssetAtPath<Piece>(path);
                    return piece;
                }
            }
            object IEnumerator.Current => Current;
            public void Dispose() { }
        }
    }
}
#endif
