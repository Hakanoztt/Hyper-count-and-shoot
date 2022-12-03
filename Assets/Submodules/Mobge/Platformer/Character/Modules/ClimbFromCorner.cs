using System;
using UnityEngine;
using Mobge.Animation;

namespace Mobge.Platformer.Character {
    public class ClimbFromCorner : MonoBehaviour, BaseMoveModule
    {
        public SurfaceFilter groundFilter;
        public ClimbSetup[] setups;
        private float _endClimbTime;
        private ClimbParams _climbParams;
        private Mode _mode;
        protected void Awake() {
            Array.Sort(setups, CompareSetups);
        }
        private int CompareSetups(ClimbSetup cs1, ClimbSetup cs2){
            var y1 = cs1.maxHeight;
            var y2 = cs2.maxHeight;
            if(y1 == y2) return 0;
            return y1 > y2 ? 1 : -1;
        }
        public void SetEnabled(Character2D character, bool enabled)
        {
            if(enabled) {
                if(_mode == Mode.Disabled) {
                    _mode = Mode.Climbing;
                }
            }
            else{
                _mode = Mode.Disabled;
            }
        }
        private static void testPrint(object message){
            Debug.Log("climb: " + message);
        }
        public bool UpdateModule(Character2D character, ref GroundContact groundContact, WalkMode walkMode)
        {
            if(walkMode != WalkMode.Normal) return false;
            switch(_mode) {
                default:
                case Mode.Disabled:
                    return TryStartClimb(character, ref groundContact);
                case Mode.Climbing:
                    if(!UpdateClimb(character, ref groundContact)) {
                        _mode = Mode.Climbed;
                    }
                    return true;
                case Mode.Climbed:
                    return false;
            }
        }
        private bool TryStartClimb(Character2D character, ref GroundContact groundContact){
            if(character.CurrentVelocity.y > 0){
                return false;
            }
            Vector2 normal;
            Vector2 point;
            var ground = character.FindWallTop(out point, out normal);
            if(!ground) {
                //testPrint("no ground on side");
                return false;
            }
            
            if(-normal.x * character.Direction < 0) {
                //testPrint("ground at wrong direction");
                return false;
            }
            
            for(int i = 0; i < setups.Length; i++) {
                var s = setups[i];
                var chrPos = character.Position;
                bool lookingRight = character.Direction > 0;
                float xOffset = lookingRight ? s.climbedOffset.x : -s.climbedOffset.x;
                Vector2 checkPos = new Vector2(chrPos.x + xOffset, chrPos.y + s.maxHeight);
                if(checkPos.y > point.y) {
                    RaycastHit2D groundInfo;
                    var canGo = character.FindGroundAtPosition(checkPos, checkPos.y-chrPos.y, out groundInfo);
                    if(canGo) {
                        groundContact.ground = ground;
                        groundContact.normal = normal;
                        var end = checkPos;
                        end.y -= groundInfo.distance;
                        var pos = end-new Vector2(xOffset, s.climbedOffset.y);
                        var anim = character.Animation.PlayAnimation(s.climbAnimation, false);
                        anim.Speed = anim.Duration / s.duration;
                        _endClimbTime = Time.fixedTime + s.duration;
                        _climbParams.startPoint = ground.transform.InverseTransformPoint(pos);
                        character.Position = pos;
                        _climbParams.endPoint = ground.transform.InverseTransformPoint(end);
                        _climbParams.targetBody = ground;

                        return true;
                    }
                }
            }
            return false;
        }
        /**
         * <summary> Returns true if climbing continues. </summary>
         */
        private bool UpdateClimb(Character2D character, ref GroundContact groundContact) {
            character.Position = _climbParams.targetBody.transform.TransformPoint(_climbParams.startPoint);
            if(Time.fixedTime >= _endClimbTime) {
                character.Position = _climbParams.targetBody.transform.TransformPoint(_climbParams.endPoint);
                character.Animation.PlayAnimation(0,true);
                groundContact.ground = _climbParams.targetBody;
                groundContact.normal = new Vector2(0,1);
                return false;
            }
            else {
                return true;
            }
        }
        [Serializable]
        public class ClimbSetup {
            [AnimationAttribute] public int climbAnimation;
            public bool requireAir;
            public Vector2 climbedOffset;
            public float maxHeight;
            public float duration = 0.25f;
        }
        private struct ClimbParams
        {
            public Vector2 startPoint, endPoint;
            public Rigidbody2D targetBody;

        }
        private enum Mode {
            Disabled = 0,
            Climbing = 1,
            Climbed = 2,
        }
    }
}