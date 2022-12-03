using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mobge {

    [Serializable]
    public struct TimedFlowControl {
        public float minCooldown;
        public float increaseCooldown;
        public float maxStack;

        private float _lastSpawnTime;
        private float _stack;

        public void Initialize() {
            _lastSpawnTime = float.NegativeInfinity;
            _stack = maxStack * increaseCooldown;
        }
        public bool TryGetOne(float time) {
            var passed = time - _lastSpawnTime;
            if (passed < minCooldown) {
                return false;
            }
            var total = _stack + passed;
            if (total < increaseCooldown) {
                return false;
            }
            _stack = total;

            var maxStackValue = maxStack * increaseCooldown;

            if (_stack > maxStackValue) {
                _stack = maxStackValue;
            }
            _stack -= increaseCooldown;
            _lastSpawnTime = time;
            return true;
        }
    }
}
