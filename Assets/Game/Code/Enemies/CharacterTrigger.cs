using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.CountAndShoot {
    public class CharacterTrigger : MonoBehaviour {
        public Action OnPlayerTriggered;
        private void OnTriggerEnter(Collider other) {
            if (Player.TryGetCharacter(other, out Player p)) {
                OnPlayerTriggered?.Invoke();
            }
        }
    }

}