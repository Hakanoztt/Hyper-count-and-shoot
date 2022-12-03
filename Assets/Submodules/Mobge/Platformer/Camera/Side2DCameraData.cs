using System;
using UnityEngine;

namespace Mobge.Platformer {
    /// <summary>
    /// Data class to determine behaviour of <see cref="Side2DCamera"/> instances. </summary>
    [Serializable]
    public class Side2DCameraData {
        /// <summary>
        /// Maximum z offset that camera can go when target(s) does not fit the frame. </summary> 
        public float maxZOffset = 25;
        /// <summary>
        /// If true camera only updates y position when target touches ground. </summary>
        public bool snapGround = true;
        /// <summary>
        /// Max horizontal offset that target can go before last snap is cancelled. </summary>
        public float snapBreakOffset = 0.7f;
        /// <summary>
        /// Horizonral offset that camera can go. (no offset, edge of the frame) => (0, 1). </summary>
        public float maxHorizonralOffsetRate = 0.2f;
        /// <summary>
        /// Follow tightness of the camera. Strict follow: (1,1) Dont follow: (0,0)</summary>
        public Vector3 followForce = new Vector3(10, 10, 10);
        /// <summary>
        /// Z offset from the target. </summary>
        public float zOffset = 15;
        public Vector2 movementRate = new Vector2(1,1);
        public float verticalOffset = -0.5f;
        public float horizontalOffset = 0;
        public float verticalOffsetAir = 0;
        [NonSerialized] public CenterData centerData;
        [NonSerialized] public Side2DCamera.Modifier[] modifiers = null; 
        public Side2DCameraData Clone() {
            return (Side2DCameraData)base.MemberwiseClone();
        }
        public struct CenterData {
            private Transform _centerObject;
            private Vector3 _pos;
            public Vector3 Position {
                get{
                    if(_centerObject){
                        return _centerObject.position;
                    }
                    return _pos;
                }
                set {
                    _centerObject = null;
                    _pos = value;
                }
            }
            public Transform CenterObject {
                get {
                    return _centerObject;
                }
                set {
                    _centerObject = value;
                }
            }
        }
    }
}