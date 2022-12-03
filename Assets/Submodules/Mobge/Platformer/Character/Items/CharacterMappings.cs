using System;
using System.Collections;
using System.Collections.Generic;
using Mobge.Platformer.Character;
using UnityEngine;
using Mobge.Animation;

namespace Mobge.Platformer.Character {
    public class CharacterMappings : MonoBehaviour
    {
        public CharacterMappingScheme scheme;
        public State[] modules;
        public AimationSplit[] animations;
        public AnimationAttachment[] animationAttachments;
        public int GetModuleIndex(int hash) {
            for(int i = 0; i < modules.Length; i++) {
                if(modules[i].hash == hash) return i;
            }
            return -1;
        }
        public AnimationSpliter GetAnimation(object owner, AnimationSplitDurations durations, out int track) {
            
            for(int i = 0; i < animations.Length; i++) {
                var s = animations[i];
                if(s.hash == durations.splitterId) {
                    s.SetDurations(owner, durations.durations);
                    track = s.track;
                    return s.spliter;
                }
            }
            track = -1;
            return default(AnimationSpliter);
        }
        
        public int GetAnimation(object owner, int hash, out int track) {
            
            for(int i = 0; i < animations.Length; i++) {
                var s = animations[i];
                if(s.hash == hash) {
                    track = s.track;
                    return s.spliter.animation;
                }
            }
            track = -1;
            return -1;
        }
        [Serializable]
        public struct AnimationAttachment {
            public int hash;
            public string boneName;
            public string visualName;
        }
        [Serializable]
        public struct AimationSplit {
            public int hash;
            public AnimationSpliter spliter;
            public int track;
            private object _lastOwner;
            public void SetDurations(object owner, float[] durations) {
                if (_lastOwner != owner) {
                    _lastOwner = owner;
                    for (int i = 0; i < spliter.divisions.Length; i++) {
                        spliter.divisions[i].duration = durations[i];
                    }
                }
            }
        }
        [Serializable]
        public struct State {
            public int hash;
            public IStateModule module;
        }
    }
    public class CharacterMappingAttribute : PropertyAttribute {
        public Mapping map;
        public CharacterMappingAttribute(Mapping map) {
            this.map = map;
        }
        public enum Mapping {
            Animation = 0,
            AnimationAttachment = 1,
            Module = 2,
        }
    }
    
    [Serializable]
    public struct AnimationSplitDurations {
        public int splitterId;
        public float[] durations;
    }
}