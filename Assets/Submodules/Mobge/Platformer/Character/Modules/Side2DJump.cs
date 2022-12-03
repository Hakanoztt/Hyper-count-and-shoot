using UnityEngine;
using Mobge.Animation;

namespace Mobge.Platformer.Character{
    public class Side2DJump : MonoBehaviour, BaseJumpModule
    {
        public float jumpInputTime = 0.3f;
        public float jumpStartVelocity = 6;
        public float jumpEndVelocity = 1;
        public float maxJumpImpulse = 40;
        public int airJumpCount = 1;
        [AnimationAttribute] public int jumpAnimation;


        private bool _continueJump;
        private float _jumpStartTime;
        public void SetEnabled(Character2D character, bool enabled)
        {
            if(!enabled){
                _continueJump = false;
            }
        }
        public bool UpdateModule(Character2D character, ref int airJumpCount, WalkMode walkMode)
        {
            if(walkMode == WalkMode.NoMoveOrAnimate) {
                return false;
            }
            var jump = character.Input.Jump;
            bool onGround = character.GroundContact.OnGround;
            bool jumpStart = false;
            if(jump) {
                if(_continueJump) {
                    UpdateJump(character, false);
                }
                else{
                    if(!jump.PreviousValue) {
                        if(onGround) {
                            StartJump(character);
                            jumpStart = true;
                        }
                        else{
                            if(airJumpCount < this.airJumpCount) {
                                airJumpCount++;
                                StartJump(character);
                                jumpStart = true;
                            }
                        }
                    }
                }
            }
            else{
                _continueJump = false;
            }
            bool b = !onGround || jumpStart;
            if(b && walkMode == WalkMode.Normal) {
                UpdateJumpAnim(character);
            }
            return b;
        }
        private void StartJump(Character2D character) {
            _continueJump = true;
            _jumpStartTime = Time.fixedTime;
            UpdateJump(character, true);
            character.JumpStart();
        }
        private Vector2 UpdateJump(Character2D character, bool firstTime){
            float passedTime = Time.fixedTime - _jumpStartTime;
            bool end = passedTime > jumpInputTime;
            float progress;
            if(end) {
                progress = 1;
            }
            else{
                progress = passedTime / jumpInputTime;
            }
            float targetVelocity = Mathf.LerpUnclamped(jumpStartVelocity, jumpEndVelocity, progress);
            var currentVelocity = character.CurrentVelocity;
            float xForce = 0;
            if(currentVelocity.y < targetVelocity) {
                float forceFactor = character.Mass / Time.fixedDeltaTime;
                float requiredForce = (targetVelocity - currentVelocity.y);
                if(!firstTime) {
                    if(requiredForce > this.maxJumpImpulse){
                        requiredForce = this.maxJumpImpulse;
                    }
                }
                requiredForce *= forceFactor;
                //Debug.Log(requiredForce);
                character.AddForce(new Vector2(xForce, requiredForce));
            }
                //Debug.Log("update");
            _continueJump = !end;
            return currentVelocity;
        }
        private void UpdateJumpAnim(Character2D character) {
            UpdateJumpAnim(character, jumpStartVelocity, jumpAnimation, character.Animation.defaultTrack, float.PositiveInfinity);
        }
        public static void UpdateJumpAnim(Character2D character, float jumpStartVelocity, int jumpAnimation, int track, float limitAngularVelocity) {
            var currentVelocity = character.CurrentVelocity;
            var anim = character.Animation;
            var c = anim.GetCurrent(track);
            if(c == null || c.AnimationId != jumpAnimation) {
                c = anim.PlayAnimation(track, jumpAnimation, false);
                c.Speed = 0;
            }

            float progress;
            var av = character.AngularVelocity;
            if (av < limitAngularVelocity && av > -limitAngularVelocity) {
                progress = (-currentVelocity.y + jumpStartVelocity) / (2 * jumpStartVelocity);
                progress = Mathf.Clamp01(progress);
            }
            else {
                progress =0.5f;
            }
            //Debug.Log(progress + "  " + limitAngularVelocity);
            c.Time = progress * c.Duration;
        }
    }
}