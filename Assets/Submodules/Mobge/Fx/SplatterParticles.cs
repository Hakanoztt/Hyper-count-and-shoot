using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Mobge.LineRendererPlus;
using UnityEngine;

namespace Mobge.Fx {

    [Serializable]
    public struct SplatterParticles {
        private static MeshBuilder s_mb = new MeshBuilder();
        public Sprite[] sprites;
        public float fadeInTime;
        public float fadeOutTime;
        public float size;
        public float lifeTime;
        public Color color;
        [OwnComponent(true)]
        public MeshFilter meshFilter;
        public ReusableReference splatEffect;
        private ExposedList<Particle> _particles;
        private static float _time;

        public ExposedList<Particle> Particles => _particles;

        public void Initialize() {
            _particles = new ExposedList<Particle>();
            meshFilter.sharedMesh = null;
        }
        public void Emit(Vector3 position, Transform parent) {
            Particle p;
            p.birthTime = Time.time;
            p.parent = parent;
            p.position = parent ? parent.InverseTransformPoint(position) : position;
            var a = UnityEngine.Random.Range(0, 2 * Mathf.PI);
            p.up = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0);
            p.s = this.sprites[UnityEngine.Random.Range(0, this.sprites.Length)];
            _particles.Add(p);

            splatEffect.SpawnItem(position, parent);
        }
        public void UpdateMesh() {
            _time = Time.time;
            var m = meshFilter.sharedMesh;
            if (m == null) {
                m = new Mesh();
                meshFilter.sharedMesh = m;
            }
            var arr = _particles.array;
            for (int i = 0; i < _particles.Count;) {
                if (UpdateParticle(arr[i])) {

                    i++;
                }
                else {
                    _particles.RemoveFast(i);
                }
            }


            s_mb.BuildMesh(m);
            s_mb.Clear();
        }
        private bool UpdateParticle(in Particle p) {
            var age = _time - p.birthTime;
            if (age > lifeTime) {
                return false;
            }
            DrawSpace spc;
            float alpha;

            float leftTime;
            if (age < fadeInTime) {
                alpha = age / fadeInTime;
            }
            else if ((leftTime = (lifeTime - age)) < fadeOutTime) {
                alpha = leftTime / fadeOutTime;
            }
            else {
                alpha = 1;
            }

            if (p.parent) {
                spc.orgin = p.parent.TransformPoint(p.position);
                spc.up = p.parent.TransformDirection(p.up);
            }
            else {
                spc.orgin = p.position;
                spc.up = p.up;
            }
            spc.right = new Vector3(-spc.up.y, spc.up.x, 0);

            float hs = size * 0.5f;
            int index = s_mb.vertices.Count;
            s_mb.vertices.Add(spc.orgin - spc.right * hs - spc.up * hs);
            s_mb.vertices.Add(spc.orgin - spc.right * hs + spc.up * hs);
            s_mb.vertices.Add(spc.orgin + spc.right * hs + spc.up * hs);
            s_mb.vertices.Add(spc.orgin + spc.right * hs - spc.up * hs);

            var rect = p.s.rect;
            var mult = new Vector2(1f / p.s.texture.width, 1f / p.s.texture.height);
            var uvMin = rect.max * mult;
            var uvMax = rect.min * mult;
            //uvMin.y = 1 - uvMin.y;
            //uvMax.y = 1 - uvMax.y;
            s_mb.uvs.Add(new Vector2(uvMin.x, uvMin.y));
            s_mb.uvs.Add(new Vector2(uvMin.x, uvMax.y));
            s_mb.uvs.Add(new Vector2(uvMax.x, uvMax.y));
            s_mb.uvs.Add(new Vector2(uvMax.x, uvMin.y));

            var c = color;
            c.a = alpha;
            s_mb.colors.Add(c);
            s_mb.colors.Add(c);
            s_mb.colors.Add(c);
            s_mb.colors.Add(c);

            s_mb.AddTriangle(index, index + 1, index + 2);
            s_mb.AddTriangle(index, index + 2, index + 3);

            return true;
        }

        private struct DrawSpace {
            public Vector3 orgin;
            public Vector3 up, right;
        }

        public struct Particle {
            public Vector3 position;
            public Transform parent;
            public float birthTime;
            public Vector3 up;
            public Sprite s;
        }
    }

}
