using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Mobge.Core {
    public abstract class ComponentDefinition<T> : ComponentDefinition where T : BaseComponent {
	    public override Type DataType => typeof(T);
#if UNITY_EDITOR
        public sealed override FieldInfo EditorValueField {
            get {
                var t = typeof(ComponentDefinition<T>);
                var fi = t.GetField(nameof(_editorValue), BindingFlags.NonPublic | BindingFlags.Instance);
                return fi;
            }
        }
        [SerializeField]
        protected T _editorValue;
        public sealed override ComponentDefinition Clone() {
	        // var def = (ComponentDefinition)Activator.CreateInstance(this.GetType());
	        var def = (ComponentDefinition) ScriptableObject.CreateInstance(this.GetType());
	        return def;
        }
#endif
	}
    public abstract class ComponentDefinition : ScriptableObject { 
	    public abstract Type DataType { get; }
#if UNITY_EDITOR
		public Texture2D icon;
		public int menuPriority = 0;
		public abstract FieldInfo EditorValueField { get; }
		public abstract ComponentDefinition Clone();
#endif
	}
}
