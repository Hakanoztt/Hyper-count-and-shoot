using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Mobge.Platformer.PropertyModifier {
    

    public interface IModifierDescription {
        Property[] FindMethods(Component target);
    }
    public struct Modifier {
        public float duration;
        public float amount;
    }
    public struct ModifierManager {
        private struct ModifierData {
            public Property[] properties;
            public ExposedList<ModifierEffect> modifiers;
        }
        private struct ModifierEffect {
            public float endTime;
            public float amount;
        }
        private struct NearestModifier {
            public float nextTime;
            public ModifierData nextModifier;
            public int nextIndex;
        }
        private NearestModifier _nearest;
        private Dictionary<IModifierDescription, ModifierData> _modifiers; 
        public void Ensure() {
            if(_modifiers == null) {
                _modifiers = new Dictionary<IModifierDescription, ModifierData>();
            }
        }
        public void Reset() {
            if(_modifiers != null) {
                RestoreValues();
                _modifiers.Clear();
            }
            _nearest.nextTime = float.PositiveInfinity;
        }
        private void RestoreValues() {
            var e = _modifiers.GetEnumerator();
            while(e.MoveNext()) {
                var c = e.Current.Value;
                for(int i = 0; i < c.properties.Length; i++) {
                    var p = c.properties[i];
                    p.Set(p.OriginalValue);
                }
            }
        }
        private void ApplyModifiers() {
            var e = _modifiers.GetEnumerator();
            while(e.MoveNext()) {
                var c = e.Current.Value;
                for(int i = 0; i < c.properties.Length; i++) {
                    var p = c.properties[i];
                    var v = p.Get();
                    for(int j = 0; j < c.modifiers.Count; j++) {
                        v *= c.modifiers.array[i].amount;
                    }
                    p.Set(v);
                }
            }
        }
        private void ApplyModifier(ref ModifierData data, ref Modifier m) {
            var c = data.properties;
            for(int i = 0; i < c.Length; i++) {
                var v = c[i].Get();
                v *= m.amount;
                c[i].Set(v);
            }
        }
        private void UpdateNearest() {
            var e = _modifiers.GetEnumerator();
            _nearest.nextTime = float.PositiveInfinity;
            while(e.MoveNext()) {
                var c = e.Current.Value;
                for(int i = 0; i < c.properties.Length; i++) {
                    for(int j = 0; j < c.modifiers.Count; j++) {
                        var et = c.modifiers.array[j].endTime;
                        if(et < _nearest.nextTime) {
                            _nearest.nextTime = et;
                            _nearest.nextModifier = c;
                            _nearest.nextIndex = j;
                        }
                    }
                }
            }
        }
        public void AddModifier(Component owner, IModifierDescription description, Modifier modifier) {
            ModifierData d;
            if(!_modifiers.TryGetValue(description, out d)) {
                RestoreValues();
                d.properties = description.FindMethods(owner);
                for(int i = 0; i < d.properties.Length; i++) {
                    d.properties[i].UpdateOriginal();
                }
                d.modifiers = new ExposedList<ModifierEffect>();
                ApplyModifiers();
                _modifiers.Add(description, d);
            }
            ModifierEffect e;
            e.amount = modifier.amount;
            e.endTime = modifier.duration + Time.fixedTime;
            if(e.endTime < _nearest.nextTime) {
                _nearest.nextTime = e.endTime;
                _nearest.nextModifier = d;
                _nearest.nextIndex = d.modifiers.Count;
            }
            d.modifiers.Add(e);
            ApplyModifier(ref d, ref modifier);
        }
        public void FixedUpdate() {
            if(_nearest.nextTime <= Time.fixedTime) {
                RestoreValues();
                _nearest.nextModifier.modifiers.RemoveFast(_nearest.nextIndex);
                ApplyModifiers();
                UpdateNearest();
            }
        }
    }
    public struct Property {
        private float _originalValue;
        public FieldInfo field;
        public PropertyInfo property;
        public object target;
        public void UpdateOriginal() {
            _originalValue = Get();
        }
        public float OriginalValue => _originalValue;
        public float Get() {
            if(field != null) {
                return (float)field.GetValue(target);
            }
            else{
                return (float)property.GetValue(target);
            }
        }
        public void Set(float value) {
            if(field != null) {
                field.SetValue(target, value);
            }
            else{
                property.SetValue(target, value);
            }
        }
    }
}
