using Mobge.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.Platformer
{
    public class Camera2DShake : ComponentDefinition<Camera2DShake.Data>
    {
        [Serializable]
        public class Data : BaseComponent, Side2DCamera.Modifier {

            public float frequency = 8;
            public Vector3 amplitude = new Vector3(1, 0, 0);
            public Vector3 rotationalAmplitude;
            public Animation.Curve amplitudeOverTime;
            public float time;
            private LevelPlayer _player;
            private float _startTime;
            private Side2DCamera Camera {
                get => ACameraController.FindOrCreateForLevel<Side2DCamera>(_player);
            }
            public override void Start(in InitArgs initData) {
                _player = initData.player;
                amplitudeOverTime.EnsureInit(false);
            }
            public override object HandleInput(ILogicComponent sender, int index, object input) {
                switch (index) {
                    default:
                    case 0:
                        Camera.Modifiers.Add(this);
                        _startTime = Time.time;
                        break;
                    case 1:
                        Camera.Modifiers.Remove(this);
                        break;
                }
                return null;
            }
            void Side2DCamera.Modifier.UpdatePosition(in Side2DCamera.UpdateArgs args, ref Side2DCamera.UpdateData data) {
                var passedTime = Time.time - _startTime;
                if(passedTime > time) {
                    Camera.Modifiers.Remove(this);
                }
                else {
                    var prog = passedTime / time;
                    var mul = amplitudeOverTime.Evaluate(prog);
                    var wave = Mathf.Sin(passedTime * frequency * (2 * Mathf.PI));
                    data.effectOffset += (mul * wave) * amplitude;
                    data.effectEulerRotation += (mul * wave) * rotationalAmplitude;
                }
            }
#if UNITY_EDITOR
            public override void EditorInputs(List<LogicSlot> slots) {
                slots.Add(new LogicSlot("start", 0));
                slots.Add(new LogicSlot("stop immediately", 1));
            }
#endif
        }
    }
}