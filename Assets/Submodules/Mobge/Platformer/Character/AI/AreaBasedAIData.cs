using System;
using UnityEngine;

namespace Mobge.Platformer.Character {
    public class AreaBasedAIData : ScriptableObject {

        public Rule[] rules;
        public Rect detectArea = new Rect(-10,-5,20, 10);
        public Rect releaseArea = new Rect(-15, -8, 30, 16);
        public ActionSequence defaultActions;
        [Serializable]
        public class Rule {
            public Rect area;
            public float weight;
            public ActionSequence actions;
            public override string ToString(){
                return area.ToString();
            }
        }
        [Serializable]
        public struct Action {
            public ActionType type;
            public int ActionParameterI {
                get{return (int)_actionParameter; }
                set{_actionParameter = value; }
            }
            public float ActionParameterF{
                get{return _actionParameter;}
                set{_actionParameter= value;}
            }
            [SerializeField]
            private float _actionParameter;
            public override string ToString(){
                return type.ToString();
            }
        }
        [Serializable]
        public struct ActionSequence {
            public Action[] actions;
        }
        public enum ActionType {
            WalkInputX = 0,
            ActionPress = 1,
            ActionRelease = 2,
            WaitTime = 3,
            WaitForAction = 4,
            JumpStart = 5,
            JumpEnd = 6,
            WaitWhileTargetInside = 7,
        }
    }
}