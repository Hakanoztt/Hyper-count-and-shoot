using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.Core {
    public class RuleTile : ScriptableObject, IPieceVisualizer {
        public List<Rule> rules;
        public Material material;

        public virtual Type m_NeighborType { get { return typeof(Rule.Neighbor); } }

        public void UpdateData(IPieceRenderer renderer, GridInfo gi, Vector3 offset, int decorIndex, bool final)
        {
            var tm = renderer as Tilemesh;
            tm.UpdateMesh(gi, material, decorIndex, this, offset);
        }
        public IPieceRenderer Visualize(GridInfo gi, Vector3 offset, int decorIndex, bool final, Transform parent) {
            var tr = new GameObject("tile").transform;
            tr.SetParent(parent, false);
            var tm = tr.gameObject.AddComponent<Tilemesh>();
            tm.UpdateMesh(gi, material, decorIndex, this, offset);
            return tm;
        }
        public bool SupportCollider()
        {
            return false;
        }
        public void UpdateCollider(IPieceRenderer renderer)
        {
            return;
        }
        public void UpdateVisuals(IPieceRenderer obj, GridInfo gi)
        {
            var tm = obj.Transform.GetComponent<Tilemesh>();
            tm.UpdateMesh(gi, material);
        }

        public void UpdateVisuals(IPieceRenderer obj, GridInfo gi, int decorIndex)
        {
            throw new NotImplementedException();
        }

        //private static readonly int[,] RotatedOrMirroredIndexes =
        //{
        //    {2, 4, 7, 1, 6, 0, 3, 5}, // 90
        //    {7, 6, 5, 4, 3, 2, 1, 0}, // 180, XY
        //    {5, 3, 0, 6, 1, 7, 4, 2}, // 270
        //    {2, 1, 0, 4, 3, 7, 6, 5}, // X
        //    {5, 6, 7, 3, 4, 0, 1, 2}, // Y
        //};
        private static readonly int c_Neighbor = 8;


        public Sprite m_DefaultSprite;
        //public TileCollider.ColliderTypes m_DefaultColliderType;
        //private Tile[] _cachedNeighboringTiles = new Tile[c_Neighbor];

        [Serializable]
        public class Rule{
            public Sprite sprite;
            public int[] neighbors;
            public Sprite[] sprites;
            public OutputSprite outputSprite;
            public float m_PerlinScale;
            public Transform m_RuleTransform;
            public Transform m_RandomTransform;
            public float m_AnimationSpeed;
            public TileCollider.ColliderTypes m_ColliderType;

            public Rule()
            {
                outputSprite = OutputSprite.Single;
                neighbors = new int[c_Neighbor];
                sprites = new Sprite[1];
                for (int i = 0; i < neighbors.Length; i++)
                    neighbors[i] = Neighbor.DontCare;
            }

            public class Neighbor
            {
                public const int DontCare = 0;
                public const int This = 1;
                public const int NotThis = 2;
            }
            public enum Transform { Fixed, Rotated, MirrorX, MirrorY }
            public enum OutputSprite { Single, Random, Animation }
        }

        //private void GetMatchingNeighboringTiles(GridInfo gi, Vector3Int position, ref Tile[] neighboringTiles)
        //{
        //    if (neighboringTiles != null)
        //        return;

        //    if (_cachedNeighboringTiles == null || _cachedNeighboringTiles.Length < c_Neighbor)
        //        _cachedNeighboringTiles = new Tile[c_Neighbor];

        //    int index = 0;
        //    for (int y = 1; y >= -1; y--)
        //    {
        //        for (int x = -1; x <= 1; x++)
        //        {
        //            if (x != 0 || y != 0)
        //            {
        //                Vector3Int tilePosition = new Vector3Int(position.x + x, position.y + y, position.z);
        //                //_cachedNeighboringTiles[index++] = gi.GetTile(tilePosition);
        //            }
        //        }
        //    }
        //    neighboringTiles = _cachedNeighboringTiles;
        //}

        //public void GetTileData(Vector3Int position, GridInfo gi, ref TileData tileData)
        //{
        //    Tile[] neighboringTiles = null;
        //    GetMatchingNeighboringTiles(gi, position, ref neighboringTiles);
        //    tileData.sprite = m_DefaultSprite;
        //    tileData.transform = Matrix4x4.identity;

        //    foreach (Rule rule in rules)
        //    {
        //        Matrix4x4 transform = Matrix4x4.identity;
        //        if (RuleMatches(rule, ref neighboringTiles, ref transform))
        //        {
        //            switch (rule.outputSprite)
        //            {
        //                case Rule.OutputSprite.Single:
        //                case Rule.OutputSprite.Animation:
        //                    tileData.sprite = rule.sprites[0];
        //                    break;
        //                case Rule.OutputSprite.Random:
        //                    int index = Mathf.Clamp(Mathf.FloorToInt(GetPerlinValue(position, rule.m_PerlinScale, 100000f) * rule.sprites.Length), 0, rule.sprites.Length - 1);
        //                    tileData.sprite = rule.sprites[index];
        //                    if (rule.m_RandomTransform != Rule.Transform.Fixed)
        //                        transform = ApplyRandomTransform(rule.m_RandomTransform, transform, rule.m_PerlinScale, position);
        //                    break;
        //            }
        //            tileData.transform = transform;
        //            break;
        //        }
        //    }
        //}

        //protected virtual Matrix4x4 ApplyRandomTransform(Rule.Transform type, Matrix4x4 original, float perlinScale, Vector3Int position)
        //{
        //    float perlin = GetPerlinValue(position, perlinScale, 200000f);
        //    switch (type)
        //    {
        //        case Rule.Transform.MirrorX:
        //            return original * Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(perlin < 0.5 ? 1f : -1f, 1f, 1f));
        //        case Rule.Transform.MirrorY:
        //            return original * Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(1f, perlin < 0.5 ? 1f : -1f, 1f));
        //        case Rule.Transform.Rotated:
        //            int angle = Mathf.Clamp(Mathf.FloorToInt(perlin * 4), 0, 3) * 90;
        //            return Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0f, 0f, -angle), Vector3.one);
        //    }
        //    return original;
        //}

        //private bool RuleMatches(Rule rule, ref Tile[] neighboringTiles, ref Matrix4x4 transform)
        //{
        //    // Check rule against rotations of 0, 90, 180, 270
        //    for (int angle = 0; angle <= (rule.m_RuleTransform == Rule.Transform.Rotated ? 270 : 0); angle += 90)
        //    {
        //        if (RuleMatches(rule, ref neighboringTiles, angle))
        //        {
        //            transform = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0f, 0f, -angle), Vector3.one);
        //            return true;
        //        }
        //    }

        //    // Check rule against x-axis mirror
        //    if ((rule.m_RuleTransform == Rule.Transform.MirrorX) && RuleMatches(rule, ref neighboringTiles, true, false))
        //    {
        //        transform = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(-1f, 1f, 1f));
        //        return true;
        //    }

        //    // Check rule against y-axis mirror
        //    if ((rule.m_RuleTransform == Rule.Transform.MirrorY) && RuleMatches(rule, ref neighboringTiles, false, true))
        //    {
        //        transform = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(1f, -1f, 1f));
        //        return true;
        //    }

        //    return false;
        //}

        //private bool RuleMatches(Rule rule, ref Tile[] neighboringTiles, int angle)
        //{
        //    for (int i = 0; i < c_Neighbor; ++i)
        //    {
        //        int index = GetRotatedIndex(i, angle);
        //        Tile tile = neighboringTiles[index];
        //        //if (tile is RuleOverrideTile)
        //            //tile = (tile as RuleOverrideTile).m_RuntimeTile.m_Self;
        //        if (!RuleMatch(rule.neighbors[i], tile))
        //        {
        //            return false;
        //        }
        //    }
        //    return true;
        //}

        //private bool RuleMatches(Rule rule, ref Tile[] neighboringTiles, bool mirrorX, bool mirrorY)
        //{
        //    for (int i = 0; i < c_Neighbor; ++i)
        //    {
        //        int index = GetMirroredIndex(i, mirrorX, mirrorY);
        //        Tile tile = neighboringTiles[index];
        //        //if (tile is RuleOverrideTile)
        //            //tile = (tile as RuleOverrideTile).m_RuntimeTile.m_Self;
        //        if (!RuleMatch(rule.neighbors[i], tile))
        //        {
        //            return false;
        //        }
        //    }
        //    return true;
        //}

        //public virtual bool RuleMatch(int neighbor, Tile tile)
        //{
        //    switch (neighbor)
        //    {
        //        case Rule.Neighbor.This: return true;//tile == this;
        //        case Rule.Neighbor.NotThis: return false;//tile != this;
        //    }
        //    return true;
        //}

        //private int GetRotatedIndex(int original, int rotation)
        //{
        //    switch (rotation)
        //    {
        //        case 0:
        //            return original;
        //        case 90:
        //            return RotatedOrMirroredIndexes[0, original];
        //        case 180:
        //            return RotatedOrMirroredIndexes[1, original];
        //        case 270:
        //            return RotatedOrMirroredIndexes[2, original];
        //    }
        //    return original;
        //}

        //private int GetMirroredIndex(int original, bool mirrorX, bool mirrorY)
        //{
        //    if (mirrorX && mirrorY)
        //    {
        //        return RotatedOrMirroredIndexes[1, original];
        //    }
        //    if (mirrorX)
        //    {
        //        return RotatedOrMirroredIndexes[3, original];
        //    }
        //    if (mirrorY)
        //    {
        //        return RotatedOrMirroredIndexes[4, original];
        //    }
        //    return original;
        //}

        //private static float GetPerlinValue(Vector3Int position, float scale, float offset)
        //{
        //    return Mathf.PerlinNoise((position.x + offset) * scale, (position.y + offset) * scale);
        //}
    }
}