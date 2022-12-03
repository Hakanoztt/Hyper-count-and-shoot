using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Mobge.HyperCasualSetup.RoadGenerator {
    public class RoadPieceGenerator : ScriptableObject {
        public Mesh pieceMesh;
        public Material[] pieceMaterials;


        public float straightRoadLength = 5;
        public RotatedPieceDefinition[] rotatedPieces;

        public Line3DRoadPiece straightLinePrefab;
        public Line3DRoadPiece[] extraPiecePrefabs;

        [Serializable]
        public struct RotatedPieceDefinition {
            public float length;
            public float rotation;
            public Line3DRoadPiece piecePrefab;
        }

    }
}