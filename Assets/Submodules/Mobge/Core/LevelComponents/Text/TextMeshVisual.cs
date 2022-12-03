using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.Core.Components
{
    public class TextMeshVisual : MonoBehaviour
    {
        [SerializeField] private TextReference[] _texts;
        public string Text {
            get => _texts[0].text.text;
            set {
                for(int i = 0; i < _texts.Length; i++) {
                    _texts[i].text.text = value;
                }
            }
        }

        public Color Color {
            get => _texts[0].text.color;
            set {
                for (int i = 0; i < _texts.Length; i++) {
                    if (_texts[i].updateColor) {
                        _texts[i].text.color = value;
                    }
                }
            }
        }
        [Serializable]
        public struct TextReference
        {
            public TextMesh text;
            public bool updateColor;
        }
    }
}