using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.CountAndShoot {
    public class PowerUp : MonoBehaviour, IPowerUp {

        bool isTriggered = false;
        public List<GameObject> ballList;
        public float speed;
        void Update() {
            BallsDown();
        }
        public void Activate() {
            isTriggered = true;
        }
        void BallsDown() {
            if (isTriggered) {
                for (int i = 0; i < ballList.Count; i++) {
                    ballList[i].gameObject.tag = "Ball";
                    ballList[i].gameObject.SetActive(true);
                    ballList[i].gameObject.transform.SetParent(null);
                }
                gameObject.SetActive(false);
            }
        }
    }
    public interface IPowerUp {
        public void Activate();
    }
}


