using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge {
    
    public struct LogicSlot {
        public Type parameter;
        public Type returnType;
        private bool hidden;
        public int id;
        public string name;
        public override string ToString() {
            return name + "\n" + S(parameter) + "->" + S(returnType);
        }
        private string S(Type t) {
            return t == null ? "void" : t.Name;
        }
        public LogicSlot(string name, int id, Type parameterType = null, Type returnType = null, bool hidden = false) {
            this.id = id;
            this.name = name;
            this.parameter = parameterType;
            this.returnType = returnType;
            this.hidden = hidden;
        }
    }
}