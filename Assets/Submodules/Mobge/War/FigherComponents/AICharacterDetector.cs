using Mobge.StateMachineAI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.War {
    public class AICharacterDetector : AIObjectDetector<IDamagable> {

        Character _character;


        public override void OnAIEnable(bool enabled) {
            base.OnAIEnable(enabled);
            if (enabled) {
                _character = GetComponentInParent<Character>();
            }
        }

        public override bool IsValid(IDamagable t) {
            return t.IsAlive;
        }

        public override bool TryGetObject(Collider tr, out IDamagable t) {

            if(tr.TryGetComponent(out t)) {
                if(IsValid(t) && _character.WarManager.IsEnemy(_character.Team, t.Team)) {
                    return true;
                }
            }
            t = default;
            return false;

        }
    }
}