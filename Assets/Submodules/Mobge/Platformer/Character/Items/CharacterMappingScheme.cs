using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.Platformer.Character {
    [CreateAssetMenu(menuName = "Mobge/Platformer/Character/Mapping Scheme")]
    public class CharacterMappingScheme : ScriptableObject
    {
        public AnimationMapping[] animations;
        public AnimationAttachment[] animationAttachments;
        public Module[] modules;
        [Serializable]
        public class AnimationMapping : Mapping {
            public string[] sectionNames;
        }
        [Serializable]
        public class AnimationAttachment : Mapping {
        }
        [Serializable]
        public class Module : Mapping {
        }
        public abstract class Mapping {
            public string name;
            private int _hash;
            public void EsureHash() {
                {
                    _hash = name.GetHashCode();
                }
            }
            public override string ToString() {
                return name;
            }
            public int Hash {
                get {
                    #if UNITY_EDITOR
                    if(!Application.isPlaying) EsureHash();
                    #endif
                    return _hash;
                }
            }
        }
    }
}