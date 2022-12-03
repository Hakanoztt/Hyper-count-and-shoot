using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge {
    
    public class WeightedSelector {
        private static WeightedSelector _instance;

        public static WeightedSelector Instance {
            get {
                if (_instance == null) _instance = new WeightedSelector();
                return _instance;
            }
        }

        private ExposedList<float> _weights;
        private float _totalWeight;

        public float TotalWeight => _totalWeight;

        public WeightedSelector(params float[] weights) {
            _weights = new ExposedList<float>();
            AddWeights(weights);
        }

        public void AddWeights(float[] weights) {
            for (int i = 0; i < weights.Length; i++) {
                AddWeight(weights[i]);
            }
        }

        public WeightedSelector() {
            _weights = new ExposedList<float>();
        }
        public void AddWeight(float weight) {
            _weights.Add(weight);
            _totalWeight += weight;
        }
        public int SelectNext() {
            
            float r = Random.Range(0f, _totalWeight);
            float c = 0f;
            int count = _weights.Count;
            var arr = _weights.array;
            int i = 0;
            for (; i < count; i++) {
                c += arr[i];
                if (r > c) continue;
                break;
            }
            return i;
        }
        public int SelectAndReset() {
            int result = SelectNext();
            Reset();
            return result;
        }
        public void Reset() {
            _weights.ClearFast();
            _totalWeight = 0;
        }
    }

    public static class PoseExtensions {
        public static Pose GetInverseTransformedBy(this in Pose pose, in Pose source) {
            var relativePose = pose.position - source.position;
            var iRot = Quaternion.Inverse(source.rotation);
            relativePose = iRot * relativePose;
            return new Pose(relativePose, iRot * pose.rotation);
        }
        public static Vector3 TransformPoint(this in Pose pose, in Vector3 point) {
            return pose.position + pose.rotation * point;
        }
        public static Vector3 InverseTransformPoint(this in Pose pose, in Vector3 point) {
            return Quaternion.Inverse(pose.rotation) * (point - pose.position);
        }
    }
}