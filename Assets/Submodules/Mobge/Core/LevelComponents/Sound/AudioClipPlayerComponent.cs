using System;
using System.Collections.Generic;
using Mobge;
using Mobge.Core;
using UnityEngine;

namespace Mobge.Core.Components
{
    public class AudioClipPlayerComponent : ComponentDefinition<AudioClipPlayerComponent.Data>
    {
        public const int c_maxClipCount = 256;
        [Serializable]
        public class Data : BaseComponent, IChild
        {
            public AudioSource _audioSourceRes;
            public AudioClip[] clips;
            public int playOnStart = -1;

            private AudioSource _instance;
            

            [SerializeField, HideInInspector] private ElementReference _parent = -1;
            ElementReference IChild.Parent { get => _parent; set => _parent = value; }

            //[SerializeField] [HideInInspector] private LogicConnections _connections;
            public override void Start(in InitArgs initData) {
                if (_audioSourceRes == null) {
                    _instance = new GameObject("audio").AddComponent<AudioSource>();
                    _instance.transform.SetParent(initData.parentTr, false);
                    _instance.playOnAwake = false;
                }
                else {
                    _instance = Instantiate(_audioSourceRes, initData.parentTr, false);
                }
                var itr = _instance.transform;
                itr.localPosition = position;
                if (playOnStart >= 0) {
                    Play(playOnStart);
                }
            }

            // public override LogicConnections Connections {
            //     get => _connections;
            //     set => _connections = value;
            // }
            public override object HandleInput(ILogicComponent sender, int index, object input) {
                switch (index) {
                    case c_maxClipCount + 0:
                        _instance.Stop();
                        break;
                    case c_maxClipCount + 1:
                        Play((int)(float)input);
                        break;
                    case c_maxClipCount + 2:
                        Play(UnityEngine.Random.Range(0, clips.Length));
                        break;
                    default:
                        Play(index);
                        break;
                }
                return null;
            }
            private void Play(int index) {
                _instance.clip = clips[index];
                _instance.Play();
            }
#if UNITY_EDITOR
            public override void EditorInputs(List<LogicSlot> slots) {
                for(int i = 0; i < clips.Length; i++) {
                    slots.Add(new LogicSlot("play: " + clips[i], i));
                }
                slots.Add(new LogicSlot("stop all", c_maxClipCount + 0));
                slots.Add(new LogicSlot("play at index", c_maxClipCount + 1, typeof(float)));
                slots.Add(new LogicSlot("play random", c_maxClipCount + 2));
            }
#endif
        }
    }
}