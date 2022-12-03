using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Mobge.UI {
    [Serializable]
    public struct TextReference {
        [OwnComponent] public Text[] texts;
        [OwnComponent] public TextMeshProUGUI[] textsTMP;
        private string _text;



        public string Text {
            get => _text;
            set {
                if (value != _text) {
                    _text = value;
                    if (texts != null) {
                        for (int i = 0; i < texts.Length; i++) {
                            texts[i].text = _text;
                        }
                    }
                    if (textsTMP!= null) {
                        for (int i = 0; i < textsTMP.Length; i++) {
                            textsTMP[i].text = _text;
                        }
                    }
                }
            }
        }

        public void SetActive(bool active) {
            if (texts != null) {
                for (int i = 0; i < texts.Length; i++) {
                    if (texts[i].gameObject.activeSelf != active) {
                        texts[i].gameObject.SetActive(active);
                    }
                }
            }
            if (textsTMP != null) {
                for (int i = 0; i < textsTMP.Length; i++) {
                    if (textsTMP[i].gameObject.activeSelf != active) {
                        textsTMP[i].gameObject.SetActive(active);
                    }
                }
            }
        }
    }
}