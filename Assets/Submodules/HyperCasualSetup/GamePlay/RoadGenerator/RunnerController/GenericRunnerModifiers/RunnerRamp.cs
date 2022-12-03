using Mobge.Animation;
using Mobge.Platformer.Character;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.HyperCasualSetup.RoadGenerator {
    public class RunnerRamp : MonoBehaviour, TriggerListener, RunnerController.IModifier {

        public Configuration configuration;
        

        [OwnComponent] public Collider trigger;
        [OwnComponent] public Animator animator;

        [AnimatorState] public int jumpAnimation;

        private Dictionary<RunnerController, JumpProgress> _jumpProgresses;


        private void Awake() {
            _jumpProgresses = new Dictionary<RunnerController, JumpProgress>();
            trigger.gameObject.AddComponent<TriggerCallbacks>().listener = this;
        }

        void TriggerListener.OnTriggerEnter(TriggerCallbacks sender, Collider collider) {
            if(RunnerController.TryGet(collider, out var controller)) {
                if (controller.AddModifier(this)) {
                    controller.DisableGround();
                    _jumpProgresses[controller] = new JumpProgress();
                }
            }
        }

        void TriggerListener.OnTriggerExit(TriggerCallbacks sender, Collider collider) {

        }

        bool RunnerController.IModifier.Modify(float modifierTime, ref RunnerController.MoveData data, ref Pose pose, RunnerController controller) {
            
            JumpProgress prg = _jumpProgresses[controller];
            float step = CalculateStep(controller.Velocity);
            float newDistance = prg.lastDistance + step;

            float totalDistance = configuration.TotalDistance;
            if (newDistance > totalDistance) {
                _jumpProgresses.Remove(controller);
                return false;
            }
            else {
                prg.lastDistance = newDistance;
                _jumpProgresses[controller] = prg;
                //pose.position.y += Time.deltaTime * 2f;
                data.gravityMode = RunnerController.GravityMode.None;
                pose.position.y = configuration.EvaluateHeight(newDistance) + transform.position.y;
                return true;
            }
        }
        private float CalculateStep(Vector3 velocity) {
            Vector3 direction = transform.forward;

            float step = Vector3.Dot(direction, velocity);
            float dt = Time.deltaTime;
            float velocityToApply = step / dt;
            velocityToApply = this.configuration.accurateForSpeeds.Clamp(velocityToApply);
            step = velocityToApply * dt;
            return step;
        }

        [Serializable]
        public class Configuration {
            public Curve heightByDistance;
            public float curveDistanceMultiplayer = 10;
            public float curveHeightMultiplayer = 4;
            public Range accurateForSpeeds = new Range(3f, 12f);

            public float TotalDistance {
                get {
                    heightByDistance.EnsureInit(false);
                    return heightByDistance.TotalTime * curveDistanceMultiplayer;
                }
            }
            public float EvaluateHeight(float distance) {
                heightByDistance.EnsureInit();
                return heightByDistance.Evaluate(distance / curveDistanceMultiplayer) * curveHeightMultiplayer;
            }
        }
        [Serializable]
        public struct Range {
            public float min, max;

            public Range(float min, float max) {
                this.min = min;
                this.max = max;
            }

            public float Clamp(float value) {
                return Mathf.Clamp(value, min, max);
            }
        }
        [Serializable]
        private struct JumpProgress {
            public float lastDistance;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos() {
            EditorDrawGizmos(false);

        }

        private void OnDrawGizmosSelected() {
            EditorDrawGizmos(true);
        }

        private void EditorDrawGizmos(bool forceInitialize) {
            ref var c = ref this.configuration.heightByDistance;
            c.EnsureInit(forceInitialize);
            float totalDistance = configuration.TotalDistance;
            int sampleCount = 30;
            float step = totalDistance / sampleCount;
            Vector3 origin = transform.position;
            Vector3 forward = transform.forward;
            Vector3 up = Vector3.up;
            Vector3 prevPoint = origin;
            for(float d = step; d < totalDistance; d+=step) {
                float height = configuration.EvaluateHeight(d);
                Vector3 point = origin + forward * d + up * height;
                Gizmos.DrawLine(prevPoint, point);
                prevPoint = point;
            }
        }
#endif
    }
}