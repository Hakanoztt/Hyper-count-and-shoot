using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.Core
{
    public class Tile
    {
        public enum ColliderType { None, Sprite, Grid } // Non functional at the moment
    }

    public class TileData
    {
        public Color color;             // Color of the Tile.
        public GameObject gameObject;   //GameObject of the Tile.
        public Sprite sprite;           //Sprite to be rendered at the Tile.
        public Matrix4x4 transform;     //Transform matrix of the Tile..

        public TileData(Color color, GameObject gameObject, Sprite sprite, Matrix4x4 transform)
        {
            this.color = color;
            this.gameObject = gameObject;
            this.sprite = sprite;
            this.transform = transform;
        }
    }
}