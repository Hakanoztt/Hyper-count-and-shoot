using System;
using Mobge.Platformer.Character;
using UnityEngine;

namespace Mobge.Platformer {
    [Serializable]
    public struct ProjectileShootData {
        public float speed;
        public float angle;
        public float reduceRate;
        public AimMode aimMode;
        private static float[] s_results = new float[2];
        public enum AimMode {
            ReduceAngleChooseSlower = 0,
            ReduceAngleChooseFaster,
            ReduceSpeed,
            FixedValues,
            RelativeHeight,
        }
        public static Results CalculateProjectileAngles(float velocity, float gravity, Vector2 target) {
            // https://www.youtube.com/watch?v=bqYtNrhdDAY
            var teta = Mathf.Atan(target.y / target.x);
            var xsqr = target.x * target.x;
            var a1 = Mathf.Asin((-gravity * xsqr/(velocity * velocity) + target.y) / Mathf.Sqrt(xsqr + target.y * target.y));
            Results r;
            r.r1 = 0.5f * (a1 + teta);
            r.r2 = 0.5f * (Mathf.PI - a1 + teta);
            return r;
        }
        public static Vector2 CalculateProjectileVelocity(float rAngle, float gravity, Vector2 target) {
            var sin = Mathf.Sin(rAngle);
            var cos = Mathf.Cos(rAngle);
            var speed = Mathf.Sqrt(target.x * target.x * gravity / (2*cos *(target.y*cos-target.x*sin)));
            return new Vector3(cos * speed, sin * speed);
        }
        public static Vector3 CalculateProjectileVelocity(float rAngle, float gravity, Vector3 target) {
            Vector2 difGround = new Vector2(target.x, target.z);
            float magGround = difGround.magnitude;
            var target2 = new Vector2(magGround, target.y);
            var speed2 = CalculateProjectileVelocity(rAngle, gravity, target2);

            Vector2 groundDir = difGround / magGround;
            Vector3 speed = new Vector3(groundDir.x * speed2.x, speed2.y, groundDir.y * speed2.x);
            return speed;
        }
        private bool TryCalculateProjectileSpeed(float rAngle, float gravity, Vector3 target, out Vector3 result, bool clamp) {
            result = CalculateProjectileVelocity(rAngle, gravity, target);
            var minv = reduceRate * speed;
            float sqMag = result.sqrMagnitude;
            float clamped = sqMag;
            if(Inside(ref clamped, minv * minv, speed * speed)) {
                return true;
            }
            if(clamp) {
                result *= Mathf.Sqrt(clamped / sqMag);
                return true;
            }
            return false;
        }
        private bool Inside(ref float value, float min, float max) {
            if(!(value >= min)) {
                value = min;
                return false;
            }
            if(!(value <= max)) {
                value = max;
                return false;
            }
            return true;
        }
        public bool TryCalculatingVelocity(out Vector3 velocity, float gravity, Vector3 target, bool facingLeft, bool clamp = false) {
            if(facingLeft) {
                target.x = -target.x;
            }
            var r = TryCalculatingVelocity(out velocity, gravity, target, clamp);
            if(facingLeft) {
                velocity.x = -velocity.x;
            }
            return r;
        }
        private bool TryCalculatingVelocity(out Vector3 velocity, float gravity, Vector3 target, bool clamp) {
            velocity = Vector3.zero;
            if(target.x < 0) {
                return false;
            }
            switch(aimMode) {
                default:
                case AimMode.ReduceSpeed:
                return TryCalculateProjectileSpeed(this.angle * Mathf.Deg2Rad, gravity, target, out velocity, clamp);
                case AimMode.ReduceAngleChooseFaster:
                {
                    float rAngle;
                    var speed = this.speed;
                    var r = TryCalculateProjectileAngle(speed, gravity, target, true, out rAngle, clamp);
                    if(r) {
                        velocity = ToVelocity(speed, rAngle);
                    }
                    return r;
                }
                case AimMode.ReduceAngleChooseSlower:
                {
                    float rAngle;
                    var speed = this.speed;
                    var r = TryCalculateProjectileAngle(speed, gravity, target, false, out rAngle, clamp);
                    if(r) {
                        velocity = ToVelocity(speed, rAngle);
                    }
                    return r;
                }
                case AimMode.FixedValues:
                velocity = ToVelocity(this.speed, this.angle * Mathf.Deg2Rad);
                return true;
                case AimMode.RelativeHeight:
                return TryCalculateRelativeHeight(speed, gravity, target, this.angle * Mathf.Deg2Rad, this.reduceRate, out velocity, clamp);
            }
        }
        private Vector3 ToVelocity(float speed, float rAngle) {
            return new Vector2(Mathf.Cos(rAngle), Mathf.Sin(rAngle)) * speed;
        }
        public bool TryCalculateProjectileAngle(float velocity, float gravity, Vector2 target, bool faster, out float result, bool clamp) {
            var angles = CalculateProjectileAngles(velocity, gravity, target);
            var max = angle * Mathf.Deg2Rad;
            var min = max * reduceRate;
            if(Inside(ref angles.r1, min, max)) {
                result = angles.r1;
                if(Inside(ref angles.r2, min, max)) {
                    if(faster == (angles.r1 < angles.r2)) {
                        result = angles.r1;
                    }
                    else {
                        result = angles.r2;
                    }
                }
                return true;
            }
            else {
                if(Inside(ref angles.r2, min, max)){
                    result = angles.r2;
                    return true;
                }
            }
            if(clamp) {
                if(faster == (angles.r1 < angles.r2)) {
                    result = angles.r1;
                }
                else {
                    result = angles.r2;
                }
                return true;
            }
            else{
                result = float.NaN;
            }
            return false;
        }
        public static Vector3 CalculateVelocityForRelativeHeight(float gravity, Vector3 target, float relativeHeight, out float time) {
            
            float height = Mathf.Max(target.y, 0) + relativeHeight;
            float iGravity = 1f / gravity;
            // (1) h = v * t + 0.5 * a * t * t
            // v = -a * t
            // h = -0.5 * a * t * t
            // t = (-2 * h / a) ^ 0.5
            float time1 = Mathf.Sqrt(-2f * height * iGravity);
            float velY = -time1 * gravity;
            // (1) v = 0
            // h = 0.5 * a * t * t
            // t = (2 * h / a) ^ 0.5
            float time2 = Mathf.Sqrt(2f*(target.y - height)*iGravity);
            time = time1 + time2;
            float velX = target.x / time;
            return new Vector2(velX, velY);
        }
        public static bool TryCalculateRelativeHeight(float maxSpeed, float gravity, Vector2 target, float rAngle, float relativeHeight, out Vector3 result, bool clamp) {
            result = CalculateVelocityForRelativeHeight(gravity, target, relativeHeight, out float time);
            var magsqr = result.sqrMagnitude;
            var maxSqr = maxSpeed * maxSpeed;
            bool canShoot = magsqr < maxSqr;
            if(!canShoot) {
                if(clamp) {
                    result *= Mathf.Sqrt(maxSqr / magsqr);
                    magsqr = maxSqr;
                }
                else {
                    return false;
                }
            }
            var sin = Mathf.Sin(rAngle);
            float y2 = result.y*result.y;
            if((y2 / magsqr) < (sin*sin)) {
                if(clamp) {
                    var mag = Mathf.Sqrt(magsqr);
                    result.y = mag*sin;
                    result.x = Mathf.Sqrt(magsqr - y2);
                }
                else{
                    return false;
                }
            }
            return true;
        }
        public struct Results {
            public float r1, r2; 
            public override string ToString() {
                return "(" + r1 + ", " + r2 + ")";
            }
        }
    }
}