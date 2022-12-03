using Mobge.Animation;
using Mobge.StateMachineAI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.War {
    public abstract class BaseAIAttack : MonoBehaviour, IAIActionSource {
        [SerializeField] public float attackRange;
        [SerializeField] public AICharacterDetector characterDetector;



        protected Character _character;


        protected abstract bool UpdateAttack();

        public abstract bool TryStartAttack();

        int IAIActionSource.ActionCount => 2;


        
        public Character FindOwnCharacter() {
            return GetComponentInParent<Character>();
        }

        protected void Awake() {
            _character = FindOwnCharacter();
        }



        bool IAIActionSource.StartAction(int index) {
            switch (index) {
                default:
                    return true;
                case 1:
                    return TryStartAttack();
            }
        }

        string IAIActionSource.GetActionName(int index) {
            switch (index) {
                default:
                    return "enemy is in range";
                case 1:
                    return "attacking";
            }
        }

        bool IAIActionSource.CheckAction(int index) {
            switch (index) {
                default:
                    characterDetector.ChooseTarget(out var target);
                    return target != null && _character.GetDistanceSquared(target) <= attackRange * attackRange;

                case 1:
                    return UpdateAttack();
            }
        }
    }

}
