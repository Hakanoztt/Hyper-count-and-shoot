using UnityEngine;
using Mobge.Animation;

namespace Mobge.Platformer.Character {
    public class Side2DMove : MonoBehaviour, BaseMoveModule {
        public const float GroundToleranceTime = 0.05f;
        public float groundForce = 30;
        public float airForce = 15;
        public float breakForce = 30;
        public float walkSpeed = 10;
        public float walkAnimMultiplayer = 1;
        private float _lastGroundTime;
        [AnimationAttribute(true)] public int landingAnimation = -1;
        [AnimationAttribute] public int walkAnimation = 1;

        
        private Mobge.Animation.AnimationState _landingAnimState;

        public void SetEnabled(Character2D character, bool enabled) {
        }
        public bool UpdateModule(Character2D character, ref GroundContact groundContact, WalkMode walkMode)
        {
            Vector2 groundNormal;
            bool prevOnGround = groundContact.OnGround;
            var mi = character.Input.MoveInput;
            var currentVel = character.CurrentVelocity;
            if(!character.IsRigidbodySleeping || mi != Vector2.zero) {
                    
                GroundContact gc;
                gc.ground = character.FindGround(out gc.normal);
                if(!groundContact.ground || gc.ground || (Time.fixedTime > _lastGroundTime + GroundToleranceTime)) {
                    groundContact = gc;
                    if(groundContact.ground) {
                        _lastGroundTime = Time.fixedTime;
                    }
                }
                if(walkMode == WalkMode.NoMoveOrAnimate) {
                    return true;
                }
                
                float maxForce;
                Vector2 groundVelocity;
                if(groundContact.ground) {
                    maxForce = groundForce;
                    groundVelocity = groundContact.ground.velocity;
                }
                else{
                    groundNormal = new Vector2(0, 1);
                    maxForce = airForce;
                    groundVelocity = Vector2.zero;
                    if(mi.x == 0) {
                        return true;
                    }
                }
                character.Direction = mi.x;
                if(maxForce <= 0) {
                    return true;
                }
                float targetVelocity = mi.x * walkSpeed;
                if(!Mathf.Approximately(targetVelocity, currentVel.x)) {
                float requiredVelocity = targetVelocity - currentVel.x;
                    if(targetVelocity == 0 || currentVel.x * targetVelocity < 0){
                        maxForce += breakForce;
                        //maxForce = 0.0f;
                    }
                    float requiredForce = requiredVelocity * character.Mass / Time.deltaTime;
                    requiredForce = Mathf.Clamp(requiredForce, -maxForce, maxForce);
                    //character.EditorPrint(currentVel.x  + " " + targetVelocity);
                    character.AddForce(new Vector2(requiredForce, 0));
                }
            }
            

            #region update animation
            if(walkMode == WalkMode.Normal) {
                var anim = character.Animation;
                if(groundContact.OnGround) {
                    if(!prevOnGround && landingAnimation >= 0) {
                        _landingAnimState = anim.PlayAnimation(landingAnimation, false);
                    }
                    if(_landingAnimState == null || !anim.IsPlaying(_landingAnimState))
                    {
                        _landingAnimState = null;
                        int animIndex;
                        bool walking = mi.x != 0;
                        if(walking) {
                            animIndex = walkAnimation;
                        }
                        else{
                            animIndex = 0;
                        }
                        var currentState = anim.GetCurrent();
                        if(currentState == null || currentState.AnimationId != animIndex){
                            
                            currentState = anim.PlayAnimation(animIndex, true);
                        }
                        if(groundContact.ground && walking) {
                            float animVel = walkAnimMultiplayer * currentVel.x / this.walkSpeed;
                            if(animVel < 0) {
                                animVel = -animVel;
                            }
                            currentState.Speed = animVel;
                            //Debug.Log(animVel);
                        }
                    }
                }
            }
            #endregion // update animation
            
            return true;
        }
    }
}