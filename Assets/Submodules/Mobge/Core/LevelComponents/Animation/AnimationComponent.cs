using Mobge.Animation;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.Core.Components {
    public class AnimationComponent : ComponentDefinition<AnimationComponent.Data>{

        public enum PlayMode {
            LoopLast = 0x0,
            //LoopWhole = 0x1,
            PlayStraight = 0x2,
        }
        [Serializable]
        public class SequenceMap : AutoIndexedMap<Sequence> {

        }
        [Serializable]
        public struct Sequence {
            public int track;
            public string name;
            public int[] animations;
            public float firstAnimationTime;
            public float crossFadeTime;
            public PlayMode mode;
        }
        [Serializable]
        public class Data : BaseComponent, IChild, IRotationOwner {
            private const int id_playSequence = 0x40000000;
            private const int id_playAnimation = 0x40000001;
            private const int id_this = 0x40000002;
            [SerializeField]
            [HideInInspector]
            private ElementReference _parent = -1;
            ElementReference IChild.Parent { get => _parent; set => _parent = value; }
            public Quaternion Rotation { get => rotation; set => rotation = value; }
            
            [HideInInspector] public AAnimation animation;
            [HideInInspector] public SequenceMap sequences;
            [HideInInspector] public int defaultAnimation = -1;
            private AAnimation _instance;
            [HideInInspector] public Quaternion rotation = Quaternion.identity;
            [HideInInspector] public Vector3 scale = Vector3.one;
            private LevelPlayer _player;

            public AAnimation Instance => _instance;

            public override void Start(in InitArgs initData) {
                _instance = CreateAnimation(initData.parentTr);
                _player = initData.player;
                if (defaultAnimation >= 0) {
                    _instance.PlayAnimation(defaultAnimation, true);
                }
            }
            public AAnimation CreateAnimation(Transform parent) {
                var a = Instantiate(animation, parent, false);
                
                var tr = a.transform;
                tr.localPosition = position;
                tr.localRotation = rotation;
                UpdateVisual(a);
                return a;
            }
            public void UpdateVisual(AAnimation anim) {
                var tr = anim.transform;
                tr.localScale = scale;
            }
            public override object HandleInput(ILogicComponent sender, int index, object input) {
                switch (index) {
                    case id_playAnimation:
                        int anim = (int)(float)input;
                        var stt = _instance.PlayAnimation(anim, false);
                        return stt.PlayDuration;
                    case id_playSequence:
                        return PlaySequence((int)(float)input);
                    case id_this:
                        return this;
                    default:
                        return PlaySequence(index);
                }
            }
            private float PlaySequence(int id) {
                var s = sequences[id];
                Animation.AnimationState firstStt;
                Animation.AnimationState lastStt;
                switch (s.mode) {
                    default:
                    case PlayMode.LoopLast:
                        firstStt = PlayFirstAnimation(s);
                        lastStt = firstStt;
                        for(int i = 1; i < s.animations.Length; i++) {
                            lastStt = _instance.QueueAnimation(s.track, s.animations[i], false);
                        }
                        lastStt.Loop = true;
                        break;
                    case PlayMode.PlayStraight:
                        firstStt = PlayFirstAnimation(s);
                        lastStt = firstStt;
                        for (int i = 1; i < s.animations.Length; i++) {
                            lastStt = _instance.QueueAnimation(s.track, s.animations[i], false);
                        }
                        break;
                }
                return firstStt.PlayDuration;
            }
            private Animation.AnimationState PlayFirstAnimation(in Sequence sequence) {
                return _instance.PlayAnimation(sequence.track, sequence.animations[0], false, sequence.crossFadeTime);
            }
#if UNITY_EDITOR
            public override void EditorInputs(List<LogicSlot> slots) {
                if (sequences != null) {
                    var e = sequences.GetKeyEnumerator();
                    while (e.MoveNext()) {
                        var id = e.Current;
                        var sq = sequences[id];
                        slots.Add(new LogicSlot("play: " + sq.name, id, null, typeof(float)));
                    }
                }
                slots.Add(new LogicSlot("play sequence", id_playSequence, typeof(float), typeof(float)));
                slots.Add(new LogicSlot("play animation", id_playAnimation, typeof(float), typeof(float)));
                slots.Add(new LogicSlot("this", id_this, null, typeof(Data), true));
            }
#endif
        }
    }
}