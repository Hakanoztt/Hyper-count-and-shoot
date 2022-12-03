using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge {
    [Serializable]
    public class GradientWrapper {

        public static string c_colorKeysName => nameof(colorKeys);
        public static string c_alphaKeysName => nameof(alphaKeys);
        public static string c_modeName => nameof(mode);

        public ColorKey[] colorKeys;
        public AlphaKey[] alphaKeys;
        public GradientMode mode;

        private Gradient _gradient;



        public void EnsureInit(bool forced) {
            if (_gradient == null || forced) {
                _gradient = ToGradient();
            }
        }

        public Gradient ToGradient() {
            Gradient g = new Gradient();
            g.mode = mode;
            var ck = new GradientColorKey[colorKeys.Length];
            for (int i = 0; i < ck.Length; i++) {
                GradientColorKey c;
                c.color = colorKeys[i].color;
                c.time = colorKeys[i].time;
                ck[i] = c;
            }
            g.colorKeys = ck;


            var ak = new GradientAlphaKey[alphaKeys.Length];
            for (int i = 0; i < ak.Length; i++) {
                GradientAlphaKey c;
                c.alpha = alphaKeys[i].alpha;
                c.time = alphaKeys[i].time;
                ak[i] = c;
            }
            g.colorKeys = ck;

            return g;
        }



        public void UpdateValues(Gradient gradient) {
            var ck = gradient.colorKeys;
            Array.Resize(ref colorKeys, ck.Length);
            for (int i = 0; i < colorKeys.Length; i++) {
                colorKeys[i].color = ck[i].color;
                colorKeys[i].time = ck[i].time;
            }


            var ak = gradient.alphaKeys;
            Array.Resize(ref alphaKeys, ak.Length);
            for (int i = 0; i < alphaKeys.Length; i++) {
                alphaKeys[i].alpha = ak[i].alpha;
                alphaKeys[i].time = ak[i].time;
            }


            mode = gradient.mode;
            _gradient = gradient;
        }

        [Serializable]
        public struct ColorKey {
            public float time;
            public Color color;

            public ColorKey(Color color, float time) {
                this.color = color;
                this.time = time;
            }

            public static implicit operator UnityEngine.GradientColorKey(ColorKey kf) {
                return new UnityEngine.GradientColorKey(kf.color, kf.time);
            }
            public static implicit operator ColorKey(UnityEngine.GradientColorKey kf) {
                return new ColorKey(kf.color, kf.time);
            }
        }
        [Serializable]
        public struct Color {
            public float r, g, b, a;

            public static implicit operator UnityEngine.Color(Color c) {
                return new UnityEngine.Color(c.r, c.g, c.b, c.a);
            }
            public static implicit operator Color(UnityEngine.Color c) {
                Color cc;
                cc.r = c.r;
                cc.g = c.g;
                cc.b = c.b;
                cc.a = c.a;
                return cc;
            }
        }
        [Serializable]
        public struct AlphaKey {
            public float time;
            public float alpha;

            public AlphaKey(float alpha, float time) {
                this.alpha = alpha;
                this.time = time;
            }

            public static implicit operator UnityEngine.GradientAlphaKey(AlphaKey kf) {
                return new UnityEngine.GradientAlphaKey(kf.alpha, kf.time);
            }
            public static implicit operator AlphaKey(UnityEngine.GradientAlphaKey kf) {
                return new AlphaKey(kf.alpha, kf.time);
            }
        }
    }
}