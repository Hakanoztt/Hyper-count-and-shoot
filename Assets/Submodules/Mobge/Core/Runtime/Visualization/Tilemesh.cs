using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Mobge.Core
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class Tilemesh : MonoBehaviour, IPieceRenderer
    {
        [HideInInspector]
        public Vector2 spriteSize;

        public Transform Transform => transform;
        private MeshFilter _mf;
        private MeshRenderer _mr;
        Sprite sprite;
        private void EnsureComponent<T>(ref T field) where T : Component{
            if(field) return;
            field = GetComponent<T>();
            if(field == null) field = gameObject.AddComponent<T>();
        }

        /// <summary>
        /// Generates the mesh for the given list of (vertices + triangles + uvs).
        /// </summary>
        /// <returns>The mesh.</returns>
        /// <param name="v">Vertices list.</param>
        /// <param name="t">Triangles list.</param>
        /// <param name="u">Uvs list.</param>
        private Mesh GenerateMeshObj(List<Vector3> v, List<int> t, List<Vector2> u){
            var mesh = _mf.sharedMesh;
            if(!mesh) {
                mesh = new Mesh();
            }
            mesh.SetVertices(v);
            mesh.SetTriangles(t, 0);
            mesh.SetUVs(0, u);
            return mesh;
        }

        /// <summary>
        /// Generates and populates the mesh for the given level
        /// </summary>
        /// <param name="_decorSet">Decor set.</param>
        /// <param name="_level">Level.</param>
        /// <param name="mat">Mat.</param>
        public void UpdateMesh(GridInfo gi, Material mat, int decorIndex, RuleTile rule, Vector3 offset) 
        {
            EnsureComponent(ref _mr);
            EnsureComponent(ref _mf);
            _mf.sharedMesh = PrepareMeshFromGridInfo(gi, decorIndex, rule);
            _mr.sharedMaterial = mat;
            transform.localPosition = offset;
            MaterialPropertyBlock mpb = new MaterialPropertyBlock();
            _mr.GetPropertyBlock(mpb);
            mpb.SetTexture("_MainTex", rule.m_DefaultSprite.texture); // rules[0].sprite.texture);
            _mr.SetPropertyBlock(mpb);
        }

        public void UpdateMesh(GridInfo gi, Material mat)
        {
            EnsureComponent(ref _mr);
            EnsureComponent(ref _mf);
            _mf.sharedMesh = PrepareMeshFromGridInfo(gi);
            _mr.sharedMaterial = mat;
            //transform.localPosition = offset;
            MaterialPropertyBlock mpb = new MaterialPropertyBlock();
            _mr.GetPropertyBlock(mpb);
            //mpb.SetTexture("_MainTex", rule.m_DefaultSprite.texture); // rules[0].sprite.texture);
            _mr.SetPropertyBlock(mpb);
        }

        private Mesh PrepareMeshFromGridInfo(GridInfo gi, int index, RuleTile rule)
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var uv = new List<Vector2>();

            int i_atom = 0;
            foreach (GridInfo.Int2 key in gi.Data.Keys)
            {
                if (gi.Data[key].decorID == index)
                {
                    sprite = rule.m_DefaultSprite;
                    spriteSize = new Vector2(sprite.bounds.extents.x, sprite.bounds.extents.y);
                    int s_ver = sprite.vertices.Length;
                    int s_tri = sprite.triangles.Length;
                    for (int i_ver = 0; i_ver < s_ver; i_ver++)
                    {
                        vertices.Add(new Vector3(sprite.vertices[i_ver].x + key.x,
                                                sprite.vertices[i_ver].y + key.y));
                        uv.Add(sprite.uv[i_ver]);
                    }

                    // All the needed triangles
                    for (int i_tri = 0; i_tri < s_tri; i_tri++)
                    {
                        triangles.Add(sprite.triangles[i_tri] + (i_atom * s_ver));
                    }
                    i_atom++;
                }
            }

            return GenerateMeshObj(vertices, triangles, uv);
        }

        private Mesh PrepareMeshFromGridInfo(GridInfo gi)
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var uv = new List<Vector2>();
            int i_atom = 0;
            foreach (GridInfo.Int2 key in gi.Data.Keys)
            {
                //if (gi.Data[key].decorID)
                {
                    spriteSize = new Vector2(sprite.bounds.extents.x, sprite.bounds.extents.y);
                    int s_ver = sprite.vertices.Length;
                    int s_tri = sprite.triangles.Length;
                    for (int i_ver = 0; i_ver < s_ver; i_ver++)
                    {
                        vertices.Add(new Vector3(sprite.vertices[i_ver].x + key.x,
                                                sprite.vertices[i_ver].y + key.y));
                        uv.Add(sprite.uv[i_ver]);
                    }
                    // All the needed triangles
                    for (int i_tri = 0; i_tri < s_tri; i_tri++)
                    {
                        triangles.Add(sprite.triangles[i_tri] + (i_atom * s_ver));
                    }
                    i_atom++;
                }
            }
            return GenerateMeshObj(vertices, triangles, uv);
        }
    }
}
