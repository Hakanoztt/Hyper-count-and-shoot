using System;
using UnityEngine;

namespace Mobge.Core
{
    [Serializable]
    public class TileMeshSettings
    {
        /// <summary>
        /// The number of tiles on the x axis.
        /// </summary>
        [SerializeField]
        public int TilesX;

        /// <summary>
        /// The number of tiles on the y axis.
        /// </summary>
        [SerializeField]
        public int TilesY;

        /// <summary>
        /// The number of pixels along each axis on a tile.
        /// </summary>
        [SerializeField]
        public int TileResolution = 16;

        /// <summary>
        /// The size of one tile in Unity units.
        /// </summary>
        [SerializeField]
        public float TileSize = 1f;

        /// <summary>
        /// The format of the texture built for the mesh.
        /// Only used in SingleQuad mode.
        /// </summary>
        [SerializeField]
        public TextureFormat TextureFormat = TextureFormat.RGBA32;

        /// <summary>
        /// The filter mode of the texture built for the mesh.
        /// Only used in SingleQuad mode.
        /// </summary>
        [SerializeField]
        public FilterMode TextureFilterMode = FilterMode.Point;

        public TileMeshSettings()
        {
        }

        public TileMeshSettings(int tilesX, int tilesY)
        {
            TilesX = tilesX;
            TilesY = tilesY;
            TileResolution = 16;
        }

        public TileMeshSettings(int tilesX, int tilesY, int tileResolution)
        {
            TilesX = tilesX;
            TilesY = tilesY;
            TileResolution = tileResolution;
            TileSize = 1f;
        }

        public TileMeshSettings(int tilesX, int tilesY, int tileResolution, float tileSize)
        {
            TilesX = tilesX;
            TilesY = tilesY;
            TileResolution = tileResolution;
            TileSize = tileSize;
            TextureFormat = TextureFormat.RGBA32;
        }

        public TileMeshSettings(int tilesX, int tilesY, int tileResolution, float tileSize, TextureFormat textureFormat)
        {
            TilesX = tilesX;
            TilesY = tilesY;
            TileResolution = tileResolution;
            TileSize = tileSize;
            TextureFormat = textureFormat;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
                return true;
            if (!(obj is TileMeshSettings o))
            {
                return false;
            }

            return TilesX == o.TilesX && 
                    TilesY == o.TilesY &&
                    TileResolution == o.TileResolution && 
                    TileSize == o.TileSize &&
                    TextureFormat == o.TextureFormat;
        }

        // xor all object references to try to generate unique hash
        public override int GetHashCode()
        {
            return TilesX.GetHashCode() ^
                   TilesY.GetHashCode() ^
                   TileResolution.GetHashCode() ^ 
                   TileSize.GetHashCode() ^
                   TextureFormat.GetHashCode();
        }
    }
}
