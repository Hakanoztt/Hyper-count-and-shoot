using System;
using UnityEngine;

namespace Mobge {
    public class Rope : MonoBehaviour
    {
        public LineRendererPlus line;
        public RopePhysics physics;

        [SerializeField] [HideInInspector] private Vector2[] pointPositions;
        [SerializeField] [HideInInspector] private int linkCount = 2;
        [SerializeField] [HideInInspector] private float thickness = 1;
        
        public int LinkCount {
            get => linkCount;
            set {
                value = Mathf.Max(1, value);
                if (linkCount != value) {
                    linkCount = value;
                    Reconstruct(linkCount);
                }
            }
        }
        public float Thickness {
            get => thickness;
            set {
                value = Mathf.Max(0, value);
                if (thickness != value) {
                    thickness = value;
                    SetPoints(pointPositions, thickness);
                }
            }
        }
        public void Reconstruct(int linkCount) {
            this.linkCount = linkCount;
            //Defaulting
            thickness = 1f;
            pointPositions = new Vector2[linkCount + 1];
            for (int i = 0; i < linkCount + 1; i++) {
                pointPositions[i] = 2 * i * thickness * Vector2.right;
            }
            
            line.Initialize(pointPositions, thickness * 2);
            Transform[] transforms = line.GetPieces();
            physics.Construct(transforms);
            physics.UpdatePoints(pointPositions, thickness);
        }
        public void SetPoints(Vector2[] points, float thickness) {
            pointPositions = points;
            this.thickness = thickness;
            physics.UpdatePoints(points, thickness);
            line.Initialize(points, thickness * 2);
        }

    }
}
