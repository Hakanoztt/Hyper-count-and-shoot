using System;
using UnityEngine;

namespace Mobge.Platformer.Character{
    /// <summary>
    /// This class is designed for <see cref="Character2D"> objects to consume input. 
    /// It unifies input scheme so any kind of input device can be mapped to this class to control characters. 
    /// Note: Call <see cref="Consumed"> "after" consuming inputs from Update or FixedUpdate functions. </summary>
    public class CharacterInput
    {
        public const int JUMP_ACTION_ID = -10;
        public Button Jump;
        public Vector2 MoveInput;
        private Indexer<Button> _actions;
        private Indexer<Vector2> _axises;
        private Vector3 _targetPosition;
        public Character2D Target { get; set; }
        public Vector3 TargetPosition { 
            get {
                if(Target) {
                    return Target.Position;
                }
                return _targetPosition;
            }
            set {
                Target = null;
                _targetPosition = value;
            }
        }
        public Indexer<Button> Actions {
            get{
                return _actions;
            }
        }
        public Indexer<Vector2> Axises => _axises;
        public bool HasTargetPosition {
            get {
                return Target != null || !float.IsNaN(_targetPosition.x);
            }
        }
        public void RemoveTarget() {
            Target = null;
            _targetPosition.x = float.NaN;
        }
        public CharacterInput() {
            _actions.ensureSize(2);
            _axises.ensureSize(0);
        }
        public void UpdateJump(bool input) {
            Jump.Value = input;
        }
        public void UpdateAxis(int index, Vector2 value) {
            _axises.ensureSize(index+1);
            _axises.elements[index] = value;
        }
        public void UpdateActions(bool a1, bool a2) {
            _actions.ensureSize(2);
            _actions.elements[0].Value = a1;
            _actions.elements[1].Value = a2;
        }
        public void UpdateActions(bool a1, bool a2, bool a3) {
            _actions.ensureSize(3);
            _actions.elements[0].Value = a1;
            _actions.elements[1].Value = a2;
            _actions.elements[2].Value = a3;
        }
        public void UpdateActions(bool a1, bool a2, bool a3, bool a4) {
            _actions.ensureSize(4);
            _actions.elements[0].Value = a1;
            _actions.elements[1].Value = a2;
            _actions.elements[2].Value = a3;
            _actions.elements[3].Value = a4;
        }
        public void UpdateAction(int index, bool value) {
            switch(index) {
                case JUMP_ACTION_ID:
                Jump.Value = value;
                break;
                default:
                _actions.ensureSize(index + 1);
                _actions.elements[index].Value = value;
                break;
            }
        }
        /// <summary>
        /// Call this function "after" reading values. </summary>
        public void Consumed() {
            Jump.Consumed();
            for(int i = 0; i < _actions.elements.Length; i++) {
                _actions.elements[i] = _actions.elements[i].Consumed();
            }
        }
        public void Reset()
        {
            Jump = new Button();
            MoveInput = Vector2.zero;
            if(_actions.elements!=null){
                for(int i = 0; i < _actions.elements.Length; i++){
                    _actions.elements[i] = new Button();
                }
            }
            RemoveTarget();
        }
        public struct Button {
            private bool _value;
            public bool Value {
                get => _value;
                set {
                    _value = value;
                }
            }
            public bool PreviousValue{
                get; private set;
            }
            public bool Down {
                get => Value & !PreviousValue;
            }
            public bool Up {
                get => !Value & PreviousValue;
            }
            internal Button Consumed(){
                PreviousValue = Value;
                return this;
            }
            public static implicit operator bool(Button b){
                return b.Value;
            }
        }


        public struct Indexer<T> {
            internal T[] elements;
            public T this[int index]
            {
                get {
                    if(elements == null || elements.Length <= index) 
                        return default(T); 
                    return elements[index];
                }
            }
            internal void ensureSize(int size) {
                if(elements == null) {
                    elements = new T[size + size / 2];
                }
                else{
                    if(elements.Length < size) {
                        System.Array.Resize(ref elements, size + size / 2);
                    }
                }
            }
        }

    }
    
}